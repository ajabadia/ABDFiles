/**
 * @purpose Gestiona el proceso de eliminación de datos para cumplir con la normativa GDPR, incluyendo la eliminación de datos por usuario y teniente.
 * @purpose_en Handles the purge operation for GDPR compliance, including per-user and tenant data deletion.
 * @refactorable true (contains too many state variables and UI parts)
 * @classification Business Service
 * @complexity Medium
 * @fingerprint exports:1,imports:3,sig:12hyktu
 * @lastUpdated 2026-06-25T11:51:13.888Z
 */

import { NextRequest, NextResponse } from 'next/server';
import { connectDB, getTenantConnection } from '@ajabadia/satellite-sdk/db';
import { z } from 'zod';

const TenantPurgeSchema = z.object({
  tenantId: z.string().min(1),
  dbPrefix: z.string().min(1),
  isolationStrategy: z.enum(['COLLECTION_PREFIX', 'DATABASE_PER_TENANT']),
});

const UserPurgeSchema = z.object({
  tenantId: z.string().min(1),
  userId: z.string().min(1),
});

export async function POST(request: NextRequest) {
  try {
    const authHeader = request.headers.get('authorization');
    const secret = authHeader?.startsWith('Bearer ') ? authHeader.substring(7) : authHeader;
    if (!secret || secret !== process.env.ABD_INTERNAL_SECRET) {
      return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });
    }

    const raw = await request.json();

    // 1. Per-user GDPR purge (right to be forgotten)
    if (raw.userId) {
      const body = UserPurgeSchema.parse(raw);
      await connectDB();
      const conn = getTenantConnection(body.tenantId, 'COLLECTION_PREFIX');

      // Anonymize deletedBy in documents
      const Document = conn.models.Document;
      let docResult = { modifiedCount: 0 };
      if (Document) {
        const result = await Document.updateMany(
          { tenantId: body.tenantId, deletedBy: body.userId },
          { $set: { deletedBy: '[GDPR_ERASED]' } }
        );
        docResult = { modifiedCount: result.modifiedCount };
      }

      // Anonymize actorId in document events
      const DocumentEvent = conn.models.DocumentEvent;
      let eventResult = { modifiedCount: 0 };
      if (DocumentEvent) {
        const result = await DocumentEvent.updateMany(
          { actorId: body.userId },
          { $set: { actorId: '[GDPR_ERASED]' } }
        );
        eventResult = { modifiedCount: result.modifiedCount };
      }

      return NextResponse.json({
        success: true,
        type: 'user',
        userId: body.userId,
        tenantId: body.tenantId,
        purged: {
          documents: docResult.modifiedCount,
          documentEvents: eventResult.modifiedCount,
        }
      });
    }

    // 2. Full tenant data purge
    const body = TenantPurgeSchema.parse(raw);
    await connectDB();
    const conn = getTenantConnection(body.dbPrefix, body.isolationStrategy);

    if (!conn.db) {
      return NextResponse.json({ error: 'Database connection not initialized' }, { status: 500 });
    }

    if (body.isolationStrategy === 'DATABASE_PER_TENANT') {
      await conn.db.dropDatabase();
      console.log(`[GDPR_PURGE] Dropped database ${body.dbPrefix} for tenant ${body.tenantId}`);
    } else {
      const collections = await conn.db.listCollections().toArray();
      const targetPrefix = `${body.dbPrefix}_`;
      let count = 0;
      for (const coll of collections) {
        if (coll.name.startsWith(targetPrefix)) {
          await conn.db.dropCollection(coll.name);
          count++;
        }
      }
      console.log(`[GDPR_PURGE] Dropped ${count} collections starting with ${targetPrefix} for tenant ${body.tenantId}`);
    }

    return NextResponse.json({ success: true, type: 'tenant', message: `Tenant ${body.tenantId} purged successfully` });
  } catch (error) {
    console.error('[ABDFiles_GDPR_PURGE_ERROR]', error);
    return NextResponse.json({ error: error instanceof Error ? error.message : 'Internal Server Error' }, { status: 500 });
  }
}
