import mongoose, { Schema, Document } from 'mongoose';
import { getTenantModel } from '@ajabadia/satellite-sdk';

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
