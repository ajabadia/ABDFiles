import { NextRequest, NextResponse } from 'next/server';
import { ensureIndustrialAccess } from '@ajabadia/satellite-sdk';
import { ConnectorService } from '@/services/connector-service';
import { assertAccess } from '@/lib/abac';

/**
 * GET /api/v1/connectors/[connectorId]
 * Retrieve details for a specific storage connector.
 */
export async function GET(
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
      action: 'view'
    });

    const connector = await ConnectorService.getConnector(user.tenantId, connectorId);
    if (!connector) {
      return NextResponse.json({ error: 'Storage connector not found' }, { status: 404 });
    }

    return NextResponse.json(connector);
  } catch (error: unknown) {
    console.error('[GET_CONNECTOR_BY_ID_ERROR]', error);
    const err = error as Error;
    return NextResponse.json({ error: err.message || 'Internal Server Error' }, { status: 500 });
  }
}

/**
 * PATCH /api/v1/connectors/[connectorId]
 * Update parameters of an existing storage connector.
 */
export async function PATCH(
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

    const body = await request.json();
    const { status, credentialsRef, allowedScopes, retentionPolicy, auditMode } = body;

    const connector = await ConnectorService.updateConnector(user.tenantId, connectorId, {
      status,
      credentialsRef,
      allowedScopes,
      retentionPolicy,
      auditMode
    });

    return NextResponse.json(connector);
  } catch (error: unknown) {
    console.error('[PATCH_CONNECTOR_ERROR]', error);
    const err = error as Error;
    return NextResponse.json({ error: err.message || 'Internal Server Error' }, { status: 500 });
  }
}

/**
 * DELETE /api/v1/connectors/[connectorId]
 * Delete/deregister a storage connector.
 */
export async function DELETE(
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
      action: 'delete'
    });

    await ConnectorService.deleteConnector(user.tenantId, connectorId);
    return NextResponse.json({ success: true, message: 'Connector deleted successfully' });
  } catch (error: unknown) {
    console.error('[DELETE_CONNECTOR_ERROR]', error);
    const err = error as Error;
    return NextResponse.json({ error: err.message || 'Internal Server Error' }, { status: 500 });
  }
}
