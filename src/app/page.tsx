/**
 * @purpose Redirige a los usuarios hacia la ruta '/es'.
 * @purpose_en Redirects users to the '/es' route.
 * @refactorable false
 * @classification UI Component
 * @complexity Low
 * @fingerprint exports:1,imports:1,sig:j9ijto
 * @lastUpdated 2026-06-21T14:33:00.286Z
 */

import { redirect } from 'next/navigation';

export default function RootPage() {
  redirect('/es');
}
