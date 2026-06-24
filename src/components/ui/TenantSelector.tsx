'use client';

/**
 * @purpose Renderiza un componente selector de inquilino que se conecta con el TenantSelectorConnector mediante las propiedades usuario de sesión, variante y abierta.
 * @purpose_en Renders a tenant selector component that connects to the TenantSelectorConnector with session user, variant, and isOpen props.
 * @refactorable false
 * @classification UI Component
 * @complexity Low
 * @fingerprint exports:1,imports:1,sig:n4uwad
 * @lastUpdated 2026-06-21T14:52:27.555Z
 */

import { TenantSelectorConnector } from "@ajabadia/ecosystem-widgets";

interface SessionUser {
  id?: string;
  email?: string;
  role?: string;
  tenantId?: string;
}

interface TenantSelectorProps {
  sessionUser?: SessionUser;
  variant?: 'dropdown' | 'trigger' | 'content';
  isOpen?: boolean;
}

export function TenantSelector({ sessionUser, variant, isOpen }: TenantSelectorProps) {
  return <TenantSelectorConnector sessionUser={sessionUser} variant={variant} isOpen={isOpen} enableContexts />;
}
