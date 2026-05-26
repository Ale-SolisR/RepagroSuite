import api from './client'
import type {
  ApiResponse, PagedResult, ReservationDto, CalendarEventDto,
  CreateReservationRequest, ApproveReservationRequest,
  RejectReservationRequest, CancelReservationRequest,
  CreateRecurringReservationRequest, RecurringReservationResult,
  ReservationGroupDto, BulkActionResult
} from '@/types'

export const reservationsApi = {
  getAll: (params?: { page?: number; pageSize?: number; status?: string; userId?: string; roomId?: string; from?: string; to?: string; sortDescending?: boolean }) =>
    api.get<ApiResponse<PagedResult<ReservationDto>>>('/reservations', { params }),

  // Auditoría agrupada: cada entrada es una reserva individual o el resumen de una serie periódica.
  getAudit: (params?: { page?: number; pageSize?: number; status?: string; userId?: string; roomId?: string; sortDescending?: boolean }) =>
    api.get<ApiResponse<PagedResult<ReservationGroupDto>>>('/reservations/audit', { params }),

  getGroupOccurrences: (groupId: string) =>
    api.get<ApiResponse<ReservationDto[]>>(`/reservations/recurring/${groupId}/occurrences`),

  approveGroup: (groupId: string) =>
    api.post<ApiResponse<BulkActionResult>>(`/reservations/recurring/${groupId}/approve`),

  rejectGroup: (groupId: string, data: RejectReservationRequest) =>
    api.post<ApiResponse<BulkActionResult>>(`/reservations/recurring/${groupId}/reject`, { adminComment: data.reason }),

  getMy: (params?: { page?: number; pageSize?: number; status?: string; sortDescending?: boolean }) =>
    api.get<ApiResponse<PagedResult<ReservationDto>>>('/reservations/my', { params }),

  getById: (id: string) =>
    api.get<ApiResponse<ReservationDto>>(`/reservations/${id}`),

  getCalendar: (params: { from: string; to: string; roomId?: string }) =>
    api.get<ApiResponse<CalendarEventDto[]>>('/reservations/calendar', { params }),

  create: (data: CreateReservationRequest) =>
    api.post<ApiResponse<ReservationDto>>('/reservations', data),

  createRecurring: (data: CreateRecurringReservationRequest) =>
    api.post<ApiResponse<RecurringReservationResult>>('/reservations/recurring', data),

  // El backend espera 'adminComment'; mapeamos desde comment/reason para que el texto sí se guarde.
  approve: (id: string, data: ApproveReservationRequest) =>
    api.post<ApiResponse<ReservationDto>>(`/reservations/${id}/approve`, { adminComment: data.comment }),

  reject: (id: string, data: RejectReservationRequest) =>
    api.post<ApiResponse<ReservationDto>>(`/reservations/${id}/reject`, { adminComment: data.reason }),

  cancel: (id: string, data: CancelReservationRequest) =>
    api.post<ApiResponse<ReservationDto>>(`/reservations/${id}/cancel`, data),
}
