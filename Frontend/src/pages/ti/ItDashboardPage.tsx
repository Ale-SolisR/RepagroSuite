import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { Plus, AlertCircle, Cpu } from 'lucide-react'
import {
  BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, Cell,
} from 'recharts'

import { itAssetsApi } from '@/api/itAssets'
import { qk, staleTimes } from '@/lib/queryKeys'
import KpiCard from '@/components/dashboard/KpiCard'
import { KpiCardSkeleton } from '@/components/ui/Skeleton'

const BRAND = '#0E6B4B'
const STATUS_COLORS = ['#0E6B4B', '#2563EB', '#F59E0B', '#B42318', '#6B7280', '#0891B2', '#7C3AED', '#475569']

function useItDashboard() {
  return useQuery({
    queryKey: qk.ti.dashboard,
    queryFn: () => itAssetsApi.getDashboard().then(r => r.data.data!),
    staleTime: staleTimes.tiDashboard,
  })
}

function ChartCard({ title, data }: { title: string; data: { label: string; count: number }[] }) {
  return (
    <div className="rounded-[10px] border border-line bg-paper p-4 shadow-sh1">
      <h2 className="mb-4 text-sm font-semibold text-ink leading-none">{title}</h2>
      {data.length === 0 ? (
        <div className="flex h-[200px] items-center justify-center text-sm text-ink2">Sin datos</div>
      ) : (
        <div style={{ height: 200 }}>
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={data} layout="vertical" margin={{ top: 0, right: 16, left: 8, bottom: 0 }}>
              <XAxis type="number" hide domain={[0, 'dataMax + 1']} />
              <YAxis
                type="category" dataKey="label" width={120}
                tick={{ fontSize: 11, fill: '#4A5750' }} axisLine={false} tickLine={false}
              />
              <Tooltip
                cursor={{ fill: 'rgba(14,107,75,.06)' }}
                contentStyle={{ fontSize: 12, borderRadius: 8, border: '1px solid #D9DEE5' }}
              />
              <Bar dataKey="count" radius={[0, 4, 4, 0]} barSize={18}>
                {data.map((_, i) => <Cell key={i} fill={STATUS_COLORS[i % STATUS_COLORS.length]} />)}
              </Bar>
            </BarChart>
          </ResponsiveContainer>
        </div>
      )}
    </div>
  )
}

function AlertRow({ label, count, danger }: { label: string; count: number; danger?: boolean }) {
  return (
    <div className="flex items-center justify-between border-b border-line py-2.5 last:border-0">
      <span className="text-[13px] text-ink">{label}</span>
      <span
        className="font-mono text-[13px] font-semibold rounded-full px-2 py-0.5"
        style={count > 0
          ? { background: danger ? '#FEF2F2' : '#FFFBEB', color: danger ? '#991B1B' : '#92400E' }
          : { background: '#ECFDF5', color: '#065F46' }}
      >
        {count}
      </span>
    </div>
  )
}

export default function ItDashboardPage() {
  const { data, isLoading, error, refetch } = useItDashboard()

  const fmt = (n: number, c: string) =>
    new Intl.NumberFormat('es-CR', { style: 'currency', currency: c, maximumFractionDigits: 0 }).format(n)

  return (
    <div className="flex min-h-full flex-col">
      <header className="sticky top-0 z-10 flex items-center gap-4 border-b border-line bg-paper px-6 py-3" style={{ minHeight: 64 }}>
        <div className="min-w-0 flex-1">
          <p className="font-mono text-[12px] text-ink2 mb-0.5 leading-none">TI / Dashboard</p>
          <h1 className="text-[18px] font-semibold text-ink leading-tight tracking-tight flex items-center gap-2">
            <Cpu className="h-4.5 w-4.5" style={{ color: BRAND }} /> Inventario tecnológico
          </h1>
        </div>
        <Link
          to="/ti/assets/new"
          className="inline-flex items-center gap-1.5 rounded-[8px] px-3.5 py-2 text-sm font-medium text-white transition-colors hover:opacity-90"
          style={{ background: BRAND }}
        >
          <Plus className="h-4 w-4" /> Nuevo activo
        </Link>
      </header>

      <div className="flex-1 p-6 bg-bg space-y-3.5">
        {error && (
          <div className="flex items-center gap-3 rounded-[10px] border p-4" style={{ background: '#FEF2F2', borderColor: '#FECACA' }} role="alert">
            <AlertCircle className="h-5 w-5 shrink-0" style={{ color: '#B42318' }} />
            <p className="flex-1 text-sm font-medium" style={{ color: '#991B1B' }}>No se pudo cargar el dashboard de TI.</p>
            <button onClick={() => refetch()} className="text-sm font-medium" style={{ color: '#991B1B' }}>Reintentar</button>
          </div>
        )}

        {/* KPIs */}
        <div className="grid grid-cols-2 xl:grid-cols-4 gap-3.5">
          {isLoading ? (
            Array.from({ length: 4 }).map((_, i) => <KpiCardSkeleton key={i} />)
          ) : (
            <>
              <KpiCard label="Activos reales" value={data?.totalAssets ?? 0} sub="Total en inventario" />
              <KpiCard label="Asignados" value={data?.assigned ?? 0} sub={`${data?.available ?? 0} disponibles`} />
              <KpiCard label="En taller" value={(data?.underRepair ?? 0) + (data?.underMaintenance ?? 0)}
                sub={`${data?.underRepair ?? 0} reparación · ${data?.underMaintenance ?? 0} mant.`} />
              <KpiCard label="Dados de baja" value={data?.disposed ?? 0}
                sub={data ? `${fmt(data.totalCostCrc, 'CRC')} + ${fmt(data.totalCostUsd, 'USD')}` : undefined} />
            </>
          )}
        </div>

        {/* Charts */}
        <div className="grid gap-3.5 lg:grid-cols-2">
          <ChartCard title="Activos por tipo" data={data?.byType ?? []} />
          <ChartCard title="Activos por estado" data={data?.byStatus ?? []} />
        </div>

        {/* Department + Alerts */}
        <div className="grid gap-3.5 lg:grid-cols-2">
          <ChartCard title="Activos por departamento" data={data?.byDepartment ?? []} />
          <div className="rounded-[10px] border border-line bg-paper p-4 shadow-sh1">
            <h2 className="mb-2 text-sm font-semibold text-ink leading-none">Alertas de calidad de datos</h2>
            {isLoading ? (
              <div className="h-40 animate-pulse rounded-lg bg-gray-100" />
            ) : (
              <div>
                <AlertRow label="Equipos sin número de serie" count={data?.withoutSerial ?? 0} danger />
                <AlertRow label="Equipos sin etiqueta/placa" count={data?.withoutTag ?? 0} />
                <AlertRow label="Equipos sin responsable" count={data?.withoutHolder ?? 0} />
                <AlertRow label="Garantías por vencer (≤60 días)" count={data?.warrantyExpiringSoon ?? 0} />
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}
