import { useEffect, useRef, useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { X, Loader2, UserMinus, AlertTriangle, Search, ShieldX } from 'lucide-react'
import toast from 'react-hot-toast'

import { itTicketsApi } from '@/api/itTickets'
import { invalidate } from '@/lib/queryKeys'
import { extractApiError } from '@/utils'
import PhotoCapture from '@/components/ti/PhotoCapture'
import SignaturePad from '@/components/ti/SignaturePad'
import { CONDITION_OPTIONS } from '@/components/ti/itStatus'
import type { ItAssetDto, ItTicketDto, PhysicalCondition, SignatureInput } from '@/types'

const BRAND = '#0E6B4B'

export type MovementKind = 'deassign' | 'damaged' | 'lost' | 'stolen'

interface MovementModalProps {
  open: boolean
  onClose: () => void
  asset: ItAssetDto
  kind: MovementKind
  onDone: (ticket: ItTicketDto) => void
}

const META: Record<MovementKind, {
  title: string
  icon: typeof UserMinus
  accent: string
  tint: string
  cta: string
  requireEmployeeSignature: boolean
  needsReason: boolean
}> = {
  deassign: {
    title: 'Desasignar equipo', icon: UserMinus, accent: BRAND, tint: '#ECFDF5',
    cta: 'Emitir boleta de desasignación', requireEmployeeSignature: true, needsReason: false,
  },
  damaged: {
    title: 'Marcar como dañado', icon: AlertTriangle, accent: '#BE123C', tint: '#FFF1F2',
    cta: 'Emitir boleta por daño', requireEmployeeSignature: false, needsReason: true,
  },
  lost: {
    title: 'Marcar como perdido', icon: Search, accent: '#B91C1C', tint: '#FEF2F2',
    cta: 'Emitir boleta por pérdida', requireEmployeeSignature: false, needsReason: true,
  },
  stolen: {
    title: 'Marcar como robado', icon: ShieldX, accent: '#BE185D', tint: '#FDF2F8',
    cta: 'Emitir boleta por robo', requireEmployeeSignature: false, needsReason: true,
  },
}

/**
 * Modal unificado de movimiento con firma para los flujos de un solo activo:
 * Desasignar, Danado, Perdido y Robado. Bottom-sheet en movil, tarjeta centrada en escritorio.
 * Las firmas requeridas se validan aquí y también en el servidor.
 */
export default function MovementModal({ open, onClose, asset, kind, onDone }: MovementModalProps) {
  const qc = useQueryClient()
  const meta = META[kind]
  const Icon = meta.icon

  const [reason, setReason] = useState('')
  const [condition, setCondition] = useState<PhysicalCondition | ''>('')
  const [photos, setPhotos] = useState<string[]>([])
  const [sigEmployee, setSigEmployee] = useState<string | null>(null)
  const [sigIt, setSigIt] = useState<string | null>(null)
  const submittingRef = useRef(false)

  // Reset al abrir/cerrar o cambiar de tipo.
  useEffect(() => {
    if (open) {
      setReason(''); setCondition(''); setPhotos([])
      setSigEmployee(null); setSigIt(null); submittingRef.current = false
    }
  }, [open, kind])

  // Cerrar con ESC.
  useEffect(() => {
    if (!open) return
    const onKey = (e: KeyboardEvent) => { if (e.key === 'Escape') onClose() }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [open, onClose])

  const mutation = useMutation({
    mutationFn: () => {
      const signatures: SignatureInput[] = []
      if (sigEmployee) signatures.push({ signerType: 'Colaborador', signerName: asset.currentHolderName ?? undefined, imageBase64: sigEmployee })
      if (sigIt) signatures.push({ signerType: 'ResponsableTI', imageBase64: sigIt })

      if (kind === 'deassign') {
        return itTicketsApi.createDeassignment({
          assetId: asset.id, notes: reason || undefined, photos, signatures,
        }).then(r => r.data.data!)
      }
      const targetStatus = kind === 'damaged' ? 'Damaged' : kind === 'lost' ? 'Lost' : 'Stolen'
      return itTicketsApi.createIncident({
        assetId: asset.id, targetStatus, reason: reason.trim(),
        condition: condition || undefined, photos, signatures,
      }).then(r => r.data.data!)
    },
    onSuccess: (ticket) => {
      invalidate.ti(qc)
      toast.success(`Boleta ${ticket.ticketNumber} emitida.`)
      onDone(ticket)
    },
    onError: (e) => { submittingRef.current = false; toast.error(extractApiError(e)) },
  })

  if (!open) return null

  function submit() {
    if (submittingRef.current) return
    if (meta.needsReason && !reason.trim()) return toast.error('Indique el motivo.')
    if (meta.requireEmployeeSignature && !sigEmployee) return toast.error('Falta la firma del colaborador.')
    if (!sigIt) return toast.error('Falta la firma del responsable de TI.')
    submittingRef.current = true
    mutation.mutate()
  }

  const reasonLabel = kind === 'deassign'
    ? 'Observaciones'
    : kind === 'damaged' ? 'Motivo del daño *' : kind === 'lost' ? 'Motivo de la pérdida *' : 'Motivo del robo *'

  return (
    <div
      className="fixed inset-0 z-[1000] flex items-end justify-center bg-black/55 sm:items-center sm:p-4"
      onMouseDown={(e) => { if (e.target === e.currentTarget) onClose() }}
      role="dialog"
      aria-modal="true"
      aria-label={meta.title}
    >
      <div className="flex max-h-[92dvh] w-full flex-col overflow-hidden rounded-t-2xl bg-paper shadow-xl sm:max-w-lg sm:rounded-2xl">
        {/* Header */}
        <div className="flex items-center gap-3 border-b border-line px-4 py-3 sm:px-5">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full" style={{ background: meta.tint, color: meta.accent }}>
            <Icon className="h-5 w-5" />
          </span>
          <div className="min-w-0 flex-1">
            <h2 className="text-[15px] font-semibold leading-tight text-ink">{meta.title}</h2>
            <p className="truncate font-mono text-[12px] text-ink2">
              {asset.internalCode}{asset.model ? ` · ${asset.model}` : ''}
            </p>
          </div>
          <button onClick={onClose} className="flex h-9 w-9 items-center justify-center rounded-full text-ink2 hover:bg-bg hover:text-ink" aria-label="Cerrar">
            <X className="h-5 w-5" />
          </button>
        </div>

        {/* Body (scroll) */}
        <div className="flex-1 space-y-4 overflow-y-auto px-4 py-4 sm:px-5">
          <div className="rounded-[10px] bg-bg p-3 text-[13px] text-ink2">
            {asset.assetTypeName}{asset.brandName ? ` · ${asset.brandName}` : ''} ·
            Responsable actual: <span className="font-medium text-ink">{asset.currentHolderName ?? '—'}</span>
          </div>

          {kind === 'damaged' && (
            <div>
              <label className="mb-1 block text-[12px] font-medium text-ink2">Estado físico resultante</label>
              <select
                className="h-11 w-full rounded-[8px] border border-line bg-paper px-3 text-sm text-ink focus:border-brand-400 focus:outline-none"
                value={condition}
                onChange={e => setCondition(e.target.value as PhysicalCondition | '')}
              >
                <option value="">Sin cambios</option>
                {CONDITION_OPTIONS.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
              </select>
            </div>
          )}

          <div>
            <label className="mb-1 block text-[12px] font-medium text-ink2">{reasonLabel}</label>
            <textarea
              className="min-h-[80px] w-full rounded-[8px] border border-line bg-paper px-3 py-2 text-sm text-ink focus:border-brand-400 focus:outline-none"
              value={reason}
              onChange={e => setReason(e.target.value)}
              placeholder={kind === 'deassign' ? 'Motivo del cierre, notas…' : 'Describa lo ocurrido…'}
            />
          </div>

          <PhotoCapture value={photos} onChange={setPhotos} max={3} hint="Evidencia fotográfica (opcional)." />

          <SignaturePad
            label={`Firma del colaborador${meta.requireEmployeeSignature ? ' *' : ' (opcional)'}`}
            value={sigEmployee}
            onConfirm={setSigEmployee}
            onClear={() => setSigEmployee(null)}
          />
          <SignaturePad
            label="Firma del responsable TI *"
            value={sigIt}
            onConfirm={setSigIt}
            onClear={() => setSigIt(null)}
          />
        </div>

        {/* Footer */}
        <div className="flex items-center gap-2.5 border-t border-line px-4 py-3 sm:px-5">
          <button
            onClick={onClose}
            className="rounded-[8px] border border-line bg-paper px-4 py-2.5 text-sm font-medium text-ink hover:bg-bg"
          >
            Cancelar
          </button>
          <button
            onClick={submit}
            disabled={mutation.isPending}
            className="inline-flex flex-1 items-center justify-center gap-2 rounded-[8px] px-4 py-2.5 text-sm font-semibold text-white transition-opacity hover:opacity-90 disabled:opacity-50"
            style={{ background: meta.accent }}
          >
            {mutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Icon className="h-4 w-4" />}
            {meta.cta}
          </button>
        </div>
      </div>
    </div>
  )
}
