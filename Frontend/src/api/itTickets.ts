import api from './client'
import type {
  ApiResponse, PagedResult,
  ItTicketListDto, ItTicketDto,
  CreateAssignmentRequest, CreateReturnRequest, CreateGenericTicketRequest, VoidTicketRequest,
  CreateDeassignmentRequest, CreateIncidentRequest,
} from '@/types'

export interface ItTicketListParams {
  page?: number
  pageSize?: number
  type?: string
  status?: string
  search?: string
  employeeId?: string
}

export const itTicketsApi = {
  getAll: (params?: ItTicketListParams) =>
    api.get<ApiResponse<PagedResult<ItTicketListDto>>>('/ti/tickets', { params }),

  getById: (id: string) =>
    api.get<ApiResponse<ItTicketDto>>(`/ti/tickets/${id}`),

  // PDF como blob para descarga / vista en nueva pestaña.
  // Se devuelve la respuesta completa para leer el nombre del Content-Disposition.
  getPdf: (id: string) =>
    api.get(`/ti/tickets/${id}/pdf`, { responseType: 'blob' }),

  // Regenera el PDF de una boleta con el formato actual (cédulas, condición en español…).
  regeneratePdf: (id: string) =>
    api.post<ApiResponse<ItTicketDto>>(`/ti/tickets/${id}/regenerate-pdf`),

  createAssignment: (data: CreateAssignmentRequest) =>
    api.post<ApiResponse<ItTicketDto>>('/ti/tickets/assignments', data),

  createReturn: (data: CreateReturnRequest) =>
    api.post<ApiResponse<ItTicketDto>>('/ti/tickets/returns', data),

  createDeassignment: (data: CreateDeassignmentRequest) =>
    api.post<ApiResponse<ItTicketDto>>('/ti/tickets/deassignments', data),

  createIncident: (data: CreateIncidentRequest) =>
    api.post<ApiResponse<ItTicketDto>>('/ti/tickets/incidents', data),

  createGeneric: (data: CreateGenericTicketRequest) =>
    api.post<ApiResponse<ItTicketDto>>('/ti/tickets', data),

  void: (id: string, data: VoidTicketRequest) =>
    api.post<ApiResponse<ItTicketDto>>(`/ti/tickets/${id}/void`, data),
}
