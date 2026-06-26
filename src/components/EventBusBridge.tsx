'use client';

import { useEffect } from 'react';
import { processConnectorEvents } from '@/services/connector-listener';

export function EventBusBridge() {
  useEffect(() => {
    processConnectorEvents();
    const onVisible = () => processConnectorEvents();
    document.addEventListener('visibilitychange', onVisible);
    return () => document.removeEventListener('visibilitychange', onVisible);
  }, []);

  return null;
}
