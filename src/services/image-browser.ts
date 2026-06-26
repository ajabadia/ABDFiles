const BROWSER_FORMATS = ['image/jpeg', 'image/png', 'image/webp', 'image/gif', 'image/bmp'] as const;

export function isBrowserImageSupported(): boolean {
  return typeof document !== 'undefined' && typeof HTMLCanvasElement !== 'undefined';
}

export function isFormatSupportedInBrowser(mimeType: string): boolean {
  return BROWSER_FORMATS.includes(mimeType as typeof BROWSER_FORMATS[number]);
}

export function convertImageLocally(
  file: File,
  options: { to: string; quality?: number },
): Promise<Blob> {
  return new Promise((resolve, reject) => {
    const img = new Image();
    const url = URL.createObjectURL(file);

    img.onload = () => {
      URL.revokeObjectURL(url);
      try {
        const canvas = document.createElement('canvas');
        canvas.width = img.width;
        canvas.height = img.height;
        const ctx = canvas.getContext('2d');
        if (!ctx) {
          reject(new Error('Canvas context not available'));
          return;
        }
        ctx.drawImage(img, 0, 0);

        const formatMap: Record<string, string> = {
          'image/jpeg': 'image/jpeg',
          'image/png': 'image/png',
          'image/webp': 'image/webp',
          'image/gif': 'image/gif',
          'image/bmp': 'image/bmp',
        };

        const mimeType = formatMap[options.to] || 'image/png';
        canvas.toBlob(
          (blob) => {
            if (blob) resolve(blob);
            else reject(new Error('Failed to encode image'));
          },
          mimeType,
          options.quality ? options.quality / 100 : undefined,
        );
      } catch (e) {
        reject(e);
      }
    };

    img.onerror = () => {
      URL.revokeObjectURL(url);
      reject(new Error('Failed to load image'));
    };

    img.src = url;
  });
}
