/**
 * @purpose Gestiona la solicitud PATCH para actualizar metadata de un activo de documento.
 * @purpose_en Handles the PATCH request to update metadata for a document asset.
 * @refactorable false
 * @classification Business Service
 * @complexity Medium
 * @fingerprint exports:2,imports:5,sig:8u8h83
 * @lastUpdated 2026-06-25T10:19:41.518Z
 */

import { NextRequest, NextResponse } from 'next/server';
import { ensureIndustrialAccess } from '@ajabadia/satellite-sdk/auth-middleware';
import { logger } from '@ajabadia/satellite-sdk/logger';
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

    await logger.audit({
      tenantId: user.tenantId,
      action: 'METADATA_UPDATED',
      entityType: 'DOCUMENT',
      entityId: assetId,
      userId: user.email || 'system',
      userEmail: user.email || 'system@abd.com',
      changedFields: { title: title || null, tags: tags || null, sensitivityLevel: sensitivityLevel || null },
    });

    return NextResponse.json({ success: true });
  } catch (error: unknown) {
    const err = error as Error;
    const { assetId } = await params;
    await logger.audit({
      tenantId: 'unknown',
      action: 'PATCH_METADATA_ERROR',
      entityType: 'DOCUMENT',
      entityId: assetId,
      userId: 'system',
      userEmail: 'system@abd.com',
      changedFields: { error: err.message, assetId },
    });
    console.error('[PATCH_METADATA_ERROR]', error);
    return NextResponse.json({ error: err.message || 'Internal Server Error' }, { status: 500 });
  }
}
