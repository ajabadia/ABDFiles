import { describe, it, expect, vi, beforeEach } from 'vitest';
import { setupTestEnvironment } from './test-setup';
import Document from '../Document';
import DocumentVersion from '../DocumentVersion';
import DocumentEvent from '../DocumentEvent';
import IdempotencyKey from '../IdempotencyKey';
import StorageConnector from '../StorageConnector';
import { DocumentService } from '../../services/document-service';
import { WebhookService } from '../../services/webhook-service';
import { getCachedResponse, saveResponse } from '../../lib/idempotency';

setupTestEnvironment();

describe('ABDFiles Fase 4 - Idempotency, Concurrencia y Webhooks', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.spyOn(StorageConnector, 'findOne').mockResolvedValue(null);
  });

  describe('Idempotency Helper', () => {
    it('should retrieve cached response if it exists', async () => {
      const mockRecord = {
        key: 'idemp-1',
        tenantId: 'tenant-1',
        responseBody: { success: true },
        statusCode: 201
      };

      vi.spyOn(IdempotencyKey, 'findOne').mockResolvedValue(mockRecord as unknown as never);

      const result = await getCachedResponse('tenant-1', 'idemp-1');
      expect(result.cached).toBe(true);
      if (result.cached) {
        expect(result.response.status).toBe(201);
      }
    });

    it('should return cached false if key is not found', async () => {
      vi.spyOn(IdempotencyKey, 'findOne').mockResolvedValue(null);

      const result = await getCachedResponse('tenant-1', 'idemp-2');
      expect(result.cached).toBe(false);
    });

    it('should save response payload successfully', async () => {
      const createSpy = vi.spyOn(IdempotencyKey, 'create').mockResolvedValue({} as unknown as never);
      await saveResponse('tenant-1', 'idemp-3', { status: 'ok' }, 200);
      expect(createSpy).toHaveBeenCalled();
    });
  });

  describe('Optimistic Concurrency Control', () => {
    it('should reject new version creation if document version changes concurrently', async () => {
      const mockDoc = {
        tenantId: 'tenant-1',
        assetId: '123',
        status: 'active',
        version: 1,
        save: vi.fn().mockResolvedValue(true)
      };

      // Mock first findOne to return document version 1, and second findOne to return version 2 (simulating concurrent edit)
      vi.spyOn(Document, 'findOne')
        .mockResolvedValueOnce(mockDoc as unknown as never)
        .mockResolvedValueOnce({ ...mockDoc, version: 2 } as unknown as never);

      vi.spyOn(DocumentVersion, 'findOne').mockReturnValue({
        sort: vi.fn().mockResolvedValue({ versionNumber: 1 })
      } as unknown as never);
      vi.spyOn(DocumentVersion, 'updateMany').mockResolvedValue({} as unknown as never);
      vi.spyOn(DocumentVersion, 'create').mockResolvedValue({} as unknown as never);

      await expect(
        DocumentService.createNewVersion({
          tenantId: 'tenant-1',
          actorId: 'admin-1',
          assetId: '123',
          fileBuffer: Buffer.from('hello'),
          mimeType: 'text/plain',
          sizeBytes: 5
        })
      ).rejects.toThrow('VersionConflictError');
    });
  });

  describe('Webhooks Dispatcher', () => {
    it('should attempt post requests on emitWebhook', async () => {
      process.env.DOCS_WEBHOOK_URL = 'http://docs-test/webhook';
      
      const fetchSpy = vi.fn().mockResolvedValue({ ok: true });
      global.fetch = fetchSpy;

      await WebhookService.emitWebhook('DOCUMENT_CREATED', 'tenant-1', { assetId: '123' });
      expect(fetchSpy).toHaveBeenCalled();
    });
  });
});
