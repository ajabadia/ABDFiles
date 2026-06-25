/**
 * @purpose Gestiona un esquema de Mongoose para el modelo `AssetSpaceLink`, que representa la relación entre activos y espacios dentro de un inquilino.
 * @purpose_en Defines a Mongoose schema for the `AssetSpaceLink` model, which represents the relationship between assets and spaces within a tenant.
 * @refactorable false
 * @classification Type Definition
 * @complexity Low
 * @fingerprint exports:1,imports:2,sig:my0ofa
 * @lastUpdated 2026-06-25T10:21:00.466Z
 */

import mongoose, { Schema, Document } from 'mongoose';
import { getTenantModel } from '@ajabadia/satellite-sdk/db';

export type TAssetSpaceLink = Document & {
  linkId: string;
  tenantId: string;
  assetId: string;
  spaceId: string;
  spacePath: string;
  isPrimary: boolean;
  createdAt: Date;
  createdBy?: string;
};

const AssetSpaceLinkSchema = new Schema<TAssetSpaceLink>(
  {
    linkId: { type: String, required: true, unique: true },
    tenantId: { type: String, required: true, index: true },
    assetId: { type: String, required: true },
    spaceId: { type: String, required: true },
    spacePath: { type: String, required: true },
    isPrimary: { type: Boolean, default: false, required: true },
    createdBy: { type: String }
  },
  { timestamps: { createdAt: true, updatedAt: false } }
);

// Indexes
AssetSpaceLinkSchema.index({ tenantId: 1, spaceId: 1, assetId: 1 }, { unique: true });

export default getTenantModel<TAssetSpaceLink>('AssetSpaceLink', AssetSpaceLinkSchema);
