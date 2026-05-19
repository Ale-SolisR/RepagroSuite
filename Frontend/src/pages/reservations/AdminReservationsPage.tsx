import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Check, X, ChevronLeft, ChevronRight } from 'lucide-react'
import { reservationsApi } from '@/api/reservations'
import { formatDateTime, extractApiError } from '@/utils'
import { qk, staleTimes, invalidate } from '@/lib/queryKeys'
import Button from '@/components/ui/Button'
import Badge from '@/components/ui/Badge'
import Spinner from '@/components/ui/Spinner'
import Modal from '@/components/ui/Modal'
import Textarea from '@/components/ui/Textarea'
import toast from 'react-hot-toast'
import type { ReservationDto } from '@/types'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'

const rejectSchema = z.object({ reason: z.string().min(5, 'Motivo requerido') })
const approveSchema = z.object({ comment: z.string().optional() })

const TABS = [
  { key: '', label: 'Todas' },
  { key: 'Pending', label: 'Pendientes' },
  { key: 'Approved', label: 'Aprobadas' },
  { key: 'Rejected', label: 'Rechazadas' },
  { key: 'Cancelled', label: 'Canceladas' },
]

export default function AdminReservationsPage() {
  const qc = useQueryClient()
  const [page, setPage] = useState(1)
  const [activeTab, setActiveTab] = useState('')
  const [approveTarget, setApproveTarget] = useState<ReservationDto | null>(null)
  const [rejectTarget, setRejectTarget] = useState<ReservationDto | null>(null)

  const { data, isLoading } = useQuery({
    queryKey: qk.reservations.admin(page, activeTab),
    queryFn: () => reservationsApi.getAll({
      page, pageSize: 20,
      status: activeTab || undefined,
    }).then(r => r.data.data!),
    staleTime: staleTimes.adminReservations,
  })

  const approveMutation = useMutation({
    mutationFn: ({ id, comment }: { id: string; comment?: string }) =>
      reservationsApi.approve(id, { comment }),
    onSuccess: () => { invalidate.reservations(qc); toast.success('Reserva aprobada'); setApproveTarget(null) },
    onError: (err) => toast.error(extractApiError(err)),
  })

  const rejectMutation = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      reservationsApi.reject(id, { reason }),
    onSuccess: () => { invalidate.reservations(qc); toast.success('Reserva rechazada'); setRejectTarget(null) },
    onError: (err) => toast.error(extractApiError(err)),
  })

  const ApproveForm = () => {
    const { register, handleSubmit } = useForm({ resolver: zodResolver(approveSchema) })
    return (
      <form onSubmit={handleSubmit(d => approveMutation.mutate({ id: approveTarget!.id, comment: d.comment }))} className="space-y-4">
        <p className="text-sm text-gray-600">Aprobando reserva de <strong>{approveTarget?.userFullName}</strong> en <strong>{approveTarget?.roomName}</strong></p>
        <Textarea label="Comentario (opcional)" rows={2} {...register('comment')} />
        <div className="flex justify-end gap-2">
          <Button type="button" variant="secondary" onClick={() => setApproveTarget(null)}>Cancelar</Button>
          <Button type="submit" loading={approveMutation.isPending}>Aprobar</Button>
        </div>
      </form>
    )
  }

  const RejectForm = () => {
    const { register, handleSubmit, formState: { errors } } = useForm({ resolver: zodResolver(rejectSchema) })
    return (
      <form onSubmit={handleSubmit(d => rejectMutation.mutate({ id: rejectTarget!.id, reason: d.reason }))} className="space-y-4">
        <p className="text-sm text-gray-600">Rechazando reserva de <strong>{rejectTarget?.userFullName}</strong> en <strong>{rejectTarget?.roomName}</strong></p>
        <Textarea label="Motivo de rechazo" required error={errors.reason?.message} {...register('reason')} />
        <div className="flex justify-end gap-2">
          <Button type="button" variant="secondary" onClick={() => setRejectTarget(null)}>Cancelar</Button>
          <Button type="submit" variant="danger" loading={rejectMutation.isPending}>Rechazar</Button>
        </div>
      </form>
    )
  }

  const total = data?.totalCount ?? 0
  const totalPages = data?.totalPages ?? 1

  return (
    <div className="p-6 max-w-7xl mx-auto">
      {/* Header */}
      <div className="mb-6">
        <p className="text-xs text-gray-400 tracking-wide mb-1">Administración / Reservas</p>
        <h1 className="text-2xl font-bold text-gray-900">
          Gestión de Reservas{' '}
          <span className="font-normal text-gray-400">· {total} en total</span>
        </h1>
      </div>

      {/* Main card */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        {/* Tabs */}
        <div className="flex px-5 border-b border-gray-100">
          {TABS.map(tab => (
            <button
              key={tab.key}
              onClick={() => { setActiveTab(tab.key); setPage(1) }}
              className={`px-4 py-3 text-sm font-medium border-b-2 transition-colors ${
                activeTab === tab.key
                  ? 'border-green-600 text-green-700'
                  : 'border-transparent text-gray-500 hover:text-gray-700'
              }`}
            >
              {tab.label}
            </button>
          ))}
        </div>

        {/* Table */}
        {isLoading ? (
          <div className="flex justify-center py-20"><Spinner /></div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm min-w-[700px]">
              <thead>
                <tr className="border-b border-gray-100">
                  <th className="text-left px-5 py-3 text-[11px] font-semibold text-gray-400 uppercase tracking-wider">Usuario</th>
                  <th className="text-left px-5 py-3 text-[11px] font-semibold text-gray-400 uppercase tracking-wider">Sala</th>
                  <th className="text-left px-5 py-3 text-[11px] font-semibold text-gray-400 uppercase tracking-wider">Inicio</th>
                  <th className="text-left px-5 py-3 text-[11px] font-semibold text-gray-400 uppercase tracking-wider">Fin</th>
                  <th className="text-left px-5 py-3 text-[11px] font-semibold text-gray-400 uppercase tracking-wider">Estado</th>
                  <th className="px-5 py-3 w-24" />
                </tr>
              </thead>
              <tbody>
                {data?.items.map((r) => (
                  <tr key={r.id} className="border-b border-gray-50 hover:bg-gray-50/60 transition-colors">
                    <td className="px-5 py-3.5">
                      <p className="font-medium text-gray-900">{r.userFullName}</p>
                      <p className="text-xs text-gray-400 mt-0.5 truncate max-w-[180px]">{r.purpose}</p>
                    </td>
                    <td className="px-5 py-3.5 font-medium text-gray-700">{r.roomName}</td>
                    <td className="px-5 py-3.5 text-gray-600 whitespace-nowrap">{formatDateTime(r.startDateTime)}</td>
                    <td className="px-5 py-3.5 text-gray-600 whitespace-nowrap">{formatDateTime(r.endDateTime)}</td>
                    <td className="px-5 py-3.5"><Badge status={r.status} /></td>
                    <td className="px-5 py-3.5">
                      {r.status === 'Pending' && (
                        <div className="flex gap-1.5">
                          <button
                            onClick={() => setApproveTarget(r)}
                            className="flex items-center gap-1.5 px-2.5 py-1.5 text-xs font-medium text-emerald-700 bg-emerald-50 hover:bg-emerald-100 border border-emerald-200 rounded-lg transition-colors"
                          >
                            <Check className="h-3.5 w-3.5" /> Aprobar
                          </button>
                          <button
                            onClick={() => setRejectTarget(r)}
                            className="flex items-center gap-1.5 px-2.5 py-1.5 text-xs font-medium text-red-600 bg-red-50 hover:bg-red-100 border border-red-200 rounded-lg transition-colors"
                          >
                            <X className="h-3.5 w-3.5" /> Rechazar
                          </button>
                        </div>
                      )}
                    </td>
                  </tr>
                ))}
                {!data?.items.length && (
                  <tr>
                    <td colSpan={6} className="px-5 py-16 text-center text-gray-400 text-sm">
                      No hay reservas con este filtro
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}

        {/* Pagination footer */}
        <div className="flex items-center justify-between px-5 py-3 border-t border-gray-100 bg-gray-50/40">
          <span className="text-sm text-gray-400">
            {total} reservas · página {page} de {totalPages}
          </span>
          <div className="flex gap-1">
            <button
              disabled={page === 1}
              onClick={() => setPage(p => p - 1)}
              className="flex items-center gap-1 px-3 py-1.5 text-sm text-gray-600 border border-gray-200 rounded-md hover:bg-white disabled:opacity-40 disabled:cursor-not-allowed bg-white transition-colors"
            >
              <ChevronLeft className="h-3.5 w-3.5" /> Anterior
            </button>
            <button
              disabled={page === totalPages}
              onClick={() => setPage(p => p + 1)}
              className="flex items-center gap-1 px-3 py-1.5 text-sm text-gray-600 border border-gray-200 rounded-md hover:bg-white disabled:opacity-40 disabled:cursor-not-allowed bg-white transition-colors"
            >
              Siguiente <ChevronRight className="h-3.5 w-3.5" />
            </button>
          </div>
        </div>
      </div>

      <Modal open={!!approveTarget} onClose={() => setApproveTarget(null)} title="Aprobar Reserva" size="sm">
        <ApproveForm />
      </Modal>
      <Modal open={!!rejectTarget} onClose={() => setRejectTarget(null)} title="Rechazar Reserva" size="sm">
        <RejectForm />
      </Modal>
    </div>
  )
}
