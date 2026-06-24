/**
 * @purpose Gestiona el enlace y desconexión de activos documentales a espacios lógicos, incluyendo la creación de eventos de auditoría para estas acciones.
 * @purpose_en Manages the linking and unlinking of document assets to logical spaces, including creating audit events for these actions.
 * @refactorable false
 * @classification Business Service
 * @complexity Low
 * @fingerprint exports:1,imports:5,sig:1q5v2gf
 * @lastUpdated 2026-06-23T23:04:26.442Z
 */

import crypto from 'crypto';
import Document, { TDocument } from '@/models/Document';
import DocumentVersion from '@/models/DocumentVersion';
import DocumentEvent from '@/models/DocumentEvent';
import AssetSpaceLink, { TAssetSpaceLink } from '@/models/AssetSpaceLink';

export const SpaceLinkService = {
  /**
   * Links a document asset to a space logically.
   */
  async linkAssetToSpace(
    tenantId: string,
    assetId: string,
    spaceId: string,
    spacePath: string,
    isPrimary: boolean,
    createdBy?: string
  ): Promise<TAssetSpaceLink> {
    const linkId = crypto.randomUUID();

    const existing = await AssetSpaceLink.findOne({ tenantId, assetId, spaceId });
    if (existing) {
      return existing;
    }

    const newLink = await AssetSpaceLink.create({
      linkId,
      tenantId,
      assetId,
      spaceId,
      spacePath,
      isPrimary,
      createdBy
    });

    // Write audit event
    await DocumentEvent.create({
      eventId: crypto.randomUUID(),
      tenantId,
      assetId,
      type: 'ASSET_SPACE_LINKED',
      actorId: createdBy || 'system',
      correlationId: crypto.randomUUID(),
      payload: { spaceId, spacePath, isPrimary }
    });

    return newLink;
  },

  /**
   * Unlinks an asset from a logical space.
   */
  async unlinkAssetFromSpace(tenantId: string, assetId: string, spaceId: string, actorId = 'system'): Promise<void> {
    await AssetSpaceLink.deleteOne({ tenantId, assetId, spaceId });

    await DocumentEvent.create({
      eventId: crypto.randomUUID(),
      tenantId,
      assetId,
      type: 'ASSET_SPACE_UNLINKED',
      actorId,
      correlationId: crypto.randomUUID(),
      payload: { spaceId }
    });
  },

  /**
   * Retrieves all spaces linked to a given asset.
   */
  async getAssetSpaceLinks(tenantId: string, assetId: string): Promise<TAssetSpaceLink[]> {
    return AssetSpaceLink.find({ tenantId, assetId });
  }
};
