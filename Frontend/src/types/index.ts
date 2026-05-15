// ─── API Response Wrapper ──────────────────────────────────────────────────────
export interface ApiResponse<T> {
  success: boolean
  data?: T
  message?: string
  errors?: string[]
  code?: string
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

// ─── Auth ──────────────────────────────────────────────────────────────────────
export interface LoginRequest {
  email: string
  password: string
}

export interface AuthResponse {
  accessToken: string
  refreshToken: string
  expiresIn: number
  user: UserInfo
}

export interface UserInfo {
  id: string
  fullName: string
  email: string
  roles: string[]
  permissions: string[]
  mustChangePassword: boolean
  status: string
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
  confirmNewPassword: string
}

export interface ForcedChangePasswordRequest {
  currentPassword: string
  newPassword: string
  confirmNewPassword: string
}

export interface ForgotPasswordRequest {
  email: string
}

export interface ResetPasswordRequest {
  token: string
  newPassword: string
  confirmNewPassword: string
}

// ─── Users ─────────────────────────────────────────────────────────────────────
export type UserStatus = 'Pending' | 'Active' | 'Rejected' | 'Blocked'
export type IdentificationType = 'PhysicalId' | 'Dimex' | 'Passport' | 'Juridical'

export interface UserDto {
  id: string
  identificationType: IdentificationType
  identificationNumber: string
  fullName: string
  email: string
  phoneNumber?: string
  department?: string
  position?: string
  status: UserStatus
  statusDisplay: string
  profileImageUrl?: string
  mustChangePassword: boolean
  lastLoginAt?: string
  createdAt: string
  roles: string[]
}

export interface RegisterRequest {
  identificationNumber: string
  email: string
  phoneNumber?: string
  department?: string
  position?: string
}

export interface ApproveUserRequest {
  roleIds: string[]
  notes?: string
}

export interface RejectUserRequest {
  reason: string
}

// ─── Rooms ─────────────────────────────────────────────────────────────────────
export type RoomStatus = 'Available' | 'Maintenance' | 'Inactive'

export interface RoomDto {
  id: string
  name: string
  code: string
  capacity: number
  location?: string
  floor?: string
  description?: string
  status: RoomStatus
  statusDisplay: string
  imageUrl?: string
  color?: string
  features: FeatureDto[]
}

export interface FeatureDto {
  id: string
  name: string
  iconName?: string
}

export interface CreateRoomRequest {
  name: string
  code: string
  capacity: number
  location?: string
  floor?: string
  description?: string
  color?: string
  featureIds?: string[]
}

export interface UpdateRoomRequest extends CreateRoomRequest {}

export interface RoomAvailabilityDto {
  id: string
  dayOfWeek: number
  dayName: string
  isAvailable: boolean
  openTime: string
  closeTime: string
  minReservationMinutes: number
  maxReservationMinutes: number
  slotIntervalMinutes: number
}

export interface UpsertRoomAvailabilityRequest {
  dayOfWeek: number
  isAvailable: boolean
  openTime: string
  closeTime: string
  minReservationMinutes: number
  maxReservationMinutes: number
  slotIntervalMinutes: number
}

export interface RoomSlotDto {
  startTime: string
  endTime: string
  isAvailable: boolean
}

export interface RoomBlockDto {
  id: string
  blockType: string
  reason?: string
  isRecurring: boolean
  recurringDayOfWeek?: number
  startTime?: string
  endTime?: string
  specificDate?: string
  specificStartDateTime?: string
  specificEndDateTime?: string
  isActive: boolean
}

export interface CreateRoomBlockRequest {
  blockType: string
  reason?: string
  isRecurring: boolean
  recurringDayOfWeek?: number
  startTime?: string
  endTime?: string
  specificDate?: string
  specificStartDateTime?: string
  specificEndDateTime?: string
}

// ─── Reservations ──────────────────────────────────────────────────────────────
export type ReservationStatus = 'Pending' | 'Approved' | 'Rejected' | 'Cancelled'

export interface ReservationDto {
  id: string
  roomId: string
  roomName: string
  roomCode: string
  userId: string
  userFullName: string
  startDateTime: string
  endDateTime: string
  peopleCount: number
  purpose: string
  notes?: string
  status: ReservationStatus
  statusDisplay: string
  adminComment?: string
  cancellationReason?: string
  isDirectAdminReservation: boolean
  approvedByName?: string
  approvedAt?: string
  rejectedByName?: string
  rejectedAt?: string
  cancelledByName?: string
  cancelledAt?: string
  createdAt: string
}

export interface CalendarEventDto {
  id: string
  title: string
  start: string
  end: string
  roomId: string
  roomName: string
  status: ReservationStatus
  color: string
}

export interface CreateReservationRequest {
  roomId: string
  startDateTime: string
  endDateTime: string
  peopleCount: number
  purpose: string
  notes?: string
}

export interface ApproveReservationRequest {
  comment?: string
}

export interface RejectReservationRequest {
  reason: string
}

export interface CancelReservationRequest {
  reason: string
}

// ─── Dashboard ─────────────────────────────────────────────────────────────────
export interface DashboardStatsDto {
  totalRooms: number
  activeRooms: number
  pendingReservations: number
  todayReservations: number
  thisMonthReservations: number
  pendingUsers: number
  trend: ReservationTrendDto[]
  roomUsage: RoomUsageDto[]
}

export interface ReservationTrendDto {
  date: string
  count: number
}

export interface RoomUsageDto {
  roomId: string
  roomName: string
  totalReservations: number
  approvedReservations: number
}

// ─── Settings ──────────────────────────────────────────────────────────────────
export interface SettingDto {
  id: string
  key: string
  value?: string
  defaultValue?: string
  description?: string
  module?: string
  dataType: string
  isEncrypted: boolean
  isReadOnly: boolean
}

export interface UpdateSettingRequest {
  value: string
}

export interface BulkUpdateSettingsRequest {
  settings: Record<string, string>
}

// ─── Identification ────────────────────────────────────────────────────────────
export interface IdentificationResultDto {
  identificationType: string
  identificationTypeName?: string
  identificationNumber: string
  fullName: string
  firstName?: string
  firstName1?: string
  firstName2?: string
  lastName?: string
  lastName1?: string
  lastName2?: string
  legalName?: string
  source: string
  fromCache?: boolean
  found?: boolean
}
