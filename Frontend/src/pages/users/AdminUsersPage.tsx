import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Search, CheckCircle, XCircle, Lock, Unlock, ChevronLeft, ChevronRight, ShieldCheck, ShieldOff, UserMinus, KeyRound, Crown, Hash, Briefcase, Building2, CalendarDays, Mail, RotateCcw, Eye, EyeOff, Lock as LockIcon } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { usersApi } from '@/api/users'
import { formatDate, extractApiError } from '@/utils'
import { useAuthStore } from '@/store/authStore'
import { qk, staleTimes, invalidate } from '@/lib/queryKeys'
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
  Administrator: 'bg-violet-100 text-violet-700',
  Administrador: 'bg-violet-100 text-violet-700',
  ADMINISTRATOR: 'bg-violet-100 text-violet-700',
  Master: 'bg-amber-100 text-amber-700',
  Coordinador: 'bg-blue-100 text-blue-700',
  Colaborador: 'bg-gray-100 text-gray-600',
  Usuario: 'bg-slate-100 text-slate-600',
}

const ROLE_LABEL: Record<string, string> = {
  Admin: 'Administrador',
  Administrator: 'Administrador',
  ADMINISTRATOR: 'Administrador',
}

type ModalView = 'detail' | 'reject' | 'change-password'

const passwordSchema = z.object({
  newPassword: z.string().min(8, 'Mínimo 8 caracteres').regex(
    /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d])/,
    'Debe contener mayúscula, minúscula, número y carácter especial'
  ),
  confirmPassword: z.string(),
}).refine(d => d.newPassword === d.confirmPassword, {
  message: 'Las contraseñas no coinciden',
  path: ['confirmPassword'],
})

// ─── Status styles para el banner del modal ──────────────────────────────────
const STATUS_BANNER: Record<string, { bg: string; ring: string; dot: string; text: string }> = {
  Active:   { bg: 'bg-emerald-50',  ring: 'ring-emerald-200',  dot: 'bg-emerald-500', text: 'text-emerald-700' },
  Pending:  { bg: 'bg-amber-50',    ring: 'ring-amber-200',    dot: 'bg-amber-500',   text: 'text-amber-700'   },
  Blocked:  { bg: 'bg-slate-50',    ring: 'ring-slate-200',    dot: 'bg-slate-500',   text: 'text-slate-700'   },
  Rejected: { bg: 'bg-red-50',      ring: 'ring-red-200',      dot: 'bg-red-500',     text: 'text-red-700'     },
  Inactive: { bg: 'bg-slate-50',    ring: 'ring-slate-200',    dot: 'bg-slate-400',   text: 'text-slate-600'   },
}

// ─── Action item con icono tintado, label y descripción ──────────────────────
type ActionTone = 'neutral' | 'success' | 'danger' | 'primary' | 'warning' | 'gold'

function ActionItem({
  icon: Icon, label, description, tone, onClick, loading,
}: {
  icon: LucideIcon
  label: string
  description: string
  tone: ActionTone
  onClick: () => void
  loading?: boolean
}) {
  const toneStyles: Record<ActionTone, { iconBg: string; iconColor: string }> = {
    neutral: { iconBg: 'bg-slate-100',   iconColor: 'text-slate-600' },
    success: { iconBg: 'bg-emerald-100', iconColor: 'text-emerald-600' },
    danger:  { iconBg: 'bg-red-100',     iconColor: 'text-red-600' },
    primary: { iconBg: 'bg-violet-100',  iconColor: 'text-violet-600' },
    warning: { iconBg: 'bg-amber-100',   iconColor: 'text-amber-600' },
    gold:    { iconBg: 'bg-amber-100',   iconColor: 'text-amber-700' },
  }
  const t = toneStyles[tone]
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={loading}
      className="group flex w-full items-center gap-3 rounded-lg border border-gray-200 bg-white px-3 py-2.5 text-left transition-all hover:border-gray-300 hover:bg-gray-50 disabled:cursor-wait disabled:opacity-60"
    >
      <div className={`flex h-9 w-9 shrink-0 items-center justify-center rounded-lg ${t.iconBg}`}>
        {loading ? (
          <svg className={`h-4 w-4 animate-spin ${t.iconColor}`} viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" />
          </svg>
        ) : (
          <Icon className={`h-4 w-4 ${t.iconColor}`} strokeWidth={1.75} />
        )}
      </div>
      <div className="min-w-0 flex-1">
        <p className="text-sm font-medium text-gray-900">{label}</p>
        <p className="text-[12px] text-gray-500 truncate">{description}</p>
      </div>
      <ChevronRight className="h-4 w-4 text-gray-300 group-hover:text-gray-500 transition-colors shrink-0" />
    </button>
  )
}

