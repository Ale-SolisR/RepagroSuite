import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Search, MoreHorizontal, CheckCircle, XCircle, Lock, Unlock, ChevronLeft, ChevronRight } from 'lucide-react'
import { usersApi } from '@/api/users'
import { formatDate, extractApiError } from '@/utils'
import Spinner from '@/components/ui/Spinner'
import Modal from '@/components/ui/Modal'
import Textarea from '@/components/ui/Textarea'
import Button from '@/components/ui/Button'
import toast from 'react-hot-toast'
import type { UserDto } from '@/types'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'

const rejectSchema = z.object({ reason: z.string().min(5, 'Motivo requerido') })

const AVATAR_COLORS = [
  'bg-blue-500', 'bg-violet-500', 'bg-emerald-500', 'bg-rose-500',
  'bg-amber-500', 'bg-teal-500', 'bg-indigo-500', 'bg-pink-500',
  'bg-cyan-600', 'bg-orange-500',
]

function avatarColor(name: string) {
  return AVATAR_COLORS[name.charCodeAt(0) % AVATAR_COLORS.length]
}

function initials(name: string) {
  const parts = name.trim().split(' ').filter(Boolean)
  if (parts.length >= 2) return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
  return name.substring(0, 2).toUpperCase()
}

const TABS = [
  { key: '', label: 'Todos' },
  { key: 'Pending', label: 'Pendientes' },
  { key: 'Active', label: 'Activos' },
  { key: 'Blocked', label: 'Bloqueados' },
  { key: 'Rejected', label: 'Rechazados' },
]

const STATUS_DOT: Record<string, string> = {
  Pending: 'bg-amber-400',
  Active: 'bg-emerald-400',
  Blocked: 'bg-gray-400',
  Rejected: 'bg-red-400',
  Inactive: 'bg-gray-300',
}

const STATUS_LABEL: Record<string, string> = {
  Pending: 'Pendiente',
  Active: 'Activo',
  Blocked: 'Bloqueado',
  Rejected: 'Rechazado',
  Inactive: 'Inactivo',
}

const ROLE_PILL: Record<string, string> = {
  Admin: 'bg-violet-100 text-violet-700',
  Administrador: 'bg-violet-100 text-violet-700',
  Coordinador: 'bg-blue-100 text-blue-700',
  Colaborador: 'bg-gray-100 text-gray-600',
}

