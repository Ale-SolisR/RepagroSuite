import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import {
  Plus, AlertCircle, Cpu, FileDown, RefreshCw, ShieldCheck, Activity,
  Boxes, CheckCircle2, Wrench, AlertTriangle, Clock, Lightbulb,
} from 'lucide-react'
import {
  BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, Cell,
  PieChart, Pie, AreaChart, Area, CartesianGrid, LabelList,
} from 'recharts'
import toast from 'react-hot-toast'

import { itAssetsApi } from '@/api/itAssets'
import { qk, staleTimes } from '@/lib/queryKeys'
import { openBlob } from '@/lib/download'
import { extractApiError } from '@/utils'
import KpiCard from '@/components/dashboard/KpiCard'
import { KpiCardSkeleton } from '@/components/ui/Skeleton'
import type { ItCountByLabelDto, ItDashboardDto, ItValueByLabelDto } from '@/types'

const BRAND = '#0E6B4B'
const PALETTE = ['#0E6B4B', '#2563EB', '#F59E0B', '#B42318', '#0891B2', '#7C3AED', '#475569', '#DB2777', '#65A30D', '#EA580C']

function useItDashboard() {
  return useQuery({
    queryKey: qk.ti.dashboard,
    queryFn: () => itAssetsApi.getDashboard().then(r => r.data.data!),
    staleTime: staleTimes.tiDashboard,
  })
}

const tooltipStyle = { fontSize: 12, borderRadius: 8, border: '1px solid #D9DEE5', boxShadow: '0 4px 12px rgba(0,0,0,.06)' }

// ─── Wrapper de panel con insight en vivo ───────────────────────────────────────
function Panel({ title, insight, children, action }: {
  title: string; insight?: string; children: React.ReactNode; action?: React.ReactNode
}) {
  return (
    <div className="flex flex-col rounded-[12px] border border-line bg-paper p-4 shadow-sh1">
      <div className="mb-1 flex items-start justify-between gap-2">
        <h2 className="text-sm font-semibold text-ink leading-tight">{title}</h2>
        {action}
      </div>
      {insight && (
        <p className="mb-3 flex items-start gap-1.5 text-[12px] leading-snug text-ink2">
          <Lightbulb className="mt-px h-3.5 w-3.5 shrink-0" style={{ color: '#F59E0B' }} />
          <span>{insight}</span>
        </p>
      )}
      <div className="flex-1">{children}</div>
    </div>
  )
}

// ─── Barras horizontales con leyenda de conteo ──────────────────────────────────
function HBars({ data, height = 200 }: { data: ItCountByLabelDto[]; height?: number }) {
  if (!data.length) return <Empty h={height} />
  return (
    <div style={{ height }}>
      <ResponsiveContainer width="100%" height="100%">
        <BarChart data={data} layout="vertical" margin={{ top: 0, right: 28, left: 8, bottom: 0 }}>
          <XAxis type="number" hide domain={[0, 'dataMax + 1']} />
          <YAxis type="category" dataKey="label" width={120}
            tick={{ fontSize: 11, fill: '#4A5750' }} axisLine={false} tickLine={false} />
          <Tooltip cursor={{ fill: 'rgba(14,107,75,.06)' }} contentStyle={tooltipStyle} />
          <Bar dataKey="count" radius={[0, 4, 4, 0]} barSize={16}
            label={{ position: 'right', fontSize: 11, fill: '#13211C' }}>
            {data.map((_, i) => <Cell key={i} fill={PALETTE[i % PALETTE.length]} />)}
          </Bar>
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}

function Empty({ h }: { h: number }) {
  return <div className="flex items-center justify-center text-sm text-ink2" style={{ height: h }}>Sin datos</div>
}

// ─── Valor de inventario por colaborador (gráfica + tooltip con desglose) ───────
const crcFmt = new Intl.NumberFormat('es-CR', { style: 'currency', currency: 'CRC', maximumFractionDigits: 0 })
const usdFmt = new Intl.NumberFormat('es-CR', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 })

