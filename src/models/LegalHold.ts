import mongoose, { Schema, Document } from 'mongoose';
import { getTenantModel } from '@ajabadia/satellite-sdk';

export type TLegalHold = Document & {
  holdId: string;
  tenantId: string;
  assetId: string;
  reason: string;
  status: 'active' | 'released';
  createdBy: string;
  createdAt: Date;
  releasedAt?: Date;
};

const LegalHoldSchema = new Schema<TLegalHold>(
  {
    holdId: { type: String, required: true, unique: true },
    tenantId: { type: String, required: true, index: true },
    assetId: { type: String, required: true },
    reason: { type: String, required: true },
    status: { type: String, enum: ['active', 'released'], default: 'active', required: true },
    createdBy: { type: String, required: true },
    releasedAt: { type: Date }
  },
  { timestamps: { createdAt: true, updatedAt: false } }
);

// Indexes
LegalHoldSchema.index({ tenantId: 1, assetId: 1, status: 1 });

export default getTenantModel<TLegalHold>('LegalHold', LegalHoldSchema);
