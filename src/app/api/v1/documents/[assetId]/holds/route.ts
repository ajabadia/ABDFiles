/**
 * @purpose Gestiona holds legales para activos documentales mediante el manejo de solicitudes GET y POST para listar y aplicar holds.
 * @purpose_en Manages legal holds for document assets by handling GET and POST requests to list and apply holds.
 * @refactorable false
 * @classification Business Service
 * @complexity Medium
 * @fingerprint exports:3,imports:5,sig:ld6bdh
 * @lastUpdated 2026-06-23T23:03:17.720Z
 */

import { NextRequest, NextResponse } from 'next/server';
import { ensureIndustrialAccess } from '@ajabadia/satellite-sdk';
import { DocumentService } from '@/services/document-service';
import { assertAccess } from '@/lib/abac';
import LegalHold from '@/models/LegalHold';

export const revalidate = 0;

/**
 * GET /api/v1/documents/[assetId]/holds
 * Lists active and released holds for the document asset.
 */
export async function GET(
  request: NextRequest,
  { params }: { params: Promise<{ assetId: string }> }
) {
  try {
    const user = await ensureIndustrialAccess();
    const { assetId } = await params;
    await assertAccess({ userId: user.email || 'system', tenantId: user.tenantId, resource: 'document/' + assetId, action: 'view' });

    const holds = await LegalHold.find({ tenantId: user.tenantId, assetId }).sort({ createdAt: -1 });
    return NextResponse.json(holds);
  } catch (error: unknown) {
    console.error('[GET_HOLDS_ERROR]', error);
    const err = error as Error;
    return NextResponse.json({ error: err.message || 'Internal Server Error' }, { status: 500 });
  }
}

/**
 * POST /api/v1/documents/[assetId]/holds
 * Applies a new legal hold to the document asset.
 */
export async function POST(
  request: NextRequest,
  { params }: { params: Promise<{ assetId: string }> }
) {
  try {
    const user = await ensureIndustrialAccess();
    const { assetId } = await params;
    await assertAccess({ userId: user.email || 'system', tenantId: user.tenantId, resource: 'document/' + assetId, action: 'apply_hold' });
    const body = await request.json();
    const reason = body.reason || 'No reason provided';

    await DocumentService.applyLegalHold(
      user.tenantId,
      assetId,
      reason,
      user.email || 'system'
    );

    return NextResponse.json({ success: true });
  } catch (error: unknown) {
    console.error('[POST_HOLD_ERROR]', error);
    const err = error as Error;
    return NextResponse.json({ error: err.message || 'Internal Server Error' }, { status: 500 });
  }
}
