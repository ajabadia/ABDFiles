'use client';

/**
 * @purpose Renderiza un panel de control para gestionar activos de documentos, incluyendo recuperar, mostrar y eliminar documentos.
 * @purpose_en Renders a dashboard for managing document assets, including fetching, displaying, and deleting documents.
 * @refactorable true (contains too many state variables and UI parts)
 * @classification UI Component
 * @complexity Medium
 * @fingerprint exports:1,imports:5,sig:2whgug
 * @lastUpdated 2026-06-21T14:33:25.885Z
 */

import React, { useState, useEffect } from 'react';
import { Eye, FileText, RefreshCw, Trash2, LayoutGrid } from 'lucide-react';
import Link from 'next/link';
import { useTranslations } from 'next-intl';
import UploadZone from './UploadZone';

interface DocumentAsset {
  assetId: string;
  assetRef: string;
  title: string;
  status: string;
  sensitivityLevel: string;
  storageProvider: string;
  createdAt: string;
}

interface DashboardClientProps {
  locale: string;
}

export default function DashboardClient({ locale }: DashboardClientProps) {
  const t = useTranslations('common');
  const adm = useTranslations('admin');
  const [documents, setDocuments] = useState<DocumentAsset[]>([]);
  const [loading, setLoading] = useState(true);

  const fetchDocuments = async (showLoading = true) => {
    try {
      if (showLoading) setLoading(true);
      const res = await fetch('/api/v1/documents');
      if (res.ok) {
        const data = await res.json();
        setDocuments(data);
      }
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void fetchDocuments(false);
  }, []);

  const handleDelete = async (assetId: string) => {
    // eslint-disable-next-line no-alert, no-restricted-globals
    if (!window['confirm']('ARE_YOU_SURE_YOU_WANT_TO_DELETE_THIS_DOCUMENT?')) return;
    try {
      const res = await fetch(`/api/v1/documents/${assetId}`, {
        method: 'DELETE'
      });
      if (res.ok) {
        void fetchDocuments();
      }
    } catch (err) {
      console.error(err);
    }
  };

  return (
    <div className="flex flex-col gap-10 w-full animate-in fade-in duration-300">
      {/* Telemetry Stats Bar */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div className="bg-card border border-border p-6 rounded-none flex flex-col gap-2">
          <span className="text-[9px] font-mono font-black text-muted-foreground uppercase tracking-widest">
            TOTAL_ACTIVE_ASSETS
          </span>
          <span className="text-3xl font-mono font-black text-primary">
            {documents.filter(d => d.status !== 'purged').length}
          </span>
        </div>
        <div className="bg-card border border-border p-6 rounded-none flex flex-col gap-2">
          <span className="text-[9px] font-mono font-black text-muted-foreground uppercase tracking-widest">
            STORAGE_PROVIDER
          </span>
          <span className="text-3xl font-mono font-black text-[#2dd4bf] uppercase">
            CLOUDINARY
          </span>
        </div>
        <div className="bg-card border border-border p-6 rounded-none flex flex-col gap-2">
          <span className="text-[9px] font-mono font-black text-muted-foreground uppercase tracking-widest">
            SYSTEM_STATUS
          </span>
          <span className="text-3xl font-mono font-black text-emerald-500 uppercase">
            SYS_ONLINE
          </span>
        </div>
      </div>

      {/* Upload Console */}
      <div className="flex flex-col gap-4">
        <h2 className="text-[10px] font-mono font-black uppercase tracking-widest text-foreground flex items-center gap-2">
          <LayoutGrid className="w-4 h-4 text-primary" />
          INGESTION_CONSOLE
        </h2>
        <UploadZone onUploadSuccess={() => void fetchDocuments()} />
      </div>

      {/* Table Console */}
      <div className="flex flex-col gap-4">
        <div className="flex items-center justify-between">
          <h2 className="text-[10px] font-mono font-black uppercase tracking-widest text-foreground flex items-center gap-2">
            <FileText className="w-4 h-4 text-primary" />
            DOCUMENT_INDEX
          </h2>
          <button
            onClick={() => void fetchDocuments()}
            className="btn-secondary-console p-2 cursor-pointer flex items-center justify-center"
            aria-label="Refresh Documents"
          >
            <RefreshCw className="w-3.5 h-3.5" />
          </button>
        </div>

        {loading ? (
          <div className="overflow-x-auto border border-border rounded-none bg-card/40 backdrop-blur-sm p-10 flex items-center justify-center">
            <span className="font-mono text-xs uppercase tracking-widest text-primary animate-pulse">
              LOADING_ASSETS...
            </span>
          </div>
        ) : documents.length === 0 ? (
          <div className="overflow-x-auto border border-border rounded-none bg-card/40 backdrop-blur-sm p-10 flex flex-col items-center justify-center gap-2">
            <span className="font-mono text-xs uppercase tracking-widest text-muted-foreground">
              NO_DOCUMENTS_FOUND
            </span>
          </div>
        ) : (
          <div className="overflow-x-auto border border-border rounded-none bg-card/40 backdrop-blur-sm">
            <table className="w-full border-collapse">
              <thead>
                <tr className="bg-secondary/40 border-b border-border">
                  <th className="px-6 py-4 text-left font-mono text-[9px] font-black uppercase tracking-widest text-muted-foreground">{adm('indexTable.id')}</th>
                  <th className="px-6 py-4 text-left font-mono text-[9px] font-black uppercase tracking-widest text-muted-foreground">{adm('indexTable.title')}</th>
                  <th className="px-6 py-4 text-left font-mono text-[9px] font-black uppercase tracking-widest text-muted-foreground">{adm('indexTable.status')}</th>
                  <th className="px-6 py-4 text-left font-mono text-[9px] font-black uppercase tracking-widest text-muted-foreground">{adm('indexTable.sensitivity')}</th>
                  <th className="px-6 py-4 text-right font-mono text-[9px] font-black uppercase tracking-widest text-muted-foreground">{adm('indexTable.actions')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border/60">
                {documents.map((doc) => (
                  <tr key={doc.assetId} className="hover:bg-primary/[0.02] transition-colors duration-150">
                    <td className="px-6 py-4 font-mono text-[10px] font-bold text-muted-foreground/80">
                      {doc.assetId.slice(0, 8).toUpperCase()}
                    </td>
                    <td className="px-6 py-4 text-xs font-sans text-foreground/90">
                      {doc.title}
                    </td>
                    <td className="px-6 py-4">
                      <span className={`inline-flex items-center gap-1.5 px-2 py-0.5 border text-[8px] font-mono font-black uppercase tracking-wider rounded-none ${
                        doc.status === 'active'
                          ? 'border-emerald-500/20 bg-emerald-500/5 text-emerald-500'
                          : doc.status === 'purged'
                          ? 'border-red-500/20 bg-red-500/5 text-red-500'
                          : 'border-amber-500/20 bg-amber-500/5 text-amber-500'
                      }`}>
                        {doc.status}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-xs font-sans text-foreground/90">
                      <span className="uppercase text-[9px] font-mono">{doc.sensitivityLevel}</span>
                    </td>
                    <td className="px-6 py-4 text-right flex justify-end gap-2">
                      <Link
                        href={`/${locale}/admin/${doc.assetId}`}
                        className="btn-secondary-console p-2 cursor-pointer flex items-center justify-center"
                        aria-label="View Details"
                      >
                        <Eye className="w-3.5 h-3.5" />
                      </Link>
                      {doc.status === 'active' && (
                        <button
                          onClick={() => void handleDelete(doc.assetId)}
                          className="btn-destructive-console p-2 cursor-pointer flex items-center justify-center"
                          aria-label="Delete Asset"
                        >
                          <Trash2 className="w-3.5 h-3.5" />
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
