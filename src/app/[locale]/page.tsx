/**
 * @purpose Renderiza la página principal del aplicativo ABDFiles, incluyendo una cabecera, enlaces de navegación y pie de página.
 * @purpose_en Renders the home page of the ABDFiles application, including a header, navigation links, and footer.
 * @refactorable true (contains too many state variables and UI parts)
 * @classification UI Component
 * @complexity Low
 * @fingerprint exports:1,imports:5,sig:rrxtte
 * @lastUpdated 2026-06-21T14:33:21.706Z
 */

import { getTranslations } from 'next-intl/server';
import { ArrowRight, HardDrive, History, FileText } from 'lucide-react';
import { HeroHeader } from '@ajabadia/styles';
import Link from 'next/link';
import { GlobalFooter } from '@ajabadia/ecosystem-widgets';

export default async function HomePage({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  const t = await getTranslations('common');
  const h = await getTranslations('home');
  
  return (
    <div className="flex min-h-screen flex-col items-center justify-center p-6 md:p-24 bg-background text-foreground selection:bg-primary/30 overflow-hidden">
      {/* Tactical grid background layer */}
      <div className="absolute inset-0 bg-industrial-grid mask-industrial-fade pointer-events-none opacity-50" aria-hidden="true" />

      <div className="z-10 w-full max-w-5xl flex flex-col gap-16 animate-in fade-in duration-500">
        
        {/* Core Brand Header — outside <main> to keep banner landmark top-level */}
        <HeroHeader
          statusText={h('status')}
          title={
            <>{'ABD'} <span className="text-[#2dd4bf]">{h('tenants')}</span></>
          }
          description={h('tagline')}
        />

        <main className="flex flex-col gap-16">
          {/* Central Tactical Action Area (CTA) */}
          <div className="flex flex-col items-center justify-center gap-4">
            <Link
              href={`/${locale}/admin`}
              className="inline-flex items-center justify-center px-10 py-5 bg-primary text-primary-foreground font-mono text-xs uppercase tracking-widest hover:bg-primary/80 transition-all duration-300 font-black cursor-pointer shadow-lg active:scale-95 border border-primary/30 rounded-lg"
            >
              {h('accessControlPlane')}
              <ArrowRight className="w-4 h-4 ml-3 animate-pulse" />
            </Link>
            <span className="font-mono text-[9px] uppercase tracking-[0.25em] text-muted-foreground">
              {locale === 'es' 
                ? 'Inicie sesión con sus credenciales federadas de ABDAuth' 
                : 'Sign in utilizing your federated credentials from ABDAuth'}
            </span>
          </div>

          {/* Tactical Key Features Grid */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6" role="region" aria-label="System Capabilities">
            
            {/* Feature 1: Secure Storage */}
            <div className="p-6 bg-card border border-border rounded-xl flex flex-col gap-4">
              <div className="p-2.5 bg-secondary/10 border border-border text-[#2dd4bf] w-fit rounded-lg">
                <HardDrive className="w-5 h-5" />
              </div>
              <h2 className="text-sm font-black uppercase tracking-wider text-foreground">
                {locale === 'es' ? 'Almacenamiento Seguro' : 'Secure Storage'}
              </h2>
              <p className="text-xs text-muted-foreground leading-relaxed">
                {locale === 'es'
                  ? 'Aislamiento físico multitenant y carga integrada con proveedores de almacenamiento como Cloudinary.'
                  : 'Physical multi-tenant isolation and integrated upload with storage providers such as Cloudinary.'}
              </p>
            </div>

            {/* Feature 2: Immutable Versioning */}
            <div className="p-6 bg-card border border-border rounded-xl flex flex-col gap-4">
              <div className="p-2.5 bg-secondary/10 border border-border text-[#2dd4bf] w-fit rounded-lg">
                <History className="w-5 h-5" />
              </div>
              <h2 className="text-sm font-black uppercase tracking-wider text-foreground">
                {locale === 'es' ? 'Versionado Inmutable' : 'Immutable Versioning'}
              </h2>
              <p className="text-xs text-muted-foreground leading-relaxed">
                {locale === 'es'
                  ? 'Historial de versiones append-only protegido contra sobrescritura o destrucción accidental.'
                  : 'Append-only version history protected against accidental overwrites or destruction.'}
              </p>
            </div>

            {/* Feature 3: Retention & Audit */}
            <div className="p-6 bg-card border border-border rounded-xl flex flex-col gap-4">
              <div className="p-2.5 bg-secondary/10 border border-border text-[#2dd4bf] w-fit rounded-lg">
                <FileText className="w-5 h-5" />
              </div>
              <h2 className="text-sm font-black uppercase tracking-wider text-foreground">
                {locale === 'es' ? 'Retención y Auditoría' : 'Retention & Audit'}
              </h2>
              <p className="text-xs text-muted-foreground leading-relaxed">
                {locale === 'es'
                  ? 'Trazabilidad bancaria de eventos a ABDLogs y ciclo de vida automatizado con purga controlada.'
                  : 'Bank-grade event traceability to ABDLogs and automated lifecycle with controlled purging.'}
              </p>
            </div>

          </div>
        </main>

        <GlobalFooter 
          separatorWidth="short"
          telemetryItems={[
            { label: locale === 'es' ? 'Aplicación' : 'Application', value: h('version') },
            { label: locale === 'es' ? 'Estilo' : 'Style', value: h('style') }
          ]}
        />

      </div>
    </div>
  );
}
