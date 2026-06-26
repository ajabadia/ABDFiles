import { createConsumer, SystemEventType } from '@ajabadia/satellite-sdk/event-bus';

let lastProcessed = 0;
const DEBOUNCE_MS = 2000;

const consumer = createConsumer({ source: 'abdfiles', pollIntervalMs: 30000 });

consumer.on(SystemEventType.STORAGE_CONNECTOR_CHANGED, async (event) => {
  const { tenantId, providerType } = event.data as { tenantId: string; providerType: string };
  if (process.env.NODE_ENV !== 'production') {
    console.log(`[CONNECTOR_LISTENER] Provider changed for tenant ${tenantId}: ${providerType}`);
  }
});

export async function processConnectorEvents(): Promise<void> {
  const now = Date.now();
  if (now - lastProcessed < DEBOUNCE_MS) return;
  lastProcessed = now;
  await consumer.processPending();
}

export function createConnectorConsumer() {
  return consumer;
}

