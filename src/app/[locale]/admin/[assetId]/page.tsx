/**
 * @purpose Renderiza una página detallada para un activo en el panel administrativo, incluyendo navegación, encabezado y detalles del documento.
 * @purpose_en Renders a detailed page for an asset in the admin console, including navigation, header, and document details.
 * @refactorable false
 * @classification UI Component
 * @complexity Medium
 * @fingerprint exports:2,imports:8,sig:711j4g
 * @lastUpdated 2026-06-25T10:20:24.530Z
 */

import { Suspense } from 'react';
import { getTranslations } from 'next-intl/server';
import { ensureIndustrialAccess } from '@ajabadia/satellite-sdk/auth-middleware';
import { FileText, ArrowLeft } from 'lucide-react';
import { AdminPageHeader } from '@ajabadia/styles';
import { GlobalFooter } from '@ajabadia/ecosystem-widgets';
import DocumentDetailClient from '@/components/admin/DocumentDetailClient';
import Link from 'next/link';

export const revalidate = 0;

export default async function DocumentDetailPage({
  params
}: {
  params: Promise<{ locale: string; assetId: string }>;
}) {
  const { locale, assetId } = await params;
  const t = await getTranslations('admin');
  const d = await getTranslations('admin.detail');

  // 🛡️ Ecosystem Identity Guard
  const user = await ensureIndustrialAccess('ADMIN');

  return (
    <main className="min-h-screen bg-background text-foreground p-6 md:p-12 selection:bg-primary/30" role="main">
      <div className="max-w-7xl mx-auto flex flex-col gap-10">
        
        {/* Back to admin index console */}
        <Link
          href={`/${locale}/admin`}
          className="inline-flex items-center gap-2 font-mono text-[10px] uppercase tracking-widest text-muted-foreground hover:text-foreground transition-colors w-fit"
        >
          <ArrowLeft className="w-3.5 h-3.5" />
          {d('backToConsole')}
        </Link>

        {/* Header Navigation */}
        <AdminPageHeader
          icon={FileText}
          breadcrumb={<>{t('controlConsole')} • {d('documentTelemetry')}</>}
          title={<>{'ABDFiles'} <span className="text-primary">{assetId.slice(0, 8).toUpperCase()}</span></>}
          description={<>{d('description')} <span className="text-primary font-bold">{user.tenantId}</span>.</>}
        />

        {/* Suspense boundary wrapping detailed console interface */}
        <Suspense fallback={
          <div className="border border-border p-10 flex items-center justify-center bg-card/45 backdrop-blur-sm">
            <span className="font-mono text-xs uppercase tracking-widest text-primary animate-pulse">
              {locale === 'es' ? 'CARGANDO PANEL INDUSTRIAL...' : 'LOADING TELEMETRY CONSOLE...'}
            </span>
          </div>
        }>
          <DocumentDetailClient assetId={assetId} locale={locale} userRole={user.role} />
        </Suspense>

        <GlobalFooter label={t('footer')} opacity={0.8} />

      </div>
    </main>
  );
}
