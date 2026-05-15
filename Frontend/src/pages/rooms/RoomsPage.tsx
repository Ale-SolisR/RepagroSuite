import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, Pencil, Trash2, Settings2, ChevronLeft, ChevronRight } from 'lucide-react'
import { roomsApi } from '@/api/rooms'
import { useAuthStore } from '@/store/authStore'
import { extractApiError } from '@/utils'
import Button from '@/components/ui/Button'
import Spinner from '@/components/ui/Spinner'
import Modal from '@/components/ui/Modal'
import RoomForm from './RoomForm'
import RoomAvailabilityPanel from './RoomAvailabilityPanel'
import toast from 'react-hot-toast'
import type { RoomDto } from '@/types'

const STATUS_PILL: Record<string, string> = {
  Available: 'bg-emerald-100 text-emerald-700',
  Maintenance: 'bg-amber-100 text-amber-700',
  Inactive: 'bg-gray-100 text-gray-500',
  Occupied: 'bg-blue-100 text-blue-700',
}

const STATUS_LABEL: Record<string, string> = {
  Available: 'Disponible',
  Maintenance: 'Mantenimiento',
  Inactive: 'Inactiva',
  Occupied: 'Ocupada',
}

export default function RoomsPage() {
  const qc = useQueryClient()
  const { hasPermission } = useAuthStore()
  const [editRoom, setEditRoom] = useState<RoomDto | null>(null)
  const [showCreate, setShowCreate] = useState(false)
  const [availRoom, setAvailRoom] = useState<RoomDto | null>(null)
  const [page, setPage] = useState(1)

  const { data, isLoading } = useQuery({
    queryKey: ['rooms', page],
    queryFn: () => roomsApi.getAll({ page, pageSize: 20 }).then(r => r.data.data!),
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => roomsApi.delete(id),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['rooms'] }); toast.success('Sala eliminada') },
    onError: (err) => toast.error(extractApiError(err)),
  })

  async function handleDelete(room: RoomDto) {
    if (!confirm(`¿Eliminar la sala "${room.name}"?`)) return
    deleteMutation.mutate(room.id)
  }

  const total = data?.totalCount ?? 0
  const totalPages = data?.totalPages ?? 1

  if (isLoading) return <Spinner />

  return (
    <div className="p-6 max-w-7xl mx-auto">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <p className="text-xs text-gray-400 tracking-wide mb-1">Salas / Catálogo</p>
          <h1 className="text-2xl font-bold text-gray-900">
            Salas{' '}
            <span className="font-normal text-gray-400">· {total} registradas</span>
          </h1>
        </div>
        {hasPermission('Rooms.Create') && (
          <Button size="sm" onClick={() => setShowCreate(true)}>
            <Plus className="h-4 w-4" /> Nueva Sala
          </Button>
        )}
      </div>

      {/* Room grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
        {data?.items.map((room) => (
          <div key={room.id} className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden hover:shadow-md transition-shadow">
            {/* Room image / color header */}
            {room.imageUrl ? (
              <img src={room.imageUrl} alt={room.name} className="h-36 w-full object-cover" />
            ) : (
              <div
                className="h-36 w-full flex items-center justify-center"
                style={{ backgroundColor: room.color ?? '#16a34a' }}
              >
                <span className="text-4xl font-bold text-white/70">{room.name[0]}</span>
              </div>
            )}

            <div className="p-4">
              <div className="flex items-start justify-between gap-2 mb-3">
                <div>
                  <h3 className="font-semibold text-gray-900">{room.name}</h3>
                  <p className="text-xs text-gray-400 mt-0.5">
                    {room.code} · {room.capacity} personas
                    {room.location && ` · ${room.location}${room.floor ? ` Piso ${room.floor}` : ''}`}
                  </p>
                </div>
                <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium shrink-0 ${STATUS_PILL[room.status] ?? 'bg-gray-100 text-gray-600'}`}>
                  {STATUS_LABEL[room.status] ?? room.status}
                </span>
              </div>

              {room.features.length > 0 && (
                <div className="flex flex-wrap gap-1 mb-3">
                  {room.features.slice(0, 4).map(f => (
                    <span key={f.id} className="text-xs bg-gray-100 text-gray-600 px-2 py-0.5 rounded-full">{f.name}</span>
                  ))}
                  {room.features.length > 4 && (
                    <span className="text-xs text-gray-400">+{room.features.length - 4}</span>
                  )}
                </div>
              )}

              <div className="flex gap-2 pt-3 border-t border-gray-100">
                {hasPermission('Rooms.Availability.Manage') && (
                  <button
                    onClick={() => setAvailRoom(room)}
                    className="flex-1 flex items-center justify-center gap-1.5 px-3 py-1.5 text-xs font-medium text-gray-600 bg-gray-50 hover:bg-gray-100 border border-gray-200 rounded-lg transition-colors"
                  >
                    <Settings2 className="h-3.5 w-3.5" /> Disponibilidad
                  </button>
                )}
                {hasPermission('Rooms.Update') && (
                  <button
                    onClick={() => setEditRoom(room)}
                    className="flex items-center justify-center px-2.5 py-1.5 text-xs text-gray-600 bg-gray-50 hover:bg-gray-100 border border-gray-200 rounded-lg transition-colors"
                  >
                    <Pencil className="h-3.5 w-3.5" />
                  </button>
                )}
                {hasPermission('Rooms.Delete') && (
                  <button
                    onClick={() => handleDelete(room)}
                    className="flex items-center justify-center px-2.5 py-1.5 text-xs text-red-500 bg-red-50 hover:bg-red-100 border border-red-200 rounded-lg transition-colors"
                  >
                    <Trash2 className="h-3.5 w-3.5" />
                  </button>
                )}
              </div>
            </div>
          </div>
        ))}

        {!data?.items.length && (
          <div className="col-span-full bg-white rounded-xl border border-gray-200 shadow-sm p-16 text-center">
            <p className="text-gray-400 text-sm">No hay salas registradas</p>
            {hasPermission('Rooms.Create') && (
              <button onClick={() => setShowCreate(true)} className="mt-2 text-green-600 text-sm hover:underline">
                Crear la primera sala
              </button>
            )}
          </div>
        )}
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between mt-5">
          <span className="text-sm text-gray-400">
            {total} salas · página {page} de {totalPages}
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
      )}

      <Modal
        open={showCreate || !!editRoom}
        onClose={() => { setShowCreate(false); setEditRoom(null) }}
        title={editRoom ? 'Editar Sala' : 'Nueva Sala'}
        size="lg"
      >
        <RoomForm room={editRoom ?? undefined} onClose={() => { setShowCreate(false); setEditRoom(null) }} />
      </Modal>

      <Modal
        open={!!availRoom}
        onClose={() => setAvailRoom(null)}
        title={`Disponibilidad — ${availRoom?.name}`}
        size="xl"
      >
        {availRoom && <RoomAvailabilityPanel roomId={availRoom.id} />}
      </Modal>
    </div>
  )
}
