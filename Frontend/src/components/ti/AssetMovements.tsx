import { useState, type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Loader2, ClipboardCheck, Undo2, AlertTriangle, Search, ShieldX, RotateCcw,
} from 'lucide-react'
import toast from 'react-hot-toast'

import { itAssetsApi } from '@/api/itAssets'
import { qk, invalidate } from '@/lib/queryKeys'
import { useAuthStore } from '@/store/authStore'
import { extractApiError } from '@/utils'
import Chip from '@/components/ui/Chip'
import MovementModal, { type MovementKind } from '@/components/ti/MovementModal'
import { statusChipVariant } from '@/components/ti/itStatus'
import type { ItAssetDto } from '@/types'

const BRAND = '#0E6B4B'

// Botón de acción con altura táctil mínima (≥44px) y sin retardo de 300ms en móvil.
const ACTION_BASE =
  'flex items-center justify-center gap-2 rounded-[8px] px-4 text-sm font-semibold transition-colors touch-manipulation min-h-[44px] disabled:opacity-50'

interface Props {
  asset: ItAssetDto
  /** Muestra el código y estado del activo en el encabezado (útil cuando hay varios, p. ej. en una boleta). */
  showAssetCode?: boolean
  /** Se llama tras un movimiento/reactivación exitosa para que el contenedor refresque lo suyo. */
  onChanged?: () => void
  /** Muestra las acciones pero deshabilitadas (p. ej. desde una boleta anulada). */
  readOnly?: boolean
}

/**
 * Panel de acciones de movimiento de un activo (Asignar, Devolver, Desasignar, Dañado,
 * Perdido, Robado, Reactivar). Reúne la lógica de estado→acciones y los modales con firma,
 * para usarse igual desde la ficha del activo y desde el detalle de una boleta.
 */
