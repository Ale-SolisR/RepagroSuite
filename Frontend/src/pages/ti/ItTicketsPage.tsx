import { useState } from 'react'
import { useQuery, keepPreviousData } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { Search, FileText, ChevronLeft, ChevronRight, ClipboardCheck } from 'lucide-react'

import { itTicketsApi } from '@/api/itTickets'
import { qk, staleTimes } from '@/lib/queryKeys'
import { useAuthStore } from '@/store/authStore'
import { format, parseISO } from 'date-fns'
import { es } from 'date-fns/locale'
import Chip from '@/components/ui/Chip'
import { ticketStatusChipVariant } from '@/components/ti/itStatus'

const BRAND = '#0E6B4B'
const PAGE_SIZE = 20

const TYPE_OPTIONS = [
  ['Entrega', 'Entrega'], ['Devolucion', 'Devolución'], ['Prestamo', 'Préstamo'],
  ['Mantenimiento', 'Mantenimiento'], ['Reparacion', 'Reparación'], ['Traslado', 'Traslado'],
  ['CambioResponsable', 'Cambio responsable'], ['AsignacionAccesorios', 'Accesorios'], ['Baja', 'Baja'],
]

export default function ItTicketsPage() {
  const { hasPermission } = useAuthStore()
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [type, setType] = useState('')
  const [status, setStatus] = useState('')

  const { data, isLoading, isFetching } = useQuery({
    queryKey: qk.ti.tickets(page, type, status, search),
    queryFn: () => itTicketsApi.getAll({
      page, pageSize: PAGE_SIZE, type: type || undefined, status: status || undefined, search: search || undefined,
    }).then(r => r.data.data!),
    staleTime: staleTimes.ti,
    placeholderData: keepPreviousData,
  })

  const items = data?.items ?? []
  const totalPages = data?.totalPages ?? 1
  const reset = (fn: () => void) => { setPage(1); fn() }

  return (
    <div className="flex min-h-full flex-col">
      <header className="sticky top-0 z-10 flex items-center gap-4 border-b border-line bg-paper px-6 py-3" style={{ minHeight: 64 }}>
        <div className="min-w-0 flex-1">
          <p className="font-mono text-[12px] text-ink2 mb-0.5 leading-none">TI / Boletas</p>
          <h1 className="text-[18px] font-semibold text-ink leading-tight tracking-tight flex items-center gap-2">
            <FileText className="h-4.5 w-4.5" style={{ color: BRAND }} /> Boletas TI
          </h1>
        </div>
        {hasPermission('Ti.Assign') && (
          <Link to="/ti/assignments/new" className="inline-flex items-center gap-1.5 rounded-[8px] px-3.5 py-2 text-sm font-medium text-white transition-colors hover:opacity-90" style={{ background: BRAND }}>
            <ClipboardCheck className="h-4 w-4" /> Nueva entrega
          </Link>
        )}
      </header>

      <div className="flex-1 p-6 bg-bg space-y-3.5">
        <div className="flex flex-wrap items-center gap-2.5">
          <label className="relative flex flex-1 min-w-[200px] items-center">
            <Search className="pointer-events-none absolute left-3 h-4 w-4 text-ink2" />
            <input type="search" value={search} onChange={e => reset(() => setSearch(e.target.value))}
              placeholder="Buscar por consecutivo" className="h-10 w-full rounded-[8px] border border-line bg-paper pl-9 pr-3 text-sm text-ink placeholder:text-ink2 focus:border-brand-400 focus:outline-none" />
          </label>
          <select value={type} onChange={e => reset(() => setType(e.target.value))} className="h-10 rounded-[8px] border border-line bg-paper px-3 text-sm text-ink focus:border-brand-400 focus:outline-none">
            <option value="">Todos los tipos</option>
            {TYPE_OPTIONS.map(([v, l]) => <option key={v} value={v}>{l}</option>)}
          </select>
          <select value={status} onChange={e => reset(() => setStatus(e.target.value))} className="h-10 rounded-[8px] border border-line bg-paper px-3 text-sm text-ink focus:border-brand-400 focus:outline-none">
            <option value="">Todos los estados</option>
            <option value="Emitida">Emitida</option>
            <option value="Anulada">Anulada</option>
          </select>
        </div>

        <div className="overflow-hidden rounded-[10px] border border-line bg-paper shadow-sh1">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-line text-left text-[11px] uppercase tracking-wider text-ink2">
                  <th className="px-4 py-3 font-medium">Consecutivo</th>
                  <th className="px-4 py-3 font-medium">Tipo</th>
                  <th className="px-4 py-3 font-medium">Colaborador</th>
                  <th className="px-4 py-3 font-medium">Activos</th>
                  <th className="px-4 py-3 font-medium">Fecha</th>
                  <th className="px-4 py-3 font-medium">Estado</th>
                </tr>
              </thead>
              <tbody>
                {isLoading ? (
                  Array.from({ length: 6 }).map((_, i) => (
                    <tr key={i} className="border-b border-line last:border-0"><td colSpan={6} className="px-4 py-3"><div className="h-5 animate-pulse rounded bg-gray-100" /></td></tr>
                  ))
                ) : items.length === 0 ? (
                  <tr><td colSpan={6} className="px-4 py-12 text-center text-ink2">No hay boletas.</td></tr>
                ) : items.map(t => (
                  <tr key={t.id} className="border-b border-line last:border-0 hover:bg-bg transition-colors">
                    <td className="px-4 py-3"><Link to={`/ti/tickets/${t.id}`} className="font-mono font-medium text-ink hover:underline">{t.ticketNumber}</Link></td>
                    <td className="px-4 py-3 text-ink">{t.ticketTypeName}</td>
                    <td className="px-4 py-3 text-ink2">{t.employeeName ?? '—'}</td>
                    <td className="px-4 py-3 font-mono text-ink2">{t.assetCount}</td>
                    <td className="px-4 py-3 text-ink2">{format(parseISO(t.issuedAt), 'd MMM yyyy · HH:mm', { locale: es })}</td>
                    <td className="px-4 py-3"><Chip variant={ticketStatusChipVariant(t.status)} label={t.statusName} /></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        {totalPages > 1 && (
          <div className="flex items-center justify-between">
            <p className="text-[13px] text-ink2">{data?.totalCount ?? 0} boletas · página {page} de {totalPages}</p>
            <div className="flex items-center gap-2">
              <button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page <= 1 || isFetching} className="inline-flex items-center gap-1 rounded-[8px] border border-line bg-paper px-3 py-2 text-sm text-ink disabled:opacity-40"><ChevronLeft className="h-4 w-4" /> Anterior</button>
              <button onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page >= totalPages || isFetching} className="inline-flex items-center gap-1 rounded-[8px] border border-line bg-paper px-3 py-2 text-sm text-ink disabled:opacity-40">Siguiente <ChevronRight className="h-4 w-4" /></button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
