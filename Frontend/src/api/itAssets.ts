import api from './client'
import type {
  ApiResponse, PagedResult,
  ItAssetListDto, ItAssetDto, ItAssetHistoryDto, ItDashboardDto,
  CreateItAssetRequest, UpdateItAssetRequest, ChangeItAssetStatusRequest,
  ItCatalogsDto, ItCatalogItemDto, CreateCatalogItemRequest,
} from '@/types'

export interface ItAssetListParams {
  page?: number
  pageSize?: number
  search?: string
  status?: string
  assetTypeId?: string
  departmentId?: string
}

export const itAssetsApi = {
  getAll: (params?: ItAssetListParams) =>
    api.get<ApiResponse<PagedResult<ItAssetListDto>>>('/ti/assets', { params }),

  getById: (id: string) =>
    api.get<ApiResponse<ItAssetDto>>(`/ti/assets/${id}`),

  getHistory: (id: string) =>
    api.get<ApiResponse<ItAssetHistoryDto[]>>(`/ti/assets/${id}/history`),

  getDashboard: () =>
    api.get<ApiResponse<ItDashboardDto>>('/ti/assets/dashboard'),

  create: (data: CreateItAssetRequest) =>
    api.post<ApiResponse<ItAssetDto>>('/ti/assets', data),

  update: (id: string, data: UpdateItAssetRequest) =>
    api.put<ApiResponse<ItAssetDto>>(`/ti/assets/${id}`, data),

  changeStatus: (id: string, data: ChangeItAssetStatusRequest) =>
    api.patch<ApiResponse<ItAssetDto>>(`/ti/assets/${id}/status`, data),

  delete: (id: string) =>
    api.delete<ApiResponse<null>>(`/ti/assets/${id}`),
}

export const itCatalogsApi = {
  getAll: () =>
    api.get<ApiResponse<ItCatalogsDto>>('/ti/catalogs'),

  createBrand: (data: CreateCatalogItemRequest) =>
    api.post<ApiResponse<ItCatalogItemDto>>('/ti/catalogs/brands', data),

  createLocation: (data: CreateCatalogItemRequest) =>
    api.post<ApiResponse<ItCatalogItemDto>>('/ti/catalogs/locations', data),

  createDepartment: (data: CreateCatalogItemRequest) =>
    api.post<ApiResponse<ItCatalogItemDto>>('/ti/catalogs/departments', data),
}
