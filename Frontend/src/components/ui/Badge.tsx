import { classNames } from '@/utils'

const STATUS_COLORS: Record<string, string> = {
  Pending: 'bg-amber-100 text-amber-700',
  Active: 'bg-emerald-100 text-emerald-700',
  Rejected: 'bg-red-100 text-red-600',
  Blocked: 'bg-gray-100 text-gray-600',
  Inactive: 'bg-gray-100 text-gray-500',
  Approved: 'bg-emerald-100 text-emerald-700',
  Cancelled: 'bg-gray-100 text-gray-500',
  Finished: 'bg-blue-100 text-blue-600',
  Available: 'bg-emerald-100 text-emerald-700',
  Maintenance: 'bg-amber-100 text-amber-700',
  Occupied: 'bg-blue-100 text-blue-600',
}

const STATUS_LABELS: Record<string, string> = {
  Pending: 'Pendiente',
  Active: 'Activo',
  Rejected: 'Rechazado',
  Blocked: 'Bloqueado',
  Inactive: 'Inactiva',
  Approved: 'Aprobada',
  Cancelled: 'Cancelada',
  Finished: 'Finalizada',
  Available: 'Disponible',
  Maintenance: 'Mantenimiento',
  Occupied: 'Ocupada',
}

interface BadgeProps {
  status: string
  className?: string
}

export default function Badge({ status, className }: BadgeProps) {
  return (
    <span className={classNames(
      'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium',
      STATUS_COLORS[status] ?? 'bg-gray-100 text-gray-600',
      className
    )}>
      {STATUS_LABELS[status] ?? status}
    </span>
  )
}
