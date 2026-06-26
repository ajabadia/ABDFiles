import { createRequire } from 'module';
import { readFileSync, existsSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath, pathToFileURL } from 'url';

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
  convert: (options: Record<string, unknown>, stdin?: string | null, files?: Record<string, Blob | string>) => Promise<{ stdout: string; stderr: string; warnings: string[] }>;
  query: (options: Record<string, boolean | string>) => Promise<{ stdout: string }>;
};

let instancePromise: Promise<PandocInstance> | null = null;

function findWasmPath(): string {
  const candidates = [
    join(process.cwd(), 'public', 'pandoc.wasm'),
    join(dirname(fileURLToPath(import.meta.url)), '..', '..', '..', 'public', 'pandoc.wasm'),
    join(dirname(fileURLToPath(import.meta.url)), '..', '..', 'public', 'pandoc.wasm'),
  ];

  for (const p of candidates) {
    if (existsSync(p)) return p;
  }

  const req = createRequire(import.meta.url);
  const pkgEntry = req.resolve('pandoc-wasm');
  const pkgDir = dirname(pkgEntry);
  const wasmInPkg = join(pkgDir, 'pandoc.wasm');
  if (existsSync(wasmInPkg)) return wasmInPkg;

  throw new Error('pandoc.wasm not found in any expected location');
}

async function getPandoc(): Promise<PandocInstance> {
  if (instancePromise) return instancePromise;

  instancePromise = (async () => {
    const wasmPath = findWasmPath();
    const wasmBinary = readFileSync(wasmPath).buffer;

    const __filename = fileURLToPath(import.meta.url);
    const req = createRequire(__filename);
    const pkgEntry = req.resolve('pandoc-wasm');
    const pkgDir = dirname(pkgEntry);
    const coreUrl = join(pkgDir, 'core.js');

    const coreModule = await import(pathToFileURL(coreUrl).href);
    const instance = await coreModule.createPandocInstance(wasmBinary);

    return {
      convert: instance.convert,
      query: instance.query,
    };
  })();

  return instancePromise;
}

export async function getPandocInfo(): Promise<PandocInfo> {
  const pandoc = await getPandoc();
  const versionResult = await pandoc.query({ 'version': true });
  const inputResult = await pandoc.query({ 'list-input-formats': true });
  const outputResult = await pandoc.query({ 'list-output-formats': true });

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
  options: ConvertOptions
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