/** Formatea un par (crc, usd) como "₡25.000", "$300" o "₡25.000 + $300". */
function money(crc: number, usd: number) {
  return [crc > 0 && crcFmt.format(crc), usd > 0 && usdFmt.format(usd)].filter(Boolean).join(' + ') || '—'
}

function ValueTooltip({ active, payload }: { active?: boolean; payload?: { payload: ItValueByLabelDto }[] }) {
  if (!active || !payload?.length) return null
  const d = payload[0].payload
  return (
    <div className="rounded-lg border border-line bg-paper px-3 py-2 shadow-md" style={{ minWidth: 200 }}>
      <p className="mb-1.5 text-[13px] font-semibold text-ink">{d.label}</p>
      <ul className="space-y-0.5">
        {d.breakdown.map(b => (
          <li key={b.label} className="flex items-center justify-between gap-4 text-[12px]">
            <span className="text-ink2">{b.count} {b.label.toLowerCase()}</span>
            <span className="font-mono text-ink">valor {money(b.crc, b.usd)}</span>
          </li>
        ))}
      </ul>
      <div className="mt-1.5 flex items-center justify-between gap-4 border-t border-line pt-1.5 text-[12px] font-semibold">
        <span className="text-ink">total {d.assetCount}</span>
        <span className="font-mono text-ink">valor {money(d.crc, d.usd)}</span>
      </div>
    </div>
  )
}

function ValueEmpty() {
  return (
    <div className="flex flex-col items-center justify-center gap-1.5 rounded-[10px] border border-dashed border-line bg-bg py-10 text-center">
      <Boxes className="h-7 w-7 text-ink2" />
      <p className="text-sm font-medium text-ink">Aún no hay valor que mostrar</p>
      <p className="max-w-sm text-[12px] text-ink2">
        Registra el <span className="font-medium text-ink">costo</span> de los activos y asígnalos a un colaborador
        para ver aquí quién custodia más dinero en inventario.
      </p>
    </div>
  )
}

