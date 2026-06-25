/**
 * @purpose Gestiona solicitudes GET y POST para procesar documentos vencidos y realizar purgas físicas mediante una tarea cronológica.
 * @purpose_en Manages GET and POST requests to process expired documents and perform physical purges using a cron job.
 * @refactorable false
 * @classification Business Service
 * @complexity Low
 * @fingerprint exports:3,imports:3,sig:irc7i4
 * @lastUpdated 2026-06-25T10:18:44.121Z
 */

import { NextRequest, NextResponse } from 'next/server';
import { logger } from '@ajabadia/satellite-sdk/logger';
import { DocumentService } from '@/services/document-service';

export const revalidate = 0;

/**
 * GET/POST /api/cron/data-lifecycle
 * Cron endpoint to process expired documents and physical purgings.
 */
async function handler(request: NextRequest) {
  try {
    // 🛡️ Validate Cron Secret
    const authHeader = request.headers.get('Authorization');
    const { searchParams } = new URL(request.url);
    const queryToken = searchParams.get('token');
    
    const token = queryToken || (authHeader?.startsWith('Bearer ') ? authHeader.substring(7) : null);
    const expectedSecret = process.env.CRON_SECRET;

    if (expectedSecret && token !== expectedSecret) {
      return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });
    }

    await DocumentService.purgeExpiredDocuments(new Date());

    await logger.audit({
      tenantId: 'system',
      action: 'CRON_DATA_LIFECYCLE_PURGE',
      entityType: 'DOCUMENT',
      entityId: 'unknown',
      userId: 'system',
      userEmail: 'system@abd.com',
      changedFields: { processedAt: new Date().toISOString() },
    });

    return NextResponse.json({ success: true, processedAt: new Date().toISOString() });
  } catch (error: unknown) {
    const err = error as Error;
    await logger.audit({
      tenantId: 'system',
      action: 'CRON_DATA_LIFECYCLE_ERROR',
      entityType: 'DOCUMENT',
      entityId: 'unknown',
      userId: 'system',
      userEmail: 'system@abd.com',
      changedFields: { error: err.message },
    });
    console.error('[CRON_DATA_LIFECYCLE_ERROR]', error);
    return NextResponse.json({ error: err.message || 'Internal Server Error' }, { status: 500 });
  }
}

export async function GET(request: NextRequest) {
  return handler(request);
}

export async function POST(request: NextRequest) {
  return handler(request);
}
