#!/usr/bin/env node

const GITHUB_API = 'https://api.github.com/repos/jgm/pandoc/releases/latest';
const NPM_PACKAGE = 'pandoc-wasm';

async function checkVersion() {
  console.log(`\n  Pandoc Version Checker`);
  console.log(`  ======================\n`);

  let localVersion = '?';
  try {
    const pkgPath = (await import('node:url')).pathToFileURL(
      (await import('node:path')).join(
        (await import('node:fs')).realpathSync(
          new URL('../node_modules/pandoc-wasm', import.meta.url)
        ),
        'package.json'
      )
    );
    const pkg = await import(pkgPath.href, { with: { type: 'json' } });
    localVersion = pkg.default.version;
  } catch {
    try {
      const pkg = JSON.parse(
        (await import('node:fs')).readFileSync(
          (await import('node:path')).join(
            (await import('node:fs')).realpathSync(
              new URL('../node_modules/pandoc-wasm', import.meta.url)
            ),
            'package.json'
          ),
          'utf-8'
        )
      );
      localVersion = pkg.version;
    } catch {}
  }

  try {
    const { query } = await import('pandoc-wasm');
    const info = await query({ version: true });
    console.log(`  Pandoc WASM binary  : ${(info.stdout || '').trim()}`);
  } catch (e) {
    console.log(`  Pandoc WASM binary  : unable to query (${e.message})`);
  }

  console.log(`  npm package version : ${localVersion}\n`);

  console.log(`  --- Upstream Checks ---\n`);

  try {
    const npmRes = await fetch(`https://registry.npmjs.org/${NPM_PACKAGE}/latest`);
    if (npmRes.ok) {
      const npmData = await npmRes.json();
      const latest = npmData.version;
      const isLatest = latest === localVersion;
      console.log(`  npm  ${NPM_PACKAGE}     : ${latest}${isLatest ? ' (current)' : ' ← NEW!'}`);
      if (!isLatest) {
        console.log(`  -> Update: pnpm add ${NPM_PACKAGE}@${latest}`);
      }
    }
  } catch (e) {
    console.log(`  npm  ${NPM_PACKAGE}     : error (${e.message})`);
  }

  try {
    const ghRes = await fetch(GITHUB_API, {
      headers: { 'Accept': 'application/vnd.github.v3+json', 'User-Agent': 'abd-files' },
    });
    if (ghRes.ok) {
      const ghData = await ghRes.json();
      console.log(`  GitHub pandoc release: ${ghData.tag_name}`);
      console.log(`       ${ghData.html_url}`);
      console.log(`       published: ${ghData.published_at}`);
    } else {
      console.log(`  GitHub pandoc release: HTTP ${ghRes.status}`);
    }
  } catch (e) {
    console.log(`  GitHub pandoc release: error (${e.message})`);
  }

  console.log(`\n  ======================\n`);
}

checkVersion().catch((e) => {
  console.error(e);
  process.exit(1);
});