// ─── Info field con icono y label/valor ──────────────────────────────────────
function InfoField({ icon: Icon, label, value }: { icon: LucideIcon; label: string; value: string | null | undefined }) {
  if (!value) return null
  return (
    <div className="flex items-start gap-2.5">
      <div className="mt-0.5 flex h-7 w-7 shrink-0 items-center justify-center rounded-md bg-gray-100">
        <Icon className="h-3.5 w-3.5 text-gray-500" strokeWidth={1.75} />
      </div>
      <div className="min-w-0 flex-1">
        <p className="text-[11px] uppercase tracking-wider text-gray-400 font-medium">{label}</p>
        <p className="text-sm text-gray-800 truncate">{value}</p>
      </div>
    </div>
  )
}

export default function AdminUsersPage() {
  const qc = useQueryClient()
  const currentUser = useAuthStore(s => s.user)
  const isCurrentUserMaster = currentUser?.isMaster === true
  const [page, setPage] = useState(1)
  const [activeTab, setActiveTab] = useState('')
  const [search, setSearch] = useState('')
  const [selectedUser, setSelectedUser] = useState<UserDto | null>(null)
  const [modalView, setModalView] = useState<ModalView>('detail')

  function openModal(u: UserDto) { setSelectedUser(u); setModalView('detail') }
  function closeModal() { setSelectedUser(null); setModalView('detail') }

  const { data, isLoading } = useQuery({
    queryKey: qk.users.admin(page, activeTab, search),
    queryFn: () => usersApi.getAll({
      page,
      pageSize: 20,
      status: activeTab || undefined,
      search: search || undefined,
    }).then(r => r.data.data!),
    staleTime: staleTimes.users,
  })

  const approveMutation = useMutation({
    mutationFn: (id: string) => usersApi.approve(id),
    onSuccess: () => { invalidate.users(qc); toast.success('Usuario aprobado'); closeModal() },
    onError: (err) => toast.error(extractApiError(err)),
  })

  const rejectMutation = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) => usersApi.reject(id, { reason }),
    onSuccess: () => { invalidate.users(qc); toast.success('Usuario rechazado'); closeModal() },
    onError: (err) => toast.error(extractApiError(err)),
  })

  const blockMutation = useMutation({
    mutationFn: ({ id, block }: { id: string; block: boolean }) =>
      block ? usersApi.block(id) : usersApi.unblock(id),
    onSuccess: () => { invalidate.users(qc); toast.success('Estado actualizado'); closeModal() },
    onError: (err) => toast.error(extractApiError(err)),
  })

  const inactivateMutation = useMutation({
    mutationFn: (id: string) => usersApi.inactivate(id),
    onSuccess: () => { invalidate.users(qc); toast.success('Usuario inactivado'); closeModal() },
    onError: (err) => toast.error(extractApiError(err)),
  })

  const promoteMutation = useMutation({
    mutationFn: (id: string) => usersApi.promoteAdmin(id),
    onSuccess: () => { invalidate.users(qc); toast.success('Usuario promovido a Administrador'); closeModal() },
    onError: (err) => toast.error(extractApiError(err)),
  })

  const demoteMutation = useMutation({
    mutationFn: (id: string) => usersApi.demoteAdmin(id),
    onSuccess: () => { invalidate.users(qc); toast.success('Rol de Administrador removido'); closeModal() },
    onError: (err) => toast.error(extractApiError(err)),
  })

  const resetPasswordMutation = useMutation({
    mutationFn: (id: string) => usersApi.generateTemporaryPassword(id),
    onSuccess: () => { toast.success('Contraseña temporal enviada al correo del usuario') },
    onError: (err) => toast.error(extractApiError(err)),
  })

  const changePasswordMutation = useMutation({
    mutationFn: ({ id, newPassword }: { id: string; newPassword: string }) =>
      usersApi.changePassword(id, newPassword),
    onSuccess: () => {
      toast.success('Contraseña actualizada correctamente')
      setModalView('detail')
    },
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

  function ChangePasswordForm() {
    const [showNew, setShowNew] = useState(false)
    const [showConfirm, setShowConfirm] = useState(false)
    const { register, handleSubmit, watch, formState: { errors, isSubmitting } } = useForm({
      resolver: zodResolver(passwordSchema),
    })
    const newPwd = watch('newPassword') ?? ''
    const confirmPwd = watch('confirmPassword') ?? ''
    const match = newPwd.length > 0 && confirmPwd.length > 0 && newPwd === confirmPwd
    const mismatch = confirmPwd.length > 0 && newPwd !== confirmPwd

    return (
      <form
        onSubmit={handleSubmit(d => changePasswordMutation.mutate({ id: selectedUser!.id, newPassword: d.newPassword }))}
        className="space-y-5"
      >
        <div className="flex items-start gap-3 rounded-lg border border-blue-200 bg-blue-50 px-3.5 py-3 text-[13px] text-blue-800">
          <LockIcon className="h-4 w-4 mt-0.5 shrink-0 text-blue-600" strokeWidth={2} />
          <div>
            <p>
              Estableciendo una nueva contraseña para <strong>{selectedUser?.fullName}</strong>.
            </p>
            <p className="mt-1 text-blue-700/80 text-[12px]">
              El usuario será obligado a cambiarla en su próximo inicio de sesión.
            </p>
          </div>
        </div>

        {/* Nueva contraseña */}
        <div className="flex flex-col gap-1.5">
          <label htmlFor="new-pwd" className="text-[13px] font-medium text-gray-700">
            Nueva contraseña <span className="text-red-500">*</span>
          </label>
          <div className="relative">
            <input
              id="new-pwd"
              type={showNew ? 'text' : 'password'}
              className={[
                'w-full rounded-md border px-3 py-2.5 pr-10 text-sm shadow-sm outline-none transition',
                'placeholder:text-gray-400',
                errors.newPassword
                  ? 'border-red-400 focus:border-red-500 focus:ring-1 focus:ring-red-500'
                  : 'border-gray-300 focus:border-green-600 focus:ring-1 focus:ring-green-600',
              ].join(' ')}
              placeholder="••••••••"
              {...register('newPassword')}
            />
            <button
              type="button"
              onClick={() => setShowNew(p => !p)}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 transition-colors"
              aria-label={showNew ? 'Ocultar contraseña' : 'Mostrar contraseña'}
            >
              {showNew ? <EyeOff className="h-4 w-4" strokeWidth={1.5} /> : <Eye className="h-4 w-4" strokeWidth={1.5} />}
            </button>
          </div>
          {errors.newPassword
            ? <p className="text-[12px] text-red-600">{errors.newPassword.message}</p>
            : <p className="text-[12px] text-gray-500">Mínimo 8 caracteres con mayúscula, minúscula, número y carácter especial.</p>
          }
        </div>

        {/* Confirmar */}
        <div className="flex flex-col gap-1.5">
          <label htmlFor="confirm-pwd" className="text-[13px] font-medium text-gray-700">
            Confirmar nueva contraseña <span className="text-red-500">*</span>
          </label>
          <div className="relative">
            <input
              id="confirm-pwd"
              type={showConfirm ? 'text' : 'password'}
              className={[
                'w-full rounded-md border px-3 py-2.5 pr-10 text-sm shadow-sm outline-none transition',
                'placeholder:text-gray-400',
                errors.confirmPassword
                  ? 'border-red-400 focus:border-red-500 focus:ring-1 focus:ring-red-500'
                  : match
                  ? 'border-emerald-500 focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500'
                  : mismatch
                  ? 'border-red-400 focus:border-red-500 focus:ring-1 focus:ring-red-500'
                  : 'border-gray-300 focus:border-green-600 focus:ring-1 focus:ring-green-600',
              ].join(' ')}
              placeholder="••••••••"
              {...register('confirmPassword')}
            />
            <button
              type="button"
              onClick={() => setShowConfirm(p => !p)}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 transition-colors"
              aria-label={showConfirm ? 'Ocultar contraseña' : 'Mostrar contraseña'}
            >
              {showConfirm ? <EyeOff className="h-4 w-4" strokeWidth={1.5} /> : <Eye className="h-4 w-4" strokeWidth={1.5} />}
            </button>
          </div>
          {errors.confirmPassword && <p className="text-[12px] text-red-600">{errors.confirmPassword.message}</p>}
          {!errors.confirmPassword && match && (
            <p className="text-[12px] text-emerald-600 flex items-center gap-1">
              <CheckCircle className="h-3.5 w-3.5" /> Las contraseñas coinciden
            </p>
          )}
        </div>

        <div className="flex justify-end gap-2 pt-1">
          <Button type="button" variant="ghost" onClick={() => setModalView('detail')}>Cancelar</Button>
          <Button type="submit" loading={isSubmitting || changePasswordMutation.isPending}>
            Guardar contraseña
          </Button>
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
      <div className="mb-6">
        <p className="text-xs text-gray-400 tracking-wide mb-1">Administración / Usuarios</p>
        <h1 className="text-2xl font-bold text-gray-900">
          Usuarios{' '}
          <span className="font-normal text-gray-400">· {total} registrados</span>
        </h1>
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
                      {u.isMaster ? (
                        <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${ROLE_PILL.Master}`}>
                          Master
                        </span>
                      ) : u.roles?.length > 0 ? (
                        <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${ROLE_PILL[u.roles[0]] ?? 'bg-gray-100 text-gray-700'}`}>
                          {ROLE_LABEL[u.roles[0]] ?? u.roles[0]}
                        </span>
                      ) : (
                        <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${ROLE_PILL.Usuario}`}>
                          Usuario
                        </span>
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
        title={
          modalView === 'reject'          ? 'Rechazar usuario'
          : modalView === 'change-password' ? 'Cambiar contraseña'
          : 'Gestión de usuario'
        }
        size="lg"
      >
        {selectedUser && modalView === 'detail' && (() => {
          const banner = STATUS_BANNER[selectedUser.status] ?? STATUS_BANNER.Inactive
          const userIsAdmin = isAdmin(selectedUser)
          const roleLabel = selectedUser.isMaster
            ? 'Master'
            : userIsAdmin
            ? 'Administrador'
            : 'Usuario'
          const rolePillStyle = selectedUser.isMaster
            ? 'bg-amber-100 text-amber-700'
            : userIsAdmin
            ? 'bg-violet-100 text-violet-700'
            : 'bg-slate-100 text-slate-600'

          // Reglas para mostrar acciones sobre contraseñas:
          // - El master puede hacerlo sobre cualquiera (excepto sobre sí mismo aquí, lo hace en su perfil).
          // - Cualquier otro admin solo puede hacerlo sobre usuarios normales (no admins, no master).
          // - Y nunca sobre sí mismo en esta página (debe usar /profile).
          const isSelf = currentUser?.id === selectedUser.id
          const canManagePassword = !isSelf && (isCurrentUserMaster || !userIsAdmin)

          return (
            <div className="space-y-5">
              {/* ─── Header con avatar grande, identidad y badges ─── */}
              <div className="flex items-start gap-4">
                <div className="relative shrink-0">
                  <div className={`h-16 w-16 rounded-full flex items-center justify-center text-white text-xl font-semibold ${avatarColor(selectedUser.fullName)} ring-4 ring-white shadow-sm`}>
                    {initials(selectedUser.fullName)}
                  </div>
                  {selectedUser.isMaster && (
                    <div className="absolute -bottom-1 -right-1 flex h-6 w-6 items-center justify-center rounded-full bg-amber-400 ring-2 ring-white" title="Administrador maestro">
                      <Crown className="h-3 w-3 text-white" strokeWidth={2.5} />
                    </div>
                  )}
                </div>
                <div className="min-w-0 flex-1">
                  <h3 className="text-base font-semibold text-gray-900 leading-tight">{selectedUser.fullName}</h3>
                  <div className="mt-0.5 flex items-center gap-1.5 text-sm text-gray-500">
                    <Mail className="h-3.5 w-3.5 shrink-0" strokeWidth={1.75} />
                    <span className="truncate">{selectedUser.email}</span>
                  </div>
                  <div className="mt-2 flex flex-wrap items-center gap-1.5">
                    <span className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[11px] font-medium ring-1 ring-inset ${banner.bg} ${banner.ring} ${banner.text}`}>
                      <span className={`h-1.5 w-1.5 rounded-full ${banner.dot}`} />
                      {STATUS_LABEL[selectedUser.status] ?? selectedUser.status}
                    </span>
                    <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-[11px] font-medium ${rolePillStyle}`}>
                      {roleLabel}
                    </span>
                  </div>
                </div>
              </div>

              {/* ─── Banner especial para master ─── */}
              {selectedUser.isMaster && (
                <div className="flex items-start gap-2.5 rounded-lg border border-amber-200 bg-amber-50 px-3.5 py-3 text-[13px] text-amber-800">
                  <Crown className="h-4 w-4 mt-0.5 shrink-0 text-amber-600" strokeWidth={2} />
                  <p>
                    Este es el <strong>administrador maestro</strong> del sistema. Su cuenta está protegida y no puede ser modificada ni eliminada.
                  </p>
                </div>
              )}

              {/* ─── Grid de información ─── */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-x-4 gap-y-3 rounded-xl bg-gray-50/60 p-4 border border-gray-100">
                <InfoField icon={Hash}         label="Cédula"       value={selectedUser.identificationNumber} />
                <InfoField icon={Building2}    label="Departamento" value={selectedUser.department} />
                <InfoField icon={Briefcase}    label="Puesto"       value={selectedUser.position} />
                <InfoField icon={CalendarDays} label="Registro"     value={formatDate(selectedUser.createdAt)} />
              </div>

              {/* ─── Acciones disponibles ─── */}
              {!selectedUser.isMaster && (
                <div>
                  <div className="flex items-center justify-between mb-2.5">
                    <p className="text-[11px] uppercase tracking-wider text-gray-400 font-semibold">Acciones disponibles</p>
                  </div>
                  <div className="space-y-2">

                    {/* Pending */}
                    {selectedUser.status === 'Pending' && (
                      <>
                        <ActionItem
                          icon={CheckCircle}
                          tone="success"
                          label="Aprobar solicitud"
                          description="Crea la cuenta y envía contraseña temporal"
                          loading={approveMutation.isPending}
                          onClick={() => approveMutation.mutate(selectedUser.id)}
                        />
                        <ActionItem
                          icon={XCircle}
                          tone="danger"
                          label="Rechazar solicitud"
                          description="Notifica al usuario con el motivo del rechazo"
                          onClick={() => setModalView('reject')}
                        />
                      </>
                    )}

                    {/* Rejected */}
                    {selectedUser.status === 'Rejected' && (
                      <ActionItem
                        icon={CheckCircle}
                        tone="success"
                        label="Aprobar solicitud"
                        description="Reactiva la solicitud y crea acceso al sistema"
                        loading={approveMutation.isPending}
                        onClick={() => approveMutation.mutate(selectedUser.id)}
                      />
                    )}

                    {/* Active */}
                    {selectedUser.status === 'Active' && (
                      <>
                        {isCurrentUserMaster && (
                          userIsAdmin ? (
                            <ActionItem
                              icon={ShieldOff}
                              tone="neutral"
                              label="Quitar rol de Administrador"
                              description="Mantiene la cuenta pero remueve los privilegios de admin"
                              loading={demoteMutation.isPending}
                              onClick={() => demoteMutation.mutate(selectedUser.id)}
                            />
                          ) : (
                            <ActionItem
                              icon={ShieldCheck}
                              tone="primary"
                              label="Designar como Administrador"
                              description="Otorga acceso completo al panel de administración"
                              loading={promoteMutation.isPending}
                              onClick={() => promoteMutation.mutate(selectedUser.id)}
                            />
                          )
                        )}
                        {canManagePassword && (
                          <>
                            <ActionItem
                              icon={LockIcon}
                              tone="primary"
                              label="Cambiar contraseña"
                              description="Establece una contraseña específica para este usuario"
                              onClick={() => setModalView('change-password')}
                            />
                            <ActionItem
                              icon={KeyRound}
                              tone="warning"
                              label="Restablecer contraseña"
                              description="Genera y envía una contraseña temporal al correo"
                              loading={resetPasswordMutation.isPending}
                              onClick={() => resetPasswordMutation.mutate(selectedUser.id)}
                            />
                          </>
                        )}
                        <ActionItem
                          icon={UserMinus}
                          tone="neutral"
                          label="Inactivar usuario"
                          description="Deshabilita el acceso de forma permanente"
                          loading={inactivateMutation.isPending}
                          onClick={() => inactivateMutation.mutate(selectedUser.id)}
                        />
                        <ActionItem
                          icon={Lock}
                          tone="danger"
                          label="Bloquear usuario"
                          description="Bloquea la sesión y previene futuros inicios de sesión"
                          loading={blockMutation.isPending}
                          onClick={() => blockMutation.mutate({ id: selectedUser.id, block: true })}
                        />
                      </>
                    )}

                    {/* Blocked */}
                    {selectedUser.status === 'Blocked' && (
                      <>
                        <ActionItem
                          icon={Unlock}
                          tone="success"
                          label="Desbloquear usuario"
                          description="Restaura el acceso y limpia los intentos fallidos"
                          loading={blockMutation.isPending}
                          onClick={() => blockMutation.mutate({ id: selectedUser.id, block: false })}
                        />
                        {canManagePassword && (
                          <>
                            <ActionItem
                              icon={LockIcon}
                              tone="primary"
                              label="Cambiar contraseña"
                              description="Establece una contraseña específica para este usuario"
                              onClick={() => setModalView('change-password')}
                            />
                            <ActionItem
                              icon={KeyRound}
                              tone="warning"
                              label="Restablecer contraseña"
                              description="Genera y envía una contraseña temporal al correo"
                              loading={resetPasswordMutation.isPending}
                              onClick={() => resetPasswordMutation.mutate(selectedUser.id)}
                            />
                          </>
                        )}
                        <ActionItem
                          icon={UserMinus}
                          tone="neutral"
                          label="Inactivar usuario"
                          description="Deshabilita el acceso de forma permanente"
                          loading={inactivateMutation.isPending}
                          onClick={() => inactivateMutation.mutate(selectedUser.id)}
                        />
                      </>
                    )}

                    {/* Inactive */}
                    {selectedUser.status === 'Inactive' && (
                      <ActionItem
                        icon={RotateCcw}
                        tone="success"
                        label="Reactivar usuario"
                        description="Restaura el acceso al sistema"
                        loading={blockMutation.isPending}
                        onClick={() => blockMutation.mutate({ id: selectedUser.id, block: false })}
                      />
                    )}
                  </div>
                </div>
              )}
            </div>
          )
        })()}
        {selectedUser && modalView === 'reject' && <RejectForm />}
        {selectedUser && modalView === 'change-password' && <ChangePasswordForm />}
      </Modal>
    </div>
  )
}
