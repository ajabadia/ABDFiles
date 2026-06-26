declare module 'pandoc-wasm' {
  export interface PandocOptions {
    from: string;
    to: string;
    standalone?: boolean;
    toc?: boolean;
    'toc-depth'?: number;
    'highlight-style'?: string;
    citeproc?: boolean;
    wrap?: 'auto' | 'none' | 'preserve';
    columns?: number;
    'embed-resources'?: boolean;
    'file-scope'?: boolean;
    [key: string]: unknown;
  }

  export interface PandocResult {
    stdout: string;
    stderr: string;
    warnings: string[];
  }

  export function convert(
    options: PandocOptions,
    stdin?: string | null,
    files?: Record<string, Blob | string>
  ): Promise<PandocResult>;

  export function query(
    options: Record<string, boolean | string>
  ): Promise<{ stdout: string }>;
}