export default function AssetMovements({ asset: a, showAssetCode = false, onChanged, readOnly = false }: Props) {
  const qc = useQueryClient()
  const { hasPermission } = useAuthStore()
  const [movement, setMovement] = useState<MovementKind | null>(null)
  const [reactivateOpen, setReactivateOpen] = useState(false)
  const [reactivateReason, setReactivateReason] = useState('')

  function refresh() {
    invalidate.ti(qc)
    qc.invalidateQueries({ queryKey: qk.ti.asset(a.id) })
    qc.invalidateQueries({ queryKey: qk.ti.history(a.id) })
    onChanged?.()
  }

  const reactivateMut = useMutation({
    mutationFn: () => itAssetsApi.reactivate(a.id, { reason: reactivateReason.trim() }).then(r => r.data.data!),
    onSuccess: () => {
      toast.success('Activo reactivado y disponible.')
      setReactivateOpen(false); setReactivateReason('')
      refresh()
    },
    onError: (e) => toast.error(extractApiError(e)),
  })

  // Estado del activo → acciones disponibles (mismas reglas que la ficha del activo).
  const isAssigned = a.status === 'Assigned' || a.status === 'Loaned'
  const isAssignable = a.status === 'Available' || a.status === 'Returned'
  const isLive = a.status === 'Available' || isAssigned
  // Reactivable: estados "detenidos" recuperables. NO incluye Perdido/Robado: un activo dado de
  // baja por pérdida o robo es definitivo y no puede reactivarse (regla de negocio).
  const isReactivatable = ['Damaged', 'Inactive', 'Returned', 'UnderReview', 'UnderMaintenance', 'UnderRepair'].includes(a.status)
  const isLostOrStolen = a.status === 'Lost' || a.status === 'Stolen'
  const canAssign = isAssignable && hasPermission('Ti.Assign')
  const canReturn = isAssigned && hasPermission('Ti.Return')
  const canIncident = isLive && hasPermission('Ti.Ticket.Create')
  const canReactivate = isReactivatable && hasPermission('Ti.Asset.Reactivate')
  const hasMovements = canAssign || canReturn || canIncident || canReactivate

  // Sin acciones y sin nada que explicar → no renderizamos nada (evita ruido).
  if (!hasMovements && !isReactivatable && !isLostOrStolen) return null

  // En modo solo-lectura (boleta anulada) los enlaces se vuelven botones inertes con el mismo aspecto.
  const ro = readOnly ? 'opacity-50 cursor-not-allowed' : 'hover:opacity-90'
  const primaryLink = (to: string, content: ReactNode) =>
    readOnly
      ? <button type="button" disabled className={`${ACTION_BASE} w-full text-white ${ro}`} style={{ background: BRAND }}>{content}</button>
      : <Link to={to} className={`${ACTION_BASE} w-full text-white ${ro}`} style={{ background: BRAND }}>{content}</Link>

  return (
    <section className="rounded-[10px] border border-line bg-paper p-5 shadow-sh1">
      <div className="mb-1 flex items-center justify-between gap-2">
        <h2 className="text-sm font-semibold text-ink">Movimientos</h2>
        {showAssetCode && (
          <span className="flex items-center gap-2">
            <span className="font-mono text-[12px] text-ink2">{a.internalCode}</span>
            <Chip variant={statusChipVariant(a.status)} label={a.statusName} />
          </span>
        )}
      </div>
      {hasMovements && (
        <p className="mb-3 text-[11px] text-ink2">Cada movimiento emite una boleta firmada y queda auditado.</p>
      )}

      {readOnly && (
        <p className="mb-3 rounded-lg bg-bg p-2.5 text-[12px] text-ink2">
          Boleta anulada · acciones de solo lectura. Para operar el activo, abre su ficha en el inventario.
        </p>
      )}

      {hasMovements && (
        <div className="space-y-2">
          {canAssign && primaryLink(`/ti/assignments/new?assetId=${a.id}`,
            <><ClipboardCheck className="h-4 w-4" /> Asignar</>)}
          {canReturn && primaryLink(`/ti/assets/${a.id}/return`,
            <><Undo2 className="h-4 w-4" /> Devolver</>)}
          {canIncident && (
            <div className="grid grid-cols-3 gap-2">
              <button onClick={() => setMovement('damaged')} disabled={readOnly}
                className={`${ACTION_BASE} border px-3 ${readOnly ? 'cursor-not-allowed' : 'hover:bg-rose-50'}`} style={{ borderColor: '#FDA4AF', color: '#BE123C' }}>
                <AlertTriangle className="h-4 w-4" /> Dañado
              </button>
              <button onClick={() => setMovement('lost')} disabled={readOnly}
                className={`${ACTION_BASE} border px-3 ${readOnly ? 'cursor-not-allowed' : 'hover:bg-red-50'}`} style={{ borderColor: '#FCA5A5', color: '#B91C1C' }}>
                <Search className="h-4 w-4" /> Perdido
              </button>
              <button onClick={() => setMovement('stolen')} disabled={readOnly}
                className={`${ACTION_BASE} border px-3 ${readOnly ? 'cursor-not-allowed' : 'hover:bg-pink-50'}`} style={{ borderColor: '#FBCFE8', color: '#BE185D' }}>
                <ShieldX className="h-4 w-4" /> Robado
              </button>
            </div>
          )}
          {canReactivate && (
            <button onClick={() => setReactivateOpen(true)} disabled={readOnly}
              className={`${ACTION_BASE} w-full text-white ${ro}`} style={{ background: BRAND }}>
              <RotateCcw className="h-4 w-4" /> Reactivar activo
            </button>
          )}
        </div>
      )}

      {isReactivatable && !canReactivate && (
        <p className="mt-3 rounded-lg bg-bg p-2.5 text-[12px] text-ink2">
          Activo inactivo. Solo un administrador puede reactivarlo.
        </p>
      )}

      {isLostOrStolen && (
        <p className="mt-3 rounded-lg bg-bg p-2.5 text-[12px] text-ink2">
          Activo dado de baja por {a.status === 'Lost' ? 'pérdida' : 'robo'}. No puede reactivarse.
        </p>
      )}

      {movement && (
        <MovementModal
          open={!!movement}
          kind={movement}
          asset={a}
          onClose={() => setMovement(null)}
          onDone={() => { setMovement(null); refresh() }}
        />
      )}

      {reactivateOpen && (
        <div className="fixed inset-0 z-[1000] flex items-end justify-center bg-black/55 sm:items-center sm:p-4"
          onMouseDown={(e) => { if (e.target === e.currentTarget) setReactivateOpen(false) }}
          role="dialog" aria-modal="true" aria-label="Reactivar activo">
          <div className="w-full rounded-t-2xl bg-paper p-5 shadow-xl sm:max-w-md sm:rounded-2xl">
            <div className="mb-3 flex items-center gap-3">
              <span className="flex h-9 w-9 items-center justify-center rounded-full" style={{ background: '#ECFDF5', color: BRAND }}>
                <RotateCcw className="h-5 w-5" />
              </span>
              <div>
                <h2 className="text-[15px] font-semibold text-ink">Reactivar activo</h2>
                <p className="font-mono text-[12px] text-ink2">{a.internalCode}</p>
              </div>
            </div>
            <p className="mb-3 rounded-lg bg-bg p-2.5 text-[12px] text-ink2">
              El activo volverá a <span className="font-medium text-ink">Disponible</span>. La acción queda auditada con su motivo.
            </p>
            <label className="mb-1 block text-[12px] font-medium text-ink2">Motivo de la reactivación *</label>
            <textarea
              className="mb-4 min-h-[80px] w-full rounded-[8px] border border-line bg-paper px-3 py-2 text-sm text-ink focus:border-brand-400 focus:outline-none"
              value={reactivateReason} onChange={e => setReactivateReason(e.target.value)}
              placeholder="Ej.: equipo reparado y verificado; aparición del equipo extraviado…"
            />
            <div className="flex items-center gap-2.5">
              <button onClick={() => setReactivateOpen(false)}
                className="rounded-[8px] border border-line bg-paper px-4 py-2.5 text-sm font-medium text-ink hover:bg-bg">Cancelar</button>
              <button onClick={() => reactivateMut.mutate()}
                disabled={!reactivateReason.trim() || reactivateMut.isPending}
                className="inline-flex flex-1 items-center justify-center gap-2 rounded-[8px] px-4 py-2.5 text-sm font-semibold text-white transition-opacity hover:opacity-90 disabled:opacity-50"
                style={{ background: BRAND }}>
                {reactivateMut.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <RotateCcw className="h-4 w-4" />}
                Reactivar
              </button>
            </div>
          </div>
        </div>
      )}
    </section>
  )
}
