'use client';

import React from 'react';
import { useRouter, usePathname } from '@/i18n/routing';
import { useLocale } from 'next-intl';
import { CommandPalette, Command } from '@ajabadia/ecosystem-widgets';
import { LayoutDashboard, BarChart3, ShieldCheck, Globe, LogOut, Settings } from 'lucide-react';

export function AnalyticsCommandPalette() {
  const router = useRouter();
  const pathname = usePathname();
  const locale = useLocale();

  const commands: Command[] = [
    {
      id: 'nav-dashboard',
      title: locale === 'es' ? 'Panel de Control' : 'Dashboard',
      description: locale === 'es' ? 'Ir al panel de analíticas central' : 'Go to the central analytics dashboard',
      category: locale === 'es' ? 'Navegación' : 'Navigation',
      shortcut: ['g', 'd'],
      icon: <LayoutDashboard className="w-4 h-4" />,
      action: () => {
        router.push('/admin');
      }
    },
    {
      id: 'nav-suite',
      title: locale === 'es' ? 'Resumen de la Suite' : 'Suite Summary',
      description: locale === 'es' ? 'Ver métricas generales del ecosistema' : 'View overall ecosystem metrics',
      category: locale === 'es' ? 'Navegación' : 'Navigation',
      shortcut: ['g', 's'],
      icon: <BarChart3 className="w-4 h-4" />,
      action: () => {
        router.push('/admin');
      }
    },
    {
      id: 'nav-security',
      title: locale === 'es' ? 'Panel de Seguridad' : 'Security Panel',
      description: locale === 'es' ? 'Estado de MFA y accesos' : 'MFA status and access logs',
      category: locale === 'es' ? 'Navegación' : 'Navigation',
      shortcut: ['g', 'm'],
      icon: <ShieldCheck className="w-4 h-4" />,
      action: () => {
        router.push('/admin');
      }
    },
    {
      id: 'action-language',
      title: locale === 'es' ? 'Switch to English' : 'Cambiar a Español',
      description: locale === 'es' ? 'Change layout language to English' : 'Cambiar el idioma a Español',
      category: locale === 'es' ? 'Configuración' : 'Settings',
      shortcut: ['c', 'l'],
      icon: <Globe className="w-4 h-4" />,
      action: () => {
        const nextLocale = locale === 'es' ? 'en' : 'es';
        router.replace(pathname, { locale: nextLocale });
      }
    },
    {
      id: 'action-settings',
      title: locale === 'es' ? 'Abrir Panel de Configuración' : 'Open System Settings',
      description: locale === 'es' ? 'Ajustar temas visuales e idioma' : 'Adjust theme modes and language',
      category: locale === 'es' ? 'Configuración' : 'Settings',
      shortcut: ['c', 's'],
      icon: <Settings className="w-4 h-4" />,
      action: () => {
        const settingsBtn = document.querySelector('[aria-label="Open Settings"]') as HTMLButtonElement;
        if (settingsBtn) {
          settingsBtn.click();
        }
      }
    },
    {
      id: 'action-logout',
      title: locale === 'es' ? 'Cerrar Sesión' : 'Sign Out',
      description: locale === 'es' ? 'Finalizar sesión de forma segura' : 'Securely end your session',
      category: locale === 'es' ? 'Configuración' : 'Settings',
      shortcut: ['q', 'q'],
      icon: <LogOut className="w-4 h-4" />,
      action: () => {
        window.location.href = '/api/auth/logout';
      }
    }
  ];

  return (
    <CommandPalette
      commands={commands}
      placeholder={locale === 'es' ? 'Escribe un comando o navega...' : 'Type a command or navigate...'}
    />
  );
}
