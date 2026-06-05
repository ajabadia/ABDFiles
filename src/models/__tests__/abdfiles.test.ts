import { describe, it, expect, vi, beforeEach } from 'vitest';
import { setupTestEnvironment } from './test-setup';
import Document, { TDocument } from '../Document';
import DocumentVersion, { TDocumentVersion } from '../DocumentVersion';
import DocumentEvent, { TDocumentEvent } from '../DocumentEvent';
import AssetSpaceLink from '../AssetSpaceLink';
import StorageConnector from '../StorageConnector';
import DeletionJob, { TDeletionJob } from '../DeletionJob';
import LegalHold from '../LegalHold';
import { DocumentService } from '../../services/document-service';
import { StorageService } from '../../services/storage-service';

setupTestEnvironment();

describe('ABDFiles Model & Services Core Layer', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.spyOn(StorageConnector, 'findOne').mockResolvedValue(null);
  });

  describe('Model Definitions', () => {
    it('should define Document model with correct schema properties', () => {
      expect(Document).toBeDefined();
      const paths = Document.schema.paths;
      expect(paths.assetId).toBeDefined();
      expect(paths.assetRef).toBeDefined();
      expect(paths.tenantId).toBeDefined();
      expect(paths.status).toBeDefined();
      expect(paths.currentVersionId).toBeDefined();
      expect(paths.latestHash).toBeDefined();
      expect(paths.storageProvider).toBeDefined();
      expect(paths.storageRefCurrent).toBeDefined();
      expect(paths.retentionClass).toBeDefined();
      expect(paths.sensitivityLevel).toBeDefined();
      expect(paths.legalHold).toBeDefined();
    });

    it('should define DocumentVersion model with correct schema properties', () => {
      expect(DocumentVersion).toBeDefined();
      const paths = DocumentVersion.schema.paths;
      expect(paths.versionId).toBeDefined();
      expect(paths.tenantId).toBeDefined();
      expect(paths.assetId).toBeDefined();
      expect(paths.versionNumber).toBeDefined();
      expect(paths.hash).toBeDefined();
      expect(paths.storageRef).toBeDefined();
      expect(paths.mimeType).toBeDefined();
      expect(paths.sizeBytes).toBeDefined();
    });

    it('should define DocumentEvent model with correct schema properties', () => {
      expect(DocumentEvent).toBeDefined();
      const paths = DocumentEvent.schema.paths;
      expect(paths.eventId).toBeDefined();
      expect(paths.tenantId).toBeDefined();
      expect(paths.assetId).toBeDefined();
      expect(paths.type).toBeDefined();
      expect(paths.actorId).toBeDefined();
      expect(paths.correlationId).toBeDefined();
      expect(paths.payload).toBeDefined();
    });

    it('should define AssetSpaceLink model with correct schema properties', () => {
      expect(AssetSpaceLink).toBeDefined();
      const paths = AssetSpaceLink.schema.paths;
      expect(paths.linkId).toBeDefined();
      expect(paths.tenantId).toBeDefined();
      expect(paths.assetId).toBeDefined();
      expect(paths.spaceId).toBeDefined();
      expect(paths.spacePath).toBeDefined();
      expect(paths.isPrimary).toBeDefined();
    });

    it('should define StorageConnector model with correct schema properties', () => {
      expect(StorageConnector).toBeDefined();
      const paths = StorageConnector.schema.paths;
      expect(paths.connectorId).toBeDefined();
      expect(paths.tenantId).toBeDefined();
      expect(paths.providerType).toBeDefined();
      expect(paths.status).toBeDefined();
      expect(paths.credentialsRef).toBeDefined();
    });

    it('should define DeletionJob model with correct schema properties', () => {
      expect(DeletionJob).toBeDefined();
      const paths = DeletionJob.schema.paths;
      expect(paths.jobId).toBeDefined();
      expect(paths.tenantId).toBeDefined();
      expect(paths.assetId).toBeDefined();
      expect(paths.purgeAt).toBeDefined();
      expect(paths.status).toBeDefined();
      expect(paths.attempts).toBeDefined();
    });

    it('should define LegalHold model with correct schema properties', () => {
      expect(LegalHold).toBeDefined();
      const paths = LegalHold.schema.paths;
      expect(paths.holdId).toBeDefined();
      expect(paths.tenantId).toBeDefined();
      expect(paths.assetId).toBeDefined();
      expect(paths.reason).toBeDefined();
      expect(paths.status).toBeDefined();
    });
  });

  describe('DocumentService Operations', () => {
    it('should perform uploadDocument successfully by creating models', async () => {
      const mockDoc = { assetId: '123', assetRef: 'files:tenant-1:123:1' };
      const mockVer = { versionId: 'v1' };
      const mockEvt = { eventId: 'e1' };

      const documentCreateSpy = vi.spyOn(Document, 'create').mockResolvedValue(mockDoc as unknown as never);
      const versionCreateSpy = vi.spyOn(DocumentVersion, 'create').mockResolvedValue(mockVer as unknown as never);
      const eventCreateSpy = vi.spyOn(DocumentEvent, 'create').mockResolvedValue(mockEvt as unknown as never);

      // Mock deduplication search to return null (no match)
      vi.spyOn(DocumentVersion, 'findOne').mockResolvedValue(null);

      const result = await DocumentService.uploadDocument({
        tenantId: 'tenant-1',
        actorId: 'user-1',
        title: 'Test Document',
        fileBuffer: Buffer.from('hello world'),
        mimeType: 'text/plain',
        sizeBytes: 11,
        retentionClass: 'standard',
        sensitivityLevel: 'low'
      });

      expect(result).toBeDefined();
      expect(result.assetRef).toBeDefined();
      expect(documentCreateSpy).toHaveBeenCalled();
      expect(versionCreateSpy).toHaveBeenCalled();
      expect(eventCreateSpy).toHaveBeenCalled();
    });

    it('should handle logical delete by changing status and creating DeletionJob', async () => {
      const mockDoc = {
        tenantId: 'tenant-1',
        assetId: '123',
        status: 'active',
        legalHold: false,
        retentionClass: 'temporary',
        version: 1,
        save: vi.fn().mockResolvedValue(true)
      };

      vi.spyOn(Document, 'findOne').mockResolvedValue(mockDoc as unknown as never);
      const deletionJobCreateSpy = vi.spyOn(DeletionJob, 'create').mockResolvedValue({} as unknown as never);
      const eventCreateSpy = vi.spyOn(DocumentEvent, 'create').mockResolvedValue({} as unknown as never);

      await DocumentService.logicalDeleteDocument({
        tenantId: 'tenant-1',
        assetId: '123',
        actorId: 'user-1'
      });

      expect(mockDoc.status).toBe('deleted_pending_retention');
      expect(mockDoc.save).toHaveBeenCalled();
      expect(deletionJobCreateSpy).toHaveBeenCalled();
      expect(eventCreateSpy).toHaveBeenCalled();
    });

    it('should block logical delete if document has active legal hold', async () => {
      const mockDoc = {
        tenantId: 'tenant-1',
        assetId: '123',
        status: 'active',
        legalHold: true,
        retentionClass: 'temporary',
        version: 1,
        save: vi.fn()
      };

      vi.spyOn(Document, 'findOne').mockResolvedValue(mockDoc as unknown as never);

      await expect(
        DocumentService.logicalDeleteDocument({
          tenantId: 'tenant-1',
          assetId: '123',
          actorId: 'user-1'
        })
      ).rejects.toThrow('Cannot delete document under active legal hold');
    });

    it('should execute purgeExpiredDocuments successfully', async () => {
      const mockJob = {
        jobId: 'job-1',
        tenantId: 'tenant-1',
        assetId: '123',
        status: 'pending',
        purgeAt: new Date(Date.now() - 10000),
        attempts: 0,
        save: vi.fn().mockResolvedValue(true)
      };

      const mockDoc = {
        tenantId: 'tenant-1',
        assetId: '123',
        status: 'deleted_pending_retention',
        currentVersionId: 'v-1',
        storageRefCurrent: 'cloudinary-ref',
        legalHold: false,
        version: 1,
        save: vi.fn().mockResolvedValue(true)
      };

      const mockVer = {
        mimeType: 'text/plain'
      };

      vi.spyOn(DeletionJob, 'find').mockResolvedValue([mockJob] as unknown as never);
      vi.spyOn(Document, 'findOne').mockResolvedValue(mockDoc as unknown as never);
      vi.spyOn(DocumentVersion, 'findOne').mockResolvedValue(mockVer as unknown as never);

      const deleteFileSpy = vi.spyOn(StorageService, 'deleteFile').mockResolvedValue(undefined);
      const eventCreateSpy = vi.spyOn(DocumentEvent, 'create').mockResolvedValue({} as unknown as never);

      await DocumentService.purgeExpiredDocuments(new Date());

      expect(deleteFileSpy).toHaveBeenCalledWith('cloudinary-ref', 'text/plain', 'tenant-1');
      expect(mockDoc.status).toBe('purged');
      expect(mockDoc.save).toHaveBeenCalled();
      expect(mockJob.status).toBe('completed');
      expect(mockJob.save).toHaveBeenCalled();
      expect(eventCreateSpy).toHaveBeenCalled();
    });

    it('should apply and release legal holds', async () => {
      const mockDoc = {
        tenantId: 'tenant-1',
        assetId: '123',
        legalHold: false,
        version: 1,
        save: vi.fn().mockResolvedValue(true)
      };

      const mockHold = {
        holdId: 'hold-1',
        tenantId: 'tenant-1',
        assetId: '123',
        status: 'active',
        save: vi.fn().mockResolvedValue(true)
      };

      vi.spyOn(Document, 'findOne').mockResolvedValue(mockDoc as unknown as never);
      const holdCreateSpy = vi.spyOn(LegalHold, 'create').mockResolvedValue(mockHold as unknown as never);
      const eventCreateSpy = vi.spyOn(DocumentEvent, 'create').mockResolvedValue({} as unknown as never);
      vi.spyOn(LegalHold, 'findOne').mockResolvedValue(mockHold as unknown as never);
      vi.spyOn(LegalHold, 'countDocuments').mockResolvedValue(0);

      await DocumentService.applyLegalHold('tenant-1', '123', 'Investigation', 'admin-1');

      expect(mockDoc.legalHold).toBe(true);
      expect(holdCreateSpy).toHaveBeenCalled();
      expect(mockDoc.save).toHaveBeenCalled();

      await DocumentService.releaseLegalHold('tenant-1', '123', 'hold-1', 'admin-1');
      expect(mockDoc.legalHold).toBe(false);
      expect(mockHold.status).toBe('released');
    });
  });
});
