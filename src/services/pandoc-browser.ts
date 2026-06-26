import { createPandocInstance } from './pandoc-core';

const CDN_WASM_URL = 'https://cdn.jsdelivr.net/npm/pandoc-wasm@1.1.0/dist/pandoc.wasm';

type PandocInstance = {
  convert: (
    options: Record<string, unknown>,
    stdin?: string | null,
    files?: Record<string, string | Blob>,
  ) => Promise<{ stdout: string; stderr: string; warnings: string[] }>;
  query: (options: Record<string, unknown>) => { stdout: string };
};

let instancePromise: Promise<PandocInstance> | null = null;

async function getInstance(): Promise<PandocInstance> {
  if (instancePromise) return instancePromise;
  instancePromise = (async () => {
    const res = await fetch(CDN_WASM_URL);
    if (!res.ok) throw new Error(`Failed to download pandoc.wasm (HTTP ${res.status})`);
    const wasmBinary = await res.arrayBuffer();
    const instance = await createPandocInstance(wasmBinary);
    return { convert: instance.convert, query: instance.query } as PandocInstance;
  })();
  return instancePromise;
}

export async function convertLocally(
  content: string,
  options: { from?: string; to: string; standalone?: boolean; toc?: boolean },
): Promise<{ output: string; warnings: string[]; stderr: string }> {
  const pandoc = await getInstance();
  const pandocOptions: Record<string, unknown> = {
    from: options.from || 'markdown',
    to: options.to,
  };
  if (options.standalone) pandocOptions.standalone = true;
  if (options.toc) pandocOptions.toc = true;

  const result = await pandoc.convert(pandocOptions, content);
  return {
    output: result.stdout || '',
    stderr: result.stderr || '',
    warnings: result.warnings || [],
  };
}

export function isBrowserPandocSupported(): boolean {
  return typeof WebAssembly !== 'undefined' && typeof fetch !== 'undefined';
}
