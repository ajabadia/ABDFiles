import { NextRequest, NextResponse } from 'next/server';
import { ensureIndustrialAccess } from '@ajabadia/satellite-sdk';
import DocumentEvent from '@/models/DocumentEvent';
import { assertAccess } from '@/lib/abac';

export const revalidate = 0;

/**
 * GET /api/v1/documents/[assetId]/events
 * Lists all audit events for the document asset.
 */
export async function GET(
  request: NextRequest,
  { params }: { params: Promise<{ assetId: string }> }
) {
  try {
    const user = await ensureIndustrialAccess();
    const { assetId } = await params;
    await assertAccess({ userId: user.email || 'system', tenantId: user.tenantId, resource: 'document/' + assetId, action: 'audit' });

    const events = await DocumentEvent.find({ tenantId: user.tenantId, assetId }).sort({ createdAt: -1 });
    return NextResponse.json(events);
  } catch (error: unknown) {
    console.error('[GET_EVENTS_ERROR]', error);
    const err = error as Error;
    return NextResponse.json({ error: err.message || 'Internal Server Error' }, { status: 500 });
  }
}
