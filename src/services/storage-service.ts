/**
 * @purpose Gestiona interacciones con proveedores de almacenamiento físico dinámicamente, incluyendo subir archivos, generar URLs firmadas y eliminar archivos.
 * @purpose_en Manages interactions with physical storage providers dynamically, including uploading files, generating signed URLs, and deleting files.
 * @refactorable true (contains multiple functions related to different storage operations)
 * @classification Business Service
 * @complexity Medium
 * @fingerprint exports:2,imports:3,sig:6slhu0
 * @lastUpdated 2026-06-25T10:22:56.085Z
 */

import StorageConnector from '@/models/StorageConnector';
import { StorageProviderRegistry } from './storage/storage-providers';
import crypto from 'crypto';

export type TUploadResult = {
  storageRef: string;
  url: string;
};

/**
 * Service to handle interaction with physical storage providers dynamically.
 */
export const StorageService = {
  /**
   * Helper to parse dynamic credentials from a connector's credentialsRef
   */
  getConnectorConfig(credentialsRef?: string): Record<string, unknown> {
    if (!credentialsRef) return {};
    try {
      if (credentialsRef.trim().startsWith('{')) {
        return JSON.parse(credentialsRef) as Record<string, unknown>;
      }
    } catch {
      // Return empty if it is not valid JSON
    }
    return { credentialsRef };
  },

  /**
   * Uploads a file buffer to the tenant's chosen storage provider.
   */
  async uploadFile(buffer: Buffer, tenantId: string, mimeType: string): Promise<TUploadResult> {
    const connector = await StorageConnector.findOne({ tenantId, status: 'active' });
    const providerType = connector?.providerType || process.env.DEFAULT_STORAGE_PROVIDER || 'cloudinary';
    const config = this.getConnectorConfig(connector?.credentialsRef);

    const provider = StorageProviderRegistry[providerType];
    if (!provider) {
      throw new Error(`Storage provider type '${providerType}' is not supported`);
    }

    const publicId = `tenants/${tenantId}/abdfiles/${crypto.randomUUID()}`;
    return provider.uploadFile(buffer, publicId, mimeType, config);
  },

  /**
   * Generates a temporary signed URL to safely consume/download the file.
   */
  async getSignedUrl(storageRef: string, mimeType: string, tenantId?: string): Promise<string> {
    let providerType = process.env.DEFAULT_STORAGE_PROVIDER || 'cloudinary';
    let config: Record<string, unknown> = {};

    if (tenantId) {
      const connector = await StorageConnector.findOne({ tenantId, status: 'active' });
      if (connector) {
        providerType = connector.providerType;
        config = this.getConnectorConfig(connector.credentialsRef);
      }
    }

    const provider = StorageProviderRegistry[providerType];
    if (!provider) {
      throw new Error(`Storage provider type '${providerType}' is not supported`);
    }

    return provider.getSignedUrl(storageRef, mimeType, config);
  },

  /**
   * Deletes a file physically from the corresponding storage provider.
   */
  async deleteFile(storageRef: string, mimeType: string, tenantId?: string): Promise<void> {
    let providerType = process.env.DEFAULT_STORAGE_PROVIDER || 'cloudinary';
    let config: Record<string, unknown> = {};

    if (tenantId) {
      const connector = await StorageConnector.findOne({ tenantId, status: 'active' });
      if (connector) {
        providerType = connector.providerType;
        config = this.getConnectorConfig(connector.credentialsRef);
      }
    }

    const provider = StorageProviderRegistry[providerType];
    if (!provider) {
      throw new Error(`Storage provider type '${providerType}' is not supported`);
    }

    await provider.deleteFile(storageRef, mimeType, config);
  }
};
