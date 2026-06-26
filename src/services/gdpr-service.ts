/**
 * @purpose Gestiona el exportación de datos del usuario en conformidad con las regulaciones de GDPR, incluyendo documentos, versiones, eventos, enlaces de espacio y retenciones legales.
 * @purpose_en Manages the export of user data in compliance with GDPR regulations, including documents, versions, events, space links, and legal holds.
 * @refactorable true (contains multiple responsibilities such as database queries, file generation, and storage service interactions)
 * @classification Business Service
 * @complexity Medium
 * @fingerprint exports:1,imports:3,sig:1hzqwsx
 * @lastUpdated 2026-06-26T06:17:19.578Z
 */

import JSZip from 'jszip';
import { connectDB, getTenantConnection } from '@ajabadia/satellite-sdk/db';
import { StorageService } from './storage-service';

export class GDPRService {
  static async exportUserData(tenantId: string, userId: string): Promise<Uint8Array> {
    await connectDB();
    const conn = getTenantConnection(tenantId, 'COLLECTION_PREFIX');

    const DocumentVersion = conn.models.DocumentVersion;
    const Document = conn.models.Document;
    const DocumentEvent = conn.models.DocumentEvent;
    const AssetSpaceLink = conn.models.AssetSpaceLink;
    const LegalHold = conn.models.LegalHold;

    const data: Record<string, unknown> = {
      tenantId,
      userId,
      exportedAt: new Date().toISOString(),
    };

    // 1. Find all versions owned by this user
    const userVersions = DocumentVersion
      ? await DocumentVersion.find({ createdBy: userId }).lean()
      : [];

    const assetIds = [...new Set(userVersions.map((v: Record<string, unknown>) => v.assetId as string))];

    // 2. Find user's documents
    let userDocuments: Record<string, unknown>[] = [];
    if (Document && assetIds.length > 0) {
      userDocuments = await Document.find({ assetId: { $in: assetIds }, tenantId }).lean();
    }

    // 3. Attach signed URLs to versions
    const versionsWithUrls = [];
    for (const v of userVersions) {
      const version = v as Record<string, unknown>;
      let signedUrl: string | undefined;
      try {
        signedUrl = await StorageService.getSignedUrl(
          version.storageRef as string,
          version.mimeType as string,
          tenantId
        );
      } catch {
        signedUrl = undefined;
      }
      versionsWithUrls.push({ ...version, signedUrl });
    }

    data.documents = userDocuments;
    data.versions = versionsWithUrls;

    // 4. Document events for user's assets
    if (DocumentEvent && assetIds.length > 0) {
      data.events = await DocumentEvent.find({ $or: [{ assetId: { $in: assetIds } }, { actorId: userId }] }).lean();
    }

    // 5. Asset-space links
    if (AssetSpaceLink && assetIds.length > 0) {
      data.spaceLinks = await AssetSpaceLink.find({ assetId: { $in: assetIds } }).lean();
    }

    // 6. Legal holds
    if (LegalHold && assetIds.length > 0) {
      data.legalHolds = await LegalHold.find({ assetId: { $in: assetIds } }).lean();
    }

    const zip = new JSZip();
    zip.file('user_data.json', JSON.stringify(data, null, 2));
    zip.file('README.txt', [
      'ABDFiles GDPR Data Export',
      '==============================================',
      `Tenant ID: ${tenantId}`,
      `User ID: ${userId}`,
      `Export Date: ${new Date().toISOString()}`,
      '',
      'Contents:',
      '- user_data.json: documents metadata, versions (with signed download URLs), events, space links, legal holds.',
      '',
      'Signed URLs in the versions array are temporary and will expire.',
      'This file contains your personal data as stored in the ABDFiles system.',
    ].join('\n'));

    return await zip.generateAsync({ type: 'uint8array' });
  }
}
