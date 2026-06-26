import { NextRequest, NextResponse } from 'next/server';
import { ensureIndustrialAccess } from '@ajabadia/satellite-sdk/auth-middleware';
import { routeConversion } from '@/services/conversion-router';

export const revalidate = 0;
export const maxDuration = 60;

export async function OPTIONS() {
  return NextResponse.json({ methods: ['POST', 'OPTIONS'] });
}

export async function GET() {
  return NextResponse.json({
    version: '1',
    engines: ['pandoc', 'sharp'],
    formats: {
      input: {
        text: ['docx', 'epub', 'html', 'markdown', 'latex', 'pdf', 'csv', 'json', 'yaml', 'rst', 'asciidoc', 'mediawiki', 'org', 'plain', 'gfm', 'commonmark'],
        image: ['jpeg', 'png', 'webp', 'avif', 'tiff', 'gif', 'heif'],
      },
      output: {
        text: ['html', 'markdown', 'plain', 'docx', 'epub', 'latex', 'csv', 'json', 'yaml', 'pptx', 'odt', 'rst', 'asciidoc'],
        image: ['image/jpeg', 'image/png', 'image/webp', 'image/avif', 'image/tiff', 'image/gif', 'image/heif'],
      },
    },
  });
}

export async function POST(request: NextRequest) {
  try {
    await ensureIndustrialAccess();

    const body = await request.json();
    const { content, mimeType, from, to, standalone, toc, width, height, quality, fit } = body;

    if (!content || typeof content !== 'string') {
      return NextResponse.json({ error: 'Missing or invalid content' }, { status: 400 });
    }
    if (!to || typeof to !== 'string') {
      return NextResponse.json({ error: 'Missing or invalid target format (to)' }, { status: 400 });
    }

    const result = await routeConversion({
      content,
      mimeType: mimeType || 'text/plain',
      from,
      to: to.toLowerCase(),
      standalone,
      toc,
      width,
      height,
      quality,
      fit,
    });

    return NextResponse.json(result);
  } catch (error: unknown) {
    const err = error as Error;
    return NextResponse.json({ error: err.message }, { status: 500 });
  }
}
