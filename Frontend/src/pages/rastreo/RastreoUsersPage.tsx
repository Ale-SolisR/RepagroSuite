import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Search, Sprout, ShieldCheck, UserCog, KeyRound, Power, PowerOff,
  ChevronLeft, ChevronRight, ChevronRight as ChevronR, Eye, EyeOff, Wand2,
  CheckCircle, Mail, CalendarDays, ShieldAlert, Info,
} from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { rastreoUsersApi } from '@/api/rastreoUsers'
import { formatDate, extractApiError } from '@/utils'
import { useAuthStore } from '@/store/authStore'
import { qk, staleTimes, invalidate } from '@/lib/queryKeys'
import Spinner from '@/components/ui/Spinner'
import Modal from '@/components/ui/Modal'
import Button from '@/components/ui/Button'
import toast from 'react-hot-toast'
import type { RastreoUserDto, RastreoRol } from '@/types'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'

// La política debe coincidir con IPasswordService del backend (mín. 8, may/min/número/especial).
const passwordRule = z.string()
  .min(8, 'Mínimo 8 caracteres')
  .regex(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d])/, 'Debe incluir mayúscula, minúscula, número y carácter especial')

const createSchema = z.object({
  nombre: z.string().trim().max(150).optional(),
  correo: z.string().trim().toLowerCase().email('Correo inválido'),
  rol: z.enum(['USUARIO', 'ADMIN']),
  password: passwordRule,
  confirmPassword: z.string(),
}).refine(d => d.password === d.confirmPassword, { message: 'Las contraseñas no coinciden', path: ['confirmPassword'] })

const resetSchema = z.object({
  newPassword: passwordRule,
  confirmPassword: z.string(),
  closeActiveSession: z.boolean(),
}).refine(d => d.newPassword === d.confirmPassword, { message: 'Las contraseñas no coinciden', path: ['confirmPassword'] })

type CreateForm = z.infer<typeof createSchema>
type ResetForm = z.infer<typeof resetSchema>

const TABS = [
  { key: '', label: 'Todos' },
  { key: 'active', label: 'Activos' },
  { key: 'inactive', label: 'Inactivos' },
]

const ROLE_PILL: Record<RastreoRol, string> = {
  ADMIN: 'bg-teal-100 text-teal-800',
  USUARIO: 'bg-slate-100 text-slate-600',
}
const ROLE_LABEL: Record<RastreoRol, string> = { ADMIN: 'Administrador', USUARIO: 'Usuario' }

const AVATAR_COLORS = ['bg-teal-600', 'bg-emerald-600', 'bg-cyan-700', 'bg-green-600', 'bg-teal-700']
function avatarColor(s: string) { return AVATAR_COLORS[s.charCodeAt(0) % AVATAR_COLORS.length] }
function initials(name: string) {
  const p = name.trim().split(' ').filter(Boolean)
  if (p.length >= 2) return (p[0][0] + p[p.length - 1][0]).toUpperCase()
  return name.substring(0, 2).toUpperCase()
}

// Genera una contraseña que cumple la política, para el botón "Generar".
function generatePassword(): string {
  const lower = 'abcdefghijkmnpqrstuvwxyz', upper = 'ABCDEFGHJKLMNPQRSTUVWXYZ', nums = '23456789', sym = '!@#$%*-_?'
  const all = lower + upper + nums + sym
  const pick = (s: string) => s[Math.floor(Math.random() * s.length)]
  let pwd = pick(lower) + pick(upper) + pick(nums) + pick(sym)
  for (let i = 0; i < 8; i++) pwd += pick(all)
  return pwd.split('').sort(() => Math.random() - 0.5).join('')
}

