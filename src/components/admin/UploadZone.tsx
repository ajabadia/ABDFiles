'use client';

/**
 * @purpose Renders a arrastre y soltar zona de subida de archivos, gestiona subidas de archivos y muestra progreso de subida y mensajes.
 * @purpose_en Renders a drag-and-drop upload zone for files, handles file uploads, and displays upload progress and messages.
 * @refactorable true (contains too many state variables and UI parts)
 * @classification UI Component
 * @complexity Medium
 * @fingerprint exports:1,imports:3,sig:sb36d4
 * @lastUpdated 2026-06-21T14:52:16.104Z
 */

import React, { useState, useRef } from 'react';
import { UploadCloud, CheckCircle, AlertTriangle } from 'lucide-react';
import { useTranslations } from 'next-intl';

interface UploadZoneProps {
  onUploadSuccess: () => void;
}

export default function UploadZone({ onUploadSuccess }: UploadZoneProps) {
  const t = useTranslations('home');
  const [dragActive, setDragActive] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [progress, setProgress] = useState(0);
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  const handleDrag = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (e.type === 'dragenter' || e.type === 'dragover') {
      setDragActive(true);
    } else if (e.type === 'dragleave') {
      setDragActive(false);
    }
  };

  const uploadFile = async (file: File) => {
    setUploading(true);
    setProgress(10);
    setMessage(null);

    const formData = new FormData();
    formData.append('file', file);
    formData.append('title', file.name);
    formData.append('retentionClass', 'default');
    formData.append('sensitivityLevel', 'low');

    try {
      setProgress(40);
      const response = await fetch('/api/v1/documents', {
        method: 'POST',
        body: formData
      });

      setProgress(80);
      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.error || 'Upload failed');
      }

      setProgress(100);
      setMessage({ type: 'success', text: 'DOCUMENT_UPLOADED_SUCCESSFULLY' });
      onUploadSuccess();
    } catch (error: unknown) {
      console.error(error);
      const err = error as Error;
      setMessage({ type: 'error', text: err.message || 'Upload failed' });
    } finally {
      setTimeout(() => {
        setUploading(false);
        setProgress(0);
      }, 500);
    }
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);
    if (e.dataTransfer.files && e.dataTransfer.files[0]) {
      void uploadFile(e.dataTransfer.files[0]);
    }
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    e.preventDefault();
    if (e.target.files && e.target.files[0]) {
      void uploadFile(e.target.files[0]);
    }
  };

  const onButtonClick = () => {
    inputRef.current?.click();
  };

  return (
    <div className="w-full flex flex-col gap-4">
      <div
        onDragEnter={handleDrag}
        onDragOver={handleDrag}
        onDragLeave={handleDrag}
        onDrop={handleDrop}
        onClick={onButtonClick}
        className={`border border-dashed p-10 flex flex-col items-center justify-center gap-4 cursor-pointer transition-all duration-200 min-h-[160px] bg-card/20 rounded-none ${
          dragActive
            ? 'border-primary bg-primary/5 text-primary scale-[0.99]'
            : 'border-border hover:border-primary/50'
        }`}
      >
        <input
          ref={inputRef}
          type="file"
          className="hidden"
          onChange={handleChange}
          accept="application/pdf,image/*"
        />

        <UploadCloud className={`w-8 h-8 ${dragActive ? 'animate-bounce text-primary' : 'text-muted-foreground'}`} />
        <div className="flex flex-col items-center text-center">
          <span className="font-mono text-[10px] font-black uppercase tracking-widest text-foreground">
            {dragActive ? 'DROP_FILE_HERE' : 'DRAG_AND_DROP_FILE_OR_CLICK_TO_UPLOAD'}
          </span>
          <span className="text-[9px] font-mono text-muted-foreground uppercase tracking-wider mt-1">
            {t('status') || 'PDF OR IMAGE FILES UP TO 10MB'}
          </span>
        </div>
      </div>

      {uploading && (
        <div className="w-full bg-secondary/20 h-1.5 rounded-none overflow-hidden relative border border-border/30">
          <div
            className="bg-primary h-full transition-all duration-300"
            // eslint-disable-next-line react/forbid-component-props
            style={Object.assign({ width: progress + '%' })}
          />
        </div>
      )}

      {message && (
        <div
          className={`p-3 border font-mono text-[9px] font-black uppercase tracking-wider flex items-center gap-2 rounded-none ${
            message.type === 'success'
              ? 'border-emerald-500/20 bg-emerald-500/5 text-emerald-500'
              : 'border-destructive/20 bg-destructive/5 text-destructive'
          }`}
        >
          {message.type === 'success' ? <CheckCircle className="w-3.5 h-3.5" /> : <AlertTriangle className="w-3.5 h-3.5" />}
          <span>{message.text}</span>
        </div>
      )}
    </div>
  );
}
