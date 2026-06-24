/**
 * @purpose Gestiona operaciones de documentos, como listar y subir documentos para el inquilino actual.
 * @purpose_en Manages document operations such as listing and uploading documents for the current tenant.
 * @refactorable true (contains business logic and data handling)
 * @classification Business Service
 * @complexity Medium
 * @fingerprint exports:3,imports:5,sig:cesab4
 * @lastUpdated 2026-06-23T23:03:07.791Z
 */

import { NextRequest, NextResponse } from 'next/server';
import { ensureIndustrialAccess } from '@ajabadia/satellite-sdk';
import { DocumentService } from '@/services/document-service';
import { getCachedResponse, saveResponse } from '@/lib/idempotency';
import { assertAccess } from '@/lib/abac';

export const revalidate = 0;

/**
 * GET /api/v1/documents
 * List documents for the current tenant.
 */
export async function GET(request: NextRequest) {
  try {
    const user = await ensureIndustrialAccess();
    await assertAccess({ userId: user.email || 'system', tenantId: user.tenantId, resource: 'document', action: 'list' });
    
    const { searchParams } = new URL(request.url);
    const status = searchParams.get('status') || undefined;
    const limit = parseInt(searchParams.get('limit') || '50', 10);
    const page = parseInt(searchParams.get('page') || '1', 10);

    const documents = await DocumentService.listDocuments(user.tenantId, {
      status,
      limit,
      page
    });

    return NextResponse.json(documents);
  } catch (error: unknown) {
    console.error('[GET_DOCUMENTS_ERROR]', error);
    const err = error as Error;
    return NextResponse.json({ error: err.message || 'Internal Server Error' }, { status: 500 });
  }
}

/**
 * POST /api/v1/documents
 * Create/Upload a new document asset with deduplication.
 */
export async function POST(request: NextRequest) {
  try {
    const user = await ensureIndustrialAccess();
    await assertAccess({ userId: user.email || 'system', tenantId: user.tenantId, resource: 'document', action: 'upload' });
    
    // Parse and handle request idempotency
    const idempotencyKey = request.headers.get('idempotency-key');
    const cachedResult = await getCachedResponse(user.tenantId, idempotencyKey);
    if (cachedResult.cached) {
      return cachedResult.response;
    }

    const formData = await request.formData();

    const file = formData.get('file') as File | null;
    if (!file) {
      return NextResponse.json({ error: 'No file provided' }, { status: 400 });
    }

    const title = formData.get('title') as string || file.name;
    const retentionClass = formData.get('retentionClass') as string || 'default';
    const rawSensitivity = formData.get('sensitivityLevel') as string || 'low';
    const sensitivityLevel = (rawSensitivity === 'low' || rawSensitivity === 'medium' || rawSensitivity === 'high' || rawSensitivity === 'restricted')
      ? rawSensitivity
      : 'low';

    const correlationId = formData.get('correlationId') as string || undefined;

    const spaceIdsStr = formData.get('spaceIds') as string || '';
    const spaceIds = spaceIdsStr ? spaceIdsStr.split(',') : [];

    const spacePathsStr = formData.get('spacePaths') as string || '';
    const spacePaths = spacePathsStr ? spacePathsStr.split(',') : [];

    const tagsStr = formData.get('tags') as string || '';
    const tags = tagsStr ? tagsStr.split(',') : [];

    // Convert file to buffer
    const arrayBuffer = await file.arrayBuffer();
    const buffer = Buffer.from(arrayBuffer);

    const result = await DocumentService.uploadDocument({
      tenantId: user.tenantId,
      actorId: user.email || 'system',
      title,
      fileBuffer: buffer,
      mimeType: file.type || 'application/octet-stream',
      sizeBytes: file.size,
      retentionClass,
      sensitivityLevel,
      spaceIds,
      spacePaths,
      tags,
      correlationId
    });

    // Save payload response cache for idempotency keys
    await saveResponse(user.tenantId, idempotencyKey, result, 201);

    return NextResponse.json(result, { status: 201 });
  } catch (error: unknown) {
    console.error('[POST_DOCUMENT_ERROR]', error);
    const err = error as Error;
    return NextResponse.json({ error: err.message || 'Internal Server Error' }, { status: 500 });
  }
}
