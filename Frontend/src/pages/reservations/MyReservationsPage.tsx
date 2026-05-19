import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Plus, X, ChevronLeft, ChevronRight } from 'lucide-react'
import { reservationsApi } from '@/api/reservations'
import { formatDateTime } from '@/utils'
import { qk, staleTimes } from '@/lib/queryKeys'
import Button from '@/components/ui/Button'
import Badge from '@/components/ui/Badge'
import Spinner from '@/components/ui/Spinner'
import Modal from '@/components/ui/Modal'
import CreateReservationForm from './CreateReservationForm'
import CancelReservationModal from './CancelReservationModal'
import type { ReservationDto } from '@/types'

export default function MyReservationsPage() {
  const [showCreate, setShowCreate] = useState(false)
  const [cancelTarget, setCancelTarget] = useState<ReservationDto | null>(null)
  const [page, setPage] = useState(1)

  const { data, isLoading } = useQuery({
    queryKey: qk.reservations.my(page),
    queryFn: () => reservationsApi.getMy({ page, pageSize: 15 }).then(r => r.data.data!),
    staleTime: staleTimes.myReservations,
  })

  const total = data?.totalCount ?? 0
  const totalPages = data?.totalPages ?? 1

  return (
    <div className="p-6 max-w-5xl mx-auto">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <p className="text-xs text-gray-400 tracking-wide mb-1">Salas / Mis Reservas</p>
          <h1 className="text-2xl font-bold text-gray-900">
            Mis Reservas{' '}
            <span className="font-normal text-gray-400">· {total} en total</span>
          </h1>
        </div>
        <Button size="sm" onClick={() => setShowCreate(true)}>
          <Plus className="h-4 w-4" /> Nueva Reserva
        </Button>
      </div>

      {/* Main card */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        {isLoading ? (
          <div className="flex justify-center py-20"><Spinner /></div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm min-w-[600px]">
              <thead>
                <tr className="border-b border-gray-100">
                  <th className="text-left px-5 py-3 text-[11px] font-semibold text-gray-400 uppercase tracking-wider">Sala</th>
                  <th className="text-left px-5 py-3 text-[11px] font-semibold text-gray-400 uppercase tracking-wider">Inicio</th>
                  <th className="text-left px-5 py-3 text-[11px] font-semibold text-gray-400 uppercase tracking-wider">Fin</th>
                  <th className="text-left px-5 py-3 text-[11px] font-semibold text-gray-400 uppercase tracking-wider">Propósito</th>
                  <th className="text-left px-5 py-3 text-[11px] font-semibold text-gray-400 uppercase tracking-wider">Estado</th>
                  <th className="px-5 py-3 w-12" />
                </tr>
              </thead>
              <tbody>
                {data?.items.map((r) => (
                  <tr key={r.id} className="border-b border-gray-50 hover:bg-gray-50/60 transition-colors">
                    <td className="px-5 py-3.5 font-medium text-gray-900">{r.roomName}</td>
                    <td className="px-5 py-3.5 text-gray-600 whitespace-nowrap">{formatDateTime(r.startDateTime)}</td>
                    <td className="px-5 py-3.5 text-gray-600 whitespace-nowrap">{formatDateTime(r.endDateTime)}</td>
                    <td className="px-5 py-3.5 text-gray-600 max-w-xs truncate">{r.purpose}</td>
                    <td className="px-5 py-3.5"><Badge status={r.status} /></td>
                    <td className="px-5 py-3.5">
                      {(r.status === 'Pending' || r.status === 'Approved') && (
                        <button
                          onClick={() => setCancelTarget(r)}
                          className="p-1.5 rounded-md text-gray-400 hover:text-red-500 hover:bg-red-50 transition-colors"
                          title="Cancelar reserva"
                        >
                          <X className="h-4 w-4" />
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
                {!data?.items.length && (
                  <tr>
                    <td colSpan={6} className="px-5 py-16 text-center text-gray-400 text-sm">
                      No tiene reservas registradas.{' '}
                      <button onClick={() => setShowCreate(true)} className="text-green-600 hover:underline">
                        Crear una ahora
                      </button>
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

      <Modal open={showCreate} onClose={() => setShowCreate(false)} title="Nueva Reserva" size="lg">
        <CreateReservationForm onClose={() => setShowCreate(false)} />
      </Modal>

      <CancelReservationModal
        reservation={cancelTarget}
        onClose={() => setCancelTarget(null)}
      />
    </div>
  )
}
