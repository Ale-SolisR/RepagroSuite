import { useParams, useNavigate, Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import {
  ArrowLeft, Loader2, UsersRound, Cpu, FileText, History, Mail, Phone, IdCard, Briefcase,
} from 'lucide-react'
import { format, parseISO } from 'date-fns'
import { es } from 'date-fns/locale'

import { itEmployeesApi } from '@/api/itEmployees'
import { qk, staleTimes } from '@/lib/queryKeys'
import Chip from '@/components/ui/Chip'
import { statusChipVariant } from '@/components/ti/itStatus'
import type { ItEmployeeAssignmentDto } from '@/types'

const BRAND = '#0E6B4B'

const CLOSED_REASON_LABEL: Record<string, string> = {
  Devolucion: 'Devolución',
  Desasignacion: 'Desasignación',
  Deterioro: 'Deterioro',
  PerdidaRobo: 'Pérdida / robo',
}

function fmt(d?: string) {
  return d ? format(parseISO(d), 'd MMM yyyy', { locale: es }) : '—'
}

function AssignmentCard({ a }: { a: ItEmployeeAssignmentDto }) {
  return (
    <div className="w-full max-w-[520px] rounded-[10px] border border-line bg-paper p-4 shadow-sm">
      <div className="flex items-start gap-3">
        {a.assetPhotos.length > 0 && (
          <Link to={`/ti/assets/${a.assetId}`} className="relative h-20 w-20 shrink-0 overflow-hidden rounded-lg border border-line bg-white shadow-sm ring-2 ring-white" title="Ver activo">
            <img src={a.assetPhotos[0]} alt={`Foto de ${a.assetCode}`} className="h-full w-full object-cover" />
            {a.assetPhotos.length > 1 && (
              <span className="absolute bottom-1 right-1 rounded bg-black/70 px-1.5 py-0.5 text-[10px] font-semibold leading-tight text-white">
                +{a.assetPhotos.length - 1}
              </span>
            )}
          </Link>
        )}
        <div className="min-w-0 flex-1">
          <Link to={`/ti/assets/${a.assetId}`} className="font-mono text-sm font-semibold text-ink hover:underline" style={{ textDecorationColor: BRAND }}>
            {a.assetCode}
          </Link>
          <p className="truncate text-[13px] text-ink2">
            {[a.assetTypeName, a.assetModel].filter(Boolean).join(' · ') || '—'}
          </p>
        </div>
        <Chip variant={statusChipVariant(a.assetStatus)} label={a.assetStatusName} />
      </div>

      <dl className="mt-3 grid grid-cols-2 gap-x-4 gap-y-1.5 text-[12px]">
        <div><dt className="text-ink2">Entregado</dt><dd className="font-medium text-ink">{fmt(a.assignedAt)}</dd></div>
        <div>
          <dt className="text-ink2">{a.isActive ? 'Estado' : 'Cerrado'}</dt>
          <dd className="font-medium text-ink">
            {a.isActive ? 'Vigente' : `${fmt(a.returnedAt)}${a.closedReason ? ` · ${CLOSED_REASON_LABEL[a.closedReason] ?? a.closedReason}` : ''}`}
          </dd>
        </div>
        {a.conditionOutName && <div><dt className="text-ink2">Condición entrega</dt><dd className="font-medium text-ink">{a.conditionOutName}</dd></div>}
        {a.conditionInName && <div><dt className="text-ink2">Condición recepción</dt><dd className="font-medium text-ink">{a.conditionInName}</dd></div>}
      </dl>

      {a.notes && <p className="mt-2 rounded-lg bg-bg p-2 text-[12px] text-ink2">{a.notes}</p>}

      <div className="mt-3 flex flex-wrap items-center gap-x-3 gap-y-1">
        {a.assignedTicketId && (
          <Link to={`/ti/tickets/${a.assignedTicketId}`} className="inline-flex items-center gap-1 text-[12px] font-medium hover:underline" style={{ color: BRAND }}>
            <FileText className="h-3.5 w-3.5" /> {a.assignedTicketNumber ?? 'Boleta entrega'}
          </Link>
        )}
        {a.closingTicketId && (
          <Link to={`/ti/tickets/${a.closingTicketId}`} className="inline-flex items-center gap-1 text-[12px] font-medium hover:underline" style={{ color: BRAND }}>
            <FileText className="h-3.5 w-3.5" /> {a.closingTicketNumber ?? 'Boleta cierre'}
          </Link>
        )}
      </div>
    </div>
  )
}

function TicketStatusChip({ status }: { status: string }) {
  const isVoided = status === 'Anulada'
  return (
    <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-[11px] font-medium ring-1 ring-inset ${
      isVoided
        ? 'bg-slate-50 text-slate-600 ring-slate-200'
        : 'bg-emerald-50 text-emerald-700 ring-emerald-200'
    }`}>
      {isVoided ? 'Anulada' : 'Activa'}
    </span>
  )
}

export default function ItEmployeeDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()

  const { data, isLoading } = useQuery({
    queryKey: qk.ti.employeeHistory(id ?? ''),
    queryFn: () => itEmployeesApi.getHistory(id!).then(r => r.data.data!),
    enabled: !!id,
    staleTime: staleTimes.ti,
  })

  if (isLoading) return <div className="flex h-full items-center justify-center"><Loader2 className="h-6 w-6 animate-spin text-ink2" /></div>
  if (!data) return <div className="p-6 text-ink2">Colaborador no encontrado.</div>

  const e = data.employee

  return (
    <div className="flex min-h-full flex-col">
      <header className="sticky top-0 z-10 flex items-center gap-3 border-b border-line bg-paper px-4 sm:px-6 py-3" style={{ minHeight: 64 }}>
        <button onClick={() => navigate(-1)} className="rounded p-1.5 text-ink2 hover:bg-bg hover:text-ink" aria-label="Volver">
          <ArrowLeft className="h-5 w-5" />
        </button>
        <div className="min-w-0 flex-1">
          <p className="font-mono text-[12px] text-ink2 mb-0.5 leading-none">TI / Colaboradores</p>
          <h1 className="flex items-center gap-2 text-[18px] font-semibold leading-tight tracking-tight text-ink">
            <UsersRound className="h-4.5 w-4.5" style={{ color: BRAND }} />
            <span className="truncate">{e.fullName}</span>
            <Chip variant={e.isActive ? 'ok' : 'gray'} label={e.isActive ? 'Activo' : 'Inactivo'} />
          </h1>
        </div>
      </header>

      <div className="flex-1 space-y-3.5 bg-bg p-4 sm:p-6">
        {/* Datos + KPIs */}
        <div className="grid gap-3.5 lg:grid-cols-3">
          <section className="rounded-[10px] border border-line bg-paper p-5 shadow-sh1 lg:col-span-2">
            <h2 className="mb-3 text-sm font-semibold text-ink">Datos del colaborador</h2>
            <div className="grid gap-3 sm:grid-cols-2">
              <Field icon={IdCard} label="Cédula" value={e.identificationNumber} mono />
              <Field icon={Briefcase} label="Puesto" value={e.position} />
              <Field icon={UsersRound} label="Departamento" value={e.department} />
              <Field icon={Mail} label="Correo" value={e.email} />
              <Field icon={Phone} label="Teléfono" value={e.phoneNumber} mono />
            </div>
          </section>

          <div className="grid grid-cols-2 gap-3.5 lg:grid-cols-1">
            <KpiCard label="Activos a cargo" value={data.currentAssetsCount} accent={BRAND} />
            <KpiCard label="Histórico de equipos" value={data.pastAssetsCount} accent="#475569" />
          </div>
        </div>

        {/* Activos actuales */}
        <section className="rounded-[10px] border border-line bg-paper p-5 shadow-sh1">
          <h2 className="mb-3 flex items-center gap-2 text-sm font-semibold text-ink">
            <Cpu className="h-4 w-4" style={{ color: BRAND }} /> Activos asignados actualmente
          </h2>
          {data.currentAssignments.length === 0 ? (
            <p className="rounded-lg bg-bg p-4 text-center text-[13px] text-ink2">Sin activos asignados.</p>
          ) : (
            <div className="grid justify-start gap-3 sm:grid-cols-[repeat(auto-fill,minmax(min(100%,360px),520px))]">
              {data.currentAssignments.map(a => <AssignmentCard key={a.id} a={a} />)}
            </div>
          )}
        </section>

        {/* Boletas (primero, por petición) */}
        <section className="rounded-[10px] border border-line bg-paper p-5 shadow-sh1">
          <h2 className="mb-3 flex items-center gap-2 text-sm font-semibold text-ink">
            <FileText className="h-4 w-4" style={{ color: BRAND }} /> Boletas del colaborador
          </h2>
          {data.tickets.length === 0 ? (
            <p className="rounded-lg bg-bg p-4 text-center text-[13px] text-ink2">Sin boletas.</p>
          ) : (
            <div className="-mx-1 overflow-x-auto">
              <table className="w-full min-w-[560px] text-sm">
                <thead>
                  <tr className="border-b border-line text-left text-[11px] uppercase tracking-wider text-ink2">
                    <th className="px-2 py-2 font-medium">Consecutivo</th>
                    <th className="px-2 py-2 font-medium">Tipo</th>
                    <th className="px-2 py-2 font-medium">Estado</th>
                    <th className="px-2 py-2 font-medium">Fecha</th>
                    <th className="px-2 py-2 font-medium text-center">Activos</th>
                  </tr>
                </thead>
                <tbody>
                  {data.tickets.map(t => (
                    <tr key={t.id} className={`border-b border-line last:border-0 hover:bg-bg ${t.status === 'Anulada' ? 'bg-slate-50/70' : ''}`}>
                      <td className="px-2 py-2.5">
                        <Link to={`/ti/tickets/${t.id}`} className="font-mono font-medium text-ink hover:underline" style={{ textDecorationColor: BRAND }}>
                          {t.ticketNumber}
                        </Link>
                      </td>
                      <td className="px-2 py-2.5 text-ink2">{t.ticketTypeName}</td>
                      <td className="px-2 py-2.5"><TicketStatusChip status={t.status} /></td>
                      <td className="px-2 py-2.5 font-mono text-[12px] text-ink2">{fmt(t.issuedAt)}</td>
                      <td className="px-2 py-2.5 text-center text-ink2">{t.assetCount}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>

        {/* Histórico (asignaciones anteriores, debajo de Boletas) */}
        <section className="rounded-[10px] border border-line bg-paper p-5 shadow-sh1">
          <h2 className="mb-3 flex items-center gap-2 text-sm font-semibold text-ink">
            <History className="h-4 w-4" style={{ color: BRAND }} /> Asignaciones anteriores
          </h2>
          {data.pastAssignments.length === 0 ? (
            <p className="rounded-lg bg-bg p-4 text-center text-[13px] text-ink2">Sin historial previo.</p>
          ) : (
            <div className="grid justify-start gap-3 sm:grid-cols-[repeat(auto-fill,minmax(min(100%,360px),520px))]">
              {data.pastAssignments.map(a => <AssignmentCard key={a.id} a={a} />)}
            </div>
          )}
        </section>
      </div>
    </div>
  )
}

function Field({ icon: Icon, label, value, mono }: { icon: typeof Mail; label: string; value?: string; mono?: boolean }) {
  return (
    <div className="flex items-start gap-2.5">
      <Icon className="mt-0.5 h-4 w-4 shrink-0 text-ink2" />
      <div className="min-w-0">
        <p className="text-[11px] uppercase tracking-wide text-ink2">{label}</p>
        <p className={`text-[13px] font-medium text-ink ${mono ? 'font-mono' : ''}`}>{value || '—'}</p>
      </div>
    </div>
  )
}

function KpiCard({ label, value, accent }: { label: string; value: number; accent: string }) {
  return (
    <div className="rounded-[10px] border border-line bg-paper p-4 shadow-sh1">
      <p className="text-[12px] text-ink2">{label}</p>
      <p className="mt-1 text-2xl font-bold tabular-nums" style={{ color: accent }}>{value}</p>
    </div>
  )
}
