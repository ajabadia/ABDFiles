'use client';

import React, { useState, useCallback } from 'react';
import { AlertTriangle } from 'lucide-react';
import type { DashboardMetrics } from '@/types/dashboard-metrics';
import SuiteTab from './tabs/SuiteTab';
import LmsTab from './tabs/LmsTab';
import SecurityTab from './tabs/SecurityTab';
import GovernanceTab from './tabs/GovernanceTab';

interface DashboardClientProps {
  metrics: DashboardMetrics;
  locale: string;
}

type TabId = 'suite' | 'lms' | 'security' | 'governance';

const TAB_IDS: TabId[] = ['suite', 'lms', 'security', 'governance'];

export default function DashboardClient({ metrics, locale }: DashboardClientProps) {
  const [activeTab, setActiveTab] = useState<TabId>('suite');

  const tabRefs = React.useRef<Record<TabId, HTMLButtonElement | null>>({
    suite: null,
    lms: null,
    security: null,
    governance: null,
  });

  const handleKeyDown = useCallback((e: React.KeyboardEvent, currentTab: TabId) => {
    let nextIndex: number;
    if (e.key === 'ArrowRight') {
      nextIndex = (TAB_IDS.indexOf(currentTab) + 1) % TAB_IDS.length;
    } else if (e.key === 'ArrowLeft') {
      nextIndex = (TAB_IDS.indexOf(currentTab) - 1 + TAB_IDS.length) % TAB_IDS.length;
    } else {
      return;
    }
    e.preventDefault();
    const nextTab = TAB_IDS[nextIndex];
    setActiveTab(nextTab);
    tabRefs.current[nextTab]?.focus();
  }, []);

  const tabLabel = (tab: TabId): string => {
    switch (tab) {
      case 'suite': return locale === 'es' ? 'RESUMEN SUITE' : 'SUITE SUMMARY';
      case 'lms': return 'LMS (ABDQUIZ)';
      case 'security': return locale === 'es' ? 'SEGURIDAD (ABDAUTH)' : 'SECURITY (ABDAUTH)';
      case 'governance': return locale === 'es' ? 'GOBERNANZA' : 'GOVERNANCE';
    }
  };

  return (
    <div className="flex flex-col gap-8 w-full">
      {/* ⚠️ Omnipresent Demo Mode Audit Banner */}
      {metrics.isDemoMode && (
        <div 
          className="relative overflow-hidden border border-warning/30 bg-warning/5 p-4 flex flex-col md:flex-row items-center justify-between gap-4 animate-pulse"
          role="alert"
          aria-live="assertive"
        >
          <div className="flex items-center gap-3">
            <AlertTriangle className="w-5 h-5 text-warning flex-shrink-0" />
            <div className="flex flex-col">
              <span className="font-mono text-xs font-black uppercase tracking-widest text-warning">
                {locale === 'es' 
                  ? '[ADVERTENCIA DE AUDITORÍA] SISTEMA EN MODO PREVIEW - DATOS SIMULADOS' 
                  : '[AUDIT WARNING] SYSTEM IN PREVIEW MODE - SIMULATED DATA'}
              </span>
              <span className="text-[10px] font-mono text-muted-foreground uppercase tracking-wider">
                {locale === 'es'
                  ? 'La base de datos actual está vacía para este Tenant. Mostrando capa analítica simulada para propósitos de demostración técnica.'
                  : 'The current database is empty for this Tenant. Showing simulated analytics layer for technical demonstration purposes.'}
              </span>
            </div>
          </div>
          <div className="px-3 py-1 border border-warning/40 text-[9px] font-mono text-warning uppercase font-bold tracking-widest bg-warning/10 select-none">
            {locale === 'es' ? 'MODO_DEMO_ACTIVO' : 'DEMO_MODE_ACTIVE'}
          </div>
        </div>
      )}

      {/* Tabs navigation in compliance with industrial design */}
      <div className="flex flex-wrap gap-2 border-b border-border/40 pb-px" role="tablist" aria-label={locale === 'es' ? 'Paneles de analíticas' : 'Analytics panels'}>
        {TAB_IDS.map((tab) => (
          <button
            key={tab}
            ref={(el) => { tabRefs.current[tab] = el; }}
            aria-label={`${tabLabel(tab)} tab`}
            onClick={() => setActiveTab(tab)}
            onKeyDown={(e) => handleKeyDown(e, tab)}
            className={`px-5 py-3 font-mono text-xs uppercase tracking-widest border transition-all duration-150 rounded-none cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/50 ${
              activeTab === tab
                ? 'border-primary border-b-transparent bg-primary/5 text-primary font-bold'
                : 'border-transparent text-muted-foreground hover:text-foreground hover:bg-secondary/20'
            }`}
            role="tab"
            aria-selected={activeTab === tab}
            tabIndex={activeTab === tab ? 0 : -1}
          >
            {tabLabel(tab)}
          </button>
        ))}
      </div>

      {/* Tab Panels */}
      <div className="min-h-[400px]" role="tabpanel" aria-label={tabLabel(activeTab)}>
        {activeTab === 'suite' && <SuiteTab metrics={metrics} locale={locale} />}
        {activeTab === 'lms' && <LmsTab metrics={metrics} locale={locale} />}
        {activeTab === 'security' && <SecurityTab metrics={metrics} locale={locale} />}
        {activeTab === 'governance' && <GovernanceTab metrics={metrics} locale={locale} />}
      </div>
    </div>
  );
}
