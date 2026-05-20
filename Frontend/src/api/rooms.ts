import api from './client'
import type {
  ApiResponse, PagedResult, RoomDto, CreateRoomRequest, UpdateRoomRequest,
  RoomAvailabilityDto, UpsertRoomAvailabilityRequest, RoomSlotDto,
  RoomBlockDto, CreateRoomBlockRequest, FeatureDto, RoomScheduleDto
} from '@/types'

export const roomsApi = {
  getAll: (params?: { page?: number; pageSize?: number; status?: string }) =>
    api.get<ApiResponse<PagedResult<RoomDto>>>('/rooms', { params }),

  getById: (id: string) =>
    api.get<ApiResponse<RoomDto>>(`/rooms/${id}`),

  getAvailable: (params: { startDateTime: string; endDateTime: string; peopleCount?: number }) =>
    api.get<ApiResponse<RoomDto[]>>('/rooms/available', { params }),

  getSlots: (id: string, params: { date: string }) =>
    api.get<ApiResponse<RoomSlotDto[]>>(`/rooms/${id}/slots`, { params }),

  create: (data: CreateRoomRequest) =>
    api.post<ApiResponse<RoomDto>>('/rooms', data),

  update: (id: string, data: UpdateRoomRequest) =>
    api.put<ApiResponse<RoomDto>>(`/rooms/${id}`, data),

  changeStatus: (id: string, status: string) =>
    api.patch<ApiResponse<RoomDto>>(`/rooms/${id}/status`, { status }),

  delete: (id: string) =>
    api.delete<ApiResponse<null>>(`/rooms/${id}`),

  getAvailability: (id: string) =>
    api.get<ApiResponse<RoomAvailabilityDto[]>>(`/rooms/${id}/availability`),

  upsertAvailability: (id: string, data: UpsertRoomAvailabilityRequest[]) =>
    api.put<ApiResponse<RoomAvailabilityDto>>(`/rooms/${id}/availability`, data),

  getBlocks: (id: string) =>
    api.get<ApiResponse<RoomBlockDto[]>>(`/rooms/${id}/blocks`),

  createBlock: (id: string, data: CreateRoomBlockRequest) =>
    api.post<ApiResponse<RoomBlockDto>>(`/rooms/${id}/blocks`, data),

  deleteBlock: (roomId: string, blockId: string) =>
    api.delete<ApiResponse<null>>(`/rooms/${roomId}/blocks/${blockId}`),

  getFeatures: () =>
    api.get<ApiResponse<FeatureDto[]>>('/rooms/features'),

  getWeeklyAvailability: () =>
    api.get<ApiResponse<RoomScheduleDto[]>>('/rooms/weekly-availability'),
}
