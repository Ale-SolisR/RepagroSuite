import { classNames } from '@/utils'

interface SkeletonProps {
  className?: string
  'aria-label'?: string
}

export function Skeleton({ className, 'aria-label': label = 'Cargando...' }: SkeletonProps) {
  return (
    <div
      role="status"
      aria-busy="true"
      aria-label={label}
      className={classNames(
        'animate-pulse rounded bg-gray-200',
        className ?? ''
      )}
    />
  )
}

export function KpiCardSkeleton() {
  return (
    <div className="rounded-[10px] border border-line bg-paper p-4 shadow-sh1" role="status" aria-busy="true" aria-label="Cargando métrica">
      <Skeleton className="h-3 w-24 mb-4" />
      <Skeleton className="h-7 w-20 mb-2" />
      <Skeleton className="h-3 w-28" />
    </div>
  )
}

export function ChartSkeleton() {
  return (
    <div className="rounded-[10px] border border-line bg-paper p-4 shadow-sh1" role="status" aria-busy="true" aria-label="Cargando gráfico">
      <div className="flex items-center justify-between mb-4">
        <Skeleton className="h-4 w-48" />
        <Skeleton className="h-7 w-28 rounded-full" />
      </div>
      <Skeleton className="h-40 w-full rounded-lg" />
    </div>
  )
}

export function ListSkeleton({ rows = 3 }: { rows?: number }) {
  return (
    <div className="rounded-[10px] border border-line bg-paper p-4 shadow-sh1" role="status" aria-busy="true" aria-label="Cargando lista">
      <Skeleton className="h-4 w-36 mb-4" />
      <div className="space-y-3">
        {Array.from({ length: rows }).map((_, i) => (
          <div key={i} className="flex items-center gap-3">
            <Skeleton className="h-4 w-12" />
            <div className="flex-1 space-y-1.5">
              <Skeleton className="h-3 w-full" />
              <Skeleton className="h-3 w-2/3" />
            </div>
            <Skeleton className="h-5 w-14 rounded-full" />
          </div>
        ))}
      </div>
    </div>
  )
}

export function BarListSkeleton({ rows = 4 }: { rows?: number }) {
  return (
    <div className="rounded-[10px] border border-line bg-paper p-4 shadow-sh1" role="status" aria-busy="true" aria-label="Cargando">
      <Skeleton className="h-4 w-40 mb-4" />
      <div className="space-y-4">
        {Array.from({ length: rows }).map((_, i) => (
          <div key={i} className="space-y-1.5">
            <div className="flex justify-between">
              <Skeleton className="h-3 w-20" />
              <Skeleton className="h-3 w-8" />
            </div>
            <Skeleton className="h-2 w-full rounded-full" />
          </div>
        ))}
      </div>
    </div>
  )
}

export default Skeleton
