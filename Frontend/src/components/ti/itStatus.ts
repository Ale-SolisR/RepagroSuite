import type { ItAssetStatus, PhysicalCondition } from '@/types'

type ChipVariant = 'ok' | 'warn' | 'danger' | 'brand' | 'gray'

export const STATUS_LABELS: Record<ItAssetStatus, string> = {
  Available: 'Disponible',
  Assigned: 'Asignado',
  Loaned: 'Prestado',
  UnderReview: 'En revisión',
  UnderMaintenance: 'En mantenimiento',
  UnderRepair: 'En reparación',
  Returned: 'Devuelto',
  Damaged: 'Dañado',
  Lost: 'Perdido',
  Stolen: 'Robado',
  Disposed: 'Dado de baja',
  Inactive: 'Inactivo',
}

export function statusChipVariant(status: ItAssetStatus): ChipVariant {
  switch (status) {
    case 'Available': return 'ok'
    case 'Assigned':
    case 'Loaned': return 'brand'
    case 'UnderReview':
    case 'UnderMaintenance':
    case 'UnderRepair': return 'warn'
    case 'Damaged':
    case 'Lost':
    case 'Stolen': return 'danger'
    default: return 'gray'
  }
}

export const STATUS_OPTIONS = (Object.keys(STATUS_LABELS) as ItAssetStatus[])
  .map(value => ({ value, label: STATUS_LABELS[value] }))

export const CONDITION_LABELS: Record<PhysicalCondition, string> = {
  New: 'Nuevo',
  Good: 'Bueno',
  Fair: 'Regular',
  Poor: 'Malo',
  Unusable: 'Inservible',
}

export const CONDITION_OPTIONS = (Object.keys(CONDITION_LABELS) as PhysicalCondition[])
  .map(value => ({ value, label: CONDITION_LABELS[value] }))

import type { ItTicketStatus } from '@/types'

export function ticketStatusChipVariant(status: ItTicketStatus): ChipVariant {
  switch (status) {
    case 'Emitida': return 'ok'
    case 'Firmada': return 'brand'
    case 'PendienteFirma': return 'warn'
    case 'Anulada': return 'danger'
    default: return 'gray'
  }
}

// Estados a los que puede quedar un activo tras una devolución (propuesta §5).
export const RETURN_RESULT_OPTIONS: { value: ItAssetStatus; label: string }[] = [
  { value: 'Available', label: 'Disponible' },
  { value: 'UnderReview', label: 'En revisión' },
  { value: 'UnderRepair', label: 'En reparación' },
  { value: 'Damaged', label: 'Dañado' },
]
