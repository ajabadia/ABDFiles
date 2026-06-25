/**
 * @purpose Gestiona el entorno de prueba para la aplicación ABDFiles, simulando dependencias y configurando conexiones con MongoDB.
 * @purpose_en Sets up the test environment for the ABDFiles application, mocking dependencies and configuring MongoDB connections.
 * @refactorable true (contains too many state variables and UI parts)
 * @classification Helper Utility
 * @complexity Low
 * @fingerprint exports:2,imports:3,sig:rfutno
 * @lastUpdated 2026-06-25T10:22:24.857Z
 */

import { vi, beforeAll, afterAll } from 'vitest';
import mongoose from 'mongoose';
import { AsyncLocalStorage } from 'async_hooks';

// ── Mock @ajabadia/satellite-sdk ───────────────────────────────
// Must be at top level for Vitest hoisting. Use `var` to avoid temporal dead zone
// in the mock factory closure (var is hoisted and initialized to undefined).
// eslint-disable-next-line prefer-const, no-var
var tenantStorage = new AsyncLocalStorage<{
  tenantId: string;
  dbPrefix: string;
  isolationStrategy: string;
}>();

vi.mock('@ajabadia/satellite-sdk/auth-middleware', () => ({
  getIndustrialSession: vi.fn().mockResolvedValue({
    authenticated: true,
    user: {
      tenantId: 'test-tenant-123',
      dbPrefix: 't123',
      isolationStrategy: 'COLLECTION_PREFIX',
    },
  }),
}));

vi.mock('@ajabadia/satellite-sdk/logger', () => ({
  logger: {
    info: vi.fn(),
    warn: vi.fn(),
    error: vi.fn(),
  },
}));

vi.mock('@ajabadia/satellite-sdk/db', () => ({
  TenantContext: class {},
  tenantStorage,
  resolveTenantUri: (prefix: string) =>
    `mongodb://localhost:27017/abd_tenant_${prefix}`,
  getTenantConnection: (_dbPrefix: string, _isolationStrategy: string) => {
    const uri = `mongodb://localhost:27017/abd_tenant_${_dbPrefix}`;
    return mongoose.createConnection(uri);
  },
  ensureConnectionReady: async () => {},
  getTenantModel: (modelName: string, schema: mongoose.Schema) => {
    const defaultModel =
      (mongoose.models[modelName] as mongoose.Model<unknown>) ||
      mongoose.model(modelName, schema);
    const defaultCollectionName = defaultModel.collection.name;

    return new Proxy(defaultModel, {
      get(target, prop, receiver) {
        // eslint-disable-next-line @typescript-eslint/no-unsafe-assignment
        const store = tenantStorage.getStore();
        if (!store) {
          // eslint-disable-next-line @typescript-eslint/no-unsafe-return
          return Reflect.get(target, prop, receiver);
        }
        // Build a tenant-specific model using the tenant URI
        const uri = `mongodb://localhost:27017/abd_tenant_${store.dbPrefix}`;
        const conn = mongoose.createConnection(uri);
        const collectionName =
          store.isolationStrategy === 'COLLECTION_PREFIX'
            ? `${store.dbPrefix}_${defaultCollectionName}`
            : defaultCollectionName;
        const tenantModel = conn.model(modelName, schema, collectionName);
        const value = Reflect.get(tenantModel, prop);
        if (typeof value === 'function') {
          return value.bind(tenantModel);
        }
        return value;
      },
      construct(target, args, newTarget) {
        const store = tenantStorage.getStore();
        if (!store) {
          return Reflect.construct(
            target as new (...args: unknown[]) => unknown,
            args,
            newTarget,
          );
        }
        const uri = `mongodb://localhost:27017/abd_tenant_${store.dbPrefix}`;
        const conn = mongoose.createConnection(uri);
        const defaultCollectionName = (
          target as unknown as { collection: { name: string } }
        ).collection.name;
        const collectionName =
          store.isolationStrategy === 'COLLECTION_PREFIX'
            ? `${store.dbPrefix}_${defaultCollectionName}`
            : defaultCollectionName;
        const tenantModel = conn.model(modelName, schema, collectionName);
        return Reflect.construct(
          tenantModel as unknown as new (...args: unknown[]) => unknown,
          args,
          newTarget,
        );
      },
    }) as unknown as mongoose.Model<unknown>;
  },
  withTenantContext: async <T>(
    callback: () => Promise<T>,
    explicitContext?: {
      tenantId: string;
      dbPrefix: string;
      isolationStrategy: string;
    },
  ): Promise<T> => {
    if (explicitContext) {
      return tenantStorage.run(explicitContext, callback);
    }
    return callback();
  },
}));

export interface MockConnection {
  models: Record<string, unknown>;
  model: ReturnType<typeof vi.fn>;
  on: ReturnType<typeof vi.fn>;
  close: ReturnType<typeof vi.fn>;
  readyState: number;
}

export let mockConnection: MockConnection;
let originalMongoUri: string | undefined;

export function setupTestEnvironment(): void {
  beforeAll(() => {
    originalMongoUri = process.env.MONGODB_URI;
    process.env.MONGODB_URI = 'mongodb://localhost:27017/abd_analytics_test';

    mockConnection = {
      models: {},
      model: vi.fn().mockImplementation((name, schema, collection) => {
        const dummyModel = {
          modelName: name,
          schema,
          collection: { name: collection || name },
          find: vi.fn(),
          findOne: vi.fn(),
          create: vi.fn(),
        };
        mockConnection.models[name] = dummyModel;
        return dummyModel;
      }),
      on: vi.fn(),
      close: vi.fn().mockResolvedValue(undefined),
      readyState: 1,
    };

    vi.spyOn(mongoose, 'createConnection').mockReturnValue(
      mockConnection as unknown as mongoose.Connection,
    );
  });

  afterAll(() => {
    process.env.MONGODB_URI = originalMongoUri;
    vi.restoreAllMocks();
  });
}
