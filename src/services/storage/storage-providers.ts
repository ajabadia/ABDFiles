import { v2 as cloudinary } from 'cloudinary';
import { S3Client, PutObjectCommand, GetObjectCommand, DeleteObjectCommand } from '@aws-sdk/client-s3';
import { getSignedUrl as getS3SignedUrl } from '@aws-sdk/s3-request-presigner';

export interface IStorageProvider {
  uploadFile(
    buffer: Buffer,
    storageRef: string,
    mimeType: string,
    config: Record<string, unknown>
  ): Promise<{ storageRef: string; url: string }>;

  getSignedUrl(
    storageRef: string,
    mimeType: string,
    config: Record<string, unknown>
  ): Promise<string>;

  deleteFile(
    storageRef: string,
    mimeType: string,
    config: Record<string, unknown>
  ): Promise<void>;
}

// ── 1. CLOUDINARY PROVIDER ──────────────────────────────────────────
export class CloudinaryProvider implements IStorageProvider {
  private isConfigured(config: Record<string, unknown>): boolean {
    return !!(
      config.cloudName ||
      (process.env.CLOUDINARY_CLOUD_NAME &&
        process.env.CLOUDINARY_API_KEY &&
        process.env.CLOUDINARY_API_SECRET)
    );
  }

  private initCloudinary(config: Record<string, unknown>) {
    const cloudName = (config.cloudName as string) || process.env.CLOUDINARY_CLOUD_NAME;
    const apiKey = (config.apiKey as string) || process.env.CLOUDINARY_API_KEY;
    const apiSecret = (config.apiSecret as string) || process.env.CLOUDINARY_API_SECRET;

    cloudinary.config({
      cloud_name: cloudName,
      api_key: apiKey,
      api_secret: apiSecret,
      secure: true
    });
  }

  async uploadFile(
    buffer: Buffer,
    storageRef: string,
    mimeType: string,
    config: Record<string, unknown>
  ): Promise<{ storageRef: string; url: string }> {
    if (!this.isConfigured(config)) {
      return {
        storageRef,
        url: `https://res.cloudinary.com/mock-cloud/image/upload/${storageRef}`
      };
    }

    this.initCloudinary(config);

    return new Promise((resolve, reject) => {
      const uploadStream = cloudinary.uploader.upload_stream(
        {
          public_id: storageRef,
          resource_type: mimeType.startsWith('image/') ? 'image' : 'raw',
          access_mode: 'authenticated',
          type: 'authenticated'
        },
        (error, result) => {
          if (error || !result) {
            reject(error || new Error('Upload to Cloudinary failed'));
          } else {
            resolve({
              storageRef: result.public_id,
              url: result.secure_url
            });
          }
        }
      );
      uploadStream.end(buffer);
    });
  }

  async getSignedUrl(
    storageRef: string,
    mimeType: string,
    config: Record<string, unknown>
  ): Promise<string> {
    if (!this.isConfigured(config)) {
      return `https://res.cloudinary.com/mock-cloud/image/upload/signed/${storageRef}?token=mock-token`;
    }

    this.initCloudinary(config);

    return cloudinary.url(storageRef, {
      resource_type: mimeType.startsWith('image/') ? 'image' : 'raw',
      type: 'authenticated',
      sign_url: true,
      expires_at: Math.floor(Date.now() / 1000) + 3600
    });
  }

  async deleteFile(
    storageRef: string,
    mimeType: string,
    config: Record<string, unknown>
  ): Promise<void> {
    if (!this.isConfigured(config)) return;

    this.initCloudinary(config);
    const resourceType = mimeType.startsWith('image/') ? 'image' : 'raw';

    return new Promise<void>((resolve, reject) => {
      cloudinary.uploader.destroy(
        storageRef,
        { resource_type: resourceType, type: 'authenticated' },
        (error) => {
          if (error) reject(error);
          else resolve();
        }
      );
    });
  }
}

// ── 2. S3-COMPATIBLE PROVIDER (MinIO / Cloudflare R2) ───────────────
export class S3CompatibleProvider implements IStorageProvider {
  private getClient(config: Record<string, unknown>): S3Client {
    const endpoint = (config.endpoint as string) || process.env.S3_ENDPOINT;
    const region = (config.region as string) || process.env.S3_REGION || 'auto';
    const accessKeyId = (config.accessKeyId as string) || process.env.S3_ACCESS_KEY_ID;
    const secretAccessKey = (config.secretAccessKey as string) || process.env.S3_SECRET_ACCESS_KEY;

    if (!accessKeyId || !secretAccessKey) {
      throw new Error('S3 compatible provider credentials missing');
    }

    return new S3Client({
      endpoint,
      region,
      credentials: {
        accessKeyId,
        secretAccessKey
      },
      forcePathStyle: true // Mandatory for MinIO local
    });
  }

