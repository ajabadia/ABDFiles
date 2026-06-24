/**
 * @purpose Gestiona y libera retenciones legales sobre activos documentales, asegurando cumplimiento con políticas de retención.
 * @purpose_en Manages the application of and release of legal holds on document assets, ensuring compliance with retention policies.
 * @refactorable false
 * @classification Business Service
 * @complexity Low
 * @fingerprint exports:1,imports:5,sig:157gkta
 * @lastUpdated 2026-06-23T23:04:20.401Z
 */

import crypto from 'crypto';
import Document, { TDocument } from '@/models/Document';
import DocumentVersion from '@/models/DocumentVersion';
import DocumentEvent from '@/models/DocumentEvent';
import LegalHold from '@/models/LegalHold';

export const LegalHoldService = {
  /**
   * Applies an active legal hold on a document asset, blocking deletions.
   */
  async applyLegalHold(tenantId: string, assetId: string, reason: string, actorId: string): Promise<void> {
    const asset = await Document.findOne({ tenantId, assetId });
    if (!asset) {
      throw new Error('Document asset not found');
    }

    const holdId = crypto.randomUUID();
    await LegalHold.create({
      holdId,
      tenantId,
      assetId,
      reason,
      status: 'active',
      createdBy: actorId
    });

    asset.legalHold = true;
    asset.version = asset.version + 1;
    await asset.save();

    await DocumentEvent.create({
      eventId: crypto.randomUUID(),
      tenantId,
      assetId,
      type: 'LEGAL_HOLD_APPLIED',
      actorId,
      correlationId: crypto.randomUUID(),
      payload: { holdId, reason }
    });
  },

  /**
   * Releases a specific legal hold on a document asset.
   */
  async releaseLegalHold(tenantId: string, assetId: string, holdId: string, actorId: string): Promise<void> {
    const asset = await Document.findOne({ tenantId, assetId });
    if (!asset) {
      throw new Error('Document asset not found');
    }

    const hold = await LegalHold.findOne({ tenantId, assetId, holdId });
    if (!hold) {
      throw new Error('Legal hold not found');
    }

    hold.status = 'released';
    hold.releasedAt = new Date();
    await hold.save();

    // Check if there are other active holds remaining
    const activeHoldsCount = await LegalHold.countDocuments({
      tenantId,
      assetId,
      status: 'active'
    });

    if (activeHoldsCount === 0) {
      asset.legalHold = false;
      asset.version = asset.version + 1;
      await asset.save();
    }

    await DocumentEvent.create({
      eventId: crypto.randomUUID(),
      tenantId,
      assetId,
      type: 'LEGAL_HOLD_RELEASED',
      actorId,
      correlationId: crypto.randomUUID(),
      payload: { holdId }
    });
  }
};
