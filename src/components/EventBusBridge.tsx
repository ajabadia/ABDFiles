'use client';

/**
 * @purpose Gestiona el procesamiento de eventos de conectores al montar un componente y cambiar su visibilidad.
 * @purpose_en Manages the processing of connector events by calling `processConnectorEvents` on component mount and visibility change.
 * @refactorable false
 * @classification UI Component
 * @complexity Low
 * @fingerprint exports:1,imports:2,sig:1pjrjcy
 * @lastUpdated 2026-06-26T06:17:10.888Z
 */

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
