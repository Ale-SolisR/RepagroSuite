import type { ItAssetStatus, PhysicalCondition } from '@/types'
import type { ChipVariant } from '@/components/ui/Chip'

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
    case 'Available': return 'available'
    case 'Assigned': return 'assigned'
    case 'Loaned': return 'loaned'
    case 'UnderReview': return 'review'
    case 'UnderMaintenance': return 'maintenance'
    case 'UnderRepair': return 'repair'
    case 'Returned': return 'returned'
    case 'Damaged': return 'damaged'
    case 'Lost': return 'lost'
    case 'Stolen': return 'stolen'
    case 'Disposed': return 'disposed'
    case 'Inactive': return 'inactive'
    default: return 'gray'
  }
}

// Estados vigentes del flujo de 5 movimientos (filtro de inventario).
// STATUS_LABELS se mantiene completo para renderizar estados heredados en chips/historial.
const ACTIVE_STATUSES: ItAssetStatus[] = ['Available', 'Assigned', 'Damaged', 'Lost', 'Stolen']

export const STATUS_OPTIONS = ACTIVE_STATUSES
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
    case 'Borrador': return 'draft'
    case 'PendienteFirma': return 'pendingSignature'
    case 'Firmada': return 'signed'
    case 'Emitida': return 'issued'
    case 'Anulada': return 'voided'
    default: return 'gray'
  }
}

// Estados a los que puede quedar un activo tras una devolución (flujo de 5 movimientos).
export const RETURN_RESULT_OPTIONS: { value: ItAssetStatus; label: string }[] = [
  { value: 'Available', label: 'Disponible' },
  { value: 'Damaged', label: 'Dañado' },
]
