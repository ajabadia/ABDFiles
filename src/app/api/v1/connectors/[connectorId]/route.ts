/**
 * @purpose Gestiona operaciones CRUD para conectores de almacenamiento.
 * @purpose_en Manages CRUD operations for storage connectors.
 * @refactorable true (contains multiple HTTP methods and business logic)
 * @classification Business Service
 * @complexity Medium
 * @fingerprint exports:3,imports:5,sig:6jjhub
 * @lastUpdated 2026-06-25T10:19:00.202Z
 */

import { NextRequest, NextResponse } from 'next/server';
import { ensureIndustrialAccess } from '@ajabadia/satellite-sdk/auth-middleware';
import { logger } from '@ajabadia/satellite-sdk/logger';
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

    await logger.audit({
      tenantId: user.tenantId,
      action: 'CONNECTOR_VIEW',
      entityType: 'CONNECTOR',
      entityId: connectorId,
      userId: user.email || 'system',
      userEmail: user.email || 'system@abd.com',
      changedFields: {},
    });

    return NextResponse.json(connector);
  } catch (error: unknown) {
    const err = error as Error;
    const { connectorId } = await params;
    await logger.audit({
      tenantId: 'unknown',
      action: 'GET_CONNECTOR_BY_ID_ERROR',
      entityType: 'CONNECTOR',
      entityId: connectorId,
      userId: 'system',
      userEmail: 'system@abd.com',
      changedFields: { error: err.message, connectorId },
    });
    console.error('[GET_CONNECTOR_BY_ID_ERROR]', error);
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

    await logger.audit({
      tenantId: user.tenantId,
      action: 'CONNECTOR_UPDATED',
      entityType: 'CONNECTOR',
      entityId: connectorId,
      userId: user.email || 'system',
      userEmail: user.email || 'system@abd.com',
      changedFields: { status, auditMode },
    });

    return NextResponse.json(connector);
  } catch (error: unknown) {
    const err = error as Error;
    const { connectorId } = await params;
    await logger.audit({
      tenantId: 'unknown',
      action: 'PATCH_CONNECTOR_ERROR',
      entityType: 'CONNECTOR',
      entityId: connectorId,
      userId: 'system',
      userEmail: 'system@abd.com',
      changedFields: { error: err.message, connectorId },
    });
    console.error('[PATCH_CONNECTOR_ERROR]', error);
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
    await logger.audit({
      tenantId: user.tenantId,
      action: 'CONNECTOR_DELETED',
      entityType: 'CONNECTOR',
      entityId: connectorId,
      userId: user.email || 'system',
      userEmail: user.email || 'system@abd.com',
      changedFields: {},
    });
    return NextResponse.json({ success: true, message: 'Connector deleted successfully' });
  } catch (error: unknown) {
    const err = error as Error;
    const { connectorId } = await params;
    await logger.audit({
      tenantId: 'unknown',
      action: 'DELETE_CONNECTOR_ERROR',
      entityType: 'CONNECTOR',
      entityId: connectorId,
      userId: 'system',
      userEmail: 'system@abd.com',
      changedFields: { error: err.message, connectorId },
    });
    console.error('[DELETE_CONNECTOR_ERROR]', error);
    return NextResponse.json({ error: err.message || 'Internal Server Error' }, { status: 500 });
  }
}
