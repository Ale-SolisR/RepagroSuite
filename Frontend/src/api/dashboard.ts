import api from './client'
import type { ApiResponse, DashboardStatsDto } from '@/types'

export const dashboardApi = {
  getStats: () =>
    api.get<ApiResponse<DashboardStatsDto>>('/dashboard/stats'),
}
