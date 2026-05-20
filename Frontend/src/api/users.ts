import api from './client'
import type { ApiResponse, PagedResult, UserDto, RegisterRequest, ApproveUserRequest, RejectUserRequest } from '@/types'

export const usersApi = {
  getAll: (params?: { page?: number; pageSize?: number; status?: string; search?: string }) =>
    api.get<ApiResponse<PagedResult<UserDto>>>('/users', { params }),

  getById: (id: string) =>
    api.get<ApiResponse<UserDto>>(`/users/${id}`),

  register: (data: RegisterRequest) =>
    api.post<ApiResponse<UserDto>>('/users/register', data),

  approve: (id: string, data?: ApproveUserRequest) =>
    api.post<ApiResponse<UserDto>>(`/users/${id}/approve`, data ?? {}),

  reject: (id: string, data: RejectUserRequest) =>
    api.post<ApiResponse<UserDto>>(`/users/${id}/reject`, data),

  block: (id: string) =>
    api.post<ApiResponse<UserDto>>(`/users/${id}/block`),

  unblock: (id: string) =>
    api.post<ApiResponse<UserDto>>(`/users/${id}/unblock`),

  generateTemporaryPassword: (id: string) =>
    api.post<ApiResponse<null>>(`/users/${id}/generate-temporary-password`),

  changePassword: (id: string, newPassword: string) =>
    api.post<ApiResponse<null>>(`/users/${id}/change-password`, { newPassword }),

  forcePasswordChange: (id: string) =>
    api.post<ApiResponse<null>>(`/users/${id}/force-password-change`),

  inactivate: (id: string) =>
    api.post<ApiResponse<UserDto>>(`/users/${id}/inactivate`),

  promoteAdmin: (id: string) =>
    api.post<ApiResponse<UserDto>>(`/users/${id}/promote-admin`),

  demoteAdmin: (id: string) =>
    api.post<ApiResponse<UserDto>>(`/users/${id}/demote-admin`),

  delete: (id: string) =>
    api.delete<ApiResponse<null>>(`/users/${id}`),
}
