import { existsSync, readFileSync, writeFileSync } from 'fs';
import { join } from 'path';
import { createPandocInstance } from './pandoc-core';

export type PandocFormat =
  | 'markdown' | 'md'
  | 'html' | 'html5' | 'html4'
  | 'docx'
  | 'epub' | 'epub2' | 'epub3'
  | 'latex' | 'pdf' | 'typst'
  | 'plain' | 'textile'
  | 'rst' | 'org' | 'asciidoc' | 'asciidoctor'
  | 'mediawiki' | 'dokuwiki'
  | 'gfm' | 'commonmark'
  | 'csv' | 'tsv'
  | 'json' | 'yaml'
  | 'man' | 'pptx' | 'odt'
  | 'opml' | 'fb2'
  | string;

export interface ConvertOptions {
  from: PandocFormat;
  to: PandocFormat;
  standalone?: boolean;
  embedResources?: boolean;
  toc?: boolean;
  tocDepth?: number;
  highlightStyle?: string;
  citeMethod?: 'citeproc';
  wrap?: 'auto' | 'none' | 'preserve';
  columns?: number;
  extraArgs?: Record<string, string | boolean | number>;
}

export interface ConvertResult {
  output: string;
  stderr: string;
  warnings: string[];
}

export interface PandocInfo {
  version: string;
  inputFormats: string[];
  outputFormats: string[];
}

type PandocInstance = {
  convert: (
    options: Record<string, unknown>,
    stdin?: string | null,
    files?: Record<string, string | Blob>,
  ) => Promise<{ stdout: string; stderr: string; warnings: string[] }>;
  query: (options: Record<string, unknown>) => { stdout: string };
};

let instancePromise: Promise<PandocInstance> | null = null;
let initError: Error | null = null;

const CDN_WASM_URL = 'https://cdn.jsdelivr.net/npm/pandoc-wasm@1.1.0/dist/pandoc.wasm';

async function loadWasmBinary(): Promise<ArrayBuffer> {
  const localPath = join(process.cwd(), 'public', 'pandoc.wasm');
  if (existsSync(localPath)) {
    return readFileSync(localPath).buffer;
  }

  const tmpPath = '/tmp/pandoc.wasm';
  if (existsSync(tmpPath)) {
    return readFileSync(tmpPath).buffer;
  }

  const baseUrl = process.env.VERCEL_URL
    ? `https://${process.env.VERCEL_URL}`
    : process.env.NEXT_PUBLIC_BASE_URL || 'http://localhost:5005';

  try {
    const res = await fetch(`${baseUrl}/pandoc.wasm`, { signal: AbortSignal.timeout(10000) });
    if (res.ok) {
      const buf = await res.arrayBuffer();
      try { writeFileSync(tmpPath, Buffer.from(buf)); } catch {}
      return buf;
    }
  } catch {}

  const cdnRes = await fetch(CDN_WASM_URL, { signal: AbortSignal.timeout(30000) });
  if (!cdnRes.ok) throw new Error(
    `Failed to load pandoc.wasm from CDN (HTTP ${cdnRes.status}). ` +
    'Run `node scripts/copy-pandoc-wasm.mjs` locally to copy it to public/.'
  );
  const buf = await cdnRes.arrayBuffer();
  try { writeFileSync(tmpPath, Buffer.from(buf)); } catch {}
  return buf;
}

async function getPandoc(): Promise<PandocInstance> {
  if (instancePromise) return instancePromise;
  if (initError) throw initError;

  instancePromise = (async () => {
    const wasmBinary = await loadWasmBinary();
    const instance = await createPandocInstance(wasmBinary);
    return {
      convert: instance.convert,
      query: instance.query,
    } as PandocInstance;
  })();

  try {
    return await instancePromise;
  } catch (e) {
    initError = e as Error;
    instancePromise = null;
    throw e;
  }
}

export async function getPandocInfo(): Promise<PandocInfo> {
  const pandoc = await getPandoc();
  const versionResult = pandoc.query({ version: true });
  const inputResult = pandoc.query({ 'list-input-formats': true });
  const outputResult = pandoc.query({ 'list-output-formats': true });

  return {
    version: (versionResult.stdout || '').trim(),
    inputFormats: (inputResult.stdout || '').trim().split('\n'),
    outputFormats: (outputResult.stdout || '').trim().split('\n'),
  };
}

function isTextFormat(format: string): boolean {
  const textFormats = new Set([
    'markdown', 'md', 'html', 'html5', 'html4', 'plain', 'textile',
    'rst', 'org', 'asciidoc', 'asciidoctor', 'mediawiki', 'dokuwiki',
    'gfm', 'commonmark', 'csv', 'tsv', 'json', 'yaml', 'man', 'opml',
    'latex',
  ]);
  return textFormats.has(format);
}

export async function convertDocument(
  content: string | Buffer,
  mimeType: string,
  options: ConvertOptions,
): Promise<ConvertResult> {
  const pandoc = await getPandoc();

  const pandocOptions: Record<string, unknown> = {
    from: options.from,
    to: options.to,
  };
  if (options.standalone) pandocOptions.standalone = true;
  if (options.embedResources) pandocOptions['embed-resources'] = true;
  if (options.toc) pandocOptions.toc = true;
  if (options.tocDepth) pandocOptions['toc-depth'] = options.tocDepth;
  if (options.highlightStyle) pandocOptions['highlight-style'] = options.highlightStyle;
  if (options.citeMethod) pandocOptions.citeproc = true;
  if (options.wrap) pandocOptions.wrap = options.wrap;
  if (options.columns) pandocOptions.columns = options.columns;
  if (options.extraArgs) {
    for (const [k, v] of Object.entries(options.extraArgs)) {
      pandocOptions[k] = v;
    }
  }

  let stdin: string | null = null;
  const files: Record<string, string | Blob> = {};

  const inputExt = options.from || mimeTypeToFormat(mimeType);

  if (typeof content === 'string' || isTextFormat(inputExt)) {
    const textContent = typeof content === 'string' ? content : content.toString('utf-8');
    stdin = textContent;
  } else {
    const filename = `input.${inputExt}`;
    files[filename] = new Blob([content as unknown as BlobPart]);
    pandocOptions['file-scope'] = true;
  }

  const result = await pandoc.convert(pandocOptions, stdin, files);

  return {
    output: result.stdout || '',
    stderr: result.stderr || '',
    warnings: result.warnings || [],
  };
}

const MIME_FORMAT_MAP: Record<string, string> = {
  'text/markdown': 'markdown',
  'text/html': 'html',
  'text/plain': 'plain',
  'text/csv': 'csv',
  'text/tab-separated-values': 'tsv',
  'text/yaml': 'yaml',
  'application/json': 'json',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document': 'docx',
  'application/vnd.openxmlformats-officedocument.presentationml.presentation': 'pptx',
  'application/vnd.oasis.opendocument.text': 'odt',
  'application/epub+zip': 'epub',
  'application/x-latex': 'latex',
  'application/pdf': 'pdf',
  'text/x-rst': 'rst',
  'text/x-asciidoc': 'asciidoc',
  'text/mediawiki': 'mediawiki',
  'text/x-org': 'org',
};

function mimeTypeToFormat(mimeType: string): string {
  return MIME_FORMAT_MAP[mimeType] || 'markdown';
}

export function isPandocAvailable(): boolean {
  return existsSync(join(process.cwd(), 'public', 'pandoc.wasm'));
}
