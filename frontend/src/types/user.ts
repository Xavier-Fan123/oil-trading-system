export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  role: UserRole;
  roleName: string;
  isActive: boolean;
  lastLoginAt?: string;
  createdAt: string;
  updatedAt: string;
}

export interface UserSummary {
  id: string;
  email: string;
  fullName: string;
  role: UserRole;
  roleName: string;
  isActive: boolean;
  lastLoginAt?: string;
}

export interface CreateUserRequest {
  email: string;
  firstName: string;
  lastName: string;
  password: string;
  role: UserRole;
}

export interface UpdateUserRequest {
  email: string;
  firstName: string;
  lastName: string;
  role: UserRole;
  isActive: boolean;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

export enum UserRole {
  Trader = 1,
  RiskManager = 2,
  Administrator = 3,
  Viewer = 4
}

export const UserRoleLabels: Record<UserRole, string> = {
  [UserRole.Trader]: 'Trader',
  [UserRole.RiskManager]: 'Risk Manager',
  [UserRole.Administrator]: 'Administrator',
  [UserRole.Viewer]: 'Viewer'
};

export interface GetUsersParams {
  page?: number;
  pageSize?: number;
  searchTerm?: string;
  role?: UserRole;
  isActive?: boolean;
  sortBy?: string;
  sortDescending?: boolean;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}