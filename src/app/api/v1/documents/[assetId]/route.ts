import { NextRequest, NextResponse } from 'next/server';
import { ensureIndustrialAccess } from '@ajabadia/satellite-sdk';
import { DocumentService } from '@/services/document-service';
import { assertAccess } from '@/lib/abac';

export const revalidate = 0;

/**
 * GET /api/v1/documents/[assetId]
 * Get specific document metadata and signed link.
 */
export async function GET(
  request: NextRequest,
  { params }: { params: Promise<{ assetId: string }> }
) {
  try {
    const user = await ensureIndustrialAccess();
    const { assetId } = await params;
    await assertAccess({ userId: user.email || 'system', tenantId: user.tenantId, resource: 'document/' + assetId, action: 'view' });

    const document = await DocumentService.getDocument(user.tenantId, assetId);
    return NextResponse.json(document);
  } catch (error: unknown) {
    console.error('[GET_DOCUMENT_DETAIL_ERROR]', error);
    const err = error as Error;
    return NextResponse.json({ error: err.message || 'Internal Server Error' }, { status: 500 });
  }
}

/**
 * DELETE /api/v1/documents/[assetId]
 * Logical delete document asset and schedule physical purge.
 */
export async function DELETE(
  request: NextRequest,
  { params }: { params: Promise<{ assetId: string }> }
) {
  try {
    const user = await ensureIndustrialAccess();
    const { assetId } = await params;
    await assertAccess({ userId: user.email || 'system', tenantId: user.tenantId, resource: 'document/' + assetId, action: 'delete' });

    await DocumentService.logicalDeleteDocument({
      tenantId: user.tenantId,
      assetId,
      actorId: user.email || 'system'
    });

    return NextResponse.json({ success: true });
  } catch (error: unknown) {
    console.error('[DELETE_DOCUMENT_ERROR]', error);
    const err = error as Error;
    return NextResponse.json({ error: err.message || 'Internal Server Error' }, { status: 500 });
  }
}
