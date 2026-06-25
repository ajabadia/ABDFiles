import { describe, it, expect, vi, beforeEach } from 'vitest';
import { assertAccess } from '../abac';
import * as satelliteSdk from '@ajabadia/satellite-sdk/auth-middleware';

vi.mock('@ajabadia/satellite-sdk/auth-middleware', () => {
  class InsufficientPrivilegesError extends Error {
    constructor(message: string) {
      super(message);
      this.name = 'InsufficientPrivilegesError';
    }
  }
  return {
    evaluateAccess: vi.fn(),
    InsufficientPrivilegesError
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
