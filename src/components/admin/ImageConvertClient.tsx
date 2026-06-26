'use client';

import React, { useState, useRef } from 'react';
import { Image, X, Download, Loader2, AlertTriangle, CheckCircle, Globe, Monitor } from 'lucide-react';
import { convertImageLocally, isBrowserImageSupported } from '@/services/image-browser';

const IMAGE_FORMATS = [
  { value: 'image/jpeg', label: 'JPEG' },
  { value: 'image/png', label: 'PNG' },
  { value: 'image/webp', label: 'WebP' },
  { value: 'image/gif', label: 'GIF' },
  { value: 'image/avif', label: 'AVIF' },
  { value: 'image/tiff', label: 'TIFF' },
];

export default function ImageConvertClient() {
  const [open, setOpen] = useState(false);
  const [file, setFile] = useState<File | null>(null);
  const [preview, setPreview] = useState<string | null>(null);
  const [toFormat, setToFormat] = useState('image/webp');
  const [quality, setQuality] = useState(80);
  const [loading, setLoading] = useState(false);
  const [localLoading, setLocalLoading] = useState(false);
  const [result, setResult] = useState<string | null>(null);
  const [resultMime, setResultMime] = useState<string>('');
  const [error, setError] = useState<string | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const browserSupported = isBrowserImageSupported();

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const f = e.target.files?.[0];
    if (!f) return;
    setFile(f);
    setResult(null);
    setError(null);
    const url = URL.createObjectURL(f);
    setPreview(url);
  };

  const toBase64 = (blob: Blob): Promise<string> => {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => {
        const result = reader.result as string;
        resolve(result.split(',')[1]);
      };
      reader.onerror = reject;
      reader.readAsDataURL(blob);
    });
  };

  const handleServerConvert = async () => {
    if (!file) return;
    setLoading(true);
    setError(null);
    setResult(null);

    try {
      const base64 = await toBase64(file);
      const res = await fetch('/api/v1/convert', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          content: base64,
          mimeType: file.type || 'image/jpeg',
          to: toFormat,
          quality,
        }),
      });

      const data = await res.json();
      if (!res.ok) throw new Error(data.error || 'Conversion failed');

      setResult(data.output);
      setResultMime(data.mimeType);
    } catch (err: unknown) {
      const e = err as Error;
      setError(e.message);
    } finally {
      setLoading(false);
    }
  };

  const handleLocalConvert = async () => {
    if (!file) return;
    setLocalLoading(true);
    setError(null);
    setResult(null);

    try {
      const blob = await convertImageLocally(file, { to: toFormat, quality });
      const base64 = await toBase64(blob);
      setResult(base64);
      setResultMime(blob.type);
    } catch (err: unknown) {
      const e = err as Error;
      setError(e.message);
    } finally {
      setLocalLoading(false);
    }
  };

  const handleDownload = () => {
    if (!result || !file) return;
    const ext = toFormat.split('/')[1] || 'jpg';
    const name = file.name.replace(/\.[^/.]+$/, '') || 'converted';
    const blob = new Blob(
      [Uint8Array.from(atob(result), (c) => c.charCodeAt(0))],
      { type: resultMime },
    );
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${name}.${ext}`;
    a.click();
    URL.revokeObjectURL(url);
  };

  if (!open) {
    return (
      <button
        onClick={() => setOpen(true)}
        className="btn-secondary-console text-[10px] font-mono tracking-widest uppercase py-3 px-6 inline-flex items-center gap-2"
      >
        <Image className="w-4 h-4" />
        CONVERT IMAGE
      </button>
    );
  }

  return (
    <div className="border border-border p-6 bg-card/40 backdrop-blur-sm flex flex-col gap-6">
      <div className="flex justify-between items-center border-b border-border/40 pb-2">
        <h3 className="font-mono text-xs font-black uppercase tracking-widest text-primary flex items-center gap-2">
          <Image className="w-4 h-4" />
          IMAGE CONVERTER
        </h3>
        <button onClick={() => setOpen(false)} className="text-muted-foreground hover:text-foreground">
          <X className="w-4 h-4" />
        </button>
      </div>

      <div className="font-mono text-xs">
        <input
          ref={inputRef}
          type="file"
          accept="image/*"
          onChange={handleFileSelect}
          className="block w-full text-[10px] file:mr-4 file:py-2 file:px-4 file:border-0 file:text-[10px] file:font-mono file:font-bold file:tracking-widest file:uppercase file:bg-primary file:text-primary-foreground hover:file:opacity-80"
        />
      </div>

      {preview && (
        <div className="border border-border/30 bg-background/50 p-2 flex items-center justify-center max-h-48 overflow-hidden">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img src={preview} alt="Preview" className="max-h-44 object-contain" />
        </div>
      )}

      {file && (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 font-mono text-xs">
          <div className="flex flex-col gap-2">
            <label className="text-muted-foreground text-[8px] tracking-wider uppercase">Output Format</label>
            <select
              value={toFormat}
              onChange={(e) => setToFormat(e.target.value)}
              className="bg-background border border-border px-3 py-2 text-xs font-mono text-foreground focus:outline-none focus:border-primary"
            >
              {IMAGE_FORMATS.map((opt) => (
                <option key={opt.value} value={opt.value}>{opt.label}</option>
              ))}
            </select>
          </div>
          <div className="flex flex-col gap-2">
            <label className="text-muted-foreground text-[8px] tracking-wider uppercase">
              Quality: {quality}
            </label>
            <input
              type="range"
              min={10}
              max={100}
              value={quality}
              onChange={(e) => setQuality(Number(e.target.value))}
              className="accent-primary w-full"
            />
          </div>
        </div>
      )}

      {file && (
        <div className="flex gap-3">
          <button
            onClick={handleServerConvert}
            disabled={loading || localLoading}
            className="btn-primary-console text-[10px] font-mono tracking-widest uppercase py-3 px-6 inline-flex items-center justify-center gap-2 disabled:opacity-50"
          >
            {loading ? (
              <>
                <Loader2 className="w-4 h-4 animate-spin" />
                CONVERTING...
              </>
            ) : (
              <>
                <Globe className="w-4 h-4" />
                CONVERT (server)
              </>
            )}
          </button>
          {browserSupported && (
            <button
              onClick={handleLocalConvert}
              disabled={loading || localLoading}
              className="btn-secondary-console text-[10px] font-mono tracking-widest uppercase py-3 px-6 inline-flex items-center justify-center gap-2 disabled:opacity-50"
            >
              {localLoading ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin" />
                  CONVERTING...
                </>
              ) : (
                <>
                  <Monitor className="w-4 h-4" />
                  CONVERT LOCALLY
                </>
              )}
            </button>
          )}
        </div>
      )}

      {error && (
        <div className="border border-destructive/20 bg-destructive/5 text-destructive p-4 flex items-center gap-2 font-mono text-[10px] font-black uppercase tracking-wider">
          <AlertTriangle className="w-4 h-4 shrink-0" />
          <span>{error}</span>
        </div>
      )}

      {result && (
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
          <div className="border border-border/30 bg-background/50 p-2 flex items-center justify-center max-h-64 overflow-hidden">
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img
              src={`data:${resultMime};base64,${result}`}
              alt="Converted"
              className="max-h-60 object-contain"
            />
          </div>
        </div>
      )}
    </div>
  );
}
