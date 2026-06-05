import { NextRequest, NextResponse } from 'next/server';
import { ensureIndustrialAccess } from '@ajabadia/satellite-sdk';
import { DocumentService } from '@/services/document-service';
import { assertAccess } from '@/lib/abac';

export const revalidate = 0;

/**
 * PATCH /api/v1/documents/[assetId]/metadata
 * Updates metadata of a document asset.
 */
export async function PATCH(
  request: NextRequest,
  { params }: { params: Promise<{ assetId: string }> }
) {
  try {
    const user = await ensureIndustrialAccess();
    const { assetId } = await params;
    await assertAccess({ userId: user.email || 'system', tenantId: user.tenantId, resource: 'document/' + assetId, action: 'update_metadata' });
    const body = await request.json();

    const title = body.title || undefined;
    const tags = body.tags || undefined;
    const sensitivityLevel = body.sensitivityLevel || undefined;

    await DocumentService.updateMetadata(user.tenantId, assetId, {
      title,
      tags,
      sensitivityLevel
    });

    return NextResponse.json({ success: true });
  } catch (error: unknown) {
    console.error('[PATCH_METADATA_ERROR]', error);
    const err = error as Error;
    return NextResponse.json({ error: err.message || 'Internal Server Error' }, { status: 500 });
  }
}
