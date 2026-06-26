#!/usr/bin/env node
import { copyFileSync, existsSync, mkdirSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';
import { createRequire } from 'module';

const __dirname = dirname(fileURLToPath(import.meta.url));
const root = join(__dirname, '..');
const publicDir = join(root, 'public');
const dest = join(publicDir, 'pandoc.wasm');

let src = '';

const pkgPath = join(root, 'node_modules', 'pandoc-wasm', 'src', 'pandoc.wasm');
if (existsSync(pkgPath)) {
  src = pkgPath;
} else {
  try {
    const req = createRequire(import.meta.url);
    const pkgEntry = req.resolve('pandoc-wasm');
    const pkgDir = dirname(pkgEntry);
    const wasmPath = join(pkgDir, 'pandoc.wasm');
    if (existsSync(wasmPath)) {
      src = wasmPath;
    }
  } catch {}
}

if (!src) {
  console.error('pandoc.wasm not found in node_modules');
  process.exit(1);
}

if (!existsSync(publicDir)) {
  mkdirSync(publicDir, { recursive: true });
}

copyFileSync(src, dest);
const size = (await import('fs')).statSync(dest).size;
console.log(`Copied pandoc.wasm (${(size / 1024 / 1024).toFixed(1)} MB) to public/`);
