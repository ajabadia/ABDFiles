/**
 * @purpose Gestiona rutas de autenticación para manejo de sesiones, salida y llamada de callback federado dinámicamente.
 * @purpose_en Manages authentication routes for session handling, logout, and federated callback dynamically.
 * @refactorable false
 * @classification Business Service
 * @complexity Low
 * @fingerprint exports:2,imports:2,sig:oaok1o
 * @lastUpdated 2026-06-25T10:18:34.571Z
 */

import { createAuthRouteHandler } from '@ajabadia/satellite-sdk/auth-middleware';
import { NextRequest } from 'next/server';

/**
 * 🛰️ Catch-All SSO Auth Route Handler
 * Manages /api/auth/session, /api/auth/logout, and /api/auth/federated/callback dynamically.
 */
const handler = createAuthRouteHandler({
  appId: process.env.NEXT_PUBLIC_APP_ID as string,
  clientId: process.env.AUTH_CLIENT_ID as string,
  clientSecret: process.env.AUTH_CLIENT_SECRET || '',
  jwtSecret: process.env.AUTH_JWT_SECRET!,
});

export async function GET(request: NextRequest) {
  return handler(request as unknown as Parameters<typeof handler>[0]);
}

export async function POST(request: NextRequest) {
  return handler(request as unknown as Parameters<typeof handler>[0]);
}
