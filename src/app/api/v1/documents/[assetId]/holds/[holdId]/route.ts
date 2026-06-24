/**
 * @purpose Gestiona la eliminación de una suspensión legal para un activo documental específico.
 * @purpose_en Handles the deletion of a legal hold for a specific document asset.
 * @refactorable false
 * @classification Business Service
 * @complexity Low
 * @fingerprint exports:2,imports:4,sig:1f98sq9
 * @lastUpdated 2026-06-23T23:03:21.716Z
 */

import { NextRequest, NextResponse } from 'next/server';
import { ensureIndustrialAccess } from '@ajabadia/satellite-sdk';
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

    return NextResponse.json({ success: true });
  } catch (error: unknown) {
    console.error('[DELETE_HOLD_ERROR]', error);
    const err = error as Error;
    return NextResponse.json({ error: err.message || 'Internal Server Error' }, { status: 500 });
  }
}
