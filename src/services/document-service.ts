/**
 * @purpose Gestiona activos documentales y historias de versiones, incluyendo subir nuevos documentos, actualizar versiones existentes, aplicar retenciones legales y liberarlos.
 * @purpose_en Manages document assets and version histories, including uploading new documents, updating existing versions, applying legal holds, and releasing them.
 * @refactorable true (contains multiple business logic functions)
 * @classification Business Service
 * @complexity Medium
 * @fingerprint exports:3,imports:11,sig:1uwkpu8
 * @lastUpdated 2026-06-23T23:04:11.840Z
 */

import crypto from 'crypto';
import Document, { TDocument } from '@/models/Document';
import DocumentVersion, { TDocumentVersion } from '@/models/DocumentVersion';
import DocumentEvent from '@/models/DocumentEvent';
import DeletionJob from '@/models/DeletionJob';
import StorageConnector from '@/models/StorageConnector';
import { StorageService } from './storage-service';
import { SpaceLinkService } from './space-link-service';
import { LegalHoldService } from './legal-hold-service';
import { WebhookService } from './webhook-service';
import { IntegrationLogsService } from './integration-logs-service';

export type UploadInput = {
  tenantId: string;
  actorId: string;
  title: string;
  fileBuffer: Buffer;
  mimeType: string;
  sizeBytes: number;
  retentionClass: string;
  sensitivityLevel: 'low' | 'medium' | 'high' | 'restricted';
  spaceIds?: string[];
  spacePaths?: string[];
  tags?: string[];
  correlationId?: string;
};

export type NewVersionInput = {
  tenantId: string;
  actorId: string;
  assetId: string;
  fileBuffer: Buffer;
  mimeType: string;
  sizeBytes: number;
  correlationId?: string;
};

/**
 * Service managing document assets and version histories.
 */
