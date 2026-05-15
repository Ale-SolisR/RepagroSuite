import api from './client'
import type {
  ApiResponse, PagedResult, ReservationDto, CalendarEventDto,
  CreateReservationRequest, ApproveReservationRequest,
  RejectReservationRequest, CancelReservationRequest
} from '@/types'

export const reservationsApi = {
  getAll: (params?: { page?: number; pageSize?: number; status?: string; roomId?: string; from?: string; to?: string }) =>
    api.get<ApiResponse<PagedResult<ReservationDto>>>('/reservations', { params }),

  getMy: (params?: { page?: number; pageSize?: number; status?: string }) =>
    api.get<ApiResponse<PagedResult<ReservationDto>>>('/reservations/my', { params }),

  getById: (id: string) =>
    api.get<ApiResponse<ReservationDto>>(`/reservations/${id}`),

  getCalendar: (params: { from: string; to: string; roomId?: string }) =>
    api.get<ApiResponse<CalendarEventDto[]>>('/reservations/calendar', { params }),

  create: (data: CreateReservationRequest) =>
    api.post<ApiResponse<ReservationDto>>('/reservations', data),

  approve: (id: string, data: ApproveReservationRequest) =>
    api.post<ApiResponse<ReservationDto>>(`/reservations/${id}/approve`, data),

  reject: (id: string, data: RejectReservationRequest) =>
    api.post<ApiResponse<ReservationDto>>(`/reservations/${id}/reject`, data),

  cancel: (id: string, data: CancelReservationRequest) =>
    api.post<ApiResponse<ReservationDto>>(`/reservations/${id}/cancel`, data),
}
