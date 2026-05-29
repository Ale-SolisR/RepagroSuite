import { useState } from 'react'
import { useQuery, keepPreviousData } from '@tanstack/react-query'
import { Search, UserPlus, Users, ChevronLeft, ChevronRight } from 'lucide-react'

import { itEmployeesApi } from '@/api/itEmployees'
import { qk, staleTimes } from '@/lib/queryKeys'
import { useAuthStore } from '@/store/authStore'
import Chip from '@/components/ui/Chip'
import EmployeeCreateModal from '@/components/ti/EmployeeCreateModal'

const BRAND = '#0E6B4B'
const PAGE_SIZE = 20

export default function ItEmployeesPage() {
  const { hasPermission } = useAuthStore()
  const canManage = hasPermission('Ti.Employee.Manage')
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [modal, setModal] = useState(false)

  const { data, isLoading, isFetching } = useQuery({
    queryKey: [...qk.ti.all, 'employees', page, search] as const,
    queryFn: () => itEmployeesApi.getAll({ page, pageSize: PAGE_SIZE, search: search || undefined }).then(r => r.data.data!),
    staleTime: staleTimes.ti,
    placeholderData: keepPreviousData,
  })

  const items = data?.items ?? []
  const totalPages = data?.totalPages ?? 1

  return (
    <div className="flex min-h-full flex-col">
      <header className="sticky top-0 z-10 flex items-center gap-4 border-b border-line bg-paper px-6 py-3" style={{ minHeight: 64 }}>
        <div className="min-w-0 flex-1">
          <p className="font-mono text-[12px] text-ink2 mb-0.5 leading-none">TI / Colaboradores</p>
          <h1 className="text-[18px] font-semibold text-ink leading-tight tracking-tight flex items-center gap-2">
            <Users className="h-4.5 w-4.5" style={{ color: BRAND }} /> Colaboradores
          </h1>
        </div>
        {canManage && (
          <button onClick={() => setModal(true)} className="inline-flex items-center gap-1.5 rounded-[8px] px-3.5 py-2 text-sm font-medium text-white transition-colors hover:opacity-90" style={{ background: BRAND }}>
            <UserPlus className="h-4 w-4" /> Nuevo colaborador
          </button>
        )}
      </header>

      <div className="flex-1 p-6 bg-bg space-y-3.5">
        <label className="relative flex max-w-md items-center">
          <Search className="pointer-events-none absolute left-3 h-4 w-4 text-ink2" />
          <input type="search" value={search} onChange={e => { setPage(1); setSearch(e.target.value) }}
            placeholder="Buscar por nombre, cédula o puesto"
            className="h-10 w-full rounded-[8px] border border-line bg-paper pl-9 pr-3 text-sm text-ink placeholder:text-ink2 focus:border-brand-400 focus:outline-none" />
        </label>

        <div className="overflow-hidden rounded-[10px] border border-line bg-paper shadow-sh1">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-line text-left text-[11px] uppercase tracking-wider text-ink2">
                  <th className="px-4 py-3 font-medium">Cédula</th>
                  <th className="px-4 py-3 font-medium">Nombre</th>
                  <th className="px-4 py-3 font-medium">Puesto</th>
                  <th className="px-4 py-3 font-medium">Estado</th>
                </tr>
              </thead>
              <tbody>
                {isLoading ? (
                  Array.from({ length: 6 }).map((_, i) => (
                    <tr key={i} className="border-b border-line last:border-0"><td colSpan={4} className="px-4 py-3"><div className="h-5 animate-pulse rounded bg-gray-100" /></td></tr>
                  ))
                ) : items.length === 0 ? (
                  <tr><td colSpan={4} className="px-4 py-12 text-center text-ink2">
                    No hay colaboradores. {canManage && <button onClick={() => setModal(true)} className="font-medium" style={{ color: BRAND }}>Crear el primero →</button>}
                  </td></tr>
                ) : items.map(e => (
                  <tr key={e.id} className="border-b border-line last:border-0 hover:bg-bg transition-colors">
                    <td className="px-4 py-3 font-mono text-ink2">{e.identificationNumber}</td>
                    <td className="px-4 py-3 font-medium text-ink">{e.fullName}</td>
                    <td className="px-4 py-3 text-ink2">{e.position ?? '—'}</td>
                    <td className="px-4 py-3"><Chip variant={e.isActive ? 'ok' : 'gray'} label={e.isActive ? 'Activo' : 'Inactivo'} /></td>
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

      <EmployeeCreateModal open={modal} onClose={() => setModal(false)} />
    </div>
  )
}