export const DocumentService = {
  /**
   * Registers a new document asset, computing the hash and applying deduplication.
   */
  async uploadDocument(input: UploadInput): Promise<{ assetRef: string; assetId: string; versionId: string }> {
    // 1. Calculate SHA-256 checksum of file buffer
    const hash = crypto.createHash('sha256').update(input.fileBuffer).digest('hex');

    // 2. Multi-tenant Deduplication: Check if this file hash already exists inside the tenant
    const existingVersion = await DocumentVersion.findOne({
      tenantId: input.tenantId,
      hash
    });

    let storageRef: string;
    if (existingVersion) {
      storageRef = existingVersion.storageRef;
    } else {
      // Physical upload
      const uploadRes = await StorageService.uploadFile(input.fileBuffer, input.tenantId, input.mimeType);
      storageRef = uploadRes.storageRef;
    }

    const assetId = crypto.randomUUID();
    const versionId = crypto.randomUUID();
    const versionNumber = 1;
    const assetRef = `files:${input.tenantId}:${assetId}:${versionNumber}`;

    const connector = await StorageConnector.findOne({ tenantId: input.tenantId, status: 'active' });
    const providerType = connector?.providerType || 'cloudinary';

    // 3. Create Document Asset
    await Document.create({
      assetId,
      assetRef,
      tenantId: input.tenantId,
      title: input.title,
      tags: input.tags || [],
      status: 'active',
      currentVersionId: versionId,
      latestHash: hash,
      storageProvider: providerType,
      storageRefCurrent: storageRef,
      retentionClass: input.retentionClass,
      sensitivityLevel: input.sensitivityLevel || 'low',
      legalHold: false,
      version: 1
    });

    // 4. Create Document Version
    await DocumentVersion.create({
      versionId,
      tenantId: input.tenantId,
      assetId,
      versionNumber,
      hash,
      checksumAlgorithm: 'SHA-256',
      storageRef,
      mimeType: input.mimeType,
      sizeBytes: input.sizeBytes,
      createdBy: input.actorId,
      isCurrent: true
    });

    // 5. Audit Log (Internal Event)
    const correlationId = input.correlationId || crypto.randomUUID();
    await DocumentEvent.create({
      eventId: crypto.randomUUID(),
      tenantId: input.tenantId,
      assetId,
      versionId,
      type: 'DOCUMENT_CREATED',
      actorId: input.actorId,
      correlationId,
      payload: {
        title: input.title,
        sizeBytes: input.sizeBytes,
        mimeType: input.mimeType,
        deduplicated: !!existingVersion
      }
    });

    // 6. Link to Spaces
    if (input.spaceIds && input.spaceIds.length > 0) {
      for (let i = 0; i < input.spaceIds.length; i++) {
        const spaceId = input.spaceIds[i];
        const spacePath = input.spacePaths?.[i] || `/spaces/${spaceId}`;
        await SpaceLinkService.linkAssetToSpace(
          input.tenantId,
          assetId,
          spaceId,
          spacePath,
          i === 0, // Mark the first one as primary
          input.actorId
        );
      }
    }

    // Integrate external webhook and logs replication
    const eventPayload = { assetId, versionId, title: input.title };
    void WebhookService.emitWebhook('DOCUMENT_CREATED', input.tenantId, eventPayload);
    void IntegrationLogsService.replicateEvent(input.tenantId, assetId, 'DOCUMENT_CREATED', eventPayload);

    return {
      assetRef,
      assetId,
      versionId
    };
  },

  /**
   * Appends an immutable new version to an existing document asset.
   */
  async createNewVersion(input: NewVersionInput): Promise<TDocumentVersion> {
    const asset = await Document.findOne({ tenantId: input.tenantId, assetId: input.assetId });
    if (!asset) {
      throw new Error('Document asset not found');
    }
    if (asset.status === 'purged') {
      throw new Error('Cannot add version to a purged document');
    }
    if (asset.legalHold) {
      throw new Error('Cannot add version to a document under active legal hold');
    }

    // 1. Calculate Hash
    const hash = crypto.createHash('sha256').update(input.fileBuffer).digest('hex');

    // 2. Physical upload (always upload new file for new versions to preserve historic integrity)
    const uploadRes = await StorageService.uploadFile(input.fileBuffer, input.tenantId, input.mimeType);
    const storageRef = uploadRes.storageRef;

    // Get latest version number
    const latestVersion = await DocumentVersion.findOne({
      tenantId: input.tenantId,
      assetId: input.assetId
    }).sort({ versionNumber: -1 });

    const newVersionNumber = latestVersion ? latestVersion.versionNumber + 1 : 1;
    const versionId = crypto.randomUUID();

    // 3. Mark old versions as not current
    await DocumentVersion.updateMany(
      { tenantId: input.tenantId, assetId: input.assetId },
      { $set: { isCurrent: false } }
    );

    // 4. Create new version
    const newVersion = await DocumentVersion.create({
      versionId,
      tenantId: input.tenantId,
      assetId: input.assetId,
      versionNumber: newVersionNumber,
      hash,
      checksumAlgorithm: 'SHA-256',
      storageRef,
      mimeType: input.mimeType,
      sizeBytes: input.sizeBytes,
      createdBy: input.actorId,
      isCurrent: true,
      supersedesVersionId: asset.currentVersionId
    });

    // 5. Update asset pointers (with optimistic concurrency check)
    const dbAsset = await Document.findOne({ tenantId: input.tenantId, assetId: input.assetId });
    if (!dbAsset || dbAsset.version !== asset.version) {
      throw new Error('VersionConflictError: Document modified concurrently');
    }

    asset.currentVersionId = versionId;
    asset.latestHash = hash;
    asset.storageRefCurrent = storageRef;
    asset.assetRef = `files:${input.tenantId}:${input.assetId}:${newVersionNumber}`;
    asset.version = asset.version + 1;
    await asset.save();

    // 6. Audit Log
    const correlationId = input.correlationId || crypto.randomUUID();
    await DocumentEvent.create({
      eventId: crypto.randomUUID(),
      tenantId: input.tenantId,
      assetId: input.assetId,
      versionId,
      type: 'DOCUMENT_VERSION_CREATED',
      actorId: input.actorId,
      correlationId,
      payload: {
        versionNumber: newVersionNumber,
        sizeBytes: input.sizeBytes,
        mimeType: input.mimeType
      }
    });

    // Integrate external webhook and logs replication
    const eventPayload = { assetId: input.assetId, versionId, versionNumber: newVersionNumber };
    void WebhookService.emitWebhook('DOCUMENT_VERSION_CREATED', input.tenantId, eventPayload);
    void IntegrationLogsService.replicateEvent(input.tenantId, input.assetId, 'DOCUMENT_VERSION_CREATED', eventPayload);

    return newVersion;
  },

  /**
   * Retrieves document metadata including signed URL of current version.
   */
  async getDocument(tenantId: string, assetId: string): Promise<TDocument & { signedUrl: string }> {
    const asset = await Document.findOne({ tenantId, assetId });
    if (!asset) {
      throw new Error('Document not found');
    }

    const currentVersion = await DocumentVersion.findOne({ tenantId, assetId, versionId: asset.currentVersionId });
    if (!currentVersion) {
      throw new Error('Current version of document not found');
    }

    const signedUrl = await StorageService.getSignedUrl(asset.storageRefCurrent, currentVersion.mimeType, tenantId);

    return {
      ...asset.toObject(),
      signedUrl
    } as unknown as TDocument & { signedUrl: string };
  },

  /**
   * Lists document assets of a tenant.
   */
  async listDocuments(tenantId: string, options: { status?: string; limit?: number; page?: number } = {}): Promise<TDocument[]> {
    const query: Record<string, unknown> = { tenantId };
    if (options.status) {
      query.status = options.status;
    }

    const limit = options.limit || 50;
    const page = options.page || 1;

    return Document.find(query)
      .sort({ updatedAt: -1 })
      .skip((page - 1) * limit)
      .limit(limit);
  },

  /**
   * Performs logical delete on a document and schedules its physical purge.
   */
  async logicalDeleteDocument(input: {
    tenantId: string;
    assetId: string;
    actorId: string;
    correlationId?: string;
  }): Promise<void> {
    const asset = await Document.findOne({ tenantId: input.tenantId, assetId: input.assetId });
    if (!asset) {
      throw new Error('Document asset not found');
    }
    if (asset.legalHold) {
      throw new Error('Cannot delete document under active legal hold');
    }

    // Determine retention duration in days
    let retentionDays = 30; // Default
    if (asset.retentionClass === 'temporary') {
      retentionDays = 7;
    } else if (asset.retentionClass === 'draft') {
      retentionDays = 1;
    } else if (asset.retentionClass === 'legal') {
      retentionDays = 365;
    }

    const purgeAt = new Date();
    purgeAt.setDate(purgeAt.getDate() + retentionDays);

    // Update document metadata
    asset.status = 'deleted_pending_retention';
    asset.deletedAt = new Date();
    asset.deletedBy = input.actorId;
    asset.purgeAt = purgeAt;
    asset.version = asset.version + 1;
    await asset.save();

    // Schedule deletion job
    const jobId = crypto.randomUUID();
    await DeletionJob.create({
      jobId,
      tenantId: input.tenantId,
      assetId: input.assetId,
      purgeAt,
      status: 'pending',
      attempts: 0
    });

    // Write audit event
    const correlationId = input.correlationId || crypto.randomUUID();
    await DocumentEvent.create({
      eventId: crypto.randomUUID(),
      tenantId: input.tenantId,
      assetId: input.assetId,
      type: 'DOCUMENT_LOGICAL_DELETED',
      actorId: input.actorId,
      correlationId,
      payload: {
        purgeAt: purgeAt.toISOString(),
        retentionClass: asset.retentionClass
      }
    });
  },

  /**
   * Worker method to process pending deletion jobs.
   */
  async purgeExpiredDocuments(now: Date): Promise<void> {
    // Find deletion jobs that are pending and due
    const pendingJobs = await DeletionJob.find({
      status: 'pending',
      purgeAt: { $lte: now }
    });

    for (const job of pendingJobs) {
      try {
        const asset = await Document.findOne({ tenantId: job.tenantId, assetId: job.assetId });
        if (!asset) {
          job.status = 'failed';
          job.lastError = 'Associated document asset not found';
          await job.save();
          continue;
        }

        // If a legal hold is active, bypass purging for now
        if (asset.legalHold) {
          continue;
        }

        // Fetch current version to know the mimeType for destruction
        const currentVersion = await DocumentVersion.findOne({
          tenantId: job.tenantId,
          assetId: job.assetId,
          versionId: asset.currentVersionId
        });

        const mimeType = currentVersion?.mimeType || 'application/octet-stream';

        // Physically delete from storage
        await StorageService.deleteFile(asset.storageRefCurrent, mimeType, job.tenantId);

        // Update document status
        asset.status = 'purged';
        asset.version = asset.version + 1;
        await asset.save();

        // Update job status
        job.status = 'completed';
        job.attempts = job.attempts + 1;
        await job.save();

        // Write audit event
        await DocumentEvent.create({
          eventId: crypto.randomUUID(),
          tenantId: job.tenantId,
          assetId: job.assetId,
          type: 'DOCUMENT_PURGED',
          actorId: 'cron-worker',
          correlationId: crypto.randomUUID(),
          payload: {
            purgedAt: now.toISOString()
          }
        });

        // Integrate external webhook and logs replication
        const eventPayload = { assetId: job.assetId, purgedAt: now.toISOString() };
        void WebhookService.emitWebhook('DOCUMENT_PURGED', job.tenantId, eventPayload);
        void IntegrationLogsService.replicateEvent(job.tenantId, job.assetId, 'DOCUMENT_PURGED', eventPayload);
      } catch (error: unknown) {
        const err = error as Error;
        job.attempts = job.attempts + 1;
        job.lastError = err.message || 'Unknown purging error';
        if (job.attempts >= 5) {
          job.status = 'failed';
        }
        await job.save();
      }
    }
  },

  /**
   * Updates metadata (title, tags, sensitivityLevel) of a document asset.
   */
  async updateMetadata(
    tenantId: string,
    assetId: string,
    metadata: { title?: string; tags?: string[]; sensitivityLevel?: 'low' | 'medium' | 'high' | 'restricted' }
  ): Promise<void> {
    const asset = await Document.findOne({ tenantId, assetId });
    if (!asset) {
      throw new Error('Document asset not found');
    }
    if (asset.status === 'purged') {
      throw new Error('Cannot update metadata of a purged document');
    }

    if (metadata.title) {
      asset.title = metadata.title;
    }
    if (metadata.tags) {
      asset.tags = metadata.tags;
    }
    if (metadata.sensitivityLevel) {
      asset.sensitivityLevel = metadata.sensitivityLevel;
    }

    asset.version = asset.version + 1;
    await asset.save();

    await DocumentEvent.create({
      eventId: crypto.randomUUID(),
      tenantId,
      assetId,
      type: 'DOCUMENT_METADATA_UPDATED',
      actorId: 'system',
      correlationId: crypto.randomUUID(),
      payload: {
        updatedFields: Object.keys(metadata)
      }
    });
  },

  /**
   * Applies an active legal hold on a document asset, blocking deletions.
   */
  async applyLegalHold(tenantId: string, assetId: string, reason: string, actorId: string): Promise<void> {
    await LegalHoldService.applyLegalHold(tenantId, assetId, reason, actorId);
  },

  /**
   * Releases a specific legal hold on a document asset.
   */
  async releaseLegalHold(tenantId: string, assetId: string, holdId: string, actorId: string): Promise<void> {
    await LegalHoldService.releaseLegalHold(tenantId, assetId, holdId, actorId);
  }
};
