import mongoose, { Schema, Document } from 'mongoose';
import { getTenantModel } from '@ajabadia/satellite-sdk';

export type TDeletionJob = Document & {
  jobId: string;
  tenantId: string;
  assetId: string;
  purgeAt: Date;
  status: 'pending' | 'completed' | 'failed';
  attempts: number;
  lastError?: string;
  createdAt: Date;
  updatedAt: Date;
};

const DeletionJobSchema = new Schema<TDeletionJob>(
  {
    jobId: { type: String, required: true, unique: true },
    tenantId: { type: String, required: true, index: true },
    assetId: { type: String, required: true },
    purgeAt: { type: Date, required: true },
    status: { type: String, enum: ['pending', 'completed', 'failed'], default: 'pending', required: true },
    attempts: { type: Number, default: 0, required: true },
    lastError: { type: String }
  },
  { timestamps: true }
);

// Indexes
DeletionJobSchema.index({ tenantId: 1, purgeAt: 1, status: 1 });

export default getTenantModel<TDeletionJob>('DeletionJob', DeletionJobSchema);
