/**
 * @purpose Gestiona GET y DELETE solicitudes para activos de documentos, asegurando el control de acceso y recuperando o eliminando metadata de documento.
 * @purpose_en Manages GET and DELETE requests for document assets, ensuring access control and retrieving or deleting document metadata.
 * @refactorable false
 * @classification Business Service
 * @complexity Medium
 * @fingerprint exports:3,imports:4,sig:1gsom3a
 * @lastUpdated 2026-06-24T10:30:52.044Z
 */

import { NextRequest, NextResponse } from 'next/server';
import { ensureIndustrialAccess, logger } from '@ajabadia/satellite-sdk';
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
    await logger.audit({
      tenantId: user.tenantId,
      action: 'DOCUMENT_VIEW',
      entityType: 'DOCUMENT',
      entityId: assetId,
      userId: user.email || 'system',
      userEmail: user.email || 'system@abd.com',
      changedFields: {},
    });
    return NextResponse.json(document);
  } catch (error: unknown) {
    const err = error as Error;
    const { assetId } = await params;
    await logger.audit({
      tenantId: 'unknown',
      action: 'GET_DOCUMENT_DETAIL_ERROR',
      entityType: 'DOCUMENT',
      entityId: assetId,
      userId: 'system',
      userEmail: 'system@abd.com',
      changedFields: { error: err.message, assetId },
    });
    console.error('[GET_DOCUMENT_DETAIL_ERROR]', error);
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

    await logger.audit({
      tenantId: user.tenantId,
      action: 'DOCUMENT_DELETE',
      entityType: 'DOCUMENT',
      entityId: assetId,
      userId: user.email || 'system',
      userEmail: user.email || 'system@abd.com',
      changedFields: {},
    });

    return NextResponse.json({ success: true });
  } catch (error: unknown) {
    const err = error as Error;
    const { assetId } = await params;
    await logger.audit({
      tenantId: 'unknown',
      action: 'DELETE_DOCUMENT_ERROR',
      entityType: 'DOCUMENT',
      entityId: assetId,
      userId: 'system',
      userEmail: 'system@abd.com',
      changedFields: { error: err.message, assetId },
    });
    console.error('[DELETE_DOCUMENT_ERROR]', error);
    return NextResponse.json({ error: err.message || 'Internal Server Error' }, { status: 500 });
  }
}
