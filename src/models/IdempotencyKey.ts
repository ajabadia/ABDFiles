/**
 * @purpose Gestiona un esquema de Mongoose y modelo para administrar claves de idempotencia, que se utilizan para asegurar que las solicitudes API pueden ser reintentadas de manera segura sin causar acciones duplicadas.
 * @purpose_en Defines a Mongoose schema and model for managing idempotency keys, which are used to ensure that API requests can be safely retried without causing duplicate actions.
 * @refactorable false
 * @classification Type Definition
 * @complexity Low
 * @fingerprint exports:1,imports:2,sig:5ubney
 * @lastUpdated 2026-06-25T10:21:54.385Z
 */

import mongoose, { Schema, Document } from 'mongoose';
import { getTenantModel } from '@ajabadia/satellite-sdk/db';

export type TIdempotencyKey = Document & {
  key: string;
  tenantId: string;
  responseBody: Record<string, unknown>;
  statusCode: number;
  createdAt: Date;
};

const IdempotencyKeySchema = new Schema<TIdempotencyKey>(
  {
    key: { type: String, required: true, unique: true },
    tenantId: { type: String, required: true, index: true },
    responseBody: { type: Schema.Types.Mixed, required: true },
    statusCode: { type: Number, required: true },
    createdAt: { type: Date, default: Date.now, expires: 86400 } // TTL 24 hours
  }
);

IdempotencyKeySchema.index({ key: 1 }, { unique: true });

export default getTenantModel<TIdempotencyKey>('IdempotencyKey', IdempotencyKeySchema);
