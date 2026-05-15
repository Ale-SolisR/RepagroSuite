import { TrendingUp, TrendingDown, Minus } from 'lucide-react'

type DeltaTone = 'ok' | 'danger' | 'neutral'

interface KpiCardProps {
  label: string
  value: string | number
  delta?: string
  deltaTone?: DeltaTone
  sub?: string
}

const toneStyles: Record<DeltaTone, { color: string; Icon: React.ElementType }> = {
  ok:      { color: '#10B981', Icon: TrendingUp },
  danger:  { color: '#B42318', Icon: TrendingDown },
  neutral: { color: '#5F6B7A', Icon: Minus },
}

export default function KpiCard({ label, value, delta, deltaTone = 'neutral', sub }: KpiCardProps) {
  const tone = toneStyles[deltaTone]

  return (
    <article
      className="rounded-[10px] border border-line bg-paper p-4 shadow-sh1"
      aria-label={`${label}: ${value}`}
    >
      <p className="text-[12px] font-medium text-ink2 mb-3 leading-none">{label}</p>

      <p className="font-mono text-[28px] font-semibold tracking-tight text-ink leading-none mb-2">
        {value}
      </p>

      {delta && (
        <p
          className="flex items-center gap-1 font-mono text-[12px] leading-none"
          style={{ color: tone.color }}
        >
          <tone.Icon className="h-3 w-3 shrink-0" strokeWidth={2} aria-hidden />
          {delta}
        </p>
      )}

      {!delta && sub && (
        <p className="text-[12px] font-mono text-ink2 leading-none">{sub}</p>
      )}
    </article>
  )
}
