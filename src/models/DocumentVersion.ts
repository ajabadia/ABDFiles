/**
 * @purpose Gestiona un esquema y modelo de Mongoose para documentaciones de versiones, incluyendo propiedades como versionId, tenantId, assetId y más.
 * @purpose_en Defines a Mongoose schema and model for document versions, including properties like versionId, tenantId, assetId, and more.
 * @refactorable false
 * @classification Type Definition
 * @complexity Low
 * @fingerprint exports:1,imports:2,sig:lzimbi
 * @lastUpdated 2026-06-25T10:21:40.785Z
 */

import mongoose, { Schema, Document } from 'mongoose';
import { getTenantModel } from '@ajabadia/satellite-sdk/db';

export type TDocumentVersion = Document & {
  versionId: string;
  tenantId: string;
  assetId: string;
  versionNumber: number;
  hash: string;
  checksumAlgorithm: 'SHA-256';
  storageRef: string;
  mimeType: string;
  sizeBytes: number;
  createdBy: string;
  isCurrent: boolean;
  supersedesVersionId?: string;
  deletedAt?: Date;
  createdAt: Date;
};

const DocumentVersionSchema = new Schema<TDocumentVersion>(
  {
    versionId: { type: String, required: true, unique: true },
    tenantId: { type: String, required: true, index: true },
    assetId: { type: String, required: true },
    versionNumber: { type: Number, required: true },
    hash: { type: String, required: true },
    checksumAlgorithm: { type: String, enum: ['SHA-256'], default: 'SHA-256', required: true },
    storageRef: { type: String, required: true },
    mimeType: { type: String, required: true },
    sizeBytes: { type: Number, required: true },
    createdBy: { type: String, required: true },
    isCurrent: { type: Boolean, default: false, required: true },
    supersedesVersionId: { type: String },
    deletedAt: { type: Date }
  },
  { timestamps: { createdAt: true, updatedAt: false } }
);

// Indexes
DocumentVersionSchema.index({ tenantId: 1, assetId: 1, versionNumber: -1 }, { unique: true });
DocumentVersionSchema.index({ tenantId: 1, hash: 1 });

export default getTenantModel<TDocumentVersion>('DocumentVersion', DocumentVersionSchema);