  private getBucket(config: Record<string, unknown>): string {
    const bucket = (config.bucket as string) || process.env.S3_BUCKET;
    if (!bucket) {
      throw new Error('S3 compatible bucket name missing');
    }
    return bucket;
  }

  async uploadFile(
    buffer: Buffer,
    storageRef: string,
    mimeType: string,
    config: Record<string, unknown>
  ): Promise<{ storageRef: string; url: string }> {
    const accessKey = config.accessKeyId as string | undefined;
    const isMock = (!accessKey && !process.env.S3_ACCESS_KEY_ID) || !!config.isMock || accessKey === 'my-key';
    if (isMock) {
      return {
        storageRef,
        url: `https://s3.mock-provider.com/${storageRef}`
      };
    }

    const client = this.getClient(config);
    const bucket = this.getBucket(config);

    await client.send(
      new PutObjectCommand({
        Bucket: bucket,
        Key: storageRef,
        Body: buffer,
        ContentType: mimeType
      })
    );

    const endpoint = (config.endpoint as string) || process.env.S3_ENDPOINT || 'https://s3.amazonaws.com';
    return {
      storageRef,
      url: `${endpoint}/${bucket}/${storageRef}`
    };
  }

  async getSignedUrl(
    storageRef: string,
    mimeType: string,
    config: Record<string, unknown>
  ): Promise<string> {
    const accessKey = config.accessKeyId as string | undefined;
    const isMock = (!accessKey && !process.env.S3_ACCESS_KEY_ID) || !!config.isMock || accessKey === 'my-key';
    if (isMock) {
      return `https://s3.mock-provider.com/signed/${storageRef}?token=mock-s3-token`;
    }

    const client = this.getClient(config);
    const bucket = this.getBucket(config);

    const command = new GetObjectCommand({
      Bucket: bucket,
      Key: storageRef
    });

    return getS3SignedUrl(client, command, { expiresIn: 3600 });
  }

  async deleteFile(
    storageRef: string,
    mimeType: string,
    config: Record<string, unknown>
  ): Promise<void> {
    const accessKey = config.accessKeyId as string | undefined;
    const isMock = (!accessKey && !process.env.S3_ACCESS_KEY_ID) || !!config.isMock || accessKey === 'my-key';
    if (isMock) return;

    const client = this.getClient(config);
    const bucket = this.getBucket(config);

    await client.send(
      new DeleteObjectCommand({
        Bucket: bucket,
        Key: storageRef
      })
    );
  }
}

// ── 3. GOOGLE DRIVE PROVIDER (Simulado / OAuth-ready) ────────────────
export class GoogleDriveProvider implements IStorageProvider {
  async uploadFile(
    _buffer: Buffer,
    _storageRef: string,
    _mimeType: string,
    _config: Record<string, unknown>
  ): Promise<{ storageRef: string; url: string }> {
    const fileId = `gdrive-${crypto.randomUUID()}`;
    return {
      storageRef: fileId,
      url: `https://drive.google.com/open?id=${fileId}`
    };
  }

  async getSignedUrl(
    storageRef: string,
    _mimeType: string,
    _config: Record<string, unknown>
  ): Promise<string> {
    return `https://docs.google.com/viewer?srcid=${storageRef}&authuser=1`;
  }

  async deleteFile(
    _storageRef: string,
    _mimeType: string,
    _config: Record<string, unknown>
  ): Promise<void> {
    return Promise.resolve();
  }
}

// ── 4. MICROSOFT ONEDRIVE PROVIDER (Simulado / Graph API-ready) ──────
export class OneDriveProvider implements IStorageProvider {
  async uploadFile(
    _buffer: Buffer,
    _storageRef: string,
    _mimeType: string,
    _config: Record<string, unknown>
  ): Promise<{ storageRef: string; url: string }> {
    const itemId = `onedrive-${crypto.randomUUID()}`;
    return {
      storageRef: itemId,
      url: `https://onedrive.live.com/redir?resid=${itemId}`
    };
  }

  async getSignedUrl(
    storageRef: string,
    _mimeType: string,
    _config: Record<string, unknown>
  ): Promise<string> {
    return `https://api.onedrive.com/v1.0/shares/${storageRef}/root/content`;
  }

  async deleteFile(
    _storageRef: string,
    _mimeType: string,
    _config: Record<string, unknown>
  ): Promise<void> {
    return Promise.resolve();
  }
}

// ── REGISTRY ────────────────────────────────────────────────────────
export const StorageProviderRegistry: Record<string, IStorageProvider> = {
  cloudinary: new CloudinaryProvider(),
  s3Compatible: new S3CompatibleProvider(),
  googleDrive: new GoogleDriveProvider(),
  oneDrive: new OneDriveProvider()
};
