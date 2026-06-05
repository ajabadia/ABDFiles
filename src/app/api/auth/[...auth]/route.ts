import { createAuthRouteHandler } from '@ajabadia/satellite-sdk';
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
