import {
  AlertTriangle,
  Archive,
  Ban,
  CheckCircle2,
  Circle,
  Clock,
  RotateCcw,
  Search,
  ShieldAlert,
  UserCheck,
  Wrench,
  XCircle,
} from 'lucide-react'

export type ChipVariant =
  | 'ok' | 'warn' | 'danger' | 'brand' | 'gray'
  | 'available' | 'assigned' | 'loaned' | 'review' | 'maintenance' | 'repair'
  | 'returned' | 'damaged' | 'lost' | 'stolen' | 'disposed' | 'inactive'
  | 'draft' | 'pendingSignature' | 'signed' | 'issued' | 'voided'

interface ChipProps {
  variant: ChipVariant
  label?: string
  dot?: boolean
  className?: string
}

const config: Record<ChipVariant, {
  bg: string; text: string; border: string; icon: React.ElementType; defaultLabel: string
}> = {
  ok:     { bg: '#ECFDF5', text: '#065F46', border: '#A7F3D0', icon: CheckCircle2, defaultLabel: 'OK' },
  warn:   { bg: '#FFFBEB', text: '#92400E', border: '#FDE68A', icon: Clock,        defaultLabel: 'Pendiente' },
  danger: { bg: '#FEF2F2', text: '#991B1B', border: '#FECACA', icon: XCircle,      defaultLabel: 'Cancelado' },
  brand:  { bg: '#DCEEE5', text: '#0A5037', border: '#8AC3A9', icon: Circle,       defaultLabel: 'Activo' },
  gray:   { bg: '#F9FAFB', text: '#4B5563', border: '#E5E7EB', icon: Circle,       defaultLabel: 'Inactivo' },

  available:   { bg: '#ECFDF5', text: '#047857', border: '#A7F3D0', icon: CheckCircle2, defaultLabel: 'Disponible' },
  assigned:    { bg: '#EFF6FF', text: '#1D4ED8', border: '#BFDBFE', icon: UserCheck,    defaultLabel: 'Asignado' },
  loaned:      { bg: '#F5F3FF', text: '#6D28D9', border: '#DDD6FE', icon: Circle,       defaultLabel: 'Prestado' },
  review:      { bg: '#FFFBEB', text: '#B45309', border: '#FDE68A', icon: Search,       defaultLabel: 'En revision' },
  maintenance: { bg: '#FFF7ED', text: '#C2410C', border: '#FED7AA', icon: Wrench,       defaultLabel: 'Mantenimiento' },
  repair:      { bg: '#ECFEFF', text: '#0E7490', border: '#A5F3FC', icon: Wrench,       defaultLabel: 'Reparacion' },
  returned:    { bg: '#EEF2FF', text: '#4338CA', border: '#C7D2FE', icon: RotateCcw,    defaultLabel: 'Devuelto' },
  damaged:     { bg: '#FFF1F2', text: '#BE123C', border: '#FDA4AF', icon: AlertTriangle, defaultLabel: 'Danado' },
  lost:        { bg: '#FEF2F2', text: '#B91C1C', border: '#FECACA', icon: Search,       defaultLabel: 'Perdido' },
  stolen:      { bg: '#FDF2F8', text: '#BE185D', border: '#FBCFE8', icon: ShieldAlert,  defaultLabel: 'Robado' },
  disposed:    { bg: '#F5F5F4', text: '#57534E', border: '#D6D3D1', icon: Archive,      defaultLabel: 'Dado de baja' },
  inactive:    { bg: '#F8FAFC', text: '#64748B', border: '#CBD5E1', icon: Circle,       defaultLabel: 'Inactivo' },

  draft:            { bg: '#F8FAFC', text: '#475569', border: '#CBD5E1', icon: Circle,       defaultLabel: 'Borrador' },
  pendingSignature: { bg: '#FFF7ED', text: '#C2410C', border: '#FED7AA', icon: Clock,        defaultLabel: 'Pendiente firma' },
  signed:           { bg: '#EEF2FF', text: '#4338CA', border: '#C7D2FE', icon: CheckCircle2, defaultLabel: 'Firmada' },
  issued:           { bg: '#ECFDF5', text: '#047857', border: '#A7F3D0', icon: CheckCircle2, defaultLabel: 'Emitida' },
  voided:           { bg: '#FEF2F2', text: '#B91C1C', border: '#FECACA', icon: Ban,          defaultLabel: 'Anulada' },
}

export default function Chip({ variant, label, dot = true, className = '' }: ChipProps) {
  const { bg, text, border, icon: Icon, defaultLabel } = config[variant]
  const displayLabel = label ?? defaultLabel

  return (
    <span
      className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[11px] font-semibold font-mono shadow-[inset_0_1px_0_rgba(255,255,255,.65)] ${className}`}
      style={{ background: bg, color: text, border: `1px solid ${border}` }}
      role="status"
    >
      {dot && <Icon className="h-3 w-3 shrink-0" strokeWidth={2} aria-hidden />}
      {displayLabel}
    </span>
  )
}

export function reservationStatusToChip(status: string): ChipVariant {
  if (status === 'Approved')  return 'ok'
  if (status === 'Pending')   return 'warn'
  if (status === 'Cancelled') return 'danger'
  if (status === 'Rejected')  return 'danger'
  return 'gray'
}

export function reservationStatusLabel(status: string): string {
  if (status === 'Approved')  return 'OK'
  if (status === 'Pending')   return 'Pend.'
  if (status === 'Cancelled') return 'Cancelado'
  if (status === 'Rejected')  return 'Rechazado'
  return status
}
