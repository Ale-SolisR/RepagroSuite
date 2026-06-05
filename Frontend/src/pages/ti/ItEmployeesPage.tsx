import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery, keepPreviousData } from '@tanstack/react-query'
import { Search, UserPlus, Users, ChevronLeft, ChevronRight, Pencil, ChevronRight as Arrow } from 'lucide-react'

import { itEmployeesApi } from '@/api/itEmployees'
import { qk, staleTimes } from '@/lib/queryKeys'
import { useAuthStore } from '@/store/authStore'
import Chip from '@/components/ui/Chip'
import EmployeeCreateModal from '@/components/ti/EmployeeCreateModal'
import type { ItEmployeeDto } from '@/types'

const BRAND = '#0E6B4B'
const PAGE_SIZE = 20

export default function ItEmployeesPage() {
  const navigate = useNavigate()
  const { hasPermission } = useAuthStore()
  const canManage = hasPermission('Ti.Employee.Manage')
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState('')
  const [creating, setCreating] = useState(false)
  const [editingEmployee, setEditingEmployee] = useState<ItEmployeeDto | null>(null)

  const { data, isLoading, isFetching } = useQuery({
    queryKey: [...qk.ti.all, 'employees', page, search, status] as const,
    queryFn: () => itEmployeesApi.getAll({
      page,
      pageSize: PAGE_SIZE,
      search: search || undefined,
      activeOnly: status === '' ? undefined : status === 'active',
    }).then(r => r.data.data!),
    staleTime: staleTimes.ti,
    placeholderData: keepPreviousData,
  })

  const items = data?.items ?? []
  const totalPages = data?.totalPages ?? 1

  return (
    <div className="flex min-h-full flex-col">
      <header className="sticky top-0 z-10 flex flex-wrap items-center gap-3 border-b border-line bg-paper px-4 sm:px-6 py-3" style={{ minHeight: 64 }}>
        <div className="min-w-0 flex-1">
          <p className="font-mono text-[12px] text-ink2 mb-0.5 leading-none">TI / Colaboradores</p>
          <h1 className="text-[18px] font-semibold text-ink leading-tight tracking-tight flex items-center gap-2">
            <Users className="h-4.5 w-4.5" style={{ color: BRAND }} /> Colaboradores
          </h1>
        </div>
        {canManage && (
          <button onClick={() => setCreating(true)} className="inline-flex items-center gap-1.5 rounded-[8px] px-3.5 py-2 text-sm font-medium text-white transition-colors hover:opacity-90" style={{ background: BRAND }}>
            <UserPlus className="h-4 w-4" /> Nuevo colaborador
          </button>
        )}
      </header>

      <div className="flex-1 p-4 sm:p-6 bg-bg space-y-3.5">
        <label className="relative flex max-w-md items-center">
          <Search className="pointer-events-none absolute left-3 h-4 w-4 text-ink2" />
          <input type="search" value={search} onChange={e => { setPage(1); setSearch(e.target.value) }}
            placeholder="Buscar por nombre, cédula o puesto"
            className="h-10 w-full rounded-[8px] border border-line bg-paper pl-9 pr-3 text-sm text-ink placeholder:text-ink2 focus:border-brand-400 focus:outline-none" />
        </label>
        <select
          value={status}
          onChange={e => { setPage(1); setStatus(e.target.value) }}
          className="h-10 w-full max-w-xs rounded-[8px] border border-line bg-paper px-3 text-sm text-ink focus:border-brand-400 focus:outline-none"
        >
          <option value="">Todos los estados</option>
          <option value="active">Activos</option>
          <option value="inactive">Inactivos</option>
        </select>

        <div className="overflow-hidden rounded-[10px] border border-line bg-paper shadow-sh1">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-line text-left text-[11px] uppercase tracking-wider text-ink2">
                  <th className="px-4 py-3 font-medium">Cédula</th>
                  <th className="px-4 py-3 font-medium">Nombre</th>
                  <th className="px-4 py-3 font-medium">Puesto</th>
                  <th className="px-4 py-3 font-medium">Contacto</th>
                  <th className="px-4 py-3 font-medium">Estado</th>
                  <th className="px-4 py-3 font-medium text-right">Acciones</th>
                </tr>
              </thead>
              <tbody>
                {isLoading ? (
                  Array.from({ length: 6 }).map((_, i) => (
                    <tr key={i} className="border-b border-line last:border-0"><td colSpan={6} className="px-4 py-3"><div className="h-5 animate-pulse rounded bg-gray-100" /></td></tr>
                  ))
                ) : items.length === 0 ? (
                  <tr><td colSpan={6} className="px-4 py-12 text-center text-ink2">
                    No hay colaboradores. {canManage && <button onClick={() => setCreating(true)} className="font-medium" style={{ color: BRAND }}>Crear el primero →</button>}
                  </td></tr>
                ) : items.map(e => (
                  <tr
                    key={e.id}
                    onClick={() => navigate(`/ti/employees/${e.id}`)}
                    className="cursor-pointer border-b border-line last:border-0 hover:bg-bg transition-colors"
                    title="Ver historial del colaborador"
                  >
                    <td className="px-4 py-3 font-mono text-ink2">{e.identificationNumber}</td>
                    <td className="px-4 py-3 font-medium text-ink">{e.fullName}</td>
                    <td className="px-4 py-3 text-ink2">{e.position ?? '—'}</td>
                    <td className="px-4 py-3 text-ink2">
                      <div className="flex flex-col gap-0.5">
                        <span>{e.email ?? '—'}</span>
                        {e.phoneNumber && <span className="font-mono text-[12px]">{e.phoneNumber}</span>}
                      </div>
                    </td>
                    <td className="px-4 py-3"><Chip variant={e.isActive ? 'ok' : 'gray'} label={e.isActive ? 'Activo' : 'Inactivo'} /></td>
                    <td className="px-4 py-3">
                      <div className="flex items-center justify-end gap-1">
                        {canManage && (
                          <button
                            onClick={(ev) => { ev.stopPropagation(); setEditingEmployee(e) }}
                            className="flex h-8 w-8 items-center justify-center rounded-[8px] text-ink2 hover:bg-paper hover:text-ink"
                            title="Editar colaborador" aria-label={`Editar ${e.fullName}`}
                          >
                            <Pencil className="h-4 w-4" />
                          </button>
                        )}
                        <Arrow className="h-4 w-4 text-ink2" />
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        {totalPages > 1 && (
          <div className="flex items-center justify-between">
            <p className="text-[13px] text-ink2">{data?.totalCount ?? 0} colaboradores · página {page} de {totalPages}</p>
            <div className="flex items-center gap-2">
              <button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page <= 1 || isFetching} className="inline-flex items-center gap-1 rounded-[8px] border border-line bg-paper px-3 py-2 text-sm text-ink disabled:opacity-40"><ChevronLeft className="h-4 w-4" /> Anterior</button>
              <button onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page >= totalPages || isFetching} className="inline-flex items-center gap-1 rounded-[8px] border border-line bg-paper px-3 py-2 text-sm text-ink disabled:opacity-40">Siguiente <ChevronRight className="h-4 w-4" /></button>
            </div>
          </div>
        )}
      </div>

      <EmployeeCreateModal
        open={creating || !!editingEmployee}
        employee={editingEmployee}
        onClose={() => { setCreating(false); setEditingEmployee(null) }}
      />
    </div>
  )
}
