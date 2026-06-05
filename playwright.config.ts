import { defineConfig, devices } from '@playwright/test';

/**
 * 🎭 Playwright Industrial Configuration — ABDAnalytics
 * SmartNavbar E2E tests for public mode.
 *
 * Dev server starts automatically via webServer.
 *
 * ⚠️  REQUIREMENTS:
 *   - Create ABDAnalytics/.env.local with:
 *       AUTH_CLIENT_ID=...
 *       AUTH_CLIENT_SECRET=...
 *       AUTH_JWT_SECRET=...
 *   - MongoDB must be reachable via MONGODB_URI in .env.local
 */
export default defineConfig({
  testDir: './tests',
  globalSetup: './tests/global-setup',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 1,
  workers: 1,
  reporter: 'list',
  timeout: 90000,
  use: {
    baseURL: 'http://localhost:5004',
    trace: 'on-first-retry',
    screenshot: 'on',
    video: 'on-first-retry',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  // NOTA: webServer eliminado — usa scripts/run-e2e.sh para arrancar
  // el servidor manualmente (evita problemas con cmd.exe en Windows).
  /* 🔐 ABDAuth (port 3400) must be started separately for auth-dependent tests. */
});
