/**
 * @purpose Gestiona solicitudes GET y POST para procesar documentos vencidos y realizar purgas físicas mediante una tarea cronica.
 * @purpose_en Manages GET and POST requests to process expired documents and perform physical purges using a cron job.
 * @refactorable false
 * @classification Business Service
 * @complexity Low
 * @fingerprint exports:3,imports:2,sig:1jhxpqt
 * @lastUpdated 2026-06-23T23:02:50.751Z
 */

import { NextRequest, NextResponse } from 'next/server';
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

    return NextResponse.json({ success: true, processedAt: new Date().toISOString() });
  } catch (error: unknown) {
    console.error('[CRON_DATA_LIFECYCLE_ERROR]', error);
    const err = error as Error;
    return NextResponse.json({ error: err.message || 'Internal Server Error' }, { status: 500 });
  }
}

export async function GET(request: NextRequest) {
  return handler(request);
}

export async function POST(request: NextRequest) {
  return handler(request);
}
