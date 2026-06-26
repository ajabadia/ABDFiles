/**
 * @purpose Gestiona rutas de autenticación para manejo de sesiones, salida y llamada callback federado dinámicamente.
 * @purpose_en Manages authentication routes for session handling, logout, and federated callback dynamically.
 * @refactorable false
 * @classification Business Service
 * @complexity Low
 * @fingerprint exports:2,imports:2,sig:ep8t2u
 * @lastUpdated 2026-06-26T15:32:26.638Z
 */

import { createAuthRouteHandler } from '@ajabadia/satellite-sdk/auth-middleware';
import { NextRequest } from 'next/server';

/**
 * Catch-All SSO Auth Route Handler
 * Manages /api/abd-auth/session, /api/abd-auth/logout, and /api/abd-auth/federated/callback dynamically.
 * NOTE: Vercel reserves /api/auth/* — this route uses /api/abd-auth/* to avoid platform shadowing.
 */
const handler = createAuthRouteHandler({
  appId: process.env.NEXT_PUBLIC_APP_ID as string,
  clientId: process.env.AUTH_CLIENT_ID as string,
  clientSecret: process.env.AUTH_CLIENT_SECRET || '',
  jwtSecret: process.env.AUTH_JWT_SECRET || 'build-time-placeholder-secret',
});

export async function GET(request: NextRequest) {
  return handler(request as unknown as Parameters<typeof handler>[0]);
}

export async function POST(request: NextRequest) {
  return handler(request as unknown as Parameters<typeof handler>[0]);
}
