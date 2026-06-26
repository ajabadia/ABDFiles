import { convertDocument, type ConvertOptions, type ConvertResult } from './pandoc-service';
import { convertImage, isImageMime, isImageOutputFormat, type ImageConvertOptions, type ImageConvertResult } from './image-service';

const TEXT_MIME_TYPES = [
  'text/',
  'application/json',
  'application/xml',
  'application/x-yaml',
  'application/vnd.openxmlformats-officedocument.wordprocessingml',
  'application/vnd.openxmlformats-officedocument.presentationml',
  'application/vnd.oasis.opendocument',
  'application/epub+zip',
  'application/x-latex',
  'application/pdf',
];

function isTextDocument(mimeType: string): boolean {
  const base = mimeType.split(';')[0].trim();
  return TEXT_MIME_TYPES.some((t) => base.startsWith(t));
}

export interface ConvertRequest {
  content: string;
  mimeType: string;
  from?: string;
  to: string;
  standalone?: boolean;
  toc?: boolean;
  width?: number;
  height?: number;
  quality?: number;
  fit?: 'cover' | 'contain' | 'fill' | 'inside' | 'outside';
}

export interface ConvertResponse {
  output: string;
  mimeType: string;
  warnings?: string[];
  engine?: string;
  width?: number;
  height?: number;
}

export async function routeConversion(req: ConvertRequest): Promise<ConvertResponse> {
  const { content, mimeType, from, to, standalone, toc, width, height, quality, fit } = req;

  const baseMime = mimeType.split(';')[0].trim();

  if (isTextDocument(baseMime) || to === 'html' || to === 'markdown' || to === 'plain') {
    const options: ConvertOptions = {
      from: (from || mimeTypeToSimple(baseMime)) as ConvertOptions['from'],
      to: to as ConvertOptions['to'],
      standalone,
      toc,
    };
    const result: ConvertResult = await convertDocument(content, baseMime, options);
    return {
      output: result.output,
      mimeType: outputMimeType(to),
      warnings: result.warnings,
      engine: 'pandoc',
    };
  }

  if (isImageMime(baseMime) || isImageOutputFormat(to) || to.startsWith('image/')) {
    const imageOptions: ImageConvertOptions = {
      to,
      width,
      height,
      quality,
      fit,
    };

    const inputBuffer = Buffer.from(content, 'base64');
    const result: ImageConvertResult = await convertImage(inputBuffer, baseMime, imageOptions);

    return {
      output: result.output.toString('base64'),
      mimeType: result.mimeType,
      width: result.width,
      height: result.height,
      engine: 'sharp',
    };
  }

  throw new Error(
    `Unsupported conversion: ${mimeType} → ${to}. ` +
    'Supported: text documents (Pandoc), images (sharp), OCR (Tesseract.js, coming soon).',
  );
}

const MIME_TO_FORMAT: Record<string, string> = {
  'text/markdown': 'markdown',
  'text/html': 'html',
  'text/plain': 'plain',
  'text/csv': 'csv',
  'application/json': 'json',
  'application/x-yaml': 'yaml',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document': 'docx',
  'application/epub+zip': 'epub',
  'application/x-latex': 'latex',
  'application/pdf': 'pdf',
};

function mimeTypeToSimple(mime: string): string {
  const base = mime.split(';')[0].trim();
  return MIME_TO_FORMAT[base] || 'markdown';
}

function outputMimeType(format: string): string {
  const FORMAT_TO_MIME: Record<string, string> = {
    html: 'text/html',
    html5: 'text/html',
    markdown: 'text/markdown',
    md: 'text/markdown',
    plain: 'text/plain',
    docx: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
    epub: 'application/epub+zip',
    latex: 'application/x-latex',
    json: 'application/json',
    yaml: 'application/x-yaml',
    csv: 'text/csv',
    pdf: 'application/pdf',
    pptx: 'application/vnd.openxmlformats-officedocument.presentationml.presentation',
    odt: 'application/vnd.oasis.opendocument.text',
    rst: 'text/x-rst',
    asciidoc: 'text/x-asciidoc',
    mediawiki: 'text/mediawiki',
    org: 'text/x-org',
    gfm: 'text/markdown',
    commonmark: 'text/markdown',
  };
  return FORMAT_TO_MIME[format] || 'application/octet-stream';
}
