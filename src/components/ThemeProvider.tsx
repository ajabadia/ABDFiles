"use client"

/**
 * @purpose Gestiona contexto de tema y proporciona un proveedor para el cambio de temas en la aplicación.
 * @purpose_en Manages theme context and provides a provider for theme switching in the application.
 * @refactorable false
 * @classification Context/Provider
 * @complexity Low
 * @fingerprint exports:1,imports:2,sig:15s0237
 * @lastUpdated 2026-06-21T14:34:07.219Z
 */

import * as React from "react"
import { ThemeProvider as NextThemesProvider } from "next-themes"

if (typeof window !== 'undefined' && process.env.NODE_ENV === 'development') {
  const orig = console.error;
  console.error = (...args: unknown[]) => {
    if (typeof args[0] === 'string' && args[0].includes('Encountered a script tag')) return;
    orig.apply(console, args);
  };
}

export function ThemeProvider({
  children,
  ...props
}: React.ComponentProps<typeof NextThemesProvider>) {
  return (
    <NextThemesProvider {...props}>
      {children}
    </NextThemesProvider>
  )
}
