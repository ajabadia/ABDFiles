/**
 * @purpose Gestiona solicitudes GET para listar eventos de auditoria de un activo de documento, asegurando el acceso industrial y la autorización.
 * @purpose_en Manages GET requests to list audit events for a document asset, ensuring industrial access and authorization.
 * @refactorable false
 * @classification Business Service
 * @complexity Medium
 * @fingerprint exports:2,imports:5,sig:203oki
 * @lastUpdated 2026-06-25T10:19:22.288Z
 */

import { NextRequest, NextResponse } from 'next/server';
import { ensureIndustrialAccess } from '@ajabadia/satellite-sdk/auth-middleware';
import { logger } from '@ajabadia/satellite-sdk/logger';
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
    await logger.audit({
      tenantId: user.tenantId,
      action: 'EVENTS_LIST',
      entityType: 'DOCUMENT',
      entityId: assetId,
      userId: user.email || 'system',
      userEmail: user.email || 'system@abd.com',
      changedFields: { count: events.length },
    });
    return NextResponse.json(events);
  } catch (error: unknown) {
    const err = error as Error;
    const { assetId } = await params;
    await logger.audit({
      tenantId: 'unknown',
      action: 'GET_EVENTS_ERROR',
      entityType: 'DOCUMENT',
      entityId: assetId,
      userId: 'system',
      userEmail: 'system@abd.com',
      changedFields: { error: err.message, assetId },
    });
    console.error('[GET_EVENTS_ERROR]', error);
    return NextResponse.json({ error: err.message || 'Internal Server Error' }, { status: 500 });
  }
}
