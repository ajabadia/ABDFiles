/**
 * @purpose Gestiona conversión entre formatos de documentos utilizando Pandoc.
 * @purpose_en Manages conversion between various document formats using Pandoc.
 * @refactorable true (contains too many state variables and UI parts)
 * @classification Business Service
 * @complexity Medium
 * @fingerprint exports:6,imports:1,sig:1dqojps
 * @lastUpdated 2026-06-26T16:35:20.807Z
 */

import { convert, query } from 'pandoc-wasm';

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

let infoCache: PandocInfo | null = null;

export async function getPandocInfo(): Promise<PandocInfo> {
  if (infoCache) return infoCache;
  const result = await query({ 'version': true });
  const inputFormats = await query({ 'list-input-formats': true });
  const outputFormats = await query({ 'list-output-formats': true });
  infoCache = {
    version: (result.stdout || '').trim(),
    inputFormats: (inputFormats.stdout || '').trim().split('\n'),
    outputFormats: (outputFormats.stdout || '').trim().split('\n'),
  };
  return infoCache;
}

function buildOptions(opts: ConvertOptions) {
  const args: Record<string, unknown> = {
    from: opts.from,
    to: opts.to,
  };
  if (opts.standalone) args.standalone = true;
  if (opts.embedResources) args['embed-resources'] = true;
  if (opts.toc) args.toc = true;
  if (opts.tocDepth) args['toc-depth'] = opts.tocDepth;
  if (opts.highlightStyle) args['highlight-style'] = opts.highlightStyle;
  if (opts.citeMethod) args['citeproc'] = true;
  if (opts.wrap) args.wrap = opts.wrap;
  if (opts.columns) args.columns = opts.columns;
  if (opts.extraArgs) {
    for (const [k, v] of Object.entries(opts.extraArgs)) {
      args[k] = v;
    }
  }
  return args as import('pandoc-wasm').PandocOptions;
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
  options: ConvertOptions
): Promise<ConvertResult> {
  const pandocOptions = buildOptions(options);

  let stdin: string | null = null;
  const files: Record<string, Blob | string> = {};

  const inputExt = options.from || mimeTypeToFormat(mimeType);

  if (typeof content === 'string' || isTextFormat(inputExt)) {
    const textContent = typeof content === 'string' ? content : content.toString('utf-8');
    stdin = textContent;
  } else {
    const filename = `input.${inputExt}`;
    files[filename] = new Blob([content as unknown as BlobPart]);
    pandocOptions['file-scope'] = true;
  }

  const result = await convert(pandocOptions, stdin, files);

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
