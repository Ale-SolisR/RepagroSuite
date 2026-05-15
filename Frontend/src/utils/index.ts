import { format, parseISO, isValid } from 'date-fns'
import { es } from 'date-fns/locale'
import type { AxiosError } from 'axios'
import type { ApiResponse } from '@/types'

export function formatDate(date: string | Date | undefined | null, fmt = 'dd/MM/yyyy'): string {
  if (!date) return '—'
  const d = typeof date === 'string' ? parseISO(date) : date
  return isValid(d) ? format(d, fmt, { locale: es }) : '—'
}

export function formatDateTime(date: string | Date | undefined | null): string {
  return formatDate(date, "dd/MM/yyyy HH:mm")
}

export function formatTime(date: string | Date | undefined | null): string {
  return formatDate(date, 'HH:mm')
}

export function extractApiError(error: unknown): string {
  const axiosErr = error as AxiosError<ApiResponse<unknown>>
  const data = axiosErr?.response?.data
  if (data?.errors?.length) return data.errors.join(', ')
  if (data?.message) return data.message
  if (axiosErr?.message) return axiosErr.message
  return 'Ha ocurrido un error inesperado'
}

export function classNames(...classes: (string | undefined | null | false)[]): string {
  return classes.filter(Boolean).join(' ')
}

export const DAYS_ES = ['Domingo', 'Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado']

export const STATUS_COLORS: Record<string, string> = {
  Pending: 'bg-yellow-100 text-yellow-800',
  Active: 'bg-green-100 text-green-800',
  Rejected: 'bg-red-100 text-red-800',
  Blocked: 'bg-gray-100 text-gray-800',
  Approved: 'bg-green-100 text-green-800',
  Cancelled: 'bg-gray-100 text-gray-800',
  Available: 'bg-green-100 text-green-800',
  Maintenance: 'bg-orange-100 text-orange-800',
  Inactive: 'bg-gray-100 text-gray-800',
}

export const STATUS_LABELS: Record<string, string> = {
  Pending: 'Pendiente',
  Active: 'Activo',
  Rejected: 'Rechazado',
  Blocked: 'Bloqueado',
  Approved: 'Aprobada',
  Cancelled: 'Cancelada',
  Available: 'Disponible',
  Maintenance: 'Mantenimiento',
  Inactive: 'Inactiva',
}
