/**
 * @purpose Gestiona solicitudes de conversión de documentos, asegurando acceso industrial y validando salidas, y procesando opciones de conversión.
 * @purpose_en Handles document conversion requests, ensuring industrial access, validating outputs, and processing conversion options.
 * @refactorable true (contains business logic and async operations)
 * @classification Business Service
 * @complexity Medium
 * @fingerprint exports:5,imports:7,sig:b1ko7v
 * @lastUpdated 2026-06-26T16:35:18.554Z
 */

import { NextRequest, NextResponse } from 'next/server';
import { ensureIndustrialAccess } from '@ajabadia/satellite-sdk/auth-middleware';
import { logger } from '@ajabadia/satellite-sdk/logger';
import { DocumentService } from '@/services/document-service';
import DocumentVersion from '@/models/DocumentVersion';
import { convertDocument, getPandocInfo, type ConvertOptions, type PandocFormat } from '@/services/pandoc-service';
import { assertAccess } from '@/lib/abac';

export const revalidate = 0;
export const maxDuration = 30;

const VALID_OUTPUTS = [
  'html', 'html5', 'markdown', 'md', 'plain', 'textile',
  'rst', 'org', 'asciidoc', 'asciidoctor',
  'mediawiki', 'dokuwiki', 'gfm', 'commonmark',
  'csv', 'tsv', 'json', 'yaml',
  'docx', 'epub', 'epub2', 'epub3',
  'latex', 'opml', 'man', 'fb2',
  'pptx', 'odt',
];

export async function OPTIONS() {
  return NextResponse.json({ methods: ['POST', 'OPTIONS'] });
}

export async function GET(request: NextRequest) {
  try {
    const info = await getPandocInfo();
    return NextResponse.json({
      version: info.version,
      inputFormats: info.inputFormats,
      outputFormats: info.outputFormats,
    });
  } catch (error: unknown) {
    const err = error as Error;
    return NextResponse.json({ error: err.message }, { status: 500 });
  }
}

export async function POST(
  request: NextRequest,
  { params }: { params: Promise<{ assetId: string }> }
) {
  try {
    const user = await ensureIndustrialAccess();
    const { assetId } = await params;
    await assertAccess({
      userId: user.email || 'system',
      tenantId: user.tenantId,
      resource: 'document/' + assetId,
      action: 'view',
    });

    const body = await request.json();
    const outputFormat = (body.to || '').toLowerCase() as PandocFormat;
    const inputFormat = (body.from || '').toLowerCase() as PandocFormat | undefined;

    if (!outputFormat || !VALID_OUTPUTS.includes(outputFormat)) {
      return NextResponse.json({
        error: 'Invalid or missing output format',
        validFormats: VALID_OUTPUTS,
      }, { status: 400 });
    }

    const doc = await DocumentService.getDocument(user.tenantId, assetId);

    const currentVersion = await DocumentVersion.findOne({
      tenantId: user.tenantId,
      assetId,
      versionId: doc.currentVersionId,
    });

    if (!currentVersion) {
      return NextResponse.json({ error: 'Current version not found' }, { status: 404 });
    }

    const mimeType = currentVersion.mimeType || 'application/octet-stream';
    const signedUrl = doc.signedUrl;

    const fileResponse = await fetch(signedUrl, { signal: AbortSignal.timeout(15000) });
    if (!fileResponse.ok) {
      return NextResponse.json({ error: 'Failed to fetch document content' }, { status: 502 });
    }

    const contentType = fileResponse.headers.get('content-type') || mimeType;
    const arrayBuffer = await fileResponse.arrayBuffer();
    const fileBuffer = Buffer.from(arrayBuffer);

    const convertOptions: ConvertOptions = {
      from: inputFormat || (mimeTypeToSimple(mimeType) as PandocFormat),
      to: outputFormat,
      standalone: body.standalone !== false,
      embedResources: body.embedResources === true,
      toc: body.toc === true,
      tocDepth: body.tocDepth,
      wrap: body.wrap || undefined,
    };

    const result = await convertDocument(fileBuffer, contentType, convertOptions);

    await logger.audit({
      tenantId: user.tenantId,
      action: 'DOCUMENT_CONVERT',
      entityType: 'DOCUMENT',
      entityId: assetId,
      userId: user.email || 'system',
      userEmail: user.email || 'system@abd.com',
      changedFields: {
        fromFormat: convertOptions.from,
        toFormat: outputFormat,
        sizeBytes: result.output.length,
      },
    });

    return NextResponse.json({
      assetId,
      from: convertOptions.from,
      to: outputFormat,
      output: result.output,
      stderr: result.stderr,
      warnings: result.warnings,
    });
  } catch (error: unknown) {
    const err = error as Error;
    return NextResponse.json({ error: err.message }, { status: 500 });
  }
}

const SIMPLE_MIME_MAP: Record<string, string> = {
  'text/markdown': 'markdown',
  'text/html': 'html',
  'text/plain': 'plain',
  'text/csv': 'csv',
  'application/json': 'json',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document': 'docx',
  'application/epub+zip': 'epub',
  'application/vnd.oasis.opendocument.text': 'odt',
  'application/x-latex': 'latex',
};

function mimeTypeToSimple(mime: string): string {
  const base = mime.split(';')[0].trim();
  return SIMPLE_MIME_MAP[base] || 'markdown';
}
