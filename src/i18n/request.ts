/**
 * @purpose Gestiona la configuración para solicitudes de internacionalización determinando el lugar, recuperando mensajes y devolviéndolos.
 * @purpose_en Handles the configuration for internationalization requests by determining the locale, fetching messages, and returning them.
 * @refactorable false
 * @classification Custom Hook
 * @complexity Low
 * @fingerprint exports:0,imports:2,sig:15kwxt7
 * @lastUpdated 2026-06-21T14:52:33.012Z
 */

import { getRequestConfig } from 'next-intl/server';
import { routing } from './routing';

export default getRequestConfig(async ({ requestLocale }) => {
  let locale = await requestLocale;
 
  if (!locale || !routing.locales.includes(locale as typeof routing.locales[number])) {
    locale = routing.defaultLocale;
  }
 
  const { locales: allMessages } = await import('@ajabadia/i18n');
  const messages = allMessages[locale as 'es' | 'en'];

  return {
    locale,
    messages
  };
});
