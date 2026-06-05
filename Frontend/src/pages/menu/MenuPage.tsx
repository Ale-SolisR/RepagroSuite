import { Link } from 'react-router-dom'
import { ChevronRight } from 'lucide-react'

import { useAuthStore } from '@/store/authStore'
import { navGroups } from '@/components/layout/navConfig'

const BRAND = '#0E6B4B'

/**
 * Menú/lanzador de módulos. En móvil es la pantalla a la que se entra tras el login:
 * el usuario ve los módulos a los que tiene acceso y elige a cuál entrar (en vez de caer
 * directo en un módulo). Reutiliza la misma definición de navegación del sidebar.
 */
export default function MenuPage() {
  const { user, hasPermission, hasRole } = useAuthStore()

  // Solo módulos navegables a los que el usuario tiene acceso (sin "Próximamente" ni placeholders).
  const groups = navGroups
    .map(g => ({
      label: g.label,
      items: g.items.filter(i =>
        !i.comingSoon && i.to !== '#' &&
        (!i.permission || hasPermission(i.permission)) &&
        (!i.role || hasRole(i.role)),
      ),
    }))
    .filter(g => g.items.length > 0)

  const firstName = (user?.fullName ?? '').trim().split(' ')[0] || 'Hola'

  return (
    <div className="min-h-full bg-bg">
      <header className="border-b border-line bg-paper px-4 py-5 sm:px-6">
        <p className="text-[13px] text-ink2">Hola, {firstName}</p>
        <h1 className="mt-0.5 text-[20px] font-semibold tracking-tight text-ink sm:text-[22px]">
          ¿A qué módulo quieres entrar?
        </h1>
      </header>

      <div className="mx-auto max-w-3xl space-y-6 p-4 sm:p-6">
        {groups.map(group => (
          <section key={group.label}>
            <h2 className="mb-2 px-1 text-[11px] font-semibold uppercase tracking-[.12em] text-ink2">
              {group.label}
            </h2>
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
              {group.items.map(({ to, label, icon: Icon }) => (
                <Link
                  key={to}
                  to={to}
                  className="group flex min-h-[96px] touch-manipulation flex-col justify-between rounded-[14px] border border-line bg-paper p-4 shadow-sh1 transition-colors hover:bg-bg active:scale-[.99]"
                >
                  <span
                    className="flex h-11 w-11 items-center justify-center rounded-[12px]"
                    style={{ background: '#ECFDF5', color: BRAND }}
                  >
                    <Icon className="h-[22px] w-[22px]" strokeWidth={1.9} />
                  </span>
                  <span className="mt-3 flex items-center justify-between gap-1">
                    <span className="text-[14px] font-semibold leading-tight text-ink">{label}</span>
                    <ChevronRight className="h-4 w-4 shrink-0 text-ink2 transition-transform group-hover:translate-x-0.5" />
                  </span>
                </Link>
              ))}
            </div>
          </section>
        ))}
      </div>
    </div>
  )
}