export default function AdminUsersPage() {
  const qc = useQueryClient()
  const [page, setPage] = useState(1)
  const [activeTab, setActiveTab] = useState('')
  const [search, setSearch] = useState('')
  const [rejectTarget, setRejectTarget] = useState<UserDto | null>(null)
  const [actionMenu, setActionMenu] = useState<string | null>(null)

  const { data, isLoading } = useQuery({
    queryKey: ['admin-users', page, activeTab, search],
    queryFn: () => usersApi.getAll({
      page,
      pageSize: 20,
      status: activeTab || undefined,
      search: search || undefined,
    }).then(r => r.data.data!),
  })

  const approveMutation = useMutation({
    mutationFn: (id: string) => usersApi.approve(id, { roleIds: [] }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-users'] }); toast.success('Usuario aprobado') },
    onError: (err) => toast.error(extractApiError(err)),
  })

  const rejectMutation = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) => usersApi.reject(id, { reason }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-users'] })
      toast.success('Usuario rechazado')
      setRejectTarget(null)
    },
    onError: (err) => toast.error(extractApiError(err)),
  })

  const blockMutation = useMutation({
    mutationFn: ({ id, block }: { id: string; block: boolean }) =>
      block ? usersApi.block(id) : usersApi.unblock(id),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-users'] }); toast.success('Estado actualizado') },
    onError: (err) => toast.error(extractApiError(err)),
  })

  const RejectForm = () => {
    const { register, handleSubmit, formState: { errors } } = useForm({ resolver: zodResolver(rejectSchema) })
    return (
      <form onSubmit={handleSubmit(d => rejectMutation.mutate({ id: rejectTarget!.id, reason: d.reason }))} className="space-y-4">
        <p className="text-sm text-gray-600">Rechazando solicitud de <strong>{rejectTarget?.fullName}</strong></p>
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
      <div className="flex items-center justify-between mb-6">
        <div>
          <p className="text-xs text-gray-400 tracking-wide mb-1">Administración / Usuarios</p>
          <h1 className="text-2xl font-bold text-gray-900">
            Usuarios{' '}
            <span className="font-normal text-gray-400">· {total} registrados</span>
          </h1>
        </div>
        <Button variant="secondary" size="sm">Exportar CSV</Button>
      </div>

      {/* Main card */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        {/* Tabs + search */}
        <div className="flex items-center justify-between px-5 border-b border-gray-100">
          <div className="flex">
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
          <div className="relative py-2">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-gray-400 pointer-events-none" />
            <input
              type="text"
              placeholder="Buscar usuario..."
              value={search}
              onChange={e => { setSearch(e.target.value); setPage(1) }}
              className="pl-8 pr-3 py-1.5 text-sm border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-green-600/20 focus:border-green-400 w-52 placeholder:text-gray-400"
            />
          </div>
        </div>

        {/* Table */}
        {isLoading ? (
          <div className="flex justify-center py-20"><Spinner /></div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm min-w-[640px]">
              <thead>
                <tr className="border-b border-gray-100">
                  <th className="text-left px-5 py-3 text-[11px] font-semibold text-gray-400 uppercase tracking-wider">Usuario</th>
                  <th className="text-left px-5 py-3 text-[11px] font-semibold text-gray-400 uppercase tracking-wider">Rol</th>
                  <th className="text-left px-5 py-3 text-[11px] font-semibold text-gray-400 uppercase tracking-wider">Equipo</th>
                  <th className="text-left px-5 py-3 text-[11px] font-semibold text-gray-400 uppercase tracking-wider">Estado</th>
                  <th className="text-left px-5 py-3 text-[11px] font-semibold text-gray-400 uppercase tracking-wider">Registro</th>
                  <th className="px-5 py-3 w-12" />
                </tr>
              </thead>
              <tbody>
                {data?.items.map((u) => (
                  <tr key={u.id} className="border-b border-gray-50 hover:bg-gray-50/60 transition-colors">
                    <td className="px-5 py-3.5">
                      <div className="flex items-center gap-3">
                        <div className={`h-9 w-9 rounded-full flex items-center justify-center text-white text-sm font-semibold shrink-0 ${avatarColor(u.fullName)}`}>
                          {initials(u.fullName)}
                        </div>
                        <div>
                          <p className="font-medium text-gray-900 leading-tight">{u.fullName}</p>
                          <p className="text-xs text-gray-400 mt-0.5">{u.email}</p>
                        </div>
                      </div>
                    </td>
                    <td className="px-5 py-3.5">
                      {u.roles?.length > 0 ? (
                        <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${ROLE_PILL[u.roles[0]] ?? 'bg-gray-100 text-gray-700'}`}>
                          {u.roles[0]}
                        </span>
                      ) : (
                        <span className="text-xs text-gray-300">—</span>
                      )}
                    </td>
                    <td className="px-5 py-3.5 text-sm text-gray-600">
                      {u.department ?? u.position ?? <span className="text-gray-300">—</span>}
                    </td>
                    <td className="px-5 py-3.5">
                      <div className="flex items-center gap-1.5">
                        <div className={`h-2 w-2 rounded-full shrink-0 ${STATUS_DOT[u.status] ?? 'bg-gray-400'}`} />
                        <span className="text-sm text-gray-700">{STATUS_LABEL[u.status] ?? u.status}</span>
                      </div>
                    </td>
                    <td className="px-5 py-3.5 text-sm text-gray-400 whitespace-nowrap">{formatDate(u.createdAt)}</td>
                    <td className="px-5 py-3.5">
                      <div className="relative">
                        <button
                          onClick={() => setActionMenu(actionMenu === u.id ? null : u.id)}
                          className="p-1.5 rounded-md text-gray-400 hover:text-gray-600 hover:bg-gray-100 transition-colors"
                        >
                          <MoreHorizontal className="h-4 w-4" />
                        </button>
                        {actionMenu === u.id && (
                          <>
                            <div className="fixed inset-0 z-10" onClick={() => setActionMenu(null)} />
                            <div className="absolute right-0 top-8 w-44 bg-white rounded-lg border border-gray-200 shadow-lg z-20 py-1">
                              {u.status === 'Pending' && (
                                <>
                                  <button
                                    onClick={() => { approveMutation.mutate(u.id); setActionMenu(null) }}
                                    className="flex w-full items-center gap-2.5 px-3.5 py-2 text-sm text-gray-700 hover:bg-gray-50"
                                  >
                                    <CheckCircle className="h-4 w-4 text-emerald-500" /> Aprobar
                                  </button>
                                  <button
                                    onClick={() => { setRejectTarget(u); setActionMenu(null) }}
                                    className="flex w-full items-center gap-2.5 px-3.5 py-2 text-sm text-gray-700 hover:bg-gray-50"
                                  >
                                    <XCircle className="h-4 w-4 text-red-400" /> Rechazar
                                  </button>
                                </>
                              )}
                              {u.status === 'Active' && (
                                <button
                                  onClick={() => { blockMutation.mutate({ id: u.id, block: true }); setActionMenu(null) }}
                                  className="flex w-full items-center gap-2.5 px-3.5 py-2 text-sm text-gray-700 hover:bg-gray-50"
                                >
                                  <Lock className="h-4 w-4 text-gray-500" /> Bloquear
                                </button>
                              )}
                              {u.status === 'Blocked' && (
                                <button
                                  onClick={() => { blockMutation.mutate({ id: u.id, block: false }); setActionMenu(null) }}
                                  className="flex w-full items-center gap-2.5 px-3.5 py-2 text-sm text-gray-700 hover:bg-gray-50"
                                >
                                  <Unlock className="h-4 w-4 text-emerald-500" /> Desbloquear
                                </button>
                              )}
                              {u.status === 'Rejected' && (
                                <span className="block px-3.5 py-2 text-xs text-gray-400">Sin acciones disponibles</span>
                              )}
                            </div>
                          </>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
                {!data?.items.length && (
                  <tr>
                    <td colSpan={6} className="px-5 py-16 text-center text-gray-400 text-sm">
                      No hay usuarios con este filtro
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
            {total} usuarios · página {page} de {totalPages}
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

      <Modal open={!!rejectTarget} onClose={() => setRejectTarget(null)} title="Rechazar Solicitud" size="sm">
        <RejectForm />
      </Modal>
    </div>
  )
}
