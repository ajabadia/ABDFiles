/**
 * @purpose Gestiona la replicación de eventos de auditoría de documentos a un servicio ABDLogs central.
 * @purpose_en Manages the replication of document audit events to a central ABDLogs service.
 * @refactorable false
 * @classification Business Service
 * @complexity Low
 * @fingerprint exports:1,imports:1,sig:auoc7q
 * @lastUpdated 2026-06-25T10:22:48.007Z
 */

import { logger } from '@ajabadia/satellite-sdk/logger';

/**
 * Service to replicate document audit events to central ABDLogs.
 */
export const IntegrationLogsService = {
  /**
   * Replicates event payload to central ABDLogs service.
   */
  async replicateEvent(
    tenantId: string,
    assetId: string,
    eventType: string,
    payload: Record<string, unknown>
  ): Promise<void> {
    const logsUrl = process.env.NEXT_PUBLIC_LOGS_URL || 'http://localhost:5003';
    const secret = process.env.LOGS_SECRET_TOKEN || 'dev-logs-token';

    try {
      // Redact sensitive patterns automatically using the SDK's built-in redact capabilities
      logger.info(`[ABDFiles Audit] Replicating event ${eventType} for asset ${assetId}`);

      const body = JSON.stringify({
        tenantId,
        assetId,
        type: eventType,
        payload,
        timestamp: new Date().toISOString()
      });

      // Fetch with timeout to prevent blocking document workflows
      const controller = new AbortController();
      const id = setTimeout(() => controller.abort(), 3000);

      const res = await fetch(`${logsUrl}/api/logs`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${secret}`
        },
        body,
        signal: controller.signal
      });

      clearTimeout(id);

      if (!res.ok) {
        console.warn(`[Logs Integration] Replicator returned status ${res.status}`);
      }
    } catch (err) {
      console.warn('[Logs Integration] Failed to replicate log event to ABDLogs:', err);
    }
  }
};