function ValueBars({ data }: { data: ItValueByLabelDto[] }) {
  const rows = data
    .map(d => ({ ...d, total: d.crc + d.usd, valueLabel: money(d.crc, d.usd) }))
    .sort((a, b) => b.total - a.total)   // siempre de mayor a menor valor
  const grandTotal = rows.reduce((a, b) => a + b.total, 0)
  if (!rows.length || grandTotal <= 0) return <ValueEmpty />

  const height = rows.length * 46 + 12
  return (
    <div style={{ height }}>
      <ResponsiveContainer width="100%" height="100%">
        <BarChart data={rows} layout="vertical" barCategoryGap="22%" margin={{ top: 4, right: 96, left: 8, bottom: 4 }}>
          <defs>
            <linearGradient id="valGrad" x1="0" y1="0" x2="1" y2="0">
              <stop offset="0%" stopColor="#0E6B4B" />
              <stop offset="100%" stopColor="#34A578" />
            </linearGradient>
          </defs>
          <XAxis type="number" hide domain={[0, (max: number) => max * 1.12]} />
          <YAxis type="category" dataKey="label" width={170}
            tick={{ fontSize: 12, fill: '#13211C' }} axisLine={false} tickLine={false}
            tickFormatter={(v: string) => (v.length > 24 ? v.slice(0, 22) + '…' : v)} />
          <Tooltip cursor={{ fill: 'rgba(14,107,75,.06)' }} content={<ValueTooltip />} />
          <Bar dataKey="total" radius={[0, 6, 6, 0]} barSize={22} fill="url(#valGrad)" isAnimationActive={false}>
            <LabelList dataKey="valueLabel" position="right" style={{ fontSize: 11, fontWeight: 600, fill: '#13211C' }} />
          </Bar>
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}

// ─── Donut con total al centro ──────────────────────────────────────────────────
function Donut({ data, total }: { data: ItCountByLabelDto[]; total: number }) {
  if (!data.length) return <Empty h={220} />
  return (
    <div className="flex items-center gap-4">
      <div className="relative" style={{ width: 160, height: 160 }}>
        <ResponsiveContainer width="100%" height="100%">
          <PieChart>
            <Pie data={data} dataKey="count" nameKey="label" cx="50%" cy="50%"
              innerRadius={52} outerRadius={76} paddingAngle={2} stroke="none">
              {data.map((_, i) => <Cell key={i} fill={PALETTE[i % PALETTE.length]} />)}
            </Pie>
            <Tooltip contentStyle={tooltipStyle} />
          </PieChart>
        </ResponsiveContainer>
        <div className="pointer-events-none absolute inset-0 flex flex-col items-center justify-center">
          <span className="font-mono text-2xl font-semibold leading-none text-ink">{total}</span>
          <span className="text-[10px] text-ink2">activos</span>
        </div>
      </div>
      <ul className="flex-1 space-y-1.5">
        {data.slice(0, 7).map((d, i) => (
          <li key={d.label} className="flex items-center gap-2 text-[12px]">
            <span className="h-2.5 w-2.5 shrink-0 rounded-full" style={{ background: PALETTE[i % PALETTE.length] }} />
            <span className="flex-1 truncate text-ink2">{d.label}</span>
            <span className="font-mono font-semibold text-ink">{d.count}</span>
            <span className="w-10 text-right font-mono text-ink2">{total ? Math.round(d.count / total * 100) : 0}%</span>
          </li>
        ))}
      </ul>
    </div>
  )
}

// ─── Barra de progreso (salud / calidad) ────────────────────────────────────────
function Meter({ label, value, total, color, hint }: {
  label: string; value: number; total: number; color: string; hint?: string
}) {
  const pct = total ? Math.round(value / total * 100) : 0
  return (
    <div>
      <div className="mb-1 flex items-baseline justify-between text-[12px]">
        <span className="text-ink2">{label}</span>
        <span className="font-mono font-semibold text-ink">{value}<span className="text-ink2"> · {pct}%</span></span>
      </div>
      <div className="h-2 overflow-hidden rounded-full bg-bg">
        <div className="h-full rounded-full transition-all" style={{ width: `${pct}%`, background: color }} />
      </div>
      {hint && <p className="mt-0.5 text-[11px] text-ink2">{hint}</p>}
    </div>
  )
}

// ─── Insights (texto vivo según datos) ──────────────────────────────────────────
function topShare(data: ItCountByLabelDto[], total: number) {
  if (!data.length || !total) return null
  const top = [...data].sort((a, b) => b.count - a.count)[0]
  return { label: top.label, count: top.count, pct: Math.round(top.count / total * 100) }
}

export default function ItDashboardPage() {
  const { data, isLoading, error, refetch, isFetching } = useItDashboard()
  const [exporting, setExporting] = useState(false)

  const fmt = (n: number, c: string) =>
    new Intl.NumberFormat('es-CR', { style: 'currency', currency: c, maximumFractionDigits: 0 }).format(n)

  async function exportPdf() {
    setExporting(true)
    try {
      const res = await itAssetsApi.exportDashboardPdf()
      openBlob(res.data as Blob)
    } catch (e) {
      toast.error(extractApiError(e))
    } finally {
      setExporting(false)
    }
  }

  const insights = useMemo(() => buildInsights(data), [data])

  return (
    <div className="flex min-h-full flex-col">
      <header className="sticky top-0 z-10 flex items-center gap-3 border-b border-line bg-paper px-6 py-3" style={{ minHeight: 64 }}>
        <div className="min-w-0 flex-1">
          <p className="font-mono text-[12px] text-ink2 mb-0.5 leading-none">TI / Dashboard</p>
          <h1 className="text-[18px] font-semibold text-ink leading-tight tracking-tight flex items-center gap-2">
            <Cpu className="h-4.5 w-4.5" style={{ color: BRAND }} /> Inventario tecnológico
          </h1>
        </div>
        {data?.generatedAt && (
          <span className="hidden font-mono text-[11px] text-ink2 sm:inline">Actualizado {data.generatedAt}</span>
        )}
        <button
          onClick={() => refetch()}
          className="inline-flex items-center gap-1.5 rounded-[8px] border border-line bg-paper px-3 py-2 text-sm font-medium text-ink transition-colors hover:bg-bg"
          title="Actualizar"
        >
          <RefreshCw className={`h-4 w-4 ${isFetching ? 'animate-spin' : ''}`} />
        </button>
        <button
          onClick={exportPdf}
          disabled={exporting || !data}
          className="inline-flex items-center gap-1.5 rounded-[8px] border border-line bg-paper px-3.5 py-2 text-sm font-medium text-ink transition-colors hover:bg-bg disabled:opacity-50"
        >
          <FileDown className="h-4 w-4" /> {exporting ? 'Generando…' : 'Exportar PDF'}
        </button>
        <Link
          to="/ti/assets/new"
          className="inline-flex items-center gap-1.5 rounded-[8px] px-3.5 py-2 text-sm font-medium text-white transition-colors hover:opacity-90"
          style={{ background: BRAND }}
        >
          <Plus className="h-4 w-4" /> Nuevo activo
        </Link>
      </header>

      <div className="flex-1 space-y-4 bg-bg p-6">
        {error && (
          <div className="flex items-center gap-3 rounded-[10px] border p-4" style={{ background: '#FEF2F2', borderColor: '#FECACA' }} role="alert">
            <AlertCircle className="h-5 w-5 shrink-0" style={{ color: '#B42318' }} />
            <p className="flex-1 text-sm font-medium" style={{ color: '#991B1B' }}>No se pudo cargar el dashboard de TI.</p>
            <button onClick={() => refetch()} className="text-sm font-medium" style={{ color: '#991B1B' }}>Reintentar</button>
          </div>
        )}

        {/* KPIs */}
        <div className="grid grid-cols-2 gap-3.5 lg:grid-cols-3 xl:grid-cols-6">
          {isLoading ? (
            Array.from({ length: 6 }).map((_, i) => <KpiCardSkeleton key={i} />)
          ) : (
            <>
              <KpiCard label="Total de activos" value={data?.totalAssets ?? 0} sub={`${data?.assetsWithCost ?? 0} con costo`} />
              <KpiCard label="Asignados" value={data?.assigned ?? 0}
                delta={`${data?.assignmentRatePct ?? 0}% de operables`} deltaTone="neutral" />
              <KpiCard label="Disponibles" value={data?.available ?? 0}
                delta={`${data?.availabilityRatePct ?? 0}% disponibilidad`} deltaTone="ok" />
              <KpiCard label="En taller" value={(data?.underRepair ?? 0) + (data?.underMaintenance ?? 0)}
                sub={`${data?.underRepair ?? 0} rep · ${data?.underMaintenance ?? 0} mant`} />
              <KpiCard label="Incidencias" value={(data?.damaged ?? 0) + (data?.lost ?? 0) + (data?.stolen ?? 0)}
                delta={`${data?.incidentRatePct ?? 0}% de la flota`} deltaTone={(data?.incidentRatePct ?? 0) > 0 ? 'danger' : 'ok'} />
              <KpiCard label="Calidad de datos" value={`${data?.dataQualityPct ?? 0}%`}
                delta={`antigüedad ${data?.avgAgeMonths ?? 0} m`} deltaTone="neutral" />
            </>
          )}
        </div>

        {isLoading ? (
          <div className="grid gap-4 lg:grid-cols-2">
            {Array.from({ length: 4 }).map((_, i) => <div key={i} className="h-72 animate-pulse rounded-[12px] bg-gray-100" />)}
          </div>
        ) : data && (
          <>
            {/* Fila 1: estado (donut) + tendencia (área) */}
            <div className="grid gap-4 lg:grid-cols-2">
              <Panel title="Distribución por estado" insight={insights.status}>
                <Donut data={data.byStatus} total={data.totalAssets} />
              </Panel>

              <Panel title="Tendencia de adquisiciones (12 meses)" insight={insights.trend}>
                <div style={{ height: 220 }}>
                  <ResponsiveContainer width="100%" height="100%">
                    <AreaChart data={data.acquisitionTrend} margin={{ top: 8, right: 12, left: -18, bottom: 0 }}>
                      <defs>
                        <linearGradient id="gTrend" x1="0" y1="0" x2="0" y2="1">
                          <stop offset="0%" stopColor={BRAND} stopOpacity={0.35} />
                          <stop offset="100%" stopColor={BRAND} stopOpacity={0} />
                        </linearGradient>
                      </defs>
                      <CartesianGrid strokeDasharray="3 3" stroke="#EEF2EF" vertical={false} />
                      <XAxis dataKey="label" tick={{ fontSize: 10, fill: '#4A5750' }} axisLine={false} tickLine={false} />
                      <YAxis allowDecimals={false} tick={{ fontSize: 10, fill: '#4A5750' }} axisLine={false} tickLine={false} width={36} />
                      <Tooltip contentStyle={tooltipStyle} />
                      <Area type="monotone" dataKey="count" stroke={BRAND} strokeWidth={2} fill="url(#gTrend)" name="Adquiridos" />
                    </AreaChart>
                  </ResponsiveContainer>
                </div>
              </Panel>
            </div>

            {/* Fila 2: tipo + departamento */}
            <div className="grid gap-4 lg:grid-cols-2">
              <Panel title="Activos por tipo" insight={insights.type}><HBars data={data.byType} /></Panel>
              <Panel title="Activos por departamento" insight={insights.dept}><HBars data={data.byDepartment} /></Panel>
            </div>

            {/* Fila 3: marca + antigüedad */}
            <div className="grid gap-4 lg:grid-cols-2">
              <Panel title="Top marcas" insight={insights.brand}><HBars data={data.byBrand} /></Panel>
              <Panel title="Antigüedad de la flota" insight={insights.age}><HBars data={data.byAgeBucket} /></Panel>
            </div>

            {/* Fila 4: salud de garantías + calidad de datos + top responsables */}
            <div className="grid gap-4 lg:grid-cols-3">
              <Panel title="Salud de garantías" insight={insights.warranty}>
                <div className="space-y-3.5 pt-1">
                  <Meter label="Vigentes" value={data.warrantyActive} total={data.totalAssets} color="#0E6B4B" />
                  <Meter label="Por vencer (≤60 días)" value={data.warrantyExpiringSoon} total={data.totalAssets} color="#F59E0B" />
                  <Meter label="Vencidas" value={data.warrantyExpired} total={data.totalAssets} color="#B42318" />
                  <Meter label="Sin garantía" value={data.withoutWarranty} total={data.totalAssets} color="#94A3A0" />
                </div>
              </Panel>

              <Panel title="Calidad de datos" insight={insights.quality}>
                <div className="space-y-3.5 pt-1">
                  <Meter label="Con número de serie" value={data.totalAssets - data.withoutSerial} total={data.totalAssets} color="#2563EB"
                    hint={data.withoutSerial > 0 ? `${data.withoutSerial} sin serie` : undefined} />
                  <Meter label="Con placa/etiqueta" value={data.totalAssets - data.withoutTag} total={data.totalAssets} color="#0891B2"
                    hint={data.withoutTag > 0 ? `${data.withoutTag} sin placa` : undefined} />
                  <Meter label="Con responsable" value={data.totalAssets - data.withoutHolder} total={data.totalAssets} color="#7C3AED"
                    hint={data.withoutHolder > 0 ? `${data.withoutHolder} sin responsable` : undefined} />
                  <div className="mt-1 flex items-center gap-2 rounded-lg bg-bg px-3 py-2">
                    <ShieldCheck className="h-4 w-4" style={{ color: BRAND }} />
                    <span className="text-[12px] text-ink2">Índice global</span>
                    <span className="ml-auto font-mono text-sm font-semibold text-ink">{data.dataQualityPct}%</span>
                  </div>
                </div>
              </Panel>

              <Panel title="Top responsables" insight={insights.holders}>
                {data.topHolders.length === 0
                  ? <Empty h={200} />
                  : <ul className="space-y-2 pt-1">
                      {data.topHolders.map((h, i) => (
                        <li key={h.label} className="flex items-center gap-2.5 text-[13px]">
                          <span className="flex h-6 w-6 items-center justify-center rounded-full font-mono text-[11px] font-semibold text-white" style={{ background: PALETTE[i % PALETTE.length] }}>{i + 1}</span>
                          <span className="flex-1 truncate text-ink">{h.label}</span>
                          <span className="font-mono font-semibold text-ink">{h.count}</span>
                        </li>
                      ))}
                    </ul>}
              </Panel>
            </div>

            {/* Valor de inventario por colaborador */}
            <Panel title="Valor de inventario asignado por colaborador" insight={insights.value}>
              <ValueBars data={data.valueByHolder} />
            </Panel>

            {/* Resumen ejecutivo */}
            <div className="rounded-[12px] border border-line bg-paper p-5 shadow-sh1">
              <h2 className="mb-3 flex items-center gap-2 text-sm font-semibold text-ink">
                <Activity className="h-4 w-4" style={{ color: BRAND }} /> Resumen ejecutivo
              </h2>
              <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
                <Stat icon={Boxes} tone={BRAND} label="Inversión registrada"
                  value={`${fmt(data.totalCostCrc, 'CRC')}`} sub={`+ ${fmt(data.totalCostUsd, 'USD')}`} />
                <Stat icon={CheckCircle2} tone="#0E6B4B" label="Tasa de disponibilidad"
                  value={`${data.availabilityRatePct}%`} sub={`${data.available} listos para entregar`} />
                <Stat icon={Wrench} tone="#F59E0B" label="En taller"
                  value={`${data.underRepair + data.underMaintenance}`} sub="reparación + mantenimiento" />
                <Stat icon={Clock} tone="#7C3AED" label="Antigüedad promedio"
                  value={`${data.avgAgeMonths} m`} sub={`${data.assetsWithPurchaseDate} con fecha de compra`} />
              </div>
              {(data.warrantyExpiringSoon > 0 || data.withoutHolder > 0 || data.incidentRatePct > 0) && (
                <div className="mt-4 flex flex-wrap gap-2">
                  {data.warrantyExpiringSoon > 0 && <Chip tone="warn" icon={AlertTriangle}>{data.warrantyExpiringSoon} garantía(s) por vencer ≤60 días</Chip>}
                  {data.withoutHolder > 0 && <Chip tone="warn" icon={AlertTriangle}>{data.withoutHolder} sin responsable asignado</Chip>}
                  {data.incidentRatePct > 0 && <Chip tone="danger" icon={AlertCircle}>{data.damaged + data.lost + data.stolen} con incidencias ({data.incidentRatePct}%)</Chip>}
                </div>
              )}
            </div>
          </>
        )}
      </div>
    </div>
  )
}

function Stat({ icon: Icon, tone, label, value, sub }: {
  icon: React.ElementType; tone: string; label: string; value: string; sub?: string
}) {
  return (
    <div className="flex items-start gap-3 rounded-[10px] border border-line bg-bg p-3.5">
      <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg" style={{ background: `${tone}1A` }}>
        <Icon className="h-4.5 w-4.5" style={{ color: tone }} />
      </span>
      <div className="min-w-0">
        <p className="text-[11px] text-ink2 leading-tight">{label}</p>
        <p className="font-mono text-[17px] font-semibold leading-tight text-ink">{value}</p>
        {sub && <p className="truncate text-[11px] text-ink2">{sub}</p>}
      </div>
    </div>
  )
}

function Chip({ tone, icon: Icon, children }: { tone: 'warn' | 'danger'; icon: React.ElementType; children: React.ReactNode }) {
  const s = tone === 'danger'
    ? { bg: '#FEF2F2', fg: '#991B1B' }
    : { bg: '#FFFBEB', fg: '#92400E' }
  return (
    <span className="inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-[12px] font-medium" style={{ background: s.bg, color: s.fg }}>
      <Icon className="h-3.5 w-3.5" /> {children}
    </span>
  )
}

// ─── Generación de insights en lenguaje natural ─────────────────────────────────
function buildInsights(d?: ItDashboardDto) {
  if (!d) return {} as Record<string, string>
  const t = d.totalAssets || 0
  const status = topShare(d.byStatus, t)
  const type = topShare(d.byType, t)
  const dept = topShare(d.byDepartment, t)
  const brand = topShare(d.byBrand, t)

  const trendSum = d.acquisitionTrend.reduce((a, b) => a + b.count, 0)
  const trendPeak = [...d.acquisitionTrend].sort((a, b) => b.count - a.count)[0]

  const aged = d.byAgeBucket.filter(b => b.label === '3–5 años' || b.label === '5+ años').reduce((a, b) => a + b.count, 0)

  return {
    status: status
      ? `${status.label} es el estado predominante con ${status.count} equipos (${status.pct}%). ${d.assigned} asignados y ${d.available} disponibles.`
      : 'Aún no hay activos registrados.',
    type: type ? `«${type.label}» lidera con ${type.count} equipos (${type.pct}% del total).` : 'Sin datos de tipo.',
    dept: dept ? `«${dept.label}» concentra ${dept.count} equipos (${dept.pct}%).` : 'Sin datos por departamento.',
    brand: brand ? `«${brand.label}» es la marca dominante con ${brand.count} equipos (${brand.pct}%).` : 'Sin datos de marca.',
    trend: trendSum === 0
      ? 'Sin adquisiciones con fecha registrada en los últimos 12 meses.'
      : `${trendSum} equipo(s) adquiridos en 12 meses; pico en ${trendPeak?.label} (${trendPeak?.count}).`,
    age: aged > 0
      ? `${aged} equipo(s) tienen 3+ años: candidatos a renovación. Antigüedad promedio ${d.avgAgeMonths} meses.`
      : `Flota joven: antigüedad promedio ${d.avgAgeMonths} meses.`,
    warranty: d.warrantyExpiringSoon > 0
      ? `${d.warrantyActive} con garantía vigente, pero ${d.warrantyExpiringSoon} vencen en ≤60 días — planifique renovación.`
      : `${d.warrantyActive} con garantía vigente; ${d.warrantyExpired} vencidas.`,
    quality: d.dataQualityPct >= 90
      ? `Excelente: ${d.dataQualityPct}% de los campos clave completos.`
      : `${d.dataQualityPct}% de campos clave completos. Complete serie, placa y responsable para mejorar trazabilidad.`,
    holders: d.topHolders.length
      ? `${d.topHolders[0].label} es quien más equipos custodia (${d.topHolders[0].count}).`
      : 'Aún no hay equipos con responsable asignado.',
    value: (() => {
      const top = d.valueByHolder[0]
      if (!top || top.crc + top.usd <= 0) return 'Registra el costo de los activos asignados para ver el valor por colaborador.'
      return `${top.label} custodia el mayor valor asignado: ${money(top.crc, top.usd)} en ${top.assetCount} equipo(s).`
    })(),
  }
}
