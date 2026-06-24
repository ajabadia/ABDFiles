/* eslint-disable @typescript-eslint/no-explicit-any */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { setupTestEnvironment } from '../../models/__tests__/test-setup';
import Document from '../../models/Document';
import DocumentVersion from '../../models/DocumentVersion';
import DocumentEvent from '../../models/DocumentEvent';
import StorageConnector from '../../models/StorageConnector';
import { DocumentService } from '../document-service';
import { StorageService } from '../storage-service';
import { WebhookService } from '../webhook-service';
import { IntegrationLogsService } from '../integration-logs-service';

setupTestEnvironment();

describe('ABDFiles Deduplication Isolation', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.spyOn(StorageConnector, 'findOne').mockResolvedValue(null);
    vi.spyOn(WebhookService, 'emitWebhook').mockResolvedValue(undefined as any);
    vi.spyOn(IntegrationLogsService, 'replicateEvent').mockResolvedValue(undefined as any);
  });

  it('should reuse existing storageRef when uploading same hash in same tenant (intra-tenant)', async () => {
    const hash = '6ae8a75555209fd6c44157c0aed8016e763ff435a19cf186f76863140143ff72'; // SHA-256 for 'test content'
    const tenantId = 'tenant-alpha';
    const existingStorageRef = 'cloudinary://tenants/tenant-alpha/abdfiles/existing-ref';

    // Mock DocumentVersion.findOne to simulate existing document version with this hash for the same tenant
    const findOneSpy = vi.spyOn(DocumentVersion, 'findOne').mockResolvedValue({
      tenantId,
      hash,
      storageRef: existingStorageRef,
    } as any);

    const uploadFileSpy = vi.spyOn(StorageService, 'uploadFile');
    const documentCreateSpy = vi.spyOn(Document, 'create').mockResolvedValue({} as any);
    const versionCreateSpy = vi.spyOn(DocumentVersion, 'create').mockResolvedValue({} as any);
    const eventCreateSpy = vi.spyOn(DocumentEvent, 'create').mockResolvedValue({} as any);

    await DocumentService.uploadDocument({
      tenantId,
      actorId: 'user-1',
      title: 'Same Tenant Doc',
      fileBuffer: Buffer.from('test content'),
      mimeType: 'text/plain',
      sizeBytes: 12,
      retentionClass: 'standard',
      sensitivityLevel: 'low',
    });

    // Verify it queries with the correct tenantId and hash
    expect(findOneSpy).toHaveBeenCalledWith({ tenantId, hash });

    // Verify it DID NOT perform a new physical upload
    expect(uploadFileSpy).not.toHaveBeenCalled();

    // Verify it created the document and version with the existing storageRef
    expect(documentCreateSpy).toHaveBeenCalledWith(
      expect.objectContaining({
        tenantId,
        storageRefCurrent: existingStorageRef,
      })
    );
    expect(versionCreateSpy).toHaveBeenCalledWith(
      expect.objectContaining({
        tenantId,
        storageRef: existingStorageRef,
      })
    );
  });

  it('should perform physical upload and NOT deduplicate when same hash exists in a different tenant', async () => {
    const hash = '6ae8a75555209fd6c44157c0aed8016e763ff435a19cf186f76863140143ff72'; // SHA-256 for 'test content'
    const otherTenantId = 'tenant-beta';
    const currentTenantId = 'tenant-alpha';
    const otherStorageRef = 'cloudinary://tenants/tenant-beta/abdfiles/other-ref';
    const newStorageRef = 'cloudinary://tenants/tenant-alpha/abdfiles/new-ref';

    // Mock DocumentVersion.findOne to return null, because we filter by currentTenantId
    const findOneSpy = vi.spyOn(DocumentVersion, 'findOne') as any;
    findOneSpy.mockImplementation(async (query: any) => {
      // If the query is for the current tenant, return null (no match)
      if (query.tenantId === currentTenantId) {
        return null;
      }
      // If query is for other tenant, return the existing record
      if (query.tenantId === otherTenantId) {
        return {
          tenantId: otherTenantId,
          hash,
          storageRef: otherStorageRef,
        };
      }
      return null;
    });

    const uploadFileSpy = vi.spyOn(StorageService, 'uploadFile').mockResolvedValue({
      storageRef: newStorageRef,
    } as any);

    const documentCreateSpy = vi.spyOn(Document, 'create').mockResolvedValue({} as any);
    const versionCreateSpy = vi.spyOn(DocumentVersion, 'create').mockResolvedValue({} as any);
    const eventCreateSpy = vi.spyOn(DocumentEvent, 'create').mockResolvedValue({} as any);

    await DocumentService.uploadDocument({
      tenantId: currentTenantId,
      actorId: 'user-1',
      title: 'Different Tenant Doc',
      fileBuffer: Buffer.from('test content'),
      mimeType: 'text/plain',
      sizeBytes: 12,
      retentionClass: 'standard',
      sensitivityLevel: 'low',
    });

    // Verify it queries with the current tenantId and hash
    expect(findOneSpy).toHaveBeenCalledWith({ tenantId: currentTenantId, hash });

    // Verify it PERFORMED a new physical upload since it belongs to a different tenant
    expect(uploadFileSpy).toHaveBeenCalledWith(
      expect.any(Buffer),
      currentTenantId,
      'text/plain'
    );

    // Verify it created the document and version with the new storageRef, not the other tenant's storageRef
    expect(documentCreateSpy).toHaveBeenCalledWith(
      expect.objectContaining({
        tenantId: currentTenantId,
        storageRefCurrent: newStorageRef,
      })
    );
    expect(versionCreateSpy).toHaveBeenCalledWith(
      expect.objectContaining({
        tenantId: currentTenantId,
        storageRef: newStorageRef,
      })
    );
  });
});
