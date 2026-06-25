/**
 * @purpose Renderiza la página principal del panel administrativo de análisis con un dashboard, maneja traducciones, autenticación de usuarios y componentes de interfaz.
 * @purpose_en Renders the central admin analytics portal page with a dashboard, handling translations, user authentication, and UI components.
 * @refactorable false
 * @classification UI Component
 * @complexity Medium
 * @fingerprint exports:1,imports:9,sig:mbqihe
 * @lastUpdated 2026-06-25T10:20:16.733Z
 */

import { Suspense } from 'react';
import { getTranslations } from 'next-intl/server';
import { ensureIndustrialAccess } from '@ajabadia/satellite-sdk/auth-middleware';
import { LayoutDashboard, ArrowLeft } from 'lucide-react';
import { AdminPageHeader } from '@ajabadia/styles';
import { GlobalFooter } from '@ajabadia/ecosystem-widgets';
import DashboardClient from '@/components/admin/DashboardClient';
import DashboardSkeleton from '@/components/admin/DashboardSkeleton';
import Link from 'next/link';

/**
 * 🛰️ Inner Data Wrapper Component that handles DB Querying asynchronously
 */
async function DashboardDataWrapper({ locale }: { locale: string }) {
  return <DashboardClient locale={locale} />;
}

/**
 * 🛰️ Central Admin Analytics Portal Page (Federated Server Component)
 */
export default async function AdminPortalPage({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  const t = await getTranslations('admin');
  const a = await getTranslations('analytics');

  // 🛡️ Ecosystem Identity Guard
  const user = await ensureIndustrialAccess('ADMIN');

  return (
    <main className="min-h-screen bg-background text-foreground p-6 md:p-12 selection:bg-primary/30" role="main">
      <div className="max-w-7xl mx-auto flex flex-col gap-10">
        
        {/* Back to home */}
        <Link
          href={`/${locale}`}
          className="inline-flex items-center gap-2 font-mono text-[10px] uppercase tracking-widest text-muted-foreground hover:text-foreground transition-colors w-fit"
        >
          <ArrowLeft className="w-3.5 h-3.5" />
          {locale === 'es' ? 'Volver a Inicio' : 'Back to Home'}
        </Link>

        {/* Header Navigation */}
        <AdminPageHeader
          icon={LayoutDashboard}
          breadcrumb={<>{t('controlConsole')} • DASHBOARD</>}
          title={<>{'ABD'} <span className="text-primary">{a('title')}</span></>}
          description={<>{t('auditDesc')} <span className="text-primary font-bold">{user.tenantId}</span>.</>}
        />

        {/* Dashboard Content under Async Suspense Boundary */}
        <Suspense fallback={<DashboardSkeleton />}>
          <DashboardDataWrapper locale={locale} />
        </Suspense>

        <GlobalFooter label={t('footer')} opacity={0.8} />

      </div>
    </main>
  );
}

