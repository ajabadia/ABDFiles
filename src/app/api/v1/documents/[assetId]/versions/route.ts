/**
 * @purpose Gestiona el recuperar y crear versiones de documentos para un activo específico.
 * @purpose_en Manages the retrieval and creation of document versions for a specific asset.
 * @refactorable true (contains too many state variables and UI parts)
 * @classification Business Service
 * @complexity Medium
 * @fingerprint exports:3,imports:6,sig:72vyli
 * @lastUpdated 2026-06-25T10:19:55.470Z
 */

import { NextRequest, NextResponse } from 'next/server';
import { ensureIndustrialAccess } from '@ajabadia/satellite-sdk/auth-middleware';
import { logger } from '@ajabadia/satellite-sdk/logger';
import { DocumentService } from '@/services/document-service';
import DocumentVersion from '@/models/DocumentVersion';
import { assertAccess } from '@/lib/abac';

export const revalidate = 0;

/**
 * GET /api/v1/documents/[assetId]/versions
 * List history of versions for an asset.
 */
export async function GET(
  request: NextRequest,
  { params }: { params: Promise<{ assetId: string }> }
) {
  try {
    const user = await ensureIndustrialAccess();
    const { assetId } = await params;
    await assertAccess({ userId: user.email || 'system', tenantId: user.tenantId, resource: 'document/' + assetId, action: 'view' });

    const versions = await DocumentVersion.find({
      tenantId: user.tenantId,
      assetId
    }).sort({ versionNumber: -1 });

    await logger.audit({
      tenantId: user.tenantId,
      action: 'VERSIONS_LIST',
      entityType: 'DOCUMENT',
      entityId: assetId,
      userId: user.email || 'system',
      userEmail: user.email || 'system@abd.com',
      changedFields: { count: versions.length },
    });

    return NextResponse.json(versions);
  } catch (error: unknown) {
    const err = error as Error;
    const { assetId } = await params;
    await logger.audit({
      tenantId: 'unknown',
      action: 'GET_VERSIONS_ERROR',
      entityType: 'DOCUMENT',
      entityId: assetId,
      userId: 'system',
      userEmail: 'system@abd.com',
      changedFields: { error: err.message, assetId },
    });
    console.error('[GET_VERSIONS_ERROR]', error);
    return NextResponse.json({ error: err.message || 'Internal Server Error' }, { status: 500 });
  }
}

/**
 * POST /api/v1/documents/[assetId]/versions
 * Create/Append a new version to an existing asset.
 */
export async function POST(
  request: NextRequest,
  { params }: { params: Promise<{ assetId: string }> }
) {
  try {
    const user = await ensureIndustrialAccess();
    const { assetId } = await params;
    await assertAccess({ userId: user.email || 'system', tenantId: user.tenantId, resource: 'document/' + assetId, action: 'upload' });

    const formData = await request.formData();
    const file = formData.get('file') as File | null;
    if (!file) {
      return NextResponse.json({ error: 'No file provided' }, { status: 400 });
    }

    const correlationId = formData.get('correlationId') as string || undefined;

    // Convert file to buffer
    const arrayBuffer = await file.arrayBuffer();
    const buffer = Buffer.from(arrayBuffer);

    const version = await DocumentService.createNewVersion({
      tenantId: user.tenantId,
      actorId: user.email || 'system',
      assetId,
      fileBuffer: buffer,
      mimeType: file.type || 'application/octet-stream',
      sizeBytes: file.size,
      correlationId
    });

    await logger.audit({
      tenantId: user.tenantId,
      action: 'DOCUMENT_VERSION_CREATED',
      entityType: 'DOCUMENT',
      entityId: assetId,
      userId: user.email || 'system',
      userEmail: user.email || 'system@abd.com',
      changedFields: { versionNumber: version?.versionNumber, sizeBytes: file.size, correlationId },
    });

    return NextResponse.json(version, { status: 201 });
  } catch (error: unknown) {
    const err = error as Error;
    const { assetId } = await params;
    await logger.audit({
      tenantId: 'unknown',
      action: 'POST_VERSION_ERROR',
      entityType: 'DOCUMENT',
      entityId: assetId,
      userId: 'system',
      userEmail: 'system@abd.com',
      changedFields: { error: err.message, assetId },
    });
    console.error('[POST_VERSION_ERROR]', error);
    return NextResponse.json({ error: err.message || 'Internal Server Error' }, { status: 500 });
  }
}
