interface StatusItem {
  label: string
  color: string
  count: number
  total: number
}

interface StatusBreakdownProps {
  items: StatusItem[]
  isLoading?: boolean
}

export type { StatusItem }

export default function StatusBreakdown({ items, isLoading }: StatusBreakdownProps) {
  return (
    <div className="rounded-[10px] border border-line bg-paper p-4 shadow-sh1 flex flex-col">
      <h2 className="text-sm font-semibold text-ink mb-4 leading-none">Estados de sala</h2>

      {isLoading ? (
        <div className="space-y-4">
          {[60, 45, 25, 15].map(w => (
            <div key={w} className="space-y-1.5 animate-pulse">
              <div className="flex items-center justify-between">
                <div className="h-3 rounded bg-gray-200" style={{ width: `${w}%` }} />
                <div className="h-3 w-6 rounded bg-gray-200" />
              </div>
              <div className="h-2 w-full rounded-full bg-gray-100" />
            </div>
          ))}
        </div>
      ) : (
        <ul className="flex flex-col gap-3">
          {items.map(({ label, color, count, total }) => {
            const pct = total > 0 ? Math.round((count / total) * 100) : 0
            return (
              <li key={label} className="space-y-1.5">
                <div className="flex items-center justify-between gap-2">
                  <div className="flex items-center gap-2 min-w-0">
                    <span
                      className="h-2 w-2 rounded-full shrink-0"
                      style={{ background: color }}
                      aria-hidden
                    />
                    <span className="text-[13px] text-ink2 truncate">{label}</span>
                  </div>
                  <span className="font-mono text-[12px] font-medium text-ink shrink-0">
                    {count}
                  </span>
                </div>
                <div className="h-1.5 w-full overflow-hidden rounded-full bg-gray-100" role="meter" aria-valuenow={pct} aria-valuemin={0} aria-valuemax={100} aria-label={`${label}: ${pct}%`}>
                  <div
                    className="h-full rounded-full transition-all duration-500"
                    style={{ width: `${pct}%`, background: color }}
                  />
                </div>
              </li>
            )
          })}
        </ul>
      )}
    </div>
  )
}
