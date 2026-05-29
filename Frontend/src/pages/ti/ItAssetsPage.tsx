import { useState } from 'react'
import { useQuery, keepPreviousData } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { Plus, Search, Cpu, ChevronLeft, ChevronRight } from 'lucide-react'

import { itAssetsApi, itCatalogsApi } from '@/api/itAssets'
import { qk, staleTimes } from '@/lib/queryKeys'
import { useAuthStore } from '@/store/authStore'
import Chip from '@/components/ui/Chip'
import { statusChipVariant, STATUS_OPTIONS } from '@/components/ti/itStatus'

const BRAND = '#0E6B4B'
const PAGE_SIZE = 20

export default function ItAssetsPage() {
  const { hasPermission } = useAuthStore()
  const canCreate = hasPermission('Ti.Inventory.Create')

  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState('')
  const [typeId, setTypeId] = useState('')

  const catalogs = useQuery({
    queryKey: qk.ti.catalogs,
    queryFn: () => itCatalogsApi.getAll().then(r => r.data.data!),
    staleTime: staleTimes.tiCatalogs,
  })

  const { data, isLoading, isFetching } = useQuery({
    queryKey: qk.ti.assets(page, PAGE_SIZE, search, status, typeId, ''),
    queryFn: () => itAssetsApi.getAll({
      page, pageSize: PAGE_SIZE,
      search: search || undefined,
      status: status || undefined,
      assetTypeId: typeId || undefined,
    }).then(r => r.data.data!),
    staleTime: staleTimes.ti,
    placeholderData: keepPreviousData,
  })

  const items = data?.items ?? []
  const totalPages = data?.totalPages ?? 1

  function resetAnd(fn: () => void) { setPage(1); fn() }

  return (
    <div className="flex min-h-full flex-col">
      <header className="sticky top-0 z-10 flex items-center gap-4 border-b border-line bg-paper px-6 py-3" style={{ minHeight: 64 }}>
        <div className="min-w-0 flex-1">
          <p className="font-mono text-[12px] text-ink2 mb-0.5 leading-none">TI / Inventario</p>
          <h1 className="text-[18px] font-semibold text-ink leading-tight tracking-tight flex items-center gap-2">
            <Cpu className="h-4.5 w-4.5" style={{ color: BRAND }} /> Activos tecnológicos
          </h1>
        </div>
        {canCreate && (
          <Link to="/ti/assets/new" className="inline-flex items-center gap-1.5 rounded-[8px] px-3.5 py-2 text-sm font-medium text-white transition-colors hover:opacity-90" style={{ background: BRAND }}>
            <Plus className="h-4 w-4" /> Nuevo activo
          </Link>
        )}
      </header>

      <div className="flex-1 p-6 bg-bg space-y-3.5">
        {/* Filtros */}
        <div className="flex flex-wrap items-center gap-2.5">
          <label className="relative flex flex-1 min-w-[220px] items-center">
            <Search className="pointer-events-none absolute left-3 h-4 w-4 text-ink2" />
            <input
              type="search" value={search}
              onChange={(e) => resetAnd(() => setSearch(e.target.value))}
              placeholder="Buscar por código, serie, modelo o etiqueta"
              className="h-10 w-full rounded-[8px] border border-line bg-paper pl-9 pr-3 text-sm text-ink placeholder:text-ink2 focus:border-brand-400 focus:outline-none"
            />
          </label>
          <select value={status} onChange={(e) => resetAnd(() => setStatus(e.target.value))}
            className="h-10 rounded-[8px] border border-line bg-paper px-3 text-sm text-ink focus:border-brand-400 focus:outline-none">
            <option value="">Todos los estados</option>
            {STATUS_OPTIONS.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
          </select>
          <select value={typeId} onChange={(e) => resetAnd(() => setTypeId(e.target.value))}
            className="h-10 rounded-[8px] border border-line bg-paper px-3 text-sm text-ink focus:border-brand-400 focus:outline-none">
            <option value="">Todos los tipos</option>
            {(catalogs.data?.types ?? []).map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
          </select>
        </div>

        {/* Tabla */}
        <div className="overflow-hidden rounded-[10px] border border-line bg-paper shadow-sh1">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-line text-left text-[11px] uppercase tracking-wider text-ink2">
                  <th className="px-4 py-3 font-medium">Código</th>
                  <th className="px-4 py-3 font-medium">Tipo</th>
                  <th className="px-4 py-3 font-medium">Marca / Modelo</th>
                  <th className="px-4 py-3 font-medium">Serie</th>
                  <th className="px-4 py-3 font-medium">Responsable</th>
                  <th className="px-4 py-3 font-medium">Estado</th>
                </tr>
              </thead>
              <tbody>
                {isLoading ? (
                  Array.from({ length: 6 }).map((_, i) => (
                    <tr key={i} className="border-b border-line last:border-0">
                      <td colSpan={6} className="px-4 py-3"><div className="h-5 animate-pulse rounded bg-gray-100" /></td>
                    </tr>
                  ))
                ) : items.length === 0 ? (
                  <tr><td colSpan={6} className="px-4 py-12 text-center text-ink2">
                    No hay activos que coincidan. {canCreate && <Link to="/ti/assets/new" className="font-medium" style={{ color: BRAND }}>Registrar el primero →</Link>}
                  </td></tr>
                ) : (
                  items.map(a => (
                    <tr key={a.id} className="border-b border-line last:border-0 hover:bg-bg transition-colors">
                      <td className="px-4 py-3">
                        <Link to={`/ti/assets/${a.id}`} className="font-mono font-medium text-ink hover:underline" style={{ textDecorationColor: BRAND }}>
                          {a.internalCode}
                        </Link>
                      </td>
                      <td className="px-4 py-3 text-ink">{a.assetTypeName}</td>
                      <td className="px-4 py-3 text-ink2">{[a.brandName, a.model].filter(Boolean).join(' · ') || '—'}</td>
                      <td className="px-4 py-3 font-mono text-[12px] text-ink2">{a.serialNumber ?? '—'}</td>
                      <td className="px-4 py-3 text-ink2">{a.currentHolderName ?? '—'}</td>
                      <td className="px-4 py-3"><Chip variant={statusChipVariant(a.status)} label={a.statusName} /></td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>

        {/* Paginación */}
        {totalPages > 1 && (
          <div className="flex items-center justify-between">
            <p className="text-[13px] text-ink2">{data?.totalCount ?? 0} activos · página {page} de {totalPages}</p>
            <div className="flex items-center gap-2">
              <button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page <= 1 || isFetching}
                className="inline-flex items-center gap-1 rounded-[8px] border border-line bg-paper px-3 py-2 text-sm text-ink disabled:opacity-40">
                <ChevronLeft className="h-4 w-4" /> Anterior
              </button>
              <button onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page >= totalPages || isFetching}
                className="inline-flex items-center gap-1 rounded-[8px] border border-line bg-paper px-3 py-2 text-sm text-ink disabled:opacity-40">
                Siguiente <ChevronRight className="h-4 w-4" />
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
