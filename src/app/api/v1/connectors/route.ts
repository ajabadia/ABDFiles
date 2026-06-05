import { NextRequest, NextResponse } from 'next/server';
import { ensureIndustrialAccess } from '@ajabadia/satellite-sdk';
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
    return NextResponse.json(connectors);
  } catch (error: unknown) {
    console.error('[GET_CONNECTORS_ERROR]', error);
    const err = error as Error;
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
    return NextResponse.json(connector, { status: 201 });
  } catch (error: unknown) {
    console.error('[POST_CONNECTOR_ERROR]', error);
    const err = error as Error;
    return NextResponse.json({ error: err.message || 'Internal Server Error' }, { status: 500 });
  }
}
