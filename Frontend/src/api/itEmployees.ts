import api from './client'
import type {
  ApiResponse, PagedResult,
  ItEmployeeDto, CreateItEmployeeRequest, UpdateItEmployeeRequest, IdentificationLookupResult,
} from '@/types'

export const itEmployeesApi = {
  getAll: (params?: { page?: number; pageSize?: number; search?: string; activeOnly?: boolean }) =>
    api.get<ApiResponse<PagedResult<ItEmployeeDto>>>('/ti/employees', { params }),

  getActive: () =>
    api.get<ApiResponse<ItEmployeeDto[]>>('/ti/employees/active'),

  getById: (id: string) =>
    api.get<ApiResponse<ItEmployeeDto>>(`/ti/employees/${id}`),

  create: (data: CreateItEmployeeRequest) =>
    api.post<ApiResponse<ItEmployeeDto>>('/ti/employees', data),

  update: (id: string, data: UpdateItEmployeeRequest) =>
    api.put<ApiResponse<ItEmployeeDto>>(`/ti/employees/${id}`, data),

  // Autocompletar nombre por cédula (reusa el proveedor de identificación existente).
  lookup: (identificationNumber: string) =>
    api.get<ApiResponse<IdentificationLookupResult>>('/identifications/lookup', { params: { identificationNumber } }),
}
