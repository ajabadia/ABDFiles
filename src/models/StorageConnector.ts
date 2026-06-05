import mongoose, { Schema, Document } from 'mongoose';
import { getTenantModel } from '@ajabadia/satellite-sdk';

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
