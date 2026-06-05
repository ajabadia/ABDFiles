/**
 * Roles matrix mappings for document management operations.
 */
const ROLE_PERMISSIONS: Record<string, string[]> = {
  FILE_VIEWER: ['view', 'list'],
  FILE_EDITOR: ['view', 'list', 'upload', 'update_metadata'],
  FILE_ADMIN: ['view', 'list', 'upload', 'update_metadata', 'delete', 'apply_hold', 'release_hold', 'audit'],
  FILE_AUDITOR: ['view', 'list', 'audit']
};

/**
 * Checks if a role has the required permission for an action.
 */
export function hasPermission(role: string, action: string): boolean {
  const permissions = ROLE_PERMISSIONS[role] || [];
  return permissions.includes(action);
}

/**
 * Validates access for a specific role and action. Throws an error if forbidden.
 */
export function assertPermission(role: string, action: string): void {
  if (!hasPermission(role, action)) {
    throw new Error(`Unauthorized: Role ${role} cannot perform action ${action}`);
  }
}
