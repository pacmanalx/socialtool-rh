// Espelho das chaves em SocialTool.Domain.Authorization.Permissions. O front usa só para mostrar ou
// esconder; quem decide de verdade é o backend, a cada requisição.
export const P = {
  UsersView: 'users.view',
  UsersInvite: 'users.invite',
  UsersEdit: 'users.edit',
  UsersImport: 'users.import',
  UsersRevoke: 'users.revoke',
  UsersAuthorize: 'users.authorize',
  UsersAssignRoles: 'users.assign_roles',
  StructureManage: 'structure.manage',
  FeedAnnounce: 'feed.announce',
  SurveysManage: 'surveys.manage',
  ReportsView: 'reports.view',
  ReportsExport: 'reports.export',
  SensitiveViewIndividual: 'sensitive.view_individual',
  AuditView: 'audit.view',
} as const;

export type Permission = (typeof P)[keyof typeof P];
