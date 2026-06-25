/**
 * @purpose Gestiona un esquema de mongoose y modelo para documentos, incluyendo campos para detalles de activos, información de inquilinos, versionado, almacenamiento, sensibilidad y gestión del ciclo de vida.
 * @purpose_en Defines a Mongoose schema and model for documents, including fields for asset details, tenant information, versioning, storage, sensitivity, and lifecycle management.
 * @refactorable false
 * @classification Type Definition
 * @complexity Low
 * @fingerprint exports:1,imports:2,sig:xf9k1x
 * @lastUpdated 2026-06-25T10:21:20.766Z
 */

import mongoose, { Schema, Document } from 'mongoose';
import { getTenantModel } from '@ajabadia/satellite-sdk/db';

export type TDocument = Document & {
  assetId: string;
  assetRef: string;
  tenantId: string;
  title: string;
  tags: string[];
  status: 'active' | 'deleted_pending_retention' | 'purge_due' | 'purged';
  currentVersionId: string;
  latestHash: string;
  storageProvider: 'cloudinary' | 's3Compatible' | 'googleDrive' | 'oneDrive';
  storageRefCurrent: string;
  retentionClass: string;
  sensitivityLevel: 'low' | 'medium' | 'high' | 'restricted';
  legalHold: boolean;
  version: number;
  createdAt: Date;
  updatedAt: Date;
  deletedAt?: Date;
  deletedBy?: string;
  purgeAt?: Date;
};

const DocumentSchema = new Schema<TDocument>(
  {
    assetId: { type: String, required: true, unique: true },
    assetRef: { type: String, required: true },
    tenantId: { type: String, required: true, index: true },
    title: { type: String, required: true },
    tags: { type: [String], default: [] },
    status: {
      type: String,
      enum: ['active', 'deleted_pending_retention', 'purge_due', 'purged'],
      default: 'active',
      required: true
    },
    currentVersionId: { type: String, required: true },
    latestHash: { type: String, required: true },
    storageProvider: {
      type: String,
      enum: ['cloudinary', 's3Compatible', 'googleDrive', 'oneDrive'],
      default: 'cloudinary',
      required: true
    },
    storageRefCurrent: { type: String, required: true },
    retentionClass: { type: String, required: true },
    sensitivityLevel: {
      type: String,
      enum: ['low', 'medium', 'high', 'restricted'],
      default: 'low',
      required: true
    },
    legalHold: { type: Boolean, default: false, required: true },
    version: { type: Number, default: 0, required: true },
    deletedAt: { type: Date },
    deletedBy: { type: String },
    purgeAt: { type: Date }
  },
  { timestamps: true }
);

// Indexes
DocumentSchema.index({ tenantId: 1, assetId: 1 }, { unique: true });
DocumentSchema.index({ tenantId: 1, status: 1, updatedAt: -1 });
DocumentSchema.index({ tenantId: 1, currentVersionId: 1 });
DocumentSchema.index({ tenantId: 1, title: 1 });
DocumentSchema.index({ tenantId: 1, tags: 1 });

export default getTenantModel<TDocument>('Document', DocumentSchema);
