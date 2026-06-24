/* eslint-disable @typescript-eslint/no-explicit-any */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { setupTestEnvironment } from '../../models/__tests__/test-setup';
import StorageConnector from '../../models/StorageConnector';
import { StorageService } from '../storage-service';
import { StorageProviderRegistry } from '../storage/storage-providers';

setupTestEnvironment();

describe('Polymorphic Storage Providers', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Registry Resolution', () => {
    it('should have all 4 storage providers defined', () => {
      expect(StorageProviderRegistry.cloudinary).toBeDefined();
      expect(StorageProviderRegistry.s3Compatible).toBeDefined();
      expect(StorageProviderRegistry.googleDrive).toBeDefined();
      expect(StorageProviderRegistry.oneDrive).toBeDefined();
    });
  });

  describe('StorageService routing', () => {
    it('should default to Cloudinary when no connector is found', async () => {
      vi.spyOn(StorageConnector, 'findOne').mockResolvedValue(null);

      const buffer = Buffer.from('hello');
      const uploadSpy = vi.spyOn(StorageProviderRegistry.cloudinary, 'uploadFile');

      const result = await StorageService.uploadFile(buffer, 'tenant-test-default', 'text/plain');

      expect(uploadSpy).toHaveBeenCalled();
      expect(result.storageRef).toContain('tenants/tenant-test-default/abdfiles/');
      expect(result.url).toContain('https://res.cloudinary.com/mock-cloud/image/upload/');
    });

    it('should use S3 provider when s3Compatible connector is active', async () => {
      const mockConnector = {
        tenantId: 'tenant-test-s3',
        providerType: 's3Compatible',
        status: 'active',
        credentialsRef: JSON.stringify({
          endpoint: 'http://localhost:9000',
          region: 'us-east-1',
          bucket: 'my-bucket',
          accessKeyId: 'my-key',
          secretAccessKey: 'my-secret'
        })
      };

      vi.spyOn(StorageConnector, 'findOne').mockResolvedValue(mockConnector as any);
      const s3UploadSpy = vi.spyOn(StorageProviderRegistry.s3Compatible, 'uploadFile');

      const buffer = Buffer.from('s3 payload');
      const result = await StorageService.uploadFile(buffer, 'tenant-test-s3', 'text/plain');

      expect(s3UploadSpy).toHaveBeenCalled();
      expect(result.storageRef).toBeDefined();
    });

    it('should use Google Drive provider when googleDrive connector is active', async () => {
      const mockConnector = {
        tenantId: 'tenant-test-gdrive',
        providerType: 'googleDrive',
        status: 'active',
        credentialsRef: 'oauth-token-ref'
      };

      vi.spyOn(StorageConnector, 'findOne').mockResolvedValue(mockConnector as any);
      const gdriveUploadSpy = vi.spyOn(StorageProviderRegistry.googleDrive, 'uploadFile');

      const buffer = Buffer.from('gdrive file');
      const result = await StorageService.uploadFile(buffer, 'tenant-test-gdrive', 'text/plain');

      expect(gdriveUploadSpy).toHaveBeenCalled();
      expect(result.storageRef).toContain('gdrive-');
      expect(result.url).toContain('https://drive.google.com/');
    });

    it('should use OneDrive provider when oneDrive connector is active', async () => {
      const mockConnector = {
        tenantId: 'tenant-test-onedrive',
        providerType: 'oneDrive',
        status: 'active',
        credentialsRef: 'onedrive-auth-ref'
      };

      vi.spyOn(StorageConnector, 'findOne').mockResolvedValue(mockConnector as any);
      const onedriveUploadSpy = vi.spyOn(StorageProviderRegistry.oneDrive, 'uploadFile');

      const buffer = Buffer.from('onedrive file');
      const result = await StorageService.uploadFile(buffer, 'tenant-test-onedrive', 'text/plain');

      expect(onedriveUploadSpy).toHaveBeenCalled();
      expect(result.storageRef).toContain('onedrive-');
      expect(result.url).toContain('https://onedrive.live.com/');
    });
  });

  describe('Signed URL and Deletion with connectors', () => {
    it('should route signed URL request to correct S3 provider', async () => {
      const mockConnector = {
        tenantId: 'tenant-test-s3',
        providerType: 's3Compatible',
        status: 'active',
        credentialsRef: JSON.stringify({
          endpoint: 'http://localhost:9000',
          region: 'us-east-1',
          bucket: 'my-bucket',
          accessKeyId: 'my-key',
          secretAccessKey: 'my-secret'
        })
      };

      vi.spyOn(StorageConnector, 'findOne').mockResolvedValue(mockConnector as any);
      const s3UrlSpy = vi.spyOn(StorageProviderRegistry.s3Compatible, 'getSignedUrl').mockResolvedValue('http://signed-s3-url');

      const url = await StorageService.getSignedUrl('my-key-ref', 'text/plain', 'tenant-test-s3');
      expect(s3UrlSpy).toHaveBeenCalled();
      expect(url).toBe('http://signed-s3-url');
    });

    it('should route deletion request to correct OneDrive provider', async () => {
      const mockConnector = {
        tenantId: 'tenant-test-onedrive',
        providerType: 'oneDrive',
        status: 'active',
        credentialsRef: 'onedrive-auth-ref'
      };

      vi.spyOn(StorageConnector, 'findOne').mockResolvedValue(mockConnector as any);
      const onedriveDeleteSpy = vi.spyOn(StorageProviderRegistry.oneDrive, 'deleteFile');

      await StorageService.deleteFile('my-key-ref', 'text/plain', 'tenant-test-onedrive');
      expect(onedriveDeleteSpy).toHaveBeenCalled();
    });
  });
});
