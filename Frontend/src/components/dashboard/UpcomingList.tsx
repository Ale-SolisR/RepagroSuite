import { Link } from 'react-router-dom'
import { Users } from 'lucide-react'
import Chip, { reservationStatusToChip, reservationStatusLabel } from '@/components/ui/Chip'

interface UpcomingItem {
  id: string
  time: string
  title: string
  room: string
  organizer: string
  attendees: number
  status: string
}

interface UpcomingListProps {
  items: UpcomingItem[]
  isLoading?: boolean
}

export type { UpcomingItem }

export default function UpcomingList({ items, isLoading }: UpcomingListProps) {
  return (
    <div className="rounded-[10px] border border-line bg-paper p-4 shadow-sh1 flex flex-col">
      {/* Header */}
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-sm font-semibold text-ink leading-none">Próximas reservas</h2>
        <Link
          to="/admin/reservations"
          className="font-mono text-[11px] font-medium transition-colors hover:opacity-75"
          style={{ color: '#0E6B4B' }}
        >
          VER TODAS
        </Link>
      </div>

      {isLoading ? (
        <div className="space-y-4">
          {[1, 2, 3].map(i => (
            <div key={i} className="flex items-start gap-3 animate-pulse">
              <div className="h-4 w-12 rounded bg-gray-200 shrink-0 mt-0.5" />
              <div className="flex-1 space-y-1.5">
                <div className="h-3 w-3/4 rounded bg-gray-200" />
                <div className="h-3 w-1/2 rounded bg-gray-200" />
              </div>
              <div className="h-5 w-14 rounded-full bg-gray-200 shrink-0" />
            </div>
          ))}
        </div>
      ) : items.length === 0 ? (
        <div className="flex flex-1 flex-col items-center justify-center gap-2 py-8 text-center">
          <p className="text-sm text-ink2">Sin reservas próximas</p>
          <Link
            to="/reservations"
            className="text-[12px] font-medium hover:opacity-75 transition-opacity"
            style={{ color: '#0E6B4B' }}
          >
            + Nueva reserva
          </Link>
        </div>
      ) : (
        <ul className="space-y-3" role="list">
          {items.map(item => (
            <li
              key={item.id}
              className="grid items-start gap-3"
              style={{ gridTemplateColumns: '72px 1fr auto' }}
            >
              <span className="font-mono text-[13px] font-semibold pt-0.5" style={{ color: '#0E6B4B' }}>
                {item.time}
              </span>
              <div className="min-w-0">
                <p className="truncate text-[13px] font-medium text-ink leading-snug">
                  {item.title} · <span className="font-normal text-ink2">{item.room}</span>
                </p>
                <p className="flex items-center gap-1 text-[11px] text-ink2 mt-0.5">
                  {item.organizer}
                  <span className="text-line" aria-hidden>·</span>
                  <Users className="h-3 w-3" strokeWidth={1.5} aria-hidden />
                  {item.attendees} invitados
                </p>
              </div>
              <Chip
                variant={reservationStatusToChip(item.status)}
                label={reservationStatusLabel(item.status)}
              />
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
