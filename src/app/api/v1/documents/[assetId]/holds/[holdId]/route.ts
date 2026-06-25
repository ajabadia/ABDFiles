/**
 * @purpose Gestiona la eliminación de una suspensión legal para un activo documental específico.
 * @purpose_en Handles the deletion of a legal hold for a specific document asset.
 * @refactorable false
 * @classification Business Service
 * @complexity Low
 * @fingerprint exports:2,imports:5,sig:tzgkax
 * @lastUpdated 2026-06-25T10:19:35.335Z
 */

import { NextRequest, NextResponse } from 'next/server';
import { ensureIndustrialAccess } from '@ajabadia/satellite-sdk/auth-middleware';
import { logger } from '@ajabadia/satellite-sdk/logger';
import { DocumentService } from '@/services/document-service';
import { assertAccess } from '@/lib/abac';

export const revalidate = 0;

/**
 * DELETE /api/v1/documents/[assetId]/holds/[holdId]
 * Releases a specific legal hold.
 */
export async function DELETE(
  request: NextRequest,
  { params }: { params: Promise<{ assetId: string; holdId: string }> }
) {
  try {
    const user = await ensureIndustrialAccess();
    const { assetId, holdId } = await params;
    await assertAccess({ userId: user.email || 'system', tenantId: user.tenantId, resource: 'document/' + assetId, action: 'release_hold' });

    await DocumentService.releaseLegalHold(
      user.tenantId,
      assetId,
      holdId,
      user.email || 'system'
    );

    await logger.audit({
      tenantId: user.tenantId,
      action: 'HOLD_RELEASED',
      entityType: 'LEGAL_HOLD',
      entityId: holdId,
      userId: user.email || 'system',
      userEmail: user.email || 'system@abd.com',
      changedFields: { assetId },
    });

    return NextResponse.json({ success: true });
  } catch (error: unknown) {
    const err = error as Error;
    const { assetId, holdId } = await params;
    await logger.audit({
      tenantId: 'unknown',
      action: 'DELETE_HOLD_ERROR',
      entityType: 'LEGAL_HOLD',
      entityId: holdId,
      userId: 'system',
      userEmail: 'system@abd.com',
      changedFields: { error: err.message, assetId, holdId },
    });
    console.error('[DELETE_HOLD_ERROR]', error);
    return NextResponse.json({ error: err.message || 'Internal Server Error' }, { status: 500 });
  }
}
