import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Search, CheckCircle, XCircle, Lock, Unlock, ChevronLeft, ChevronRight, ShieldCheck, ShieldOff, UserMinus, KeyRound } from 'lucide-react'
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
  { key: 'Inactive', label: 'Inactivos' },
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
  ADMINISTRATOR: 'bg-violet-100 text-violet-700',
  Coordinador: 'bg-blue-100 text-blue-700',
  Colaborador: 'bg-gray-100 text-gray-600',
}

type ModalView = 'detail' | 'reject'

export default function AdminUsersPage() {
  const qc = useQueryClient()
  const [page, setPage] = useState(1)
  const [activeTab, setActiveTab] = useState('')
  const [search, setSearch] = useState('')
  const [selectedUser, setSelectedUser] = useState<UserDto | null>(null)
  const [modalView, setModalView] = useState<ModalView>('detail')

  function openModal(u: UserDto) { setSelectedUser(u); setModalView('detail') }
  function closeModal() { setSelectedUser(null); setModalView('detail') }

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
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-users'] }); toast.success('Usuario aprobado'); closeModal() },
    onError: (err) => toast.error(extractApiError(err)),
  })

  const rejectMutation = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) => usersApi.reject(id, { reason }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-users'] }); toast.success('Usuario rechazado'); closeModal() },
    onError: (err) => toast.error(extractApiError(err)),
  })

  const blockMutation = useMutation({
    mutationFn: ({ id, block }: { id: string; block: boolean }) =>
      block ? usersApi.block(id) : usersApi.unblock(id),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-users'] }); toast.success('Estado actualizado'); closeModal() },
    onError: (err) => toast.error(extractApiError(err)),
  })

  const inactivateMutation = useMutation({
    mutationFn: (id: string) => usersApi.inactivate(id),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-users'] }); toast.success('Usuario inactivado'); closeModal() },
    onError: (err) => toast.error(extractApiError(err)),
  })

  const promoteMutation = useMutation({
    mutationFn: (id: string) => usersApi.promoteAdmin(id),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-users'] }); toast.success('Usuario promovido a Administrador'); closeModal() },
    onError: (err) => toast.error(extractApiError(err)),
  })

  const demoteMutation = useMutation({
    mutationFn: (id: string) => usersApi.demoteAdmin(id),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-users'] }); toast.success('Rol de Administrador removido'); closeModal() },
    onError: (err) => toast.error(extractApiError(err)),
  })

  const resetPasswordMutation = useMutation({
    mutationFn: (id: string) => usersApi.generateTemporaryPassword(id),
    onSuccess: () => { toast.success('Contraseña temporal enviada al correo del usuario') },
    onError: (err) => toast.error(extractApiError(err)),
  })

  function RejectForm() {
    const { register, handleSubmit, formState: { errors } } = useForm({ resolver: zodResolver(rejectSchema) })
    return (
      <form onSubmit={handleSubmit(d => rejectMutation.mutate({ id: selectedUser!.id, reason: d.reason }))} className="space-y-4">
        <p className="text-sm text-gray-600">Rechazando solicitud de <strong>{selectedUser?.fullName}</strong></p>
        <Textarea label="Motivo de rechazo" required error={errors.reason?.message} {...register('reason')} />
        <div className="flex justify-end gap-2">
          <Button type="button" variant="secondary" onClick={() => setModalView('detail')}>Volver</Button>
          <Button type="submit" variant="danger" loading={rejectMutation.isPending}>Confirmar rechazo</Button>
        </div>
      </form>
    )
  }

  const total = data?.totalCount ?? 0
  const totalPages = data?.totalPages ?? 1

  const isAdmin = (u: UserDto) => u.roles?.some(r => r.toUpperCase().includes('ADMIN'))

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
          <div className="flex overflow-x-auto">
            {TABS.map(tab => (
              <button
                key={tab.key}
                onClick={() => { setActiveTab(tab.key); setPage(1) }}
                className={`px-4 py-3 text-sm font-medium border-b-2 transition-colors whitespace-nowrap ${
                  activeTab === tab.key
                    ? 'border-green-600 text-green-700'
                    : 'border-transparent text-gray-500 hover:text-gray-700'
                }`}
              >
                {tab.label}
              </button>
            ))}
          </div>
          <div className="relative py-2 ml-4 shrink-0">
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
                </tr>
              </thead>
              <tbody>
                {data?.items.map((u) => (
                  <tr key={u.id} onClick={() => openModal(u)} className="border-b border-gray-50 hover:bg-gray-50/60 transition-colors cursor-pointer">
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
                  </tr>
                ))}
                {!data?.items.length && (
                  <tr>
                    <td colSpan={5} className="px-5 py-16 text-center text-gray-400 text-sm">
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

      <Modal
        open={!!selectedUser}
        onClose={closeModal}
        title={modalView === 'reject' ? 'Rechazar Usuario' : 'Gestión de Usuario'}
        size="sm"
      >
        {selectedUser && modalView === 'detail' && (
          <div className="space-y-4">
            {/* User info */}
            <div className="flex items-center gap-3 pb-4 border-b border-gray-100">
              <div className={`h-12 w-12 rounded-full flex items-center justify-center text-white text-lg font-semibold shrink-0 ${avatarColor(selectedUser.fullName)}`}>
                {initials(selectedUser.fullName)}
              </div>
              <div>
                <p className="font-semibold text-gray-900">{selectedUser.fullName}</p>
                <p className="text-sm text-gray-500">{selectedUser.email}</p>
              </div>
            </div>

            {/* Details */}
            <div className="space-y-2 text-sm">
              <div className="flex justify-between">
                <span className="text-gray-500">Estado</span>
                <div className="flex items-center gap-1.5">
                  <div className={`h-2 w-2 rounded-full ${STATUS_DOT[selectedUser.status] ?? 'bg-gray-400'}`} />
                  <span className="text-gray-800">{STATUS_LABEL[selectedUser.status] ?? selectedUser.status}</span>
                </div>
              </div>
              {selectedUser.identificationNumber && (
                <div className="flex justify-between">
                  <span className="text-gray-500">Cédula</span>
                  <span className="text-gray-800">{selectedUser.identificationNumber}</span>
                </div>
              )}
              {selectedUser.department && (
                <div className="flex justify-between">
                  <span className="text-gray-500">Departamento</span>
                  <span className="text-gray-800">{selectedUser.department}</span>
                </div>
              )}
              {selectedUser.position && (
                <div className="flex justify-between">
                  <span className="text-gray-500">Puesto</span>
                  <span className="text-gray-800">{selectedUser.position}</span>
                </div>
              )}
              {selectedUser.roles?.length > 0 && (
                <div className="flex justify-between">
                  <span className="text-gray-500">Rol</span>
                  <span className="text-gray-800">{selectedUser.roles.join(', ')}</span>
                </div>
              )}
              <div className="flex justify-between">
                <span className="text-gray-500">Registro</span>
                <span className="text-gray-800">{formatDate(selectedUser.createdAt)}</span>
              </div>
            </div>

            {/* Actions */}
            <div className="flex flex-wrap justify-end gap-2 pt-2">
              {/* Pending → Rechazar + Aprobar */}
              {selectedUser.status === 'Pending' && (
                <>
                  <Button variant="secondary" onClick={() => setModalView('reject')}>
                    <XCircle className="h-4 w-4 mr-1.5" /> Rechazar
                  </Button>
                  <Button onClick={() => approveMutation.mutate(selectedUser.id)} loading={approveMutation.isPending}>
                    <CheckCircle className="h-4 w-4 mr-1.5" /> Aprobar
                  </Button>
                </>
              )}

              {/* Rejected → solo Aprobar (no se puede rechazar de nuevo, ya está rechazado) */}
              {selectedUser.status === 'Rejected' && (
                <Button onClick={() => approveMutation.mutate(selectedUser.id)} loading={approveMutation.isPending}>
                  <CheckCircle className="h-4 w-4 mr-1.5" /> Aprobar
                </Button>
              )}

              {/* Active → Admin toggle + Resetear Contraseña + Inactivar + Bloquear */}
              {selectedUser.status === 'Active' && (
                <>
                  {isAdmin(selectedUser) ? (
                    !selectedUser.isMaster && (
                      <Button
                        variant="secondary"
                        onClick={() => demoteMutation.mutate(selectedUser.id)}
                        loading={demoteMutation.isPending}
                      >
                        <ShieldOff className="h-4 w-4 mr-1.5" /> Quitar Admin
                      </Button>
                    )
                  ) : (
                    <Button
                      variant="secondary"
                      onClick={() => promoteMutation.mutate(selectedUser.id)}
                      loading={promoteMutation.isPending}
                    >
                      <ShieldCheck className="h-4 w-4 mr-1.5" /> Hacer Admin
                    </Button>
                  )}
                  <Button
                    variant="secondary"
                    onClick={() => resetPasswordMutation.mutate(selectedUser.id)}
                    loading={resetPasswordMutation.isPending}
                  >
                    <KeyRound className="h-4 w-4 mr-1.5" /> Resetear Contraseña
                  </Button>
                  <Button
                    variant="secondary"
                    onClick={() => inactivateMutation.mutate(selectedUser.id)}
                    loading={inactivateMutation.isPending}
                  >
                    <UserMinus className="h-4 w-4 mr-1.5" /> Inactivar
                  </Button>
                  <Button
                    variant="secondary"
                    onClick={() => blockMutation.mutate({ id: selectedUser.id, block: true })}
                    loading={blockMutation.isPending}
                  >
                    <Lock className="h-4 w-4 mr-1.5" /> Bloquear
                  </Button>
                </>
              )}

              {/* Blocked → Resetear Contraseña + Desbloquear + Inactivar */}
              {selectedUser.status === 'Blocked' && (
                <>
                  <Button
                    variant="secondary"
                    onClick={() => resetPasswordMutation.mutate(selectedUser.id)}
                    loading={resetPasswordMutation.isPending}
                  >
                    <KeyRound className="h-4 w-4 mr-1.5" /> Resetear Contraseña
                  </Button>
                  <Button
                    variant="secondary"
                    onClick={() => inactivateMutation.mutate(selectedUser.id)}
                    loading={inactivateMutation.isPending}
                  >
                    <UserMinus className="h-4 w-4 mr-1.5" /> Inactivar
                  </Button>
                  <Button
                    onClick={() => blockMutation.mutate({ id: selectedUser.id, block: false })}
                    loading={blockMutation.isPending}
                  >
                    <Unlock className="h-4 w-4 mr-1.5" /> Desbloquear
                  </Button>
                </>
              )}

              {/* Inactive → Reactivar (unblock sets to Active) */}
              {selectedUser.status === 'Inactive' && (
                <Button
                  onClick={() => blockMutation.mutate({ id: selectedUser.id, block: false })}
                  loading={blockMutation.isPending}
                >
                  <CheckCircle className="h-4 w-4 mr-1.5" /> Reactivar
                </Button>
              )}
            </div>
          </div>
        )}
        {selectedUser && modalView === 'reject' && <RejectForm />}
      </Modal>
    </div>
  )
}
