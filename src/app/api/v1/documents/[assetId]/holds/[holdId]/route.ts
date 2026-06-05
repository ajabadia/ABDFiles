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
