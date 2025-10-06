import { 
  User, 
  UserSummary, 
  CreateUserRequest, 
  UpdateUserRequest, 
  ChangePasswordRequest,
  GetUsersParams,
  PagedResult 
} from '../types/user';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

class UserService {
  private async makeRequest<T>(
    endpoint: string, 
    options: RequestInit = {}
  ): Promise<T> {
    const url = `${API_BASE_URL}${endpoint}`;
    const response = await fetch(url, {
      headers: {
        'Content-Type': 'application/json',
        ...options.headers,
      },
      ...options,
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`HTTP ${response.status}: ${errorText}`);
    }

    return response.json();
  }

  async getUsers(params: GetUsersParams = {}): Promise<PagedResult<UserSummary>> {
    const searchParams = new URLSearchParams();
    
    if (params.page) searchParams.append('page', params.page.toString());
    if (params.pageSize) searchParams.append('pageSize', params.pageSize.toString());
    if (params.searchTerm) searchParams.append('searchTerm', params.searchTerm);
    if (params.role !== undefined) searchParams.append('role', params.role.toString());
    if (params.isActive !== undefined) searchParams.append('isActive', params.isActive.toString());
    if (params.sortBy) searchParams.append('sortBy', params.sortBy);
    if (params.sortDescending !== undefined) searchParams.append('sortDescending', params.sortDescending.toString());

    const queryString = searchParams.toString();
    const endpoint = `/users${queryString ? `?${queryString}` : ''}`;

    return this.makeRequest<PagedResult<UserSummary>>(endpoint);
  }

  async getUserById(id: string): Promise<User> {
    return this.makeRequest<User>(`/users/${id}`);
  }

  async createUser(request: CreateUserRequest): Promise<string> {
    return this.makeRequest<string>('/users', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async updateUser(id: string, request: UpdateUserRequest): Promise<void> {
    await this.makeRequest(`/users/${id}`, {
      method: 'PUT',
      body: JSON.stringify(request),
    });
  }

  async deleteUser(id: string): Promise<void> {
    await this.makeRequest(`/users/${id}`, {
      method: 'DELETE',
    });
  }

  async changePassword(id: string, request: ChangePasswordRequest): Promise<void> {
    await this.makeRequest(`/users/${id}/change-password`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async getTraders(): Promise<UserSummary[]> {
    return this.makeRequest<UserSummary[]>('/users/traders');
  }

  async getUsersByRole(role: number): Promise<UserSummary[]> {
    return this.makeRequest<UserSummary[]>(`/users/by-role/${role}`);
  }
}

export const userService = new UserService();