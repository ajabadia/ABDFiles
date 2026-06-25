/**
 * @purpose Proporciona el proveedor de almacenamiento activo para un usuario a través de solicitudes GET.
 * @purpose_en Handles GET requests to retrieve the active storage provider for a user.
 * @refactorable false
 * @classification Business Service
 * @complexity Low
 * @fingerprint exports:1,imports:4,sig:10k4sa6
 * @lastUpdated 2026-06-25T10:20:01.357Z
 */

import { NextRequest, NextResponse } from 'next/server';
import { ensureIndustrialAccess } from '@ajabadia/satellite-sdk/auth-middleware';
import StorageConnector from '@/models/StorageConnector';
import { assertAccess } from '@/lib/abac';

export async function GET(_request: NextRequest) {
  try {
    const user = await ensureIndustrialAccess();
    await assertAccess({ userId: user.id || user.email || 'system', tenantId: user.tenantId, resource: 'storage/provider', action: 'view' });
    const connector = await StorageConnector.findOne({ tenantId: user.tenantId, status: 'active' });
    const provider = connector?.providerType || process.env.DEFAULT_STORAGE_PROVIDER || 'cloudinary';
    return NextResponse.json({ provider });
  } catch (error) {
    console.error('[ACTIVE_PROVIDER_ERROR]', error);
    return NextResponse.json({ provider: process.env.DEFAULT_STORAGE_PROVIDER || 'cloudinary' });
  }
}
