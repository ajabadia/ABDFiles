'use client';

/**
 * @purpose Renderiza una notificacion emergente para el panel administrativo del proyecto ABDSuite.
 * @purpose_en Renders a loading skeleton for the admin dashboard in the ABDSuite project.
 * @refactorable false
 * @classification UI Component
 * @complexity Low
 * @fingerprint exports:1,imports:2,sig:19khq6n
 * @lastUpdated 2026-06-21T14:33:28.540Z
 */

import React from 'react';
import { Layers } from 'lucide-react';

export default function DashboardSkeleton() {
  return (
    <div className="flex flex-col gap-8 w-full animate-console-pulse">
      {/* Skeleton Tabs Bar */}
      <div className="flex gap-2 border-b border-border/40 pb-px">
        {[1, 2, 3, 4].map((i) => (
          <div key={i} className="px-5 py-3 border border-transparent font-mono text-xs text-muted-foreground/30 bg-secondary/10 select-none">
            [LOADING_TAB_{i}]
          </div>
        ))}
      </div>

      {/* Skeleton KPI Grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
        {[1, 2, 3, 4].map((i) => (
          <div key={i} className="glass-panel p-5 flex flex-col gap-3 rounded-none bg-card/25 border-border/20 h-32 justify-between">
            <div className="w-5 h-5 bg-muted/20 animate-pulse" />
            <div className="h-2 bg-muted/10 w-24 rounded-none" />
            <div className="h-6 bg-muted/20 w-16 rounded-none" />
          </div>
        ))}
      </div>

      {/* Skeleton Detailed Panel */}
      <div className="glass-panel p-6 rounded-none flex flex-col gap-6 bg-card/20 border-border/20">
        <div className="flex items-center gap-3">
          <Layers className="w-4 h-4 text-primary/30 animate-spin" />
          <span className="font-mono text-xs text-primary/60 uppercase tracking-widest">
            CARGANDO TELEMETRÍA DE LA SUITE...
          </span>
        </div>
        <div className="h-40 bg-muted/5 border border-border/10 flex items-center justify-center font-mono text-[10px] text-muted-foreground/45 uppercase tracking-widest">
          ESTABLECIENDO CONEXIÓN CON LOS CONTENEDORES DE DATOS...
        </div>
      </div>
    </div>
  );
}
