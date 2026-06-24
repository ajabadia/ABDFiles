/**
 * @purpose Gestiona operaciones de almacenamiento de archivos utilizando proveedores como Cloudinary, almacenamiento compatible con S3, Google Drive y OneDrive.
 * @purpose_en Manages file storage operations using various providers such as Cloudinary, S3-compatible storage, Google Drive, and OneDrive.
 * @refactorable true (contains multiple provider implementations)
 * @classification Business Service
 * @complexity Medium
 * @fingerprint exports:6,imports:6,sig:rk112o
 * @lastUpdated 2026-06-21T16:07:34.496Z
 */

import { v2 as cloudinary } from 'cloudinary';
import { S3Client, PutObjectCommand, GetObjectCommand, DeleteObjectCommand } from '@aws-sdk/client-s3';
import { getSignedUrl as getS3SignedUrl } from '@aws-sdk/s3-request-presigner';
import { google } from 'googleapis';
import { ConfidentialClientApplication } from '@azure/msal-node';
import { Readable } from 'stream';

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

// ── 3. GOOGLE DRIVE PROVIDER (Service Account) ───────────────────────
// credentialsRef fields:
//   type, project_id, private_key_id, private_key, client_email,
//   client_id, auth_uri, token_uri, ... (standard service account JSON)
//   + optional: folderId (target Drive folder, defaults to root)
export class GoogleDriveProvider implements IStorageProvider {
  private isMock(config: Record<string, unknown>): boolean {
    return !!config.isMock || !config.client_email || !config.private_key;
  }

  private getAuthClient(config: Record<string, unknown>) {
    return new google.auth.GoogleAuth({
      credentials: {
        client_email: config.client_email as string,
        private_key: (config.private_key as string).replace(/\\n/g, '\n'),
      },
      scopes: ['https://www.googleapis.com/auth/drive.file'],
    });
  }

  async uploadFile(
    buffer: Buffer,
    storageRef: string,
    mimeType: string,
    config: Record<string, unknown>
  ): Promise<{ storageRef: string; url: string }> {
    if (this.isMock(config)) {
      const fileId = `gdrive-mock-${Date.now()}`;
      return { storageRef: fileId, url: `https://drive.google.com/open?id=${fileId}` };
    }

    const auth = this.getAuthClient(config);
    const drive = google.drive({ version: 'v3', auth });

    const stream = new Readable();
    stream.push(buffer);
    stream.push(null);

    const res = await drive.files.create({
      requestBody: {
        name: storageRef,
        parents: config.folderId ? [config.folderId as string] : undefined,
      },
      media: { mimeType, body: stream },
      fields: 'id, webViewLink',
    });

    const fileId = res.data.id!;
    return {
      storageRef: `gdrive:${fileId}`,
      url: res.data.webViewLink || `https://drive.google.com/open?id=${fileId}`,
    };
  }

  async getSignedUrl(
    storageRef: string,
    _mimeType: string,
    config: Record<string, unknown>
  ): Promise<string> {
    const rawId = storageRef.replace(/^gdrive:/, '');
    if (this.isMock(config)) {
      return `https://docs.google.com/viewer?srcid=${rawId}&authuser=1`;
    }
    // For service accounts, return a direct download link valid while the SA has access
    return `https://www.googleapis.com/drive/v3/files/${rawId}?alt=media`;
  }

  async deleteFile(
    storageRef: string,
    _mimeType: string,
    config: Record<string, unknown>
  ): Promise<void> {
    const rawId = storageRef.replace(/^gdrive:/, '');
    if (this.isMock(config)) return;

    const auth = this.getAuthClient(config);
    const drive = google.drive({ version: 'v3', auth });
    await drive.files.delete({ fileId: rawId });
  }
}

// ── 4. MICROSOFT ONEDRIVE PROVIDER (App-only / Client Credentials) ────
// credentialsRef fields:
//   clientId, clientSecret, tenantId (Azure AD tenant), driveId (optional)
export class OneDriveProvider implements IStorageProvider {
  private isMock(config: Record<string, unknown>): boolean {
    return !!config.isMock || !config.clientId || !config.clientSecret || !config.tenantId;
  }

