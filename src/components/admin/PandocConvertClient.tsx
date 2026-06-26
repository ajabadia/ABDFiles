'use client';

/**
 * @purpose Gestiona la conversión de documentos utilizando Pandoc, maneja la entrada del usuario, muestra los resultados y proporciona una función de descarga.
 * @purpose_en Manages the conversion of documents using Pandoc, handling user input, displaying results, and providing download functionality.
 * @refactorable true (contains too many state variables and UI parts)
 * @classification UI Component
 * @complexity Medium
 * @fingerprint exports:1,imports:2,sig:1xvoe53
 * @lastUpdated 2026-06-26T16:35:19.817Z
 */

import React, { useState } from 'react';
import { FileOutput, X, Download, Loader2, AlertTriangle, CheckCircle } from 'lucide-react';

interface PandocConvertClientProps {
  assetId: string;
  documentTitle?: string;
}

const FORMAT_OPTIONS = [
  { value: 'html', label: 'HTML' },
  { value: 'markdown', label: 'Markdown' },
  { value: 'plain', label: 'Plain Text' },
  { value: 'docx', label: 'DOCX' },
  { value: 'epub', label: 'EPUB' },
  { value: 'latex', label: 'LaTeX' },
  { value: 'csv', label: 'CSV' },
  { value: 'json', label: 'JSON' },
  { value: 'yaml', label: 'YAML' },
  { value: 'pptx', label: 'PPTX' },
  { value: 'odt', label: 'ODT' },
  { value: 'rst', label: 'RST' },
  { value: 'asciidoc', label: 'AsciiDoc' },
  { value: 'mediawiki', label: 'MediaWiki' },
  { value: 'gfm', label: 'GFM (GitHub Markdown)' },
];

export default function PandocConvertClient({ assetId, documentTitle }: PandocConvertClientProps) {
  const [open, setOpen] = useState(false);
  const [toFormat, setToFormat] = useState('html');
  const [toc, setToc] = useState(false);
  const [standalone, setStandalone] = useState(true);
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [warnings, setWarnings] = useState<string[]>([]);

  const handleConvert = async () => {
    setLoading(true);
    setError(null);
    setResult(null);
    setWarnings([]);

    try {
      const res = await fetch(`/api/v1/documents/${assetId}/convert`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          to: toFormat,
          toc,
          standalone,
        }),
      });

      const data = await res.json();

      if (!res.ok) {
        throw new Error(data.error || 'Conversion failed');
      }

      setResult(data.output);
      setWarnings(data.warnings || []);

      if (data.stderr) {
        setWarnings((prev) => [...prev, ...data.stderr.split('\n').filter(Boolean)]);
      }
    } catch (err: unknown) {
      const e = err as Error;
      setError(e.message);
    } finally {
      setLoading(false);
    }
  };

  const handleDownload = () => {
    if (!result) return;
    const ext = toFormat === 'markdown' ? 'md' : toFormat === 'plain' ? 'txt' : toFormat;
    const filename = `${documentTitle || assetId}.${ext}`;
    const blob = new Blob([result], { type: 'text/plain;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    a.click();
    URL.revokeObjectURL(url);
  };

  if (!open) {
    return (
      <button
        onClick={() => setOpen(true)}
        className="btn-secondary-console text-[10px] font-mono tracking-widest uppercase py-3 px-6 inline-flex items-center gap-2"
      >
        <FileOutput className="w-4 h-4" />
        CONVERT DOCUMENT
      </button>
    );
  }

  return (
    <div className="border border-border p-6 bg-card/40 backdrop-blur-sm flex flex-col gap-6">
      <div className="flex justify-between items-center border-b border-border/40 pb-2">
        <h3 className="font-mono text-xs font-black uppercase tracking-widest text-primary flex items-center gap-2">
          <FileOutput className="w-4 h-4" />
          PANDOC CONVERTER
        </h3>
        <button onClick={() => setOpen(false)} className="text-muted-foreground hover:text-foreground">
          <X className="w-4 h-4" />
        </button>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4 font-mono text-xs">
        <div className="flex flex-col gap-2">
          <label className="text-muted-foreground text-[8px] tracking-wider uppercase">Output Format</label>
          <select
            value={toFormat}
            onChange={(e) => setToFormat(e.target.value)}
            className="bg-background border border-border px-3 py-2 text-xs font-mono text-foreground focus:outline-none focus:border-primary"
          >
            {FORMAT_OPTIONS.map((opt) => (
              <option key={opt.value} value={opt.value}>{opt.label}</option>
            ))}
          </select>
        </div>

        <div className="flex flex-col gap-2 justify-end">
          <div className="flex items-center gap-4">
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={toc}
                onChange={(e) => setToc(e.target.checked)}
                className="accent-primary"
              />
              <span className="text-[9px] uppercase tracking-wider text-muted-foreground">Table of Contents</span>
            </label>
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={standalone}
                onChange={(e) => setStandalone(e.target.checked)}
                className="accent-primary"
              />
              <span className="text-[9px] uppercase tracking-wider text-muted-foreground">Standalone</span>
            </label>
          </div>
        </div>
      </div>

      <button
        onClick={handleConvert}
        disabled={loading}
        className="btn-primary-console text-[10px] font-mono tracking-widest uppercase py-3 px-6 inline-flex items-center justify-center gap-2 disabled:opacity-50"
      >
        {loading ? (
          <>
            <Loader2 className="w-4 h-4 animate-spin" />
            CONVERTING...
          </>
        ) : (
          <>
            <FileOutput className="w-4 h-4" />
            CONVERT
          </>
        )}
      </button>

      {error && (
        <div className="border border-destructive/20 bg-destructive/5 text-destructive p-4 flex items-center gap-2 font-mono text-[10px] font-black uppercase tracking-wider">
          <AlertTriangle className="w-4 h-4 shrink-0" />
          <span>{error}</span>
        </div>
      )}

      {warnings.length > 0 && (
        <div className="border border-amber-500/20 bg-amber-500/5 p-3 font-mono text-[9px] text-amber-500 flex flex-col gap-1">
          {warnings.map((w, i) => (
            <span key={i} className="opacity-80">{w}</span>
          ))}
        </div>
      )}

      {result !== null && (
        <div className="flex flex-col gap-3">
          <div className="flex justify-between items-center">
            <span className="font-mono text-[10px] text-emerald-500 font-black uppercase tracking-wider flex items-center gap-2">
              <CheckCircle className="w-4 h-4" />
              CONVERSION SUCCESSFUL
            </span>
            <button
              onClick={handleDownload}
              className="btn-secondary-console text-[9px] font-mono tracking-widest uppercase py-2 px-4 inline-flex items-center gap-2"
            >
              <Download className="w-3.5 h-3.5" />
              DOWNLOAD
            </button>
          </div>
          <pre className="border border-border/30 bg-background/50 p-4 text-[10px] font-mono text-muted-foreground max-h-96 overflow-auto whitespace-pre-wrap break-all">
            {result.length > 5000 ? result.slice(0, 5000) + '\n\n... (truncated, download for full output)' : result}
          </pre>
        </div>
      )}
    </div>
  );
}
