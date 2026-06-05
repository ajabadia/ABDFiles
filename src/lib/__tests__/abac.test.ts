import { describe, it, expect, vi, beforeEach } from 'vitest';
import { assertAccess } from '../abac';
import * as satelliteSdk from '@ajabadia/satellite-sdk';

vi.mock('@ajabadia/satellite-sdk', async (importOriginal) => {
  const actual = await importOriginal() as Record<string, unknown>;
  return {
    ...actual,
    evaluateAccess: vi.fn()
  };
});

describe('ABAC Helper assertAccess', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should resolve successfully if allowed is true', async () => {
    vi.mocked(satelliteSdk.evaluateAccess).mockResolvedValue({
      allowed: true,
      reason: 'Allowed by test policy',
      allowedSpaceIds: [],
      allowedGroupIds: []
    });

    await expect(
      assertAccess({
        userId: 'user-1',
        tenantId: 'tenant-1',
        resource: 'document',
        action: 'view'
      })
    ).resolves.not.toThrow();
  });

  it('should throw InsufficientPrivilegesError if allowed is false', async () => {
    vi.mocked(satelliteSdk.evaluateAccess).mockResolvedValue({
      allowed: false,
      reason: 'Denied by test policy',
      allowedSpaceIds: [],
      allowedGroupIds: []
    });

    await expect(
      assertAccess({
        userId: 'user-1',
        tenantId: 'tenant-1',
        resource: 'document',
        action: 'view'
      })
    ).rejects.toThrow('ABAC Denied: Denied by test policy');
  });
});
