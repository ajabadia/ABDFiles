/**
 * @purpose Gestiona las notificaciones de webhook firmadas para suscriptores externos.
 * @purpose_en Manages the emission of signed webhook notifications to external subscribers.
 * @refactorable false
 * @classification Business Service
 * @complexity Low
 * @fingerprint exports:1,imports:1,sig:g7qv9o
 * @lastUpdated 2026-06-23T23:04:35.458Z
 */

import crypto from 'crypto';

/**
 * Service to emit outgoing webhook notifications to external subscribers.
 */
export const WebhookService = {
  /**
   * Dispatches signed webhook payload asynchronously.
   */
  async emitWebhook(eventType: string, tenantId: string, payload: Record<string, unknown>): Promise<void> {
    const webhookSecret = process.env.WEBHOOK_SECRET || 'dev-webhook-secret';
    const bodyStr = JSON.stringify({
      eventType,
      tenantId,
      timestamp: new Date().toISOString(),
      payload
    });

    const signature = crypto.createHmac('sha256', webhookSecret).update(bodyStr).digest('hex');

    // Default registered endpoints
    const endpoints = [
      process.env.DOCS_WEBHOOK_URL,
      process.env.TEMPLATES_WEBHOOK_URL
    ].filter(Boolean) as string[];

    for (const url of endpoints) {
      void this.dispatchWithRetry(url, bodyStr, signature);
    }
  },

  /**
   * Deliver payload to specific subscriber with retry logic.
   */
  async dispatchWithRetry(url: string, body: string, signature: string, attempt = 1): Promise<void> {
    try {
      const res = await fetch(url, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-ABD-Signature': signature
        },
        body
      });

      if (!res.ok && attempt < 3) {
        throw new Error(`Server returned code ${res.status}`);
      }
    } catch (err) {
      console.warn(`[Webhook] Dispatch to ${url} failed (Attempt ${attempt}/3):`, err);
      if (attempt < 3) {
        const delay = Math.pow(2, attempt) * 1000;
        await new Promise(resolve => setTimeout(resolve, delay));
        await this.dispatchWithRetry(url, body, signature, attempt + 1);
      }
    }
  }
};
