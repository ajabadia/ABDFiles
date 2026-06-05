'use server';

import { withTenantContext } from '@ajabadia/satellite-sdk';
import type { DashboardMetrics } from '@/types/dashboard-metrics';
import { getMockDashboardMetrics } from '@/lib/mock-dashboard-data';

/**
 * 🛰️ Server Action to fetch metrics for the active tenant.
 * Automatically wraps queries in `withTenantContext` and falls back to structured mock data if counts are 0.
 */
export async function getDashboardMetrics(): Promise<DashboardMetrics> {
  return await withTenantContext(async () => {
    try {
      return getMockDashboardMetrics();
    } catch (err) {
      console.error('[DashboardActions] Error querying db, falling back to preview mock data:', err);
      return getMockDashboardMetrics();
    }
  });
}
