/**
 * @purpose Valida las permisiones del usuario según las reglas de Control de Acceso Basado en Acceso (ABAC) utilizando el motor Guardian Engine.
 * @purpose_en Validates user permissions based on Access-Based Access Control (ABAC) rules using the Guardian Engine.
 * @refactorable false
 * @classification Business Service
 * @complexity Low
 * @fingerprint exports:2,imports:1,sig:xe7cw6
 * @lastUpdated 2026-06-25T10:20:49.984Z
 */

import { evaluateAccess, InsufficientPrivilegesError } from '@ajabadia/satellite-sdk/auth-middleware';

export interface AssertAccessParams {
  userId: string;
  tenantId: string;
  resource: string;
  action: string;
  context?: Record<string, unknown>;
}

/**
 * Asserts that the actor has permission to perform the action on the resource.
 * Throws InsufficientPrivilegesError if the Guardian Engine denies access.
 */
export async function assertAccess(params: AssertAccessParams): Promise<void> {
  const result = await evaluateAccess({
    tenantId: params.tenantId,
    userId: params.userId,
    resource: params.resource,
    action: params.action,
    context: params.context
  });

  if (!result.allowed) {
    throw new InsufficientPrivilegesError(`ABAC Denied: ${result.reason}`);
  }
}
