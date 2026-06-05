import { NextRequest, NextResponse } from 'next/server';
import { ensureIndustrialAccess } from '@ajabadia/satellite-sdk';
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
    return NextResponse.json(result);
  } catch (error: unknown) {
    console.error('[TEST_CONNECTOR_ERROR]', error);
    const err = error as Error;
    return NextResponse.json({ error: err.message || 'Internal Server Error' }, { status: 500 });
  }
}
