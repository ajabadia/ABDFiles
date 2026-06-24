/**
 * @purpose Gestiona el recuperar y crear versiones de documentos para un activo específico.
 * @purpose_en Manages the retrieval and creation of document versions for a specific asset.
 * @refactorable false
 * @classification Business Service
 * @complexity Medium
 * @fingerprint exports:3,imports:5,sig:1ojvt4y
 * @lastUpdated 2026-06-23T23:03:34.801Z
 */

import { NextRequest, NextResponse } from 'next/server';
import { ensureIndustrialAccess } from '@ajabadia/satellite-sdk';
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

    return NextResponse.json(versions);
  } catch (error: unknown) {
    console.error('[GET_VERSIONS_ERROR]', error);
    const err = error as Error;
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

    return NextResponse.json(version, { status: 201 });
  } catch (error: unknown) {
    console.error('[POST_VERSION_ERROR]', error);
    const err = error as Error;
    return NextResponse.json({ error: err.message || 'Internal Server Error' }, { status: 500 });
  }
}
