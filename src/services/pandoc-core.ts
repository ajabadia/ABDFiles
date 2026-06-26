import {
  ConsoleStdout,
  Directory,
  File,
  Inode,
  OpenFile,
  PreopenDirectory,
  WASI,
} from '@bjorn3/browser_wasi_shim';

export async function createPandocInstance(wasmBinary: ArrayBuffer) {
  const args = ['pandoc.wasm', '+RTS', '-H64m', '-RTS'];
  const env: string[] = [];
  const fileSystem = new Map<string, Directory | File>();
  const fds = [
    new OpenFile(new File(new Uint8Array(), { readonly: true })),
    ConsoleStdout.lineBuffered((msg: string) => console.log(`[WASI stdout] ${msg}`)),
    ConsoleStdout.lineBuffered((msg: string) => console.warn(`[WASI stderr] ${msg}`)),
    new PreopenDirectory('/', fileSystem),
  ];
  const options = { debug: false };
  const wasi = new WASI(args, env, fds, options);

  const wasmResult = await WebAssembly.instantiate(wasmBinary, {
    wasi_snapshot_preview1: wasi.wasiImport,
  });
  const instance = wasmResult.instance as unknown as { exports: Record<string, CallableFunction | WebAssembly.Memory> };

  wasi.initialize(wasmResult.instance as unknown as { exports: { memory: WebAssembly.Memory } });
  (instance.exports.__wasm_call_ctors as CallableFunction)();

  function memView() {
    return new DataView((instance.exports.memory as WebAssembly.Memory).buffer);
  }

  const argc_ptr = (instance.exports.malloc as CallableFunction)(4) as number;
  memView().setUint32(argc_ptr, args.length, true);
  const argv = (instance.exports.malloc as CallableFunction)(4 * (args.length + 1)) as number;
  for (let i = 0; i < args.length; ++i) {
    const arg = (instance.exports.malloc as CallableFunction)(args[i].length + 1) as number;
    new TextEncoder().encodeInto(
      args[i],
      new Uint8Array(
        (instance.exports.memory as WebAssembly.Memory).buffer,
        arg,
        args[i].length,
      ),
    );
    memView().setUint8(arg + args[i].length, 0);
    memView().setUint32(argv + 4 * i, arg, true);
  }
  memView().setUint32(argv + 4 * args.length, 0, true);
  const argv_ptr = (instance.exports.malloc as CallableFunction)(4) as number;
  memView().setUint32(argv_ptr, argv, true);

  (instance.exports.hs_init_with_rtsopts as CallableFunction)(argc_ptr, argv_ptr);

  async function addFile(filename: string, data: string | Blob, readonly: boolean) {
    let uint8Array: Uint8Array;
    if (typeof data === 'string') {
      uint8Array = new TextEncoder().encode(data);
    } else {
      const buffer = await data.arrayBuffer();
      uint8Array = new Uint8Array(buffer);
    }
    const file = new File(uint8Array, { readonly });
    fileSystem.set(filename, file);
  }

  function query(options: Record<string, unknown>) {
    const opts_str = JSON.stringify(options);
    const encoded = new TextEncoder().encode(opts_str);
    const opts_ptr = (instance.exports.malloc as CallableFunction)(encoded.length) as number;
    new TextEncoder().encodeInto(
      opts_str,
      new Uint8Array(
        (instance.exports.memory as WebAssembly.Memory).buffer,
        opts_ptr,
        encoded.length,
      ),
    );

    fileSystem.clear();
    const out_file = new File(new Uint8Array(), { readonly: false });
    const err_file = new File(new Uint8Array(), { readonly: false });
    fileSystem.set('stdout', out_file);
    fileSystem.set('stderr', err_file);

    (instance.exports.query as CallableFunction)(opts_ptr, encoded.length);

    const err_text = new TextDecoder('utf-8', { fatal: true }).decode(err_file.data);
    if (err_text) {
      console.log(err_text);
    }
    const out_text = new TextDecoder('utf-8', { fatal: true }).decode(out_file.data);
    return JSON.parse(out_text);
  }

  async function convert(
    options: Record<string, unknown>,
    stdin: string | null,
    files: Record<string, string | Blob>,
  ) {
    const opts_str = JSON.stringify(options);
    const encoded = new TextEncoder().encode(opts_str);
    const opts_ptr = (instance.exports.malloc as CallableFunction)(encoded.length) as number;
    new TextEncoder().encodeInto(
      opts_str,
      new Uint8Array(
        (instance.exports.memory as WebAssembly.Memory).buffer,
        opts_ptr,
        encoded.length,
      ),
    );

    files = { ...files };

    fileSystem.clear();
    const in_file = new File(new Uint8Array(), { readonly: true });
    const out_file = new File(new Uint8Array(), { readonly: false });
    const err_file = new File(new Uint8Array(), { readonly: false });
    const warnings_file = new File(new Uint8Array(), { readonly: false });
    fileSystem.set('stdin', in_file);
    fileSystem.set('stdout', out_file);
    fileSystem.set('stderr', err_file);
    fileSystem.set('warnings', warnings_file);

    const knownFiles = new Set<string>(['stdin', 'stdout', 'stderr', 'warnings']);

    for (const filename in files) {
      await addFile(filename, files[filename], true);
      knownFiles.add(filename);
    }

    const outputFileName = (options['output-file'] as string) || null;
    const extractMediaPath = (options['extract-media'] as string) || null;

    if (outputFileName) {
      await addFile(outputFileName, new Blob(), false);
      knownFiles.add(outputFileName);
    }

    if (extractMediaPath && extractMediaPath.endsWith('.zip')) {
      await addFile(extractMediaPath, new Blob(), false);
      knownFiles.add(extractMediaPath);
    }

    if (stdin) {
      in_file.data = new TextEncoder().encode(stdin);
    }

    (instance.exports.convert as CallableFunction)(opts_ptr, encoded.length);

    if (options['output-file']) {
      const outputFile = fileSystem.get(options['output-file'] as string);
      if (outputFile && 'data' in outputFile && outputFile.data && outputFile.data.length > 0) {
        files[options['output-file'] as string] = new Blob([outputFile.data as unknown as BlobPart]);
      }
    }

    if (options['extract-media']) {
      const mediaFile = fileSystem.get(options['extract-media'] as string);
      if (mediaFile && 'data' in mediaFile && mediaFile.data && mediaFile.data.length > 0) {
        files[options['extract-media'] as string] = new Blob([mediaFile.data as unknown as BlobPart], {
          type: 'application/zip',
        });
      }
    }

    const mediaFiles: Record<string, Blob> = {};
    const collectNewFiles = (map: Map<string, Inode>, prefix: string) => {
      for (const [name, entry] of map.entries()) {
        const path = prefix ? `${prefix}/${name}` : name;
        if (entry instanceof Directory) {
          collectNewFiles(entry.contents, path);
        } else if (
          !knownFiles.has(path) &&
          (entry as File).data &&
          (entry as File).data.length > 0
        ) {
          const fileEntry = entry as File;
          const blob = new Blob([fileEntry.data as unknown as BlobPart]);
          files[path] = blob;
          mediaFiles[path] = blob;
        }
      }
    };
    collectNewFiles(fileSystem, '');

    const rawWarnings = new TextDecoder('utf-8', { fatal: true }).decode(warnings_file.data);
    let warnings: string[] = [];
    if (rawWarnings) {
      try {
        warnings = JSON.parse(rawWarnings);
      } catch (e) {
        console.warn('Failed to parse warnings:', e);
      }
    }

    return {
      stdout: new TextDecoder('utf-8', { fatal: true }).decode(out_file.data),
      stderr: new TextDecoder('utf-8', { fatal: true }).decode(err_file.data),
      warnings,
      files,
      mediaFiles,
    };
  }

  return { convert, query };
}
