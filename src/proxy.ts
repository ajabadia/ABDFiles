/**
 * @purpose Gestiona autenticación y internacionalización para rutas en la aplicación ABDFiles utilizando middleware de Next.js.
 * @purpose_en Manages authentication and internationalization for routes in the ABDFiles application using Next.js middleware.
 * @refactorable false
 * @classification Business Service
 * @complexity Low
 * @fingerprint exports:2,imports:4,sig:3ab0ef
 * @lastUpdated 2026-06-23T23:04:00.171Z
 */

import { withIndustrialAuth } from '@ajabadia/satellite-sdk';
import createMiddleware from 'next-intl/middleware';
import { routing } from './i18n/routing';
import { NextRequest, NextResponse } from 'next/server';

const intlMiddleware = createMiddleware(routing);

/**
 * 🛰️ ABDFiles Proxy Guard
 * Next.js 16 centralized ecosystem proxy guard utilizing @ajabadia/satellite-sdk.
 */
export const proxy = withIndustrialAuth({
  appId: process.env.NEXT_PUBLIC_APP_ID as string,
  clientId: process.env.AUTH_CLIENT_ID as string,
  clientSecret: process.env.AUTH_CLIENT_SECRET || '',
  jwtSecret: process.env.AUTH_JWT_SECRET!,
  publicPaths: ['/', '/logout-success'],
  intlMiddleware: intlMiddleware as unknown as never,
});

export const config = {
  // Intercept all routes except api, static resources, and images
  matcher: ['/((?!api|_next/static|_next/image|.*\\.svg$).*)'],
};
