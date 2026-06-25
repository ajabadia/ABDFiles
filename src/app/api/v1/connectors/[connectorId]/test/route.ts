/**
 * @purpose Gestiona el solicitud POST para ejecutar una prueba física en vivo contra un proveedor de conectores.
 * @purpose_en Handles the POST request to execute a live physical test against a connector provider.
 * @refactorable false
 * @classification Business Service
 * @complexity Low
 * @fingerprint exports:1,imports:5,sig:13ghhfv
 * @lastUpdated 2026-06-25T10:19:08.164Z
 */

import { NextRequest, NextResponse } from 'next/server';
import { ensureIndustrialAccess } from '@ajabadia/satellite-sdk/auth-middleware';
import { logger } from '@ajabadia/satellite-sdk/logger';
import { ConnectorService } from '@/services/connector-service';
import { assertAccess } from '@/lib/abac';

/**
 * POST /api/v1/connectors/[connectorId]/test
 * Execute a live physical test against the connector provider.
 */
export async function POST(
  request: NextRequest,
  { params }: { params: Promise<{ connectorId: string }> }
) {
  try {
    const user = await ensureIndustrialAccess();
    const { connectorId } = await params;

    await assertAccess({
      userId: user.email || 'system',
      tenantId: user.tenantId,
      resource: 'connector',
      action: 'update'
    });

    const result = await ConnectorService.testConnection(user.tenantId, connectorId);
    await logger.audit({
      tenantId: user.tenantId,
      action: 'CONNECTOR_TEST_SUCCESS',
      entityType: 'CONNECTOR',
      entityId: connectorId,
      userId: user.email || 'system',
      userEmail: user.email || 'system@abd.com',
      changedFields: { success: result?.success },
    });
    return NextResponse.json(result);
  } catch (error: unknown) {
    const err = error as Error;
    const { connectorId } = await params;
    await logger.audit({
      tenantId: 'unknown',
      action: 'TEST_CONNECTOR_ERROR',
      entityType: 'CONNECTOR',
      entityId: connectorId,
      userId: 'system',
      userEmail: 'system@abd.com',
      changedFields: { error: err.message, connectorId },
    });
    console.error('[TEST_CONNECTOR_ERROR]', error);
    return NextResponse.json({ error: err.message || 'Internal Server Error' }, { status: 500 });
  }
}