// Campo de contraseña con toggle ver/ocultar y botón generar.
function PasswordField({
  id, label, onGenerate, error, register,
}: {
  id: string; label: string
  onGenerate: () => void; error?: string; register: Record<string, unknown>
}) {
  const [show, setShow] = useState(false)
  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={id} className="text-[13px] font-medium text-gray-700">{label} <span className="text-red-500">*</span></label>
      <div className="relative">
        <input
          id={id}
          type={show ? 'text' : 'password'}
          autoComplete="new-password"
          placeholder="••••••••"
          className={[
            'w-full rounded-md border px-3 py-2.5 pr-20 text-sm shadow-sm outline-none transition placeholder:text-gray-400',
            error ? 'border-red-400 focus:border-red-500 focus:ring-1 focus:ring-red-500'
                  : 'border-gray-300 focus:border-teal-600 focus:ring-1 focus:ring-teal-600',
          ].join(' ')}
          {...register}
        />
        <div className="absolute right-2 top-1/2 -translate-y-1/2 flex items-center gap-1">
          <button type="button" onClick={onGenerate} title="Generar contraseña segura"
            className="rounded p-1 text-gray-400 hover:text-teal-600 transition-colors" aria-label="Generar contraseña">
            <Wand2 className="h-4 w-4" strokeWidth={1.75} />
          </button>
          <button type="button" onClick={() => setShow(s => !s)}
            className="rounded p-1 text-gray-400 hover:text-gray-600 transition-colors"
            aria-label={show ? 'Ocultar contraseña' : 'Mostrar contraseña'}>
            {show ? <EyeOff className="h-4 w-4" strokeWidth={1.5} /> : <Eye className="h-4 w-4" strokeWidth={1.5} />}
          </button>
        </div>
      </div>
      {error && <p className="text-[12px] text-red-600">{error}</p>}
      {!error && <p className="text-[12px] text-gray-500">Mínimo 8 caracteres con mayúscula, minúscula, número y carácter especial.</p>}
    </div>
  )
}

function ActionItem({ icon: Icon, label, description, tone, onClick, loading }: {
  icon: LucideIcon; label: string; description: string
  tone: 'primary' | 'warning' | 'danger' | 'success'; onClick: () => void; loading?: boolean
}) {
  const styles = {
    primary: 'bg-teal-100 text-teal-700',
    warning: 'bg-amber-100 text-amber-700',
    danger: 'bg-red-100 text-red-600',
    success: 'bg-emerald-100 text-emerald-600',
  }[tone]
  return (
    <button type="button" onClick={onClick} disabled={loading}
      className="group flex w-full items-center gap-3 rounded-lg border border-gray-200 bg-white px-3 py-2.5 text-left transition-all hover:border-gray-300 hover:bg-gray-50 disabled:cursor-wait disabled:opacity-60">
      <div className={`flex h-9 w-9 shrink-0 items-center justify-center rounded-lg ${styles}`}>
        <Icon className="h-4 w-4" strokeWidth={1.75} />
      </div>
      <div className="min-w-0 flex-1">
        <p className="text-sm font-medium text-gray-900">{label}</p>
        <p className="text-[12px] text-gray-500 truncate">{description}</p>
      </div>
      <ChevronR className="h-4 w-4 text-gray-300 group-hover:text-gray-500 transition-colors shrink-0" />
    </button>
  )
}

type ModalView = 'menu' | 'reset' | 'role' | 'status'

