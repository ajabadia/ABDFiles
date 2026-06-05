import mongoose, { Schema, Document } from 'mongoose';
import { getTenantModel } from '@ajabadia/satellite-sdk';

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
