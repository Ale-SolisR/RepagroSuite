import { useState } from 'react'
import { AreaChart, Area, XAxis, YAxis, Tooltip, ResponsiveContainer } from 'recharts'
import { format, parseISO, startOfWeek, addWeeks } from 'date-fns'
import { es } from 'date-fns/locale'

interface TrendPoint {
  date: string
  count: number
}

interface DashboardChartProps {
  data: TrendPoint[]
  isLoading?: boolean
}

type ViewMode = 'day' | 'week'

function aggregateWeekly(data: TrendPoint[]) {
  const byWeek: Record<string, number> = {}
  data.forEach(({ date, count }) => {
    const d = parseISO(date)
    const weekKey = format(startOfWeek(d, { weekStartsOn: 1 }), 'yyyy-MM-dd')
    byWeek[weekKey] = (byWeek[weekKey] ?? 0) + count
  })
  return Object.entries(byWeek)
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([date, count]) => ({ date, count }))
}

function buildChartData(raw: TrendPoint[], mode: ViewMode) {
  const source = mode === 'week' ? aggregateWeekly(raw) : raw
  return source.map(({ date, count }) => ({
    count,
    label:
      mode === 'week'
        ? `Sem. ${format(parseISO(date), 'd MMM', { locale: es })}`
        : format(parseISO(date), 'd MMM', { locale: es }),
  }))
}

const BRAND = '#006F55'

function CustomTooltip({ active, payload, label }: {
  active?: boolean; payload?: { value: number }[]; label?: string
}) {
  if (!active || !payload?.length) return null
  return (
    <div
      className="rounded-[8px] border border-line bg-paper px-3 py-2 shadow-sh2"
      style={{ fontSize: 12 }}
    >
      <p className="text-ink2 mb-0.5">{label}</p>
      <p className="font-mono font-semibold text-ink">{payload[0].value} reservas</p>
    </div>
  )
}

export default function DashboardChart({ data, isLoading }: DashboardChartProps) {
  const [mode, setMode] = useState<ViewMode>('day')

  const chartData = buildChartData(data ?? [], mode)

  return (
    <div className="rounded-[10px] border border-line bg-paper p-4 shadow-sh1 flex flex-col">
      {/* Header */}
      <div className="flex items-center justify-between mb-4 gap-3">
        <h2 className="text-sm font-semibold text-ink leading-none">
          Reservas por día · últimos 14d
        </h2>
        <div
          className="flex items-center rounded-full p-0.5 shrink-0"
          style={{ background: '#F5F7FA', border: '1px solid #D9DEE5' }}
          role="group"
          aria-label="Vista del gráfico"
        >
          {(['day', 'week'] as const).map(v => (
            <button
              key={v}
              onClick={() => setMode(v)}
              className="rounded-full px-3 py-1 text-[11px] font-medium transition-colors"
              style={
                mode === v
                  ? { background: '#fff', color: '#1F2933', boxShadow: '0 1px 2px rgba(15,23,42,.08)' }
                  : { color: '#5F6B7A' }
              }
              aria-pressed={mode === v}
            >
              {v === 'day' ? 'Día' : 'Semana'}
            </button>
          ))}
        </div>
      </div>

      {/* Chart */}
      {isLoading ? (
        <div className="flex-1 min-h-[160px] animate-pulse rounded-lg bg-gray-100" />
      ) : chartData.length === 0 ? (
        <div className="flex flex-1 min-h-[160px] items-center justify-center text-sm text-ink2">
          Sin datos disponibles
        </div>
      ) : (
        <div className="flex-1 min-h-0" style={{ height: 160 }}>
          <ResponsiveContainer width="100%" height="100%">
            <AreaChart data={chartData} margin={{ top: 4, right: 4, left: -28, bottom: 0 }}>
              <defs>
                <linearGradient id="trendFill" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%"   stopColor={BRAND} stopOpacity={0.22} />
                  <stop offset="100%" stopColor={BRAND} stopOpacity={0} />
                </linearGradient>
              </defs>
              <XAxis
                dataKey="label"
                tick={{ fontSize: 10, fill: '#9CA3AF', fontFamily: 'JetBrains Mono, monospace' }}
                axisLine={false}
                tickLine={false}
                interval="preserveStartEnd"
              />
              <YAxis hide domain={[0, 'dataMax + 5']} />
              <Tooltip content={<CustomTooltip />} cursor={{ stroke: BRAND, strokeWidth: 1, strokeDasharray: '4 2' }} />
              <Area
                type="monotone"
                dataKey="count"
                stroke={BRAND}
                strokeWidth={2}
                fill="url(#trendFill)"
                dot={false}
                activeDot={{ r: 4, fill: BRAND, stroke: '#fff', strokeWidth: 2 }}
              />
            </AreaChart>
          </ResponsiveContainer>
        </div>
      )}
    </div>
  )
}
