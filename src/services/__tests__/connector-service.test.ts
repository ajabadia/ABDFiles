/* eslint-disable @typescript-eslint/no-explicit-any */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { ConnectorService } from '../connector-service';
import StorageConnector from '@/models/StorageConnector';
import { StorageProviderRegistry } from '../storage/storage-providers';

vi.mock('@/models/StorageConnector', () => {
  const mockQueries = {
    sort: vi.fn().mockImplementation(() => Promise.resolve([])),
    findOne: vi.fn().mockImplementation(() => Promise.resolve(null)),
    updateMany: vi.fn().mockImplementation(() => Promise.resolve({ matchedCount: 1, modifiedCount: 1 })),
    deleteOne: vi.fn().mockImplementation(() => Promise.resolve({ deletedCount: 1 }))
  };

  class MockConnectorClass {
    constructor(data: Record<string, unknown>) {
      Object.assign(this, data);
    }
    save = vi.fn().mockImplementation(function(this: Record<string, unknown>) {
      return Promise.resolve(this);
    });

    static find = vi.fn().mockReturnValue(mockQueries);
    static findOne = mockQueries.findOne;
    static updateMany = mockQueries.updateMany;
    static deleteOne = mockQueries.deleteOne;
  }

  return {
    default: MockConnectorClass
  };
});

vi.mock('../storage/storage-providers', () => {
  const mockProvider = {
    uploadFile: vi.fn().mockResolvedValue({ storageRef: 'test-ref', url: 'https://test-url.com' }),
    getSignedUrl: vi.fn().mockResolvedValue('https://signed-url.com'),
    deleteFile: vi.fn().mockResolvedValue(undefined)
  };

  return {
    StorageProviderRegistry: {
      cloudinary: mockProvider,
      s3Compatible: mockProvider
    }
  };
});

describe('ConnectorService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('validateCredentials', () => {
    it('should throw if credentials is not valid JSON', () => {
      expect(() =>
        ConnectorService.validateCredentials('cloudinary', 'not-json')
      ).toThrow('credentialsRef must be a valid JSON string');
    });

    it('should throw if cloudinary credentials missing cloudName', () => {
      expect(() =>
        ConnectorService.validateCredentials('cloudinary', '{}')
      ).toThrow('Cloudinary credentials must contain cloudName');
    });

    it('should throw if s3 credentials missing keys', () => {
      expect(() =>
        ConnectorService.validateCredentials('s3Compatible', '{}')
      ).toThrow('S3 credentials must contain accessKeyId');
    });

    it('should not throw if valid', () => {
      expect(() =>
        ConnectorService.validateCredentials(
          'cloudinary',
          JSON.stringify({ cloudName: 'test-cloud' })
        )
      ).not.toThrow();
    });
  });

  describe('createConnector', () => {
    it('should create a connector and deactivate others if active', async () => {
      const result = await ConnectorService.createConnector('tenant-1', {
        providerType: 'cloudinary',
        credentialsRef: JSON.stringify({ cloudName: 'test-cloud' }),
        status: 'active'
      });

      expect(StorageConnector.updateMany).toHaveBeenCalledWith(
        { tenantId: 'tenant-1', status: 'active' },
        { status: 'inactive' }
      );
      expect(result.providerType).toBe('cloudinary');
      expect(result.status).toBe('active');
    });
  });

  describe('testConnection', () => {
    it('should return success when operations complete successfully', async () => {
      (StorageConnector.findOne as any).mockResolvedValue({
        connectorId: 'conn-1',
        tenantId: 'tenant-1',
        providerType: 'cloudinary',
        credentialsRef: JSON.stringify({ cloudName: 'test-cloud' })
      });

      const result = await ConnectorService.testConnection('tenant-1', 'conn-1');
      expect(result.success).toBe(true);
      expect(result.message).toContain('passed successfully');
      expect(StorageProviderRegistry.cloudinary.uploadFile).toHaveBeenCalled();
    });

    it('should return failure when operations throw error', async () => {
      (StorageConnector.findOne as any).mockResolvedValue({
        connectorId: 'conn-1',
        tenantId: 'tenant-1',
        providerType: 'cloudinary',
        credentialsRef: JSON.stringify({ cloudName: 'test-cloud' })
      });

      vi.mocked(StorageProviderRegistry.cloudinary.uploadFile).mockRejectedValueOnce(
        new Error('Upload refused')
      );

      const result = await ConnectorService.testConnection('tenant-1', 'conn-1');
      expect(result.success).toBe(false);
      expect(result.message).toContain('Connection failed: Upload refused');
    });
  });
});
