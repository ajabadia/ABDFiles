/**
 * @purpose Gestiona conectores de almacenamiento para inquilinos proporcionando métodos para validar credenciales, listar, recuperar, crear y probar conexiones.
 * @purpose_en Manages storage connectors for tenants by providing methods to validate credentials, list, retrieve, create, and test connections.
 * @refactorable true (contains multiple responsibilities such as validation, listing, creation, and testing)
 * @classification Business Service
 * @complexity Medium
 * @fingerprint exports:1,imports:4,sig:zmpbpt
 * @lastUpdated 2026-06-25T10:22:41.347Z
 */

import { logger } from '@ajabadia/satellite-sdk/logger';
import StorageConnector, { TStorageConnector } from '@/models/StorageConnector';
import { StorageProviderRegistry } from './storage/storage-providers';
import crypto from 'crypto';

export const ConnectorService = {
  /**
   * Helper to validate JSON credentials structure
   */
  validateCredentials(providerType: string, credentialsRef: string): void {
    try {
      const parsed = JSON.parse(credentialsRef);
      if (providerType === 'cloudinary') {
        if (!parsed.cloudName && !process.env.CLOUDINARY_CLOUD_NAME) {
          throw new Error('Cloudinary credentials must contain cloudName');
        }
      } else if (providerType === 's3Compatible') {
        if (!parsed.accessKeyId && !process.env.S3_ACCESS_KEY_ID) {
          throw new Error('S3 credentials must contain accessKeyId');
        }
        if (!parsed.secretAccessKey && !process.env.S3_SECRET_ACCESS_KEY) {
          throw new Error('S3 credentials must contain secretAccessKey');
        }
        if (!parsed.bucket && !process.env.S3_BUCKET) {
          throw new Error('S3 credentials must contain bucket name');
        }
      }
    } catch (e: unknown) {
      const err = e as Error;
      if (err.message.includes('credentials must contain')) {
        throw err;
      }
      throw new Error('credentialsRef must be a valid JSON string');
    }
  },

  /**
   * List all connectors for a tenant
   */
  async listConnectors(tenantId: string): Promise<TStorageConnector[]> {
    return StorageConnector.find({ tenantId }).sort({ createdAt: -1 });
  },

  /**
   * Get a single connector by id
   */
  async getConnector(tenantId: string, connectorId: string): Promise<TStorageConnector | null> {
    return StorageConnector.findOne({ tenantId, connectorId });
  },

  /**
   * Create a new storage connector
   */
  async createConnector(
    tenantId: string,
    data: {
      providerType: 'cloudinary' | 's3Compatible' | 'googleDrive' | 'oneDrive';
      credentialsRef: string;
      allowedScopes?: string[];
      status?: 'active' | 'inactive';
      retentionPolicy?: Record<string, unknown>;
      auditMode?: string;
    }
  ): Promise<TStorageConnector> {
    this.validateCredentials(data.providerType, data.credentialsRef);

    const connectorId = `conn_${crypto.randomUUID()}`;
    const status = data.status || 'active';

    if (status === 'active') {
      // Deactivate all other connectors for this tenant
      await StorageConnector.updateMany({ tenantId, status: 'active' }, { status: 'inactive' });
    }

    const connector = new StorageConnector({
      connectorId,
      tenantId,
      providerType: data.providerType,
      credentialsRef: data.credentialsRef,
      allowedScopes: data.allowedScopes || [],
      status,
      retentionPolicy: data.retentionPolicy || {},
      auditMode: data.auditMode || 'standard'
    });

    await connector.save();
    return connector;
  },

  /**
   * Update a storage connector
   */
  async updateConnector(
    tenantId: string,
    connectorId: string,
    data: {
      status?: 'active' | 'inactive';
      credentialsRef?: string;
      allowedScopes?: string[];
      retentionPolicy?: Record<string, unknown>;
      auditMode?: string;
    }
  ): Promise<TStorageConnector> {
    const connector = await StorageConnector.findOne({ tenantId, connectorId });
    if (!connector) {
      throw new Error('Storage connector not found');
    }

    if (data.credentialsRef !== undefined) {
      this.validateCredentials(connector.providerType, data.credentialsRef);
      connector.credentialsRef = data.credentialsRef;
    }

    if (data.status !== undefined) {
      if (data.status === 'active' && connector.status !== 'active') {
        // Deactivate all other connectors
        await StorageConnector.updateMany({ tenantId, status: 'active' }, { status: 'inactive' });
      }
      connector.status = data.status;
    }

    if (data.allowedScopes !== undefined) {
      connector.allowedScopes = data.allowedScopes;
    }

    if (data.retentionPolicy !== undefined) {
      connector.retentionPolicy = data.retentionPolicy;
    }

    if (data.auditMode !== undefined) {
      connector.auditMode = data.auditMode;
    }

    await connector.save();
    return connector;
  },

  /**
   * Delete a connector
   */
  async deleteConnector(tenantId: string, connectorId: string): Promise<void> {
    const result = await StorageConnector.deleteOne({ tenantId, connectorId });
    if (result.deletedCount === 0) {
      throw new Error('Storage connector not found or already deleted');
    }
  },

  /**
   * Test connection of a connector physically
   */
  async testConnection(tenantId: string, connectorId: string): Promise<{ success: boolean; message: string }> {
    const connector = await StorageConnector.findOne({ tenantId, connectorId });
    if (!connector) {
      throw new Error('Storage connector not found');
    }

    const provider = StorageProviderRegistry[connector.providerType];
    if (!provider) {
      return { success: false, message: `Storage provider '${connector.providerType}' is not supported` };
    }

    let credentials: Record<string, unknown> = {};
    try {
      credentials = JSON.parse(connector.credentialsRef);
    } catch {
      return { success: false, message: 'Invalid credentials format JSON' };
    }

    const testRef = `test-connection-${crypto.randomUUID()}`;
    const dummyBuffer = Buffer.from('1'); // 1-byte file

    try {
      // 1. Try to upload dummy file
      await provider.uploadFile(dummyBuffer, testRef, 'text/plain', credentials);

      // 2. Try to get signed url (to verify reading works)
      const url = await provider.getSignedUrl(testRef, 'text/plain', credentials);
      if (!url) {
        throw new Error('Signed URL generation returned empty');
      }

      // 3. Try to delete the dummy file (cleanup)
      await provider.deleteFile(testRef, 'text/plain', credentials);

      return { success: true, message: 'Connection test passed successfully!' };
    } catch (e: unknown) {
      const err = e as Error;
      logger.audit({
        tenantId,
        action: 'STORAGE_CONNECTION_TEST_FAILED',
        entityType: 'CONNECTOR',
        entityId: connectorId,
        userId: 'system',
        userEmail: 'system@abd.com',
        changedFields: { error: err.message, connectorId },
      });
      console.error(`[STORAGE_CONNECTION_TEST_FAILED] Connector: ${connectorId}`, err);
      return { success: false, message: `Connection failed: ${err.message || err}` };
    }
  }
};
