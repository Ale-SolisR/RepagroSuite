import api from './client'
import type { ApiResponse, SettingDto, UpdateSettingRequest, BulkUpdateSettingsRequest } from '@/types'

export const settingsApi = {
  getAll: () =>
    api.get<ApiResponse<SettingDto[]>>('/settings'),

  getByKey: (key: string) =>
    api.get<ApiResponse<SettingDto>>(`/settings/${key}`),

  update: (key: string, data: UpdateSettingRequest) =>
    api.put<ApiResponse<SettingDto>>(`/settings/${key}`, data),

  bulkUpdate: (data: BulkUpdateSettingsRequest) =>
    api.put<ApiResponse<null>>('/settings/bulk', data),

  testEmail: () =>
    api.post<ApiResponse<null>>('/settings/email/test'),
}