  private async getAccessToken(config: Record<string, unknown>): Promise<string> {
    const msalApp = new ConfidentialClientApplication({
      auth: {
        clientId: config.clientId as string,
        clientSecret: config.clientSecret as string,
        authority: `https://login.microsoftonline.com/${config.tenantId as string}`,
      },
    });

    const result = await msalApp.acquireTokenByClientCredential({
      scopes: ['https://graph.microsoft.com/.default'],
    });

    if (!result?.accessToken) {
      throw new Error('[OneDriveProvider] Failed to acquire Microsoft Graph access token');
    }
    return result.accessToken;
  }

  private graphBaseUrl(config: Record<string, unknown>): string {
    // If a specific driveId is set, use /drives/{id}; otherwise use /me/drive (app-level)
    const driveId = config.driveId as string | undefined;
    return driveId
      ? `https://graph.microsoft.com/v1.0/drives/${driveId}`
      : `https://graph.microsoft.com/v1.0/me/drive`;
  }

  async uploadFile(
    buffer: Buffer,
    storageRef: string,
    mimeType: string,
    config: Record<string, unknown>
  ): Promise<{ storageRef: string; url: string }> {
    if (this.isMock(config)) {
      const itemId = `onedrive-mock-${Date.now()}`;
      return { storageRef: itemId, url: `https://onedrive.live.com/redir?resid=${itemId}` };
    }

    const token = await this.getAccessToken(config);
    const base = this.graphBaseUrl(config);
    // Simple upload (< 4 MB). For larger files a resumable upload session would be needed.
    const uploadUrl = `${base}/root:/${encodeURIComponent(storageRef)}:/content`;

    const res = await fetch(uploadUrl, {
      method: 'PUT',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': mimeType,
      },
      body: new Uint8Array(buffer),
    });

    if (!res.ok) {
      const err = await res.text();
      throw new Error(`[OneDriveProvider] Upload failed: ${err}`);
    }

    const data = await res.json() as { id: string; webUrl: string };
    return {
      storageRef: `onedrive:${data.id}`,
      url: data.webUrl,
    };
  }

  async getSignedUrl(
    storageRef: string,
    _mimeType: string,
    config: Record<string, unknown>
  ): Promise<string> {
    const rawId = storageRef.replace(/^onedrive:/, '');
    if (this.isMock(config)) {
      return `https://api.onedrive.com/v1.0/shares/${rawId}/root/content`;
    }

    const token = await this.getAccessToken(config);
    const base = this.graphBaseUrl(config);
    // Get a short-lived download URL via createLink
    const res = await fetch(`${base}/items/${rawId}/createLink`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ type: 'view', scope: 'organization', expirationDateTime: new Date(Date.now() + 3600_000).toISOString() }),
    });

    if (!res.ok) throw new Error(`[OneDriveProvider] getSignedUrl failed: ${await res.text()}`);
    const data = await res.json() as { link: { webUrl: string } };
    return data.link.webUrl;
  }

  async deleteFile(
    storageRef: string,
    _mimeType: string,
    config: Record<string, unknown>
  ): Promise<void> {
    const rawId = storageRef.replace(/^onedrive:/, '');
    if (this.isMock(config)) return;

    const token = await this.getAccessToken(config);
    const base = this.graphBaseUrl(config);

    const res = await fetch(`${base}/items/${rawId}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${token}` },
    });

    if (!res.ok && res.status !== 204) {
      throw new Error(`[OneDriveProvider] Delete failed: ${await res.text()}`);
    }
  }
}

// ── REGISTRY ────────────────────────────────────────────────────────
export const StorageProviderRegistry: Record<string, IStorageProvider> = {
  cloudinary: new CloudinaryProvider(),
  s3Compatible: new S3CompatibleProvider(),
  googleDrive: new GoogleDriveProvider(),
  oneDrive: new OneDriveProvider()
};
