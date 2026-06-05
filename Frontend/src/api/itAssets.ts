import api from './client'
import type {
  ApiResponse, PagedResult,
  ItAssetListDto, ItAssetDto, ItAssetHistoryDto, ItDashboardDto,
  CreateItAssetRequest, UpdateItAssetRequest, ReactivateAssetRequest,
  ItCatalogsDto, ItCatalogItemDto, CreateCatalogItemRequest, ItDepartmentDto, ItBrandDto, ItSupplierDto,
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

  exportExcel: () =>
    api.get('/ti/assets/export/excel', { responseType: 'blob' }),

  exportDashboardPdf: () =>
    api.get('/ti/assets/export/pdf', { responseType: 'blob' }),

  downloadPhoto: (assetId: string, photoId: string) =>
    api.get(`/ti/assets/${assetId}/photos/${photoId}/download`, { responseType: 'blob' }),

  create: (data: CreateItAssetRequest) =>
    api.post<ApiResponse<ItAssetDto>>('/ti/assets', data),

  update: (id: string, data: UpdateItAssetRequest) =>
    api.put<ApiResponse<ItAssetDto>>(`/ti/assets/${id}`, data),

  // Reactivar activo inactivo (Dañado/Perdido/Robado/Inactivo) → Disponible. Solo admin.
  reactivate: (id: string, data: ReactivateAssetRequest) =>
    api.post<ApiResponse<ItAssetDto>>(`/ti/assets/${id}/reactivate`, data),

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

  // Mantenimiento (CRUD) de departamentos
  listDepartments: (search?: string) =>
    api.get<ApiResponse<ItDepartmentDto[]>>('/ti/catalogs/departments', { params: search ? { search } : undefined }),

  updateDepartment: (id: string, data: CreateCatalogItemRequest) =>
    api.put<ApiResponse<ItDepartmentDto>>(`/ti/catalogs/departments/${id}`, data),

  setDepartmentStatus: (id: string, isActive: boolean) =>
    api.patch<ApiResponse<ItDepartmentDto>>(`/ti/catalogs/departments/${id}/status`, { isActive }),

  // Mantenimiento (CRUD) de marcas
  listBrands: (search?: string) =>
    api.get<ApiResponse<ItBrandDto[]>>('/ti/catalogs/brands', { params: search ? { search } : undefined }),

  updateBrand: (id: string, data: CreateCatalogItemRequest) =>
    api.put<ApiResponse<ItBrandDto>>(`/ti/catalogs/brands/${id}`, data),

  setBrandStatus: (id: string, isActive: boolean) =>
    api.patch<ApiResponse<ItBrandDto>>(`/ti/catalogs/brands/${id}/status`, { isActive }),

  // Mantenimiento (CRUD) de proveedores
  createSupplier: (data: CreateCatalogItemRequest) =>
    api.post<ApiResponse<ItCatalogItemDto>>('/ti/catalogs/suppliers', data),

  listSuppliers: (search?: string) =>
    api.get<ApiResponse<ItSupplierDto[]>>('/ti/catalogs/suppliers', { params: search ? { search } : undefined }),

  updateSupplier: (id: string, data: CreateCatalogItemRequest) =>
    api.put<ApiResponse<ItSupplierDto>>(`/ti/catalogs/suppliers/${id}`, data),

  setSupplierStatus: (id: string, isActive: boolean) =>
    api.patch<ApiResponse<ItSupplierDto>>(`/ti/catalogs/suppliers/${id}/status`, { isActive }),
}