export default function RastreoUsersPage() {
  const qc = useQueryClient()
  const hasPermission = useAuthStore(s => s.hasPermission)
  const canCreate = hasPermission('RastreoUsers.Create')
  const canReset = hasPermission('RastreoUsers.ResetPassword')
  const canRole = hasPermission('RastreoUsers.ManageRole')
  const canStatus = hasPermission('RastreoUsers.ManageStatus')

  const [page, setPage] = useState(1)
  const [activeTab, setActiveTab] = useState('')
  const [search, setSearch] = useState('')
  const [createOpen, setCreateOpen] = useState(false)
  const [selected, setSelected] = useState<RastreoUserDto | null>(null)
  const [view, setView] = useState<ModalView>('menu')

  const activeOnly = activeTab === 'active' ? true : activeTab === 'inactive' ? false : undefined

  const { data, isLoading } = useQuery({
    queryKey: qk.rastreoUsers.admin(page, activeTab, search),
    queryFn: () => rastreoUsersApi.getAll({ page, pageSize: 20, search: search || undefined, activeOnly })
      .then(r => r.data.data!),
    staleTime: staleTimes.rastreoUsers,
  })

  function openMenu(u: RastreoUserDto) { setSelected(u); setView('menu') }
  function closeMenu() { setSelected(null); setView('menu') }

  const createMutation = useMutation({
    mutationFn: (d: CreateForm) => rastreoUsersApi.create({ nombre: d.nombre, correo: d.correo, rol: d.rol, password: d.password }),
    onSuccess: () => { invalidate.rastreoUsers(qc); toast.success('Usuario de rastreo creado. Comparte la contraseña por un canal seguro.'); setCreateOpen(false) },
    onError: (e) => toast.error(extractApiError(e)),
  })

  const resetMutation = useMutation({
    mutationFn: ({ id, d }: { id: number; d: ResetForm }) =>
      rastreoUsersApi.resetPassword(id, { newPassword: d.newPassword, closeActiveSession: d.closeActiveSession }),
    onSuccess: () => { invalidate.rastreoUsers(qc); toast.success('Contraseña actualizada correctamente.'); closeMenu() },
    onError: (e) => toast.error(extractApiError(e)),
  })

  const roleMutation = useMutation({
    mutationFn: ({ id, rol }: { id: number; rol: RastreoRol }) => rastreoUsersApi.changeRole(id, rol),
    onSuccess: () => { invalidate.rastreoUsers(qc); toast.success('Rol actualizado correctamente.'); closeMenu() },
    onError: (e) => toast.error(extractApiError(e)),
  })

  const statusMutation = useMutation({
    mutationFn: ({ id, activo }: { id: number; activo: boolean }) => rastreoUsersApi.setStatus(id, activo),
    onSuccess: (_r, v) => { invalidate.rastreoUsers(qc); toast.success(v.activo ? 'Usuario activado.' : 'Usuario desactivado.'); closeMenu() },
    onError: (e) => toast.error(extractApiError(e)),
  })

  const total = data?.totalCount ?? 0
  const totalPages = data?.totalPages ?? 1

  // ─── Formulario crear ──────────────────────────────────────────────────────
  function CreateForm() {
    const { register, handleSubmit, setValue, watch, formState: { errors } } = useForm<CreateForm>({
      resolver: zodResolver(createSchema),
      defaultValues: { rol: 'USUARIO', password: '', confirmPassword: '' },
    })
    function genBoth() {
      const p = generatePassword()
      setValue('password', p, { shouldValidate: true })
      setValue('confirmPassword', p, { shouldValidate: true })
    }
    const pwd = watch('password') ?? '', conf = watch('confirmPassword') ?? ''
    const match = pwd.length > 0 && pwd === conf
    return (
      <form onSubmit={handleSubmit(d => createMutation.mutate(d))} className="space-y-4">
        <div className="flex items-start gap-2.5 rounded-lg border border-teal-200 bg-teal-50 px-3.5 py-3 text-[13px] text-teal-800">
          <Sprout className="h-4 w-4 mt-0.5 shrink-0 text-teal-600" strokeWidth={2} />
          <p>Este usuario accederá <strong>únicamente al Sistema de Rastreo</strong>, no a Repagro Suite.</p>
        </div>

        <div className="flex flex-col gap-1.5">
          <label htmlFor="r-nombre" className="text-[13px] font-medium text-gray-700">Nombre</label>
          <input id="r-nombre" {...register('nombre')} placeholder="Juan Pérez"
            className="w-full rounded-md border border-gray-300 px-3 py-2.5 text-sm shadow-sm outline-none transition focus:border-teal-600 focus:ring-1 focus:ring-teal-600 placeholder:text-gray-400" />
        </div>

        <div className="flex flex-col gap-1.5">
          <label htmlFor="r-correo" className="text-[13px] font-medium text-gray-700">Correo <span className="text-red-500">*</span></label>
          <input id="r-correo" type="email" autoComplete="off" {...register('correo')} placeholder="usuario@granja.com"
            className={['w-full rounded-md border px-3 py-2.5 text-sm shadow-sm outline-none transition placeholder:text-gray-400',
              errors.correo ? 'border-red-400 focus:border-red-500 focus:ring-1 focus:ring-red-500' : 'border-gray-300 focus:border-teal-600 focus:ring-1 focus:ring-teal-600'].join(' ')} />
          {errors.correo && <p className="text-[12px] text-red-600">{errors.correo.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5">
          <label htmlFor="r-rol" className="text-[13px] font-medium text-gray-700">Rol <span className="text-red-500">*</span></label>
          <select id="r-rol" {...register('rol')}
            className="w-full rounded-md border border-gray-300 px-3 py-2.5 text-sm shadow-sm outline-none transition bg-white focus:border-teal-600 focus:ring-1 focus:ring-teal-600">
            <option value="USUARIO">Usuario — solo ve sus propios registros</option>
            <option value="ADMIN">Administrador — ve y filtra todos los registros</option>
          </select>
        </div>

        <PasswordField id="r-pwd" label="Contraseña inicial" onGenerate={genBoth}
          error={errors.password?.message} register={register('password')} />

        <div className="flex flex-col gap-1.5">
          <label htmlFor="r-pwd2" className="text-[13px] font-medium text-gray-700">Confirmar contraseña <span className="text-red-500">*</span></label>
          <input id="r-pwd2" type="password" autoComplete="new-password" placeholder="••••••••" {...register('confirmPassword')}
            className={['w-full rounded-md border px-3 py-2.5 text-sm shadow-sm outline-none transition placeholder:text-gray-400',
              errors.confirmPassword ? 'border-red-400 focus:border-red-500 focus:ring-1 focus:ring-red-500'
                : match ? 'border-emerald-500 focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500'
                : 'border-gray-300 focus:border-teal-600 focus:ring-1 focus:ring-teal-600'].join(' ')} />
          {errors.confirmPassword && <p className="text-[12px] text-red-600">{errors.confirmPassword.message}</p>}
          {!errors.confirmPassword && match && <p className="text-[12px] text-emerald-600 flex items-center gap-1"><CheckCircle className="h-3.5 w-3.5" /> Las contraseñas coinciden</p>}
        </div>

        <div className="flex justify-end gap-2 pt-1">
          <Button type="button" variant="ghost" onClick={() => setCreateOpen(false)}>Cancelar</Button>
          <Button type="submit" loading={createMutation.isPending}>Crear usuario</Button>
        </div>
      </form>
    )
  }

  // ─── Formulario restablecer contraseña ─────────────────────────────────────
  function ResetForm() {
    const { register, handleSubmit, setValue, watch, formState: { errors } } = useForm<ResetForm>({
      resolver: zodResolver(resetSchema),
      defaultValues: { newPassword: '', confirmPassword: '', closeActiveSession: true },
    })
    function gen() {
      const p = generatePassword()
      setValue('newPassword', p, { shouldValidate: true })
      setValue('confirmPassword', p, { shouldValidate: true })
    }
    const pwd = watch('newPassword') ?? '', conf = watch('confirmPassword') ?? ''
    const match = pwd.length > 0 && pwd === conf
    return (
      <form onSubmit={handleSubmit(d => resetMutation.mutate({ id: selected!.id, d }))} className="space-y-4">
        <div className="flex items-start gap-2.5 rounded-lg border border-amber-200 bg-amber-50 px-3.5 py-3 text-[13px] text-amber-800">
          <ShieldAlert className="h-4 w-4 mt-0.5 shrink-0 text-amber-600" strokeWidth={2} />
          <div>
            <p>Se reemplazará la contraseña de <strong>{selected?.nombre ?? selected?.correo}</strong>.</p>
            <p className="mt-1 text-amber-700/80 text-[12px]">No es posible ver la contraseña anterior; solo asignar una nueva.</p>
          </div>
        </div>

        <PasswordField id="rs-pwd" label="Nueva contraseña" onGenerate={gen}
          error={errors.newPassword?.message} register={register('newPassword')} />

        <div className="flex flex-col gap-1.5">
          <label htmlFor="rs-pwd2" className="text-[13px] font-medium text-gray-700">Confirmar contraseña <span className="text-red-500">*</span></label>
          <input id="rs-pwd2" type="password" autoComplete="new-password" placeholder="••••••••" {...register('confirmPassword')}
            className={['w-full rounded-md border px-3 py-2.5 text-sm shadow-sm outline-none transition placeholder:text-gray-400',
              errors.confirmPassword ? 'border-red-400 focus:border-red-500 focus:ring-1 focus:ring-red-500'
                : match ? 'border-emerald-500 focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500'
                : 'border-gray-300 focus:border-teal-600 focus:ring-1 focus:ring-teal-600'].join(' ')} />
          {errors.confirmPassword && <p className="text-[12px] text-red-600">{errors.confirmPassword.message}</p>}
          {!errors.confirmPassword && match && <p className="text-[12px] text-emerald-600 flex items-center gap-1"><CheckCircle className="h-3.5 w-3.5" /> Las contraseñas coinciden</p>}
        </div>

        <label className="flex items-center gap-2 text-[13px] text-gray-700 select-none">
          <input type="checkbox" {...register('closeActiveSession')} className="h-4 w-4 rounded border-gray-300 text-teal-600 focus:ring-teal-500" />
          Cerrar la sesión activa del usuario (recomendado)
        </label>

        <div className="flex justify-end gap-2 pt-1">
          <Button type="button" variant="ghost" onClick={() => setView('menu')}>Cancelar</Button>
          <Button type="submit" variant="danger" loading={resetMutation.isPending}>Restablecer contraseña</Button>
        </div>
      </form>
    )
  }

  // ─── Cambiar rol ───────────────────────────────────────────────────────────
  function RoleForm() {
    const [rol, setRol] = useState<RastreoRol>(selected!.rol)
    const changed = rol !== selected!.rol
    return (
      <div className="space-y-4">
        <div className="flex items-start gap-2.5 rounded-lg border border-teal-200 bg-teal-50 px-3.5 py-3 text-[13px] text-teal-800">
          <UserCog className="h-4 w-4 mt-0.5 shrink-0 text-teal-600" strokeWidth={2} />
          <p>Cambia el rol de <strong>{selected?.nombre ?? selected?.correo}</strong> dentro del Sistema de Rastreo.</p>
        </div>
        <div className="space-y-2">
          {(['USUARIO', 'ADMIN'] as RastreoRol[]).map(r => (
            <button key={r} type="button" onClick={() => setRol(r)}
              className={['flex w-full items-center gap-3 rounded-lg border px-3.5 py-3 text-left transition-all',
                rol === r ? 'border-teal-500 bg-teal-50 ring-1 ring-teal-500' : 'border-gray-200 hover:bg-gray-50'].join(' ')}>
              <div className={`flex h-9 w-9 items-center justify-center rounded-lg ${r === 'ADMIN' ? 'bg-teal-100 text-teal-700' : 'bg-slate-100 text-slate-600'}`}>
                {r === 'ADMIN' ? <ShieldCheck className="h-4 w-4" /> : <UserCog className="h-4 w-4" />}
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-gray-900">{ROLE_LABEL[r]}</p>
                <p className="text-[12px] text-gray-500">{r === 'ADMIN' ? 'Ve y filtra todos los registros' : 'Solo ve sus propios registros'}</p>
              </div>
              {rol === r && <CheckCircle className="h-4 w-4 text-teal-600 shrink-0" />}
            </button>
          ))}
        </div>
        <div className="flex justify-end gap-2 pt-1">
          <Button type="button" variant="ghost" onClick={() => setView('menu')}>Cancelar</Button>
          <Button type="button" disabled={!changed} loading={roleMutation.isPending}
            onClick={() => roleMutation.mutate({ id: selected!.id, rol })}>Guardar rol</Button>
        </div>
      </div>
    )
  }

  // ─── Confirmar activar/desactivar ──────────────────────────────────────────
  function StatusForm() {
    const willActivate = !selected!.activo
    return (
      <div className="space-y-4">
        <div className={['flex items-start gap-2.5 rounded-lg border px-3.5 py-3 text-[13px]',
          willActivate ? 'border-emerald-200 bg-emerald-50 text-emerald-800' : 'border-red-200 bg-red-50 text-red-800'].join(' ')}>
          {willActivate ? <Power className="h-4 w-4 mt-0.5 shrink-0 text-emerald-600" strokeWidth={2} />
            : <PowerOff className="h-4 w-4 mt-0.5 shrink-0 text-red-600" strokeWidth={2} />}
          <p>
            {willActivate
              ? <>Se reactivará el acceso de <strong>{selected?.nombre ?? selected?.correo}</strong> al Sistema de Rastreo.</>
              : <>Se desactivará a <strong>{selected?.nombre ?? selected?.correo}</strong> y se cerrará su sesión. No podrá iniciar sesión en Rastreo.</>}
          </p>
        </div>
        <div className="flex justify-end gap-2 pt-1">
          <Button type="button" variant="ghost" onClick={() => setView('menu')}>Cancelar</Button>
          <Button type="button" variant={willActivate ? 'primary' : 'danger'} loading={statusMutation.isPending}
            onClick={() => statusMutation.mutate({ id: selected!.id, activo: willActivate })}>
            {willActivate ? 'Activar usuario' : 'Desactivar usuario'}
          </Button>
        </div>
      </div>
    )
  }

  return (
    <div className="p-6 max-w-7xl mx-auto">
      {/* Header */}
      <div className="mb-4">
        <p className="text-xs text-gray-400 tracking-wide mb-1">Administración / Usuarios de Rastreo</p>
        <div className="flex items-center justify-between gap-4 flex-wrap">
          <h1 className="text-2xl font-bold text-gray-900 flex items-center gap-2">
            <Sprout className="h-6 w-6 text-teal-600" />
            Usuarios de Rastreo <span className="font-normal text-gray-400 text-lg">· {total}</span>
          </h1>
          {canCreate && (
            <Button onClick={() => setCreateOpen(true)} className="!bg-teal-600 hover:!bg-teal-700">
              <Sprout className="h-4 w-4" /> Crear usuario
            </Button>
          )}
        </div>
      </div>

      {/* Banner de separación de sistemas */}
      <div className="mb-5 flex items-start gap-3 rounded-xl border border-teal-200 bg-teal-50/70 px-4 py-3">
        <Info className="h-4 w-4 mt-0.5 shrink-0 text-teal-600" strokeWidth={2} />
        <p className="text-[13px] text-teal-800">
          Estás administrando accesos del <strong>Sistema de Rastreo</strong>. Estos usuarios son
          independientes de los usuarios de Repagro Suite y <strong>no tienen acceso a Repagro Suite</strong>.
        </p>
      </div>

      {/* Card */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        <div className="flex items-center justify-between px-5 border-b border-gray-100">
          <div className="flex overflow-x-auto">
            {TABS.map(tab => (
              <button key={tab.key} onClick={() => { setActiveTab(tab.key); setPage(1) }}
                className={`px-4 py-3 text-sm font-medium border-b-2 transition-colors whitespace-nowrap ${
                  activeTab === tab.key ? 'border-teal-600 text-teal-700' : 'border-transparent text-gray-500 hover:text-gray-700'}`}>
                {tab.label}
              </button>
            ))}
          </div>
          <div className="relative py-2 ml-4 shrink-0">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-gray-400 pointer-events-none" />
            <input type="text" placeholder="Buscar por correo o nombre..." value={search}
              onChange={e => { setSearch(e.target.value); setPage(1) }}
              className="pl-8 pr-3 py-1.5 text-sm border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-teal-600/20 focus:border-teal-400 w-56 placeholder:text-gray-400" />
          </div>
        </div>

        {isLoading ? (
          <div className="flex justify-center py-20"><Spinner /></div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm min-w-[680px]">
              <thead>
                <tr className="border-b border-gray-100">
                  {['Usuario', 'Rol', 'Estado', 'Sesión', 'Creado'].map(h => (
                    <th key={h} className="text-left px-5 py-3 text-[11px] font-semibold text-gray-400 uppercase tracking-wider">{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {data?.items.map(u => (
                  <tr key={u.id} onClick={() => openMenu(u)} className="border-b border-gray-50 hover:bg-teal-50/40 transition-colors cursor-pointer">
                    <td className="px-5 py-3.5">
                      <div className="flex items-center gap-3">
                        <div className={`h-9 w-9 rounded-full flex items-center justify-center text-white text-sm font-semibold shrink-0 ${avatarColor(u.nombre ?? u.correo)}`}>
                          {initials(u.nombre ?? u.correo)}
                        </div>
                        <div className="min-w-0">
                          <p className="font-medium text-gray-900 leading-tight truncate">{u.nombre ?? <span className="text-gray-400 italic">Sin nombre</span>}</p>
                          <p className="text-xs text-gray-400 mt-0.5 truncate">{u.correo}</p>
                        </div>
                      </div>
                    </td>
                    <td className="px-5 py-3.5">
                      <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${ROLE_PILL[u.rol]}`}>{ROLE_LABEL[u.rol]}</span>
                    </td>
                    <td className="px-5 py-3.5">
                      <div className="flex items-center gap-1.5">
                        <div className={`h-2 w-2 rounded-full shrink-0 ${u.activo ? 'bg-emerald-400' : 'bg-gray-300'}`} />
                        <span className="text-sm text-gray-700">{u.activo ? 'Activo' : 'Inactivo'}</span>
                      </div>
                    </td>
                    <td className="px-5 py-3.5">
                      {u.tieneSesionActiva
                        ? <span className="inline-flex items-center gap-1 text-xs text-teal-700"><span className="h-1.5 w-1.5 rounded-full bg-teal-500" /> Conectado</span>
                        : <span className="text-xs text-gray-300">—</span>}
                    </td>
                    <td className="px-5 py-3.5 text-sm text-gray-400 whitespace-nowrap">{formatDate(u.fechaCreacion)}</td>
                  </tr>
                ))}
                {!data?.items.length && (
                  <tr><td colSpan={5} className="px-5 py-16 text-center text-gray-400 text-sm">No hay usuarios de rastreo con este filtro</td></tr>
                )}
              </tbody>
            </table>
          </div>
        )}

        <div className="flex items-center justify-between px-5 py-3 border-t border-gray-100 bg-gray-50/40">
          <span className="text-sm text-gray-400">{total} usuarios · página {page} de {totalPages}</span>
          <div className="flex gap-1">
            <button disabled={page === 1} onClick={() => setPage(p => p - 1)}
              className="flex items-center gap-1 px-3 py-1.5 text-sm text-gray-600 border border-gray-200 rounded-md hover:bg-white disabled:opacity-40 disabled:cursor-not-allowed bg-white transition-colors">
              <ChevronLeft className="h-3.5 w-3.5" /> Anterior
            </button>
            <button disabled={page === totalPages} onClick={() => setPage(p => p + 1)}
              className="flex items-center gap-1 px-3 py-1.5 text-sm text-gray-600 border border-gray-200 rounded-md hover:bg-white disabled:opacity-40 disabled:cursor-not-allowed bg-white transition-colors">
              Siguiente <ChevronRight className="h-3.5 w-3.5" />
            </button>
          </div>
        </div>
      </div>

      {/* Modal crear */}
      <Modal open={createOpen} onClose={() => setCreateOpen(false)} title="Crear usuario de Rastreo" size="lg">
        <CreateForm />
      </Modal>

      {/* Modal gestión */}
      <Modal open={!!selected} onClose={closeMenu}
        title={view === 'reset' ? 'Restablecer contraseña' : view === 'role' ? 'Cambiar rol' : view === 'status' ? (selected?.activo ? 'Desactivar usuario' : 'Activar usuario') : 'Gestión de usuario'}
        size="lg">
        {selected && view === 'menu' && (
          <div className="space-y-5">
            <div className="flex items-start gap-4">
              <div className={`h-16 w-16 rounded-full flex items-center justify-center text-white text-xl font-semibold ${avatarColor(selected.nombre ?? selected.correo)} ring-4 ring-white shadow-sm`}>
                {initials(selected.nombre ?? selected.correo)}
              </div>
              <div className="min-w-0 flex-1">
                <h3 className="text-base font-semibold text-gray-900 leading-tight">{selected.nombre ?? 'Sin nombre'}</h3>
                <div className="mt-0.5 flex items-center gap-1.5 text-sm text-gray-500">
                  <Mail className="h-3.5 w-3.5 shrink-0" strokeWidth={1.75} /><span className="truncate">{selected.correo}</span>
                </div>
                <div className="mt-2 flex flex-wrap items-center gap-1.5">
                  <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-[11px] font-medium ${ROLE_PILL[selected.rol]}`}>{ROLE_LABEL[selected.rol]}</span>
                  <span className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[11px] font-medium ${selected.activo ? 'bg-emerald-50 text-emerald-700 ring-1 ring-emerald-200' : 'bg-slate-50 text-slate-600 ring-1 ring-slate-200'}`}>
                    <span className={`h-1.5 w-1.5 rounded-full ${selected.activo ? 'bg-emerald-500' : 'bg-slate-400'}`} />{selected.activo ? 'Activo' : 'Inactivo'}
                  </span>
                  <span className="inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[11px] font-medium bg-teal-50 text-teal-700 ring-1 ring-teal-200">
                    <Sprout className="h-3 w-3" /> Rastreo
                  </span>
                </div>
              </div>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-x-4 gap-y-3 rounded-xl bg-gray-50/60 p-4 border border-gray-100">
              <div className="flex items-start gap-2.5">
                <div className="mt-0.5 flex h-7 w-7 shrink-0 items-center justify-center rounded-md bg-gray-100"><CalendarDays className="h-3.5 w-3.5 text-gray-500" strokeWidth={1.75} /></div>
                <div><p className="text-[11px] uppercase tracking-wider text-gray-400 font-medium">Creado</p><p className="text-sm text-gray-800">{formatDate(selected.fechaCreacion)}</p></div>
              </div>
              <div className="flex items-start gap-2.5">
                <div className="mt-0.5 flex h-7 w-7 shrink-0 items-center justify-center rounded-md bg-gray-100"><KeyRound className="h-3.5 w-3.5 text-gray-500" strokeWidth={1.75} /></div>
                <div><p className="text-[11px] uppercase tracking-wider text-gray-400 font-medium">Sesión</p><p className="text-sm text-gray-800">{selected.tieneSesionActiva ? 'Conectado' : 'Sin sesión activa'}</p></div>
              </div>
            </div>

            {(canReset || canRole || canStatus) ? (
              <div>
                <p className="text-[11px] uppercase tracking-wider text-gray-400 font-semibold mb-2.5">Acciones disponibles</p>
                <div className="space-y-2">
                  {canReset && <ActionItem icon={KeyRound} tone="warning" label="Restablecer contraseña" description="Asigna una nueva contraseña (no se puede ver la actual)" onClick={() => setView('reset')} />}
                  {canRole && <ActionItem icon={UserCog} tone="primary" label="Cambiar rol" description={`Actualmente: ${ROLE_LABEL[selected.rol]} · cambia entre Administrador y Usuario`} onClick={() => setView('role')} />}
                  {canStatus && (selected.activo
                    ? <ActionItem icon={PowerOff} tone="danger" label="Desactivar usuario" description="Bloquea el acceso al Sistema de Rastreo" onClick={() => setView('status')} />
                    : <ActionItem icon={Power} tone="success" label="Activar usuario" description="Restaura el acceso al Sistema de Rastreo" onClick={() => setView('status')} />)}
                </div>
              </div>
            ) : (
              <p className="text-sm text-gray-400 text-center py-4">No tienes permisos para modificar este usuario.</p>
            )}
          </div>
        )}
        {selected && view === 'reset' && <ResetForm />}
        {selected && view === 'role' && <RoleForm />}
        {selected && view === 'status' && <StatusForm />}
      </Modal>
    </div>
  )
}
