/**
 * @purpose Gestiona un esquema y modelo de Mongoose para `DocumentEvent`, que representa eventos relacionados con documentos en el sistema.
 * @purpose_en Defines a Mongoose schema and model for `DocumentEvent`, which represents events related to documents in the system.
 * @refactorable false
 * @classification Type Definition
 * @complexity Low
 * @fingerprint exports:1,imports:2,sig:kt3jcl
 * @lastUpdated 2026-06-25T10:21:30.136Z
 */

import mongoose, { Schema, Document } from 'mongoose';
import { getTenantModel } from '@ajabadia/satellite-sdk/db';

export type TDocumentEvent = Document & {
  eventId: string;
  tenantId: string;
  assetId: string;
  versionId?: string;
  type: string;
  actorId: string;
  correlationId: string;
  payload: Record<string, unknown>;
  createdAt: Date;
};

const DocumentEventSchema = new Schema<TDocumentEvent>(
  {
    eventId: { type: String, required: true, unique: true },
    tenantId: { type: String, required: true, index: true },
    assetId: { type: String, required: true },
    versionId: { type: String },
    type: { type: String, required: true },
    actorId: { type: String, required: true },
    correlationId: { type: String, required: true },
    payload: { type: Schema.Types.Mixed, default: {} }
  },
  { timestamps: { createdAt: true, updatedAt: false } }
);

// Indexes
DocumentEventSchema.index({ tenantId: 1, assetId: 1, createdAt: -1 });
DocumentEventSchema.index({ tenantId: 1, correlationId: 1 });

export default getTenantModel<TDocumentEvent>('DocumentEvent', DocumentEventSchema);
