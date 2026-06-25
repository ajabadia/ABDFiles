/**
 * @purpose Gestiona el solicitud GET para obtener una lista de inquilinos, asegurando que solo los usuarios SUPER_ADMIN tengan acceso a ella.
 * @purpose_en Manages the GET request to retrieve a list of tenants, ensuring only SUPER_ADMIN users can access it.
 * @refactorable false
 * @classification Business Service
 * @complexity Medium
 * @fingerprint exports:2,imports:6,sig:16enhor
 * @lastUpdated 2026-06-25T10:18:25.988Z
 */

import { NextResponse } from 'next/server';
import { ensureIndustrialAccess } from '@ajabadia/satellite-sdk/auth-middleware';
import { getGlobalModel } from '@ajabadia/satellite-sdk/db';
import { logger } from '@ajabadia/satellite-sdk/logger';
import { assertAccess } from '@/lib/abac';
import mongoose from 'mongoose';

export const revalidate = 0; // Evitar el cacheado estático de la API

const TenantSchema = new mongoose.Schema({ tenantId: String, name: String, active: Boolean }, { collection: 'tenants' });
const TenantModel = getGlobalModel('Tenant', TenantSchema, 'AUTH');

export async function GET() {
  try {
    // 🛡️ Verificar acceso con rol mínimo ADMIN
    const user = await ensureIndustrialAccess('ADMIN');
    await assertAccess({ userId: user.id || user.email || 'system', tenantId: user.tenantId, resource: 'admin/tenants', action: 'list' });
    
    // Solo SUPER_ADMIN puede ver todos los tenants
    if (user.role !== 'SUPER_ADMIN') {
      return NextResponse.json({ error: 'Forbidden' }, { status: 403 });
    }

    const rawTenants = await TenantModel.find().lean();
    const tenants = (rawTenants as unknown as { tenantId: string; name?: string; active?: boolean }[]).map((t) => ({
      tenantId: t.tenantId,
      name: t.name || t.tenantId,
      active: t.active !== false,
    }));

    await logger.audit({
      tenantId: user.tenantId,
      action: 'ADMIN_TENANTS_LIST',
      entityType: 'TENANT',
      entityId: 'unknown',
      userId: user.email || 'system',
      userEmail: user.email || 'system@abd.com',
      changedFields: { count: tenants.length },
    });

    return NextResponse.json(tenants);
  } catch (error: unknown) {
    const err = error as Error;
    await logger.audit({
      tenantId: 'unknown',
      action: 'API_GET_TENANTS_ERROR',
      entityType: 'TENANT',
      entityId: 'unknown',
      userId: 'system',
      userEmail: 'system@abd.com',
      changedFields: { error: err.message },
    });
    console.error('[API_GET_TENANTS_ERROR]', error);
    return NextResponse.json({ error: err.message || 'Internal Server Error' }, { status: 500 });
  }
}
