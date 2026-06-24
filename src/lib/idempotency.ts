/**
 * @purpose Valida y almacena respuestas utilizando el encabezado de clave Idempotency.
 * @purpose_en Validates and caches responses using the Idempotency-Key header.
 * @refactorable false
 * @classification Business Service
 * @complexity Low
 * @fingerprint exports:2,imports:2,sig:1cn2vpc
 * @lastUpdated 2026-06-23T23:03:55.842Z
 */

import { NextResponse } from 'next/server';
import IdempotencyKey from '@/models/IdempotencyKey';

/**
 * Validates request idempotency using the Idempotency-Key header.
 */
export async function getCachedResponse(
  tenantId: string,
  key: string | null
): Promise<{ cached: true; response: NextResponse } | { cached: false }> {
  if (!key) {
    return { cached: false };
  }

  const record = await IdempotencyKey.findOne({ tenantId, key });
  if (record) {
    return {
      cached: true,
      response: NextResponse.json(record.responseBody, { status: record.statusCode })
    };
  }

  return { cached: false };
}

/**
 * Saves a response payload for a given Idempotency-Key.
 */
export async function saveResponse(
  tenantId: string,
  key: string | null,
  responseBody: Record<string, unknown>,
  statusCode: number
): Promise<void> {
  if (!key) return;
  try {
    await IdempotencyKey.create({
      key,
      tenantId,
      responseBody,
      statusCode
    });
  } catch (err) {
    console.error('[Idempotency] Failed to cache response:', err);
  }
}
