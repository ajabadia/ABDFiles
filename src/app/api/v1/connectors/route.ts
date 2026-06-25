/**
 * @purpose Gestiona conectores de almacenamiento para los inquilinos, manejando solicitudes GET y POST para listar y crear conectores.
 * @purpose_en Manages storage connectors for tenants by handling GET and POST requests to list and create connectors.
 * @refactorable true (contains too many state variables and UI parts)
 * @classification Business Service
 * @complexity Medium
 * @fingerprint exports:3,imports:6,sig:1e3tdyw
 * @lastUpdated 2026-06-25T10:18:54.272Z
 */

import { NextRequest, NextResponse } from 'next/server';
import { ensureIndustrialAccess } from '@ajabadia/satellite-sdk/auth-middleware';
import { logger } from '@ajabadia/satellite-sdk/logger';
import { ConnectorService } from '@/services/connector-service';
import { assertAccess } from '@/lib/abac';
import { getCachedResponse, saveResponse } from '@/lib/idempotency';

export const revalidate = 0;

/**
 * GET /api/v1/connectors
 * List all storage connectors for the tenant.
 */
export async function GET() {
  try {
    const user = await ensureIndustrialAccess();
    await assertAccess({
      userId: user.email || 'system',
      tenantId: user.tenantId,
      resource: 'connector',
      action: 'list'
    });

    const connectors = await ConnectorService.listConnectors(user.tenantId);
    await logger.audit({
      tenantId: user.tenantId,
      action: 'CONNECTORS_LIST',
      entityType: 'CONNECTOR',
      entityId: 'unknown',
      userId: user.email || 'system',
      userEmail: user.email || 'system@abd.com',
      changedFields: { count: Array.isArray(connectors) ? connectors.length : 0 },
    });
    return NextResponse.json(connectors);
  } catch (error: unknown) {
    const err = error as Error;
    await logger.audit({
      tenantId: 'unknown',
      action: 'GET_CONNECTORS_ERROR',
      entityType: 'CONNECTOR',
      entityId: 'unknown',
      userId: 'system',
      userEmail: 'system@abd.com',
      changedFields: { error: err.message },
    });
    console.error('[GET_CONNECTORS_ERROR]', error);
    return NextResponse.json({ error: err.message || 'Internal Server Error' }, { status: 500 });
  }
}

/**
 * POST /api/v1/connectors
 * Register a new storage connector.
 */
export async function POST(request: NextRequest) {
  try {
    const user = await ensureIndustrialAccess();
    await assertAccess({
      userId: user.email || 'system',
      tenantId: user.tenantId,
      resource: 'connector',
      action: 'create'
    });

    // Handle idempotency
    const idempotencyKey = request.headers.get('idempotency-key');
    const cachedResult = await getCachedResponse(user.tenantId, idempotencyKey);
    if (cachedResult.cached) {
      return cachedResult.response;
    }

    const body = await request.json();
    const { providerType, credentialsRef, allowedScopes, status, retentionPolicy, auditMode } = body;

    if (!providerType || !credentialsRef) {
      return NextResponse.json({ error: 'Missing required fields: providerType, credentialsRef' }, { status: 400 });
    }

    const connector = await ConnectorService.createConnector(user.tenantId, {
      providerType,
      credentialsRef,
      allowedScopes,
      status,
      retentionPolicy,
      auditMode
    });

    await saveResponse(user.tenantId, idempotencyKey, connector.toObject(), 201);
    await logger.audit({
      tenantId: user.tenantId,
      action: 'CONNECTOR_CREATED',
      entityType: 'CONNECTOR',
      entityId: connector?.connectorId || String(connector?._id || 'unknown'),
      userId: user.email || 'system',
      userEmail: user.email || 'system@abd.com',
      changedFields: { providerType, status },
    });
    return NextResponse.json(connector, { status: 201 });
  } catch (error: unknown) {
    const err = error as Error;
    await logger.audit({
      tenantId: 'unknown',
      action: 'POST_CONNECTOR_ERROR',
      entityType: 'CONNECTOR',
      entityId: 'unknown',
      userId: 'system',
      userEmail: 'system@abd.com',
      changedFields: { error: err.message },
    });
    console.error('[POST_CONNECTOR_ERROR]', error);
    return NextResponse.json({ error: err.message || 'Internal Server Error' }, { status: 500 });
  }
}
