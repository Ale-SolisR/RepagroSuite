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
  mustChangePassword: boolean
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
  isMaster?: boolean
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
export type UserStatus = 'Pending' | 'Active' | 'Rejected' | 'Blocked' | 'Inactive'
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
  isMaster?: boolean
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
  comment?: string
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
  // Token de versión para optimistic locking. El cliente lo reenvía al hacer PUT
  // y el backend devuelve 409 CONCURRENCY_CONFLICT si otro admin lo modificó mientras tanto.
  rowVersion?: string
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
  imageUrl?: string
  color?: string
  featureIds?: string[]
}

export interface UpdateRoomRequest extends CreateRoomRequest {
  // Token de versión devuelto por el backend en GET; lo reenviamos para optimistic locking.
  rowVersion?: string
}

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
  start: string  // ISO datetime, ej. "2026-05-19T08:00:00"
  end: string
  isAvailable: boolean
}

export interface DayWindow {
  dayOfWeek: number // 0=Domingo .. 6=Sábado
  openTime: string  // HH:mm
  closeTime: string // HH:mm
}

export interface RoomScheduleDto {
  roomId: string
  roomName: string
  color?: string
  days: DayWindow[]
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
  approvedByUserId?: string | null
  approvedByName?: string
  approvedAt?: string
  rejectedByUserId?: string | null
  rejectedByName?: string
  rejectedAt?: string
  cancelledByUserId?: string | null
  cancelledByName?: string
  cancelledAt?: string
  recurrenceGroupId?: string | null
  createdAt: string
}

// Entrada de auditoría: reserva individual o resumen de una serie periódica.
export interface ReservationGroupDto {
  groupKey: string
  isRecurring: boolean
  recurrenceGroupId?: string | null
  roomId: string
  roomName: string
  userId: string
  userFullName: string
  purpose: string
  firstStart: string
  lastStart: string
  occurrenceCount: number
  pendingCount: number
  approvedCount: number
  rejectedCount: number
  cancelledCount: number
  single?: ReservationDto | null
}

export interface BulkActionResult {
  affected: number
  skipped: number
}

