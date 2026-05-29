import { CheckCircle2, Clock, XCircle, Circle } from 'lucide-react'

type ChipVariant = 'ok' | 'warn' | 'danger' | 'brand' | 'gray'

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
}

export default function Chip({ variant, label, dot = true, className = '' }: ChipProps) {
  const { bg, text, border, icon: Icon, defaultLabel } = config[variant]
  const displayLabel = label ?? defaultLabel

  return (
    <span
      className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[11px] font-medium font-mono ${className}`}
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
