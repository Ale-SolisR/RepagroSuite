interface TopRoomItem {
  roomId: string
  roomName: string
  percentage: number
}

interface TopRoomsListProps {
  items: TopRoomItem[]
  isLoading?: boolean
}

export type { TopRoomItem }

export default function TopRoomsList({ items, isLoading }: TopRoomsListProps) {
  return (
    <div className="rounded-[10px] border border-line bg-paper p-4 shadow-sh1 flex flex-col">
      <h2 className="text-sm font-semibold text-ink mb-4 leading-none">Salas más usadas</h2>

      {isLoading ? (
        <div className="space-y-4">
          {[87, 74, 58, 42].map(w => (
            <div key={w} className="space-y-1.5 animate-pulse">
              <div className="flex items-center justify-between">
                <div className="h-3 w-20 rounded bg-gray-200" />
                <div className="h-3 w-8 rounded bg-gray-200" />
              </div>
              <div className="h-1.5 w-full rounded-full bg-gray-100">
                <div className="h-full rounded-full bg-gray-200" style={{ width: `${w}%` }} />
              </div>
            </div>
          ))}
        </div>
      ) : items.length === 0 ? (
        <p className="py-8 text-center text-sm text-ink2">Sin datos de uso disponibles</p>
      ) : (
        <ul className="flex flex-col gap-4">
          {items.map(({ roomId, roomName, percentage }) => (
            <li key={roomId} className="space-y-1.5">
              <div className="flex items-center justify-between gap-2">
                <span className="truncate text-[13px] text-ink font-medium">{roomName}</span>
                <span className="font-mono text-[12px] font-medium text-ink2 shrink-0">
                  {percentage}%
                </span>
              </div>
              <div
                className="h-1.5 w-full overflow-hidden rounded-full"
                style={{ background: '#F0F7F4' }}
                role="meter"
                aria-valuenow={percentage}
                aria-valuemin={0}
                aria-valuemax={100}
                aria-label={`${roomName}: ${percentage}% de uso`}
              >
                <div
                  className="h-full rounded-full transition-all duration-500"
                  style={{ width: `${percentage}%`, background: '#006F55' }}
                />
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