export interface CalendarEventDto {
  id: string
  title: string
  start: string
  end: string
  roomId: string
  roomName: string
  userId: string
  userName: string
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

export interface CreateRecurringReservationRequest {
  roomId: string
  startDate: string // yyyy-MM-dd
  endDate: string   // yyyy-MM-dd
  startTime: string // HH:mm
  endTime: string   // HH:mm
  peopleCount: number
  purpose: string
  notes?: string
}

export interface SkippedOccurrence {
  date: string
  reason: string
}

export interface RecurringReservationResult {
  createdCount: number
  totalOccurrences: number
  skipped: SkippedOccurrence[]
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

// ─── TI / Inventario de Activos ──────────────────────────────────────────────────
// El backend serializa enums como string (JsonStringEnumConverter).
export type ItAssetStatus =
  | 'Available' | 'Assigned' | 'Loaned' | 'UnderReview' | 'UnderMaintenance'
  | 'UnderRepair' | 'Returned' | 'Damaged' | 'Lost' | 'Stolen' | 'Disposed' | 'Inactive'

export type PhysicalCondition = 'New' | 'Good' | 'Fair' | 'Poor' | 'Unusable'

export interface ItAssetListDto {
  id: string
  internalCode: string
  assetTypeName: string
  brandName?: string
  model?: string
  serialNumber?: string
  status: ItAssetStatus
  statusName: string
  locationName?: string
  departmentName?: string
  currentHolderName?: string
  imageUrl?: string
}

export interface ItAssetSpecDto {
  operatingSystem?: string
  processor?: string
  ramGb?: number
  diskGb?: number
  macEthernet?: string
  macWifi?: string
  ipAddress?: string
  domainName?: string
  anyDeskId?: string
  microsoft365User?: string
  antivirusStatus?: string
  techNotes?: string
}

export interface ItAssetDto extends ItAssetListDto {
  assetTypeId: string
  brandId?: string
  assetTag?: string
  physicalCondition: PhysicalCondition
  physicalConditionName: string
  locationId?: string
  locationDetail?: string
  departmentId?: string
  currentHolderEmployeeId?: string
  purchaseDate?: string
  supplier?: string
  cost?: number
  currency?: string
  hasWarranty: boolean
  warrantyEndDate?: string
  notes?: string
  spec?: ItAssetSpecDto
  createdAt: string
  rowVersion?: string
}

export interface ItAssetHistoryDto {
  id: string
  eventType: string
  fromStatus?: ItAssetStatus
  toStatus?: ItAssetStatus
  description?: string
  occurredAt: string
}

export interface CreateItAssetRequest {
  internalCode: string
  assetTypeId: string
  brandId?: string
  model?: string
  serialNumber?: string
  assetTag?: string
  physicalCondition: PhysicalCondition
  locationId?: string
  locationDetail?: string
  departmentId?: string
  currentHolderEmployeeId?: string
  purchaseDate?: string
  supplier?: string
  cost?: number
  currency?: string
  hasWarranty: boolean
  warrantyEndDate?: string
  notes?: string
  imageUrl?: string
  spec?: ItAssetSpecDto
}

export interface UpdateItAssetRequest extends CreateItAssetRequest {
  rowVersion?: string
}

export interface ChangeItAssetStatusRequest {
  status: ItAssetStatus
  reason?: string
}

export interface ItAssetTypeDto {
  id: string
  name: string
  code: string
  requiresSerial: boolean
  isAssignable: boolean
  hasComputeSpecs: boolean
  iconName?: string
}

export interface ItCatalogItemDto {
  id: string
  name: string
  code?: string
}

export interface ItCatalogsDto {
  types: ItAssetTypeDto[]
  brands: ItCatalogItemDto[]
  locations: ItCatalogItemDto[]
  departments: ItCatalogItemDto[]
}

export interface CreateCatalogItemRequest {
  name: string
  code?: string
}

export interface ItCountByLabelDto {
  label: string
  count: number
}

export interface ItDashboardDto {
  totalAssets: number
  assigned: number
  available: number
  underRepair: number
  underMaintenance: number
  disposed: number
  totalCostCrc: number
  totalCostUsd: number
  withoutSerial: number
  withoutTag: number
  withoutHolder: number
  warrantyExpiringSoon: number
  byType: ItCountByLabelDto[]
  byStatus: ItCountByLabelDto[]
  byDepartment: ItCountByLabelDto[]
}

// ─── TI / Boletas ────────────────────────────────────────────────────────────────
export type ItTicketType =
  | 'Entrega' | 'Devolucion' | 'Prestamo' | 'Mantenimiento' | 'Reparacion'
  | 'Traslado' | 'CambioResponsable' | 'AsignacionAccesorios' | 'Baja'

export type ItTicketStatus = 'Borrador' | 'PendienteFirma' | 'Firmada' | 'Emitida' | 'Anulada'

export interface ItTicketListDto {
  id: string
  ticketNumber: string
  ticketType: ItTicketType
  ticketTypeName: string
  status: ItTicketStatus
  statusName: string
  issuedAt: string
  employeeName?: string
  itResponsibleName?: string
  assetCount: number
}

export interface ItTicketLineDto {
  assetId?: string
  lineType: string
  internalCode?: string
  typeName?: string
  description?: string
  serialNumber?: string
  condition?: string
}

export interface ItTicketSignatureDto {
  signerType: string
  signerName?: string
  imageBase64: string
  signedAt: string
}

export interface ItTicketPhotoDto {
  id: string
  imageBase64: string
}

export interface ItTicketDto extends ItTicketListDto {
  notes?: string
  pdfSha256?: string
  hasPdf: boolean
  voidReason?: string
  voidedAt?: string
  lines: ItTicketLineDto[]
  photos: ItTicketPhotoDto[]
  signatures: ItTicketSignatureDto[]
}

export interface SignatureInput {
  signerType: string
  signerName?: string
  imageBase64: string
}

export interface CreateAssignmentRequest {
  employeeId: string
  assetIds: string[]
  conditionOut: PhysicalCondition
  accessories?: string
  notes?: string
  photos: string[]
  signatures: SignatureInput[]
}

export interface CreateReturnRequest {
  assetId: string
  conditionIn: PhysicalCondition
  resultingStatus: ItAssetStatus
  returnNotes?: string
  photos: string[]
  signatures: SignatureInput[]
}

export interface CreateGenericTicketRequest {
  ticketType: ItTicketType
  assetIds: string[]
  employeeId?: string
  notes?: string
  newAssetStatus?: ItAssetStatus
  statusReason?: string
  photos: string[]
  signatures: SignatureInput[]
}

export interface VoidTicketRequest {
  reason: string
}

// ─── TI / Colaboradores ──────────────────────────────────────────────────────────
export interface ItEmployeeDto {
  id: string
  identificationNumber: string
  fullName: string
  position?: string
  department?: string
  email?: string
  phoneNumber?: string
  isActive: boolean
}

export interface CreateItEmployeeRequest {
  identificationNumber: string
  fullName: string
  position?: string
  department?: string
  email?: string
  phoneNumber?: string
}

export interface UpdateItEmployeeRequest {
  fullName: string
  position?: string
  department?: string
  email?: string
  phoneNumber?: string
  isActive: boolean
}

// Resultado del lookup de cédula (endpoint /identifications/lookup).
export interface IdentificationLookupResult {
  identificationNumber: string
  fullName?: string
  found: boolean
}
