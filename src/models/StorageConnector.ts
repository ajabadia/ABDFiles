/**
 * @purpose Proporciona una plantilla para almacenar conectores de almacenamiento, incluyendo campos como connectorId, tenantId, tipo de proveedor, estado, referencia a credenciales, escopos permitidos, política de retención, modo de auditoria, fecha de creación y fecha de actualización.
 * @purpose_en Defines a Mongoose schema and model for storage connectors, including fields like connectorId, tenantId, providerType, status, credentialsRef, allowedScopes, retentionPolicy, auditMode, createdAt, and updatedAt.
 * @refactorable false
 * @classification Type Definition
 * @complexity Low
 * @fingerprint exports:1,imports:2,sig:1hgdivn
 * @lastUpdated 2026-06-25T10:22:16.286Z
 */

import mongoose, { Schema, Document } from 'mongoose';
import { getTenantModel } from '@ajabadia/satellite-sdk/db';

export type TStorageConnector = Document & {
  connectorId: string;
  tenantId: string;
  providerType: 'cloudinary' | 's3Compatible' | 'googleDrive' | 'oneDrive';
  status: 'active' | 'inactive';
  credentialsRef: string;
  allowedScopes: string[];
  retentionPolicy: Record<string, unknown>;
  auditMode: string;
  createdAt: Date;
  updatedAt: Date;
};

const StorageConnectorSchema = new Schema<TStorageConnector>(
  {
    connectorId: { type: String, required: true, unique: true },
    tenantId: { type: String, required: true, index: true },
    providerType: {
      type: String,
      enum: ['cloudinary', 's3Compatible', 'googleDrive', 'oneDrive'],
      required: true
    },
    status: { type: String, enum: ['active', 'inactive'], default: 'active', required: true },
    credentialsRef: { type: String, required: true },
    allowedScopes: { type: [String], default: [] },
    retentionPolicy: { type: Schema.Types.Mixed, default: {} },
    auditMode: { type: String, default: 'standard' }
  },
  { timestamps: true }
);

export default getTenantModel<TStorageConnector>('StorageConnector', StorageConnectorSchema);
