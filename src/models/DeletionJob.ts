/**
 * @purpose Gestiona la eliminación de tareas de activos en un inquilino, incluyendo seguimiento del estado y programación.
 * @purpose_en Manages deletion jobs for assets in a tenant, including job status tracking and scheduling.
 * @refactorable false
 * @classification Type Definition
 * @complexity Low
 * @fingerprint exports:1,imports:2,sig:1omvah9
 * @lastUpdated 2026-06-25T10:21:08.585Z
 */

import mongoose, { Schema, Document } from 'mongoose';
import { getTenantModel } from '@ajabadia/satellite-sdk/db';

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
