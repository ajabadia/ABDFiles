/**
 * @purpose Gestiona holds legales para activos de documentos mediante el manejo de solicitudes GET y POST para listar y aplicar los holds.
 * @purpose_en Manages legal holds for document assets by handling GET and POST requests to list and apply holds.
 * @refactorable true (contains too many state variables and UI parts)
 * @classification Business Service
 * @complexity Medium
 * @fingerprint exports:3,imports:5,sig:133s1jy
 * @lastUpdated 2026-06-24T10:30:36.918Z
 */

import { NextRequest, NextResponse } from 'next/server';
import { ensureIndustrialAccess, logger } from '@ajabadia/satellite-sdk';
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
    await logger.audit({
      tenantId: user.tenantId,
      action: 'HOLDS_LIST',
      entityType: 'LEGAL_HOLD',
      entityId: assetId,
      userId: user.email || 'system',
      userEmail: user.email || 'system@abd.com',
      changedFields: { count: holds.length },
    });
    return NextResponse.json(holds);
  } catch (error: unknown) {
    const err = error as Error;
    const { assetId } = await params;
    await logger.audit({
      tenantId: 'unknown',
      action: 'GET_HOLDS_ERROR',
      entityType: 'LEGAL_HOLD',
      entityId: assetId,
      userId: 'system',
      userEmail: 'system@abd.com',
      changedFields: { error: err.message, assetId },
    });
    console.error('[GET_HOLDS_ERROR]', error);
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

    await logger.audit({
      tenantId: user.tenantId,
      action: 'HOLD_CREATED',
      entityType: 'LEGAL_HOLD',
      entityId: assetId,
      userId: user.email || 'system',
      userEmail: user.email || 'system@abd.com',
      changedFields: { reason },
    });

    return NextResponse.json({ success: true });
  } catch (error: unknown) {
    const err = error as Error;
    const { assetId } = await params;
    await logger.audit({
      tenantId: 'unknown',
      action: 'POST_HOLD_ERROR',
      entityType: 'LEGAL_HOLD',
      entityId: assetId,
      userId: 'system',
      userEmail: 'system@abd.com',
      changedFields: { error: err.message, assetId },
    });
    console.error('[POST_HOLD_ERROR]', error);
    return NextResponse.json({ error: err.message || 'Internal Server Error' }, { status: 500 });
  }
}
