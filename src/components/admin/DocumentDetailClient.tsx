'use client';

import React, { useState, useEffect, useRef } from 'react';
import { useTranslations } from 'next-intl';
import { CheckCircle, AlertTriangle, Trash2, Shield, Lock, Unlock, UploadCloud, RefreshCw } from 'lucide-react';

interface DocumentDetailClientProps {
  assetId: string;
  locale: string;
  userRole: string;
}

interface DocumentAsset {
  assetId: string;
  assetRef: string;
  title: string;
  status: string;
  sensitivityLevel: string;
  retentionClass: string;
  legalHold: boolean;
  signedUrl: string;
  createdAt: string;
}

interface DocVersion {
  versionId: string;
  versionNumber: number;
  hash: string;
  mimeType: string;
  sizeBytes: number;
  createdBy: string;
  createdAt: string;
}

interface DocEvent {
  eventId: string;
  type: string;
  actorId: string;
  createdAt: string;
  payload?: Record<string, unknown>;
}

export default function DocumentDetailClient({ assetId, locale, userRole }: DocumentDetailClientProps) {
  const t = useTranslations('admin');
  const d = useTranslations('admin.detail');
  const [doc, setDoc] = useState<DocumentAsset | null>(null);
  const [versions, setVersions] = useState<DocVersion[]>([]);
  const [events, setEvents] = useState<DocEvent[]>([]);
  const [loading, setLoading] = useState(true);
  const [uploading, setUploading] = useState(false);
  const [holdReason, setHoldReason] = useState('');
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const fetchAllData = async (showLoading = true) => {
    try {
      if (showLoading) setLoading(true);
      const [docRes, verRes, evtRes] = await Promise.all([
        fetch(`/api/v1/documents/${assetId}`),
        fetch(`/api/v1/documents/${assetId}/versions`),
        fetch(`/api/v1/documents/${assetId}/events`)
      ]);

      if (docRes.ok) setDoc(await docRes.json());
      if (verRes.ok) setVersions(await verRes.json());
      if (evtRes.ok) setEvents(await evtRes.json());
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void fetchAllData(true);
  }, [assetId]);

  const handleLogicalDelete = async () => {
    // eslint-disable-next-line no-alert, no-restricted-globals
    if (!window['confirm']('ARE_YOU_SURE_YOU_WANT_TO_DELETE_THIS_DOCUMENT?')) return;
    try {
      const res = await fetch(`/api/v1/documents/${assetId}`, { method: 'DELETE' });
      if (res.ok) {
        setMessage({ type: 'success', text: 'DOCUMENT_DELETED_SUCCESSFULLY' });
        void fetchAllData(false);
      } else {
        const data = await res.json();
        throw new Error(data.error || 'Delete failed');
      }
    } catch (err: unknown) {
      const error = err as Error;
      setMessage({ type: 'error', text: error.message });
    }
  };

  const handleApplyHold = async () => {
    if (!holdReason.trim()) {
      setMessage({ type: 'error', text: 'REASON_REQUIRED' });
      return;
    }
    try {
      const res = await fetch(`/api/v1/documents/${assetId}/holds`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ reason: holdReason })
      });
      if (res.ok) {
        setMessage({ type: 'success', text: 'LEGAL_HOLD_APPLIED' });
        setHoldReason('');
        void fetchAllData(false);
      } else {
        const data = await res.json();
        throw new Error(data.error || 'Hold apply failed');
      }
    } catch (err: unknown) {
      const error = err as Error;
      setMessage({ type: 'error', text: error.message });
    }
  };

  const handleReleaseHold = async (holdId: string) => {
    try {
      const res = await fetch(`/api/v1/documents/${assetId}/holds/${holdId}`, { method: 'DELETE' });
      if (res.ok) {
        setMessage({ type: 'success', text: 'LEGAL_HOLD_RELEASED' });
        void fetchAllData(false);
      } else {
        const data = await res.json();
        throw new Error(data.error || 'Hold release failed');
      }
    } catch (err: unknown) {
      const error = err as Error;
      setMessage({ type: 'error', text: error.message });
    }
  };

  const handleFileUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    if (!e.target.files || !e.target.files[0]) return;
    setUploading(true);
    setMessage(null);

    const file = e.target.files[0];
    const formData = new FormData();
    formData.append('file', file);

    try {
      const res = await fetch(`/api/v1/documents/${assetId}/versions`, {
        method: 'POST',
        body: formData
      });
      if (res.ok) {
        setMessage({ type: 'success', text: 'VERSION_APPENDED' });
        void fetchAllData(false);
      } else {
        const data = await res.json();
        throw new Error(data.error || 'Upload version failed');
      }
    } catch (err: unknown) {
      const error = err as Error;
      setMessage({ type: 'error', text: error.message });
    } finally {
      setUploading(false);
      if (fileInputRef.current) fileInputRef.current.value = '';
    }
  };

  if (loading) {
    return (
      <div className="border border-border p-10 flex items-center justify-center bg-card/45 backdrop-blur-sm">
        <span className="font-mono text-xs uppercase tracking-widest text-primary animate-pulse">{d('loadingTelemetry') || 'LOADING_DOCUMENT_TELEMETRY...'}</span>
      </div>
    );
  }

  if (!doc) {
    return (
      <div className="border border-border p-10 flex items-center justify-center bg-card/45 backdrop-blur-sm">
        <span className="font-mono text-xs uppercase tracking-widest text-destructive">{d('notFound') || 'DOCUMENT_NOT_FOUND'}</span>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-10 animate-in fade-in duration-300">
      {message && (
        <div className={`p-4 border font-mono text-[10px] font-black uppercase tracking-wider flex items-center gap-2 ${
          message.type === 'success' ? 'border-emerald-500/20 bg-emerald-500/5 text-emerald-500' : 'border-destructive/20 bg-destructive/5 text-destructive'
        }`}>
          {message.type === 'success' ? <CheckCircle className="w-4 h-4" /> : <AlertTriangle className="w-4 h-4" />}
          <span>{message.text}</span>
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        <div className="lg:col-span-2 flex flex-col gap-8">
          <div className="border border-border p-6 bg-card/40 backdrop-blur-sm flex flex-col gap-6">
            <h3 className="font-mono text-xs font-black uppercase tracking-widest text-primary border-b border-border/40 pb-2">{d('information')}</h3>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6 font-mono text-xs">
              <div className="flex flex-col gap-1">
                <span className="text-muted-foreground text-[8px] tracking-wider uppercase">{d('titleLabel')}</span>
                <span className="font-bold text-foreground">{doc.title}</span>
              </div>
              <div className="flex flex-col gap-1">
                <span className="text-muted-foreground text-[8px] tracking-wider uppercase">{t('indexTable.status')}</span>
                <span className="font-bold uppercase text-primary">{doc.status}</span>
              </div>
              <div className="flex flex-col gap-1">
                <span className="text-muted-foreground text-[8px] tracking-wider uppercase">{d('retention')}</span>
                <span className="font-bold uppercase text-[#2dd4bf]">{doc.retentionClass}</span>
              </div>
              <div className="flex flex-col gap-1">
                <span className="text-muted-foreground text-[8px] tracking-wider uppercase">{d('sensitivity')}</span>
                <span className="font-bold uppercase text-[#f59e0b]">{doc.sensitivityLevel}</span>
              </div>
            </div>
            <div className="pt-4 border-t border-border/40">
              <a
                href={doc.signedUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="btn-secondary-console text-[10px] font-mono tracking-widest uppercase py-3 px-6 inline-flex"
              >
                DOWNLOAD_DECRYPTED_FILE
              </a>
            </div>
          </div>

          <div className="border border-border p-6 bg-card/40 backdrop-blur-sm flex flex-col gap-6">
            <h3 className="font-mono text-xs font-black uppercase tracking-widest text-primary border-b border-border/40 pb-2">{d('versionHistory')}</h3>
            {versions.length === 0 ? (
              <span className="font-mono text-[10px] text-muted-foreground uppercase">{d('noVersions')}</span>
            ) : (
              <div className="flex flex-col gap-4">
                {versions.map((ver) => (
                  <div key={ver.versionId} className="border border-border/30 p-4 bg-card/25 font-mono text-xs flex flex-col gap-2">
                    <div className="flex justify-between items-center">
                      <span className="font-bold text-primary">VERSION #{ver.versionNumber}</span>
                      <span className="text-[9px] text-muted-foreground">{new Date(ver.createdAt).toLocaleString()}</span>
                    </div>
                    <span className="text-[9px] text-muted-foreground break-all">{d('hash')}: {ver.hash}</span>
                    <div className="flex justify-between text-[9px] text-muted-foreground">
                      <span>{d('size')}: {(ver.sizeBytes / 1024).toFixed(2)} KB</span>
                      <span>{d('created')}: {ver.createdBy}</span>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>

        <div className="flex flex-col gap-8">
          <div className="border border-border p-6 bg-card/40 backdrop-blur-sm flex flex-col gap-6">
            <h3 className="font-mono text-xs font-black uppercase tracking-widest text-primary border-b border-border/40 pb-2">{d('actions')}</h3>
            
            <div className="flex flex-col gap-4 font-mono text-xs">
              {doc.status === 'active' && (
                <div className="border border-border/30 p-4 bg-card/20 flex flex-col gap-3">
                  <span className="font-bold uppercase tracking-wider text-destructive flex items-center gap-1.5">
                    <Trash2 className="w-3.5 h-3.5" />
                    {d('logicalDelete')}
                  </span>
                  <p className="text-[9px] text-muted-foreground leading-relaxed">{d('logicalDeleteDesc')}</p>
                  <button
                    aria-label="Execute Logical Delete"
                    onClick={handleLogicalDelete}
                    disabled={doc.legalHold}
                    className="btn-destructive-console py-2 text-[9px] font-black tracking-widest uppercase cursor-pointer"
                  >
                    EXECUTE_LOGICAL_DELETE
                  </button>
                </div>
              )}

              <div className="border border-border/30 p-4 bg-card/20 flex flex-col gap-3">
                <span className="font-bold uppercase tracking-wider text-primary flex items-center gap-1.5">
                  <Shield className="w-3.5 h-3.5" />
                  {d('legalHold')}
                </span>
                <span className={`text-[9px] font-black uppercase tracking-wider ${doc.legalHold ? 'text-amber-500' : 'text-emerald-500'}`}>
                  {doc.legalHold ? d('active') : d('inactive')}
                </span>
                
                {!doc.legalHold ? (
                  <div className="flex flex-col gap-2">
                    <input
                      type="text"
                      value={holdReason}
                      onChange={(e) => setHoldReason(e.target.value)}
                      placeholder={d('holdReasonPlaceholder')}
                      className="border border-border bg-background p-2 font-mono text-[10px] w-full"
                    />
                    <button
                      aria-label="Apply Legal Hold"
                      onClick={handleApplyHold}
                      className="btn-secondary-console py-2 text-[9px] font-black tracking-widest uppercase cursor-pointer"
                    >
                      {d('applyHold')}
                    </button>
                  </div>
                ) : (
                  <div className="flex flex-col gap-2">
                    {events.filter(e => e.type === 'LEGAL_HOLD_APPLIED').map((e, idx) => (
                      <div key={idx} className="border border-border/30 p-2 bg-background font-mono text-[9px] flex flex-col gap-1">
                        <span className="text-muted-foreground">{e.payload?.reason as string}</span>
                        <button
                          aria-label="Release Legal Hold"
                          onClick={() => void handleReleaseHold(e.payload?.holdId as string)}
                          className="btn-secondary-console py-1 mt-1 text-[8px] font-black tracking-widest uppercase cursor-pointer"
                        >
                          {d('releaseHold')}
                        </button>
                      </div>
                    ))}
                  </div>
                )}
              </div>

              {doc.status === 'active' && !doc.legalHold && (
                <div className="border border-border/30 p-4 bg-card/20 flex flex-col gap-3">
                  <span className="font-bold uppercase tracking-wider text-[#2dd4bf] flex items-center gap-1.5">
                    <UploadCloud className="w-3.5 h-3.5" />
                    {d('addVersion')}
                  </span>
                  <p className="text-[9px] text-muted-foreground leading-relaxed">{d('uploadNew')}</p>
                  <input
                    ref={fileInputRef}
                    type="file"
                    onChange={handleFileUpload}
                    className="hidden"
                    accept="application/pdf,image/*"
                  />
                  <button
                    aria-label="Select File for Version Ingestion"
                    onClick={() => fileInputRef.current?.click()}
                    disabled={uploading}
                    className="btn-secondary-console py-2 text-[9px] font-black tracking-widest uppercase cursor-pointer"
                  >
                    {uploading ? 'UPLOADING...' : 'SELECT_FILE'}
                  </button>
                </div>
              )}
            </div>
          </div>

          <div className="border border-border p-6 bg-card/40 backdrop-blur-sm flex flex-col gap-6 max-h-[400px] overflow-y-auto">
            <h3 className="font-mono text-xs font-black uppercase tracking-widest text-primary border-b border-border/40 pb-2 flex items-center justify-between">
              {d('auditEvents')}
              <button
                onClick={() => void fetchAllData(false)}
                className="btn-secondary-console p-1 cursor-pointer"
                aria-label="Refresh Events"
              >
                <RefreshCw className="w-3 h-3" />
              </button>
            </h3>
            {events.length === 0 ? (
              <span className="font-mono text-[10px] text-muted-foreground uppercase">{d('noEvents')}</span>
            ) : (
              <div className="flex flex-col gap-3 font-mono text-[10px]">
                {events.map((evt) => (
                  <div key={evt.eventId} className="border-l-2 border-primary/50 pl-3 py-1 flex flex-col gap-1">
                    <span className="font-black text-foreground uppercase tracking-wider">{evt.type}</span>
                    <span className="text-[8px] text-muted-foreground">{evt.actorId} • {new Date(evt.createdAt).toLocaleString()}</span>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
