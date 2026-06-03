import api from './client'
import type {
  ApiResponse, PagedResult,
  RastreoUserDto, RastreoRol, CreateRastreoUserRequest, ResetRastreoUserPasswordRequest,
} from '@/types'

// Administración de usuarios del SISTEMA DE RASTREO (esquema RASTREO, independientes de Repagro).
export const rastreoUsersApi = {
  getAll: (params?: { page?: number; pageSize?: number; search?: string; activeOnly?: boolean }) =>
    api.get<ApiResponse<PagedResult<RastreoUserDto>>>('/rastreo/users', { params }),

  getById: (id: number) =>
    api.get<ApiResponse<RastreoUserDto>>(`/rastreo/users/${id}`),

  create: (data: CreateRastreoUserRequest) =>
    api.post<ApiResponse<RastreoUserDto>>('/rastreo/users', data),

  resetPassword: (id: number, data: ResetRastreoUserPasswordRequest) =>
    api.post<ApiResponse<null>>(`/rastreo/users/${id}/reset-password`, data),

  changeRole: (id: number, rol: RastreoRol) =>
    api.patch<ApiResponse<RastreoUserDto>>(`/rastreo/users/${id}/role`, { rol }),

  setStatus: (id: number, activo: boolean) =>
    api.patch<ApiResponse<RastreoUserDto>>(`/rastreo/users/${id}/status`, { activo }),
}
