import { useState } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  ArrowLeft, Pencil, Trash2, Loader2, Cpu, History, ClipboardCheck, Undo2,
  Download, X, ChevronLeft, ChevronRight, ImageOff, KeyRound,
  UserMinus, AlertTriangle, Search, ShieldX, RotateCcw, FileText,
} from 'lucide-react'
import toast from 'react-hot-toast'
import { format, parseISO } from 'date-fns'
import { es } from 'date-fns/locale'

import { itAssetsApi } from '@/api/itAssets'
import { qk, staleTimes, invalidate } from '@/lib/queryKeys'
import { useAuthStore } from '@/store/authStore'
import { extractApiError } from '@/utils'
import Chip from '@/components/ui/Chip'
import AssetCredentialsModal from '@/components/ti/AssetCredentialsModal'
import MovementModal, { type MovementKind } from '@/components/ti/MovementModal'
import { statusChipVariant, STATUS_LABELS } from '@/components/ti/itStatus'

const BRAND = '#0E6B4B'

function Row({ label, value }: { label: string; value?: React.ReactNode }) {
  return (
    <div className="flex justify-between gap-4 border-b border-line py-2 last:border-0">
      <span className="text-[13px] text-ink2">{label}</span>
      <span className="text-[13px] font-medium text-ink text-right">{value ?? '—'}</span>
    </div>
  )
}

export default function ItAssetDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const qc = useQueryClient()
  const { hasPermission } = useAuthStore()
  const [lightbox, setLightbox] = useState<number | null>(null)   // índice de foto en grande
  const [downloading, setDownloading] = useState<string | null>(null)
  const [credsOpen, setCredsOpen] = useState(false)
  const [movement, setMovement] = useState<MovementKind | null>(null)
  const [reactivateOpen, setReactivateOpen] = useState(false)
  const [reactivateReason, setReactivateReason] = useState('')

  const { data: a, isLoading } = useQuery({
    queryKey: qk.ti.asset(id ?? ''),
    queryFn: () => itAssetsApi.getById(id!).then(r => r.data.data!),
    enabled: !!id,
    staleTime: staleTimes.ti,
  })

  const history = useQuery({
    queryKey: qk.ti.history(id ?? ''),
    queryFn: () => itAssetsApi.getHistory(id!).then(r => r.data.data ?? []),
    enabled: !!id,
    staleTime: staleTimes.ti,
  })

  const deleteMut = useMutation({
    mutationFn: () => itAssetsApi.delete(id!),
    onSuccess: () => { invalidate.ti(qc); toast.success('Activo eliminado.'); navigate('/ti/assets') },
    onError: (e) => toast.error(extractApiError(e)),
  })

  const reactivateMut = useMutation({
    mutationFn: () => itAssetsApi.reactivate(id!, { reason: reactivateReason.trim() }).then(r => r.data.data!),
    onSuccess: () => {
      invalidate.ti(qc)
      qc.invalidateQueries({ queryKey: qk.ti.asset(id!) })
      qc.invalidateQueries({ queryKey: qk.ti.history(id!) })
      toast.success('Activo reactivado y disponible.')
      setReactivateOpen(false); setReactivateReason('')
    },
    onError: (e) => toast.error(extractApiError(e)),
  })

  function onMovementDone() {
    setMovement(null)
    qc.invalidateQueries({ queryKey: qk.ti.asset(id!) })
    qc.invalidateQueries({ queryKey: qk.ti.history(id!) })
  }

  async function downloadPhoto(photoId: string, fileName?: string) {
    if (!id) return
    setDownloading(photoId)
    try {
      const res = await itAssetsApi.downloadPhoto(id, photoId)
      const url = URL.createObjectURL(res.data as Blob)
      const link = document.createElement('a')
      link.href = url
      link.download = fileName || `foto_${photoId}.jpg`
      document.body.appendChild(link)
      link.click()
      link.remove()
      URL.revokeObjectURL(url)
    } catch (e) {
      toast.error(extractApiError(e))
    } finally {
      setDownloading(null)
    }
  }

  if (isLoading) return <div className="flex h-full items-center justify-center"><Loader2 className="h-6 w-6 animate-spin text-ink2" /></div>
  if (!a) return <div className="p-6 text-ink2">Activo no encontrado.</div>

  const photos = a.photos ?? []

  // Estado del activo → acciones de movimiento disponibles (propuesta §4/§14).
  const isAssigned = a.status === 'Assigned' || a.status === 'Loaned'
  // Disponible o Devuelto (heredado) → asignable.
  const isAssignable = a.status === 'Available' || a.status === 'Returned'
  const isLive = a.status === 'Available' || isAssigned   // operable, puede sufrir incidente
  // Estados "detenidos" que un admin puede normalizar a Disponible (incidentes + heredados sin salida).
  // "Devuelto" se incluye para poder liberarlo del todo (limpia responsable + cierra asignación colgante).
  const isStuck = ['Damaged', 'Lost', 'Stolen', 'Inactive', 'UnderReview', 'UnderMaintenance', 'UnderRepair', 'Returned'].includes(a.status)
  const canAssign = isAssignable && hasPermission('Ti.Assign')
  const canReturn = isAssigned && hasPermission('Ti.Return')
  const canDeassign = isAssigned && hasPermission('Ti.Return')
  const canIncident = isLive && hasPermission('Ti.Ticket.Create')
  const canReactivate = isStuck && hasPermission('Ti.Asset.Reactivate')
  const hasMovements = canAssign || canReturn || canDeassign || canIncident || canReactivate

  return (
    <div className="flex min-h-full flex-col">
      <header className="sticky top-0 z-10 flex flex-wrap items-center gap-3 border-b border-line bg-paper px-4 sm:px-6 py-3" style={{ minHeight: 64 }}>
        <button onClick={() => navigate(-1)} className="rounded p-1.5 text-ink2 hover:bg-bg hover:text-ink" aria-label="Volver">
          <ArrowLeft className="h-5 w-5" />
        </button>
        <div className="min-w-0 flex-1">
          <p className="font-mono text-[12px] text-ink2 mb-0.5 leading-none">TI / Inventario / {a.internalCode}</p>
          <h1 className="text-[18px] font-semibold text-ink leading-tight tracking-tight flex items-center gap-2">
            <Cpu className="h-4.5 w-4.5" style={{ color: BRAND }} />
            {[a.assetTypeName, a.brandName, a.model].filter(Boolean).join(' · ')}
            <Chip variant={statusChipVariant(a.status)} label={a.statusName} />
          </h1>
        </div>
        <div className="flex items-center gap-2 shrink-0">
          <button onClick={() => setCredsOpen(true)}
            className="inline-flex items-center gap-1.5 rounded-[8px] border border-line bg-paper px-3 py-2 text-sm font-medium text-ink hover:bg-bg">
            <KeyRound className="h-4 w-4" style={{ color: BRAND }} /> Credenciales
          </button>
          {hasPermission('Ti.Inventory.Update') && (
            <Link to={`/ti/assets/${a.id}/edit`} className="inline-flex items-center gap-1.5 rounded-[8px] border border-line bg-paper px-3 py-2 text-sm font-medium text-ink hover:bg-bg">
              <Pencil className="h-4 w-4" /> Editar
            </Link>
          )}
          {hasPermission('Ti.Inventory.Delete') && (
            <button onClick={() => { if (confirm('¿Eliminar este activo? Se conserva en auditoría (borrado lógico).')) deleteMut.mutate() }}
              disabled={deleteMut.isPending}
              className="inline-flex items-center gap-1.5 rounded-[8px] border border-red-200 bg-red-50 px-3 py-2 text-sm font-medium text-red-700 hover:bg-red-100 disabled:opacity-50">
              <Trash2 className="h-4 w-4" /> Eliminar
            </button>
          )}
        </div>
      </header>

      <AssetCredentialsModal
        open={credsOpen}
        onClose={() => setCredsOpen(false)}
        assetId={a.id}
        assetCode={a.internalCode}
        canManage={hasPermission('Ti.Inventory.Update')}
      />

      <div className="flex-1 p-4 sm:p-6 bg-bg">
        <div className="grid gap-3.5 lg:grid-cols-3">
          {/* Columna principal */}
          <div className="space-y-3.5 lg:col-span-2">
            <section className="rounded-[10px] border border-line bg-paper p-5 shadow-sh1">
              <h2 className="mb-3 text-sm font-semibold text-ink">Ficha del activo</h2>
              <div className="grid gap-x-8 sm:grid-cols-2">
                <Row label="Código interno" value={<span className="font-mono">{a.internalCode}</span>} />
                <Row label="Tipo" value={a.assetTypeName} />
                <Row label="Marca" value={a.brandName} />
                <Row label="Modelo" value={a.model} />
                <Row label="Número de serie" value={a.serialNumber ? <span className="font-mono">{a.serialNumber}</span> : undefined} />
                <Row label="Placa / etiqueta" value={a.assetTag} />
                <Row label="Estado físico" value={a.physicalConditionName} />
                <Row label="Ubicación" value={[a.locationName, a.locationDetail].filter(Boolean).join(' · ') || undefined} />
                <Row label="Departamento" value={a.departmentName} />
                <Row label="Responsable" value={a.currentHolderName} />
                <Row label="Compra" value={a.purchaseDate ? format(parseISO(a.purchaseDate), 'd MMM yyyy', { locale: es }) : undefined} />
                <Row label="Proveedor" value={a.supplierName} />
                <Row label="Costo" value={a.cost != null ? `${a.currency ?? ''} ${a.cost.toLocaleString('es-CR')}`.trim() : undefined} />
                <Row label="N.º de factura" value={a.invoiceNumber} />
                <Row label="Garantía" value={a.hasWarranty ? (a.warrantyEndDate ? `Hasta ${format(parseISO(a.warrantyEndDate), 'd MMM yyyy', { locale: es })}` : 'Sí') : 'No'} />
              </div>
              {a.notes && <p className="mt-3 rounded-lg bg-bg p-3 text-[13px] text-ink2">{a.notes}</p>}
            </section>

            {a.spec && (
              <section className="rounded-[10px] border border-line bg-paper p-5 shadow-sh1">
                <h2 className="mb-3 text-sm font-semibold text-ink">Especificaciones técnicas</h2>
                <div className="grid gap-x-8 sm:grid-cols-2">
                  <Row label="Sistema operativo" value={a.spec.operatingSystem} />
                  <Row label="Procesador" value={a.spec.processor} />
                  <Row label="RAM" value={a.spec.ramGb ? `${a.spec.ramGb} GB` : undefined} />
                  <Row label="Disco" value={a.spec.diskGb ? `${a.spec.diskGb} GB` : undefined} />
                  <Row label="Nombre en dominio" value={a.spec.domainName} />
                  <Row label="AnyDesk ID" value={a.spec.anyDeskId} />
                  <Row label="Usuario M365" value={a.spec.microsoft365User} />
                  <Row label="Antivirus" value={a.spec.antivirusStatus} />
                </div>
              </section>
            )}

            {/* Historial / timeline */}
            <section className="rounded-[10px] border border-line bg-paper p-5 shadow-sh1">
              <h2 className="mb-3 flex items-center gap-2 text-sm font-semibold text-ink"><History className="h-4 w-4" /> Historial del activo</h2>
              {history.isLoading ? (
                <div className="h-24 animate-pulse rounded-lg bg-gray-100" />
              ) : (history.data ?? []).length === 0 ? (
                <p className="text-[13px] text-ink2">Sin eventos registrados.</p>
              ) : (
                <ol className="relative border-l border-line pl-4">
                  {(history.data ?? []).map(h => (
                    <li key={h.id} className="mb-4 last:mb-0">
                      <span className="absolute -left-[5px] mt-1 h-2.5 w-2.5 rounded-full" style={{ background: BRAND }} />
                      <p className="text-[13px] font-medium text-ink">
                        {h.eventType === 'CREATED' ? 'Activo registrado'
                          : h.eventType === 'REACTIVATED' ? 'Activo reactivado'
                          : h.eventType === 'STATUS_CHANGED'
                            ? `Estado: ${h.fromStatus ? STATUS_LABELS[h.fromStatus] : '—'} → ${h.toStatus ? STATUS_LABELS[h.toStatus] : '—'}`
                            : h.eventType}
                      </p>
                      {h.description && <p className="text-[12px] text-ink2">{h.description}</p>}
                      <div className="flex flex-wrap items-center gap-x-3 gap-y-0.5">
                        <p className="font-mono text-[11px] text-ink2">{format(parseISO(h.occurredAt), "d MMM yyyy · HH:mm", { locale: es })}</p>
                        {h.ticketId && (
                          <Link to={`/ti/tickets/${h.ticketId}`} className="inline-flex items-center gap-1 text-[11px] font-medium hover:underline" style={{ color: BRAND }}>
                            <FileText className="h-3 w-3" /> Ver boleta
                          </Link>
                        )}
                      </div>
                    </li>
                  ))}
                </ol>
              )}
            </section>
          </div>

          {/* Columna lateral */}
          <div className="space-y-3.5">
            <section className="rounded-[10px] border border-line bg-paper p-5 shadow-sh1">
              <h2 className="mb-3 flex items-center justify-between text-sm font-semibold text-ink">
                <span>Fotos del activo</span>
                {photos.length > 0 && <span className="font-mono text-[11px] text-ink2">{photos.length}/5</span>}
              </h2>
              {photos.length === 0 ? (
                <div className="flex flex-col items-center gap-2 rounded-lg bg-bg py-8 text-ink2">
                  <ImageOff className="h-7 w-7" strokeWidth={1.5} />
                  <p className="text-[13px]">Sin fotos registradas.</p>
                </div>
              ) : (
                <div className="grid grid-cols-3 gap-2">
                  {photos.map((p, i) => (
                    <div key={p.id} className="group relative aspect-square overflow-hidden rounded-lg border border-line bg-white">
                      <button
                        type="button"
                        onClick={() => setLightbox(i)}
                        className="block h-full w-full cursor-zoom-in"
                        title="Ver en grande"
                      >
                        <img src={p.url} alt={`Foto ${i + 1} de ${a.internalCode}`} className="h-full w-full object-cover transition-transform group-hover:scale-105" />
                      </button>
                      <button
                        type="button"
                        onClick={() => downloadPhoto(p.id, p.fileName)}
                        disabled={downloading === p.id}
                        className="absolute right-1 top-1 flex h-6 w-6 items-center justify-center rounded-full bg-black/55 text-white opacity-0 transition-opacity hover:bg-black/75 group-hover:opacity-100 disabled:opacity-50"
                        title="Descargar foto"
                        aria-label={`Descargar foto ${i + 1}`}
                      >
                        {downloading === p.id ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Download className="h-3.5 w-3.5" />}
                      </button>
                    </div>
                  ))}
                </div>
              )}
            </section>

            {hasMovements && (
              <section className="rounded-[10px] border border-line bg-paper p-5 shadow-sh1">
                <h2 className="mb-1 text-sm font-semibold text-ink">Movimientos</h2>
                <p className="mb-3 text-[11px] text-ink2">Cada movimiento emite una boleta firmada y queda auditado.</p>
                <div className="space-y-2">
                  {canAssign && (
                    <Link to={`/ti/assignments/new?assetId=${a.id}`}
                      className="flex w-full items-center justify-center gap-2 rounded-[8px] px-4 py-2.5 text-sm font-semibold text-white transition-opacity hover:opacity-90"
                      style={{ background: BRAND }}>
                      <ClipboardCheck className="h-4 w-4" /> Asignar
                    </Link>
                  )}
                  {canReturn && (
                    <Link to={`/ti/assets/${a.id}/return`}
                      className="flex w-full items-center justify-center gap-2 rounded-[8px] px-4 py-2.5 text-sm font-semibold text-white transition-opacity hover:opacity-90"
                      style={{ background: BRAND }}>
                      <Undo2 className="h-4 w-4" /> Devolver
                    </Link>
                  )}
                  {canDeassign && (
                    <button onClick={() => setMovement('deassign')}
                      className="flex w-full items-center justify-center gap-2 rounded-[8px] border border-line bg-paper px-4 py-2.5 text-sm font-medium text-ink transition-colors hover:bg-bg">
                      <UserMinus className="h-4 w-4" style={{ color: BRAND }} /> Desasignar
                    </button>
                  )}
                  {canIncident && (
                    <div className="grid grid-cols-3 gap-2">
                      <button onClick={() => setMovement('damaged')}
                        className="flex items-center justify-center gap-1.5 rounded-[8px] border px-3 py-2.5 text-sm font-semibold transition-colors hover:bg-rose-50"
                        style={{ borderColor: '#FDA4AF', color: '#BE123C' }}>
                        <AlertTriangle className="h-4 w-4" /> Dañado
                      </button>
                      <button onClick={() => setMovement('lost')}
                        className="flex items-center justify-center gap-1.5 rounded-[8px] border px-3 py-2.5 text-sm font-semibold transition-colors hover:bg-red-50"
                        style={{ borderColor: '#FCA5A5', color: '#B91C1C' }}>
                        <Search className="h-4 w-4" /> Perdido
                      </button>
                      <button onClick={() => setMovement('stolen')}
                        className="flex items-center justify-center gap-1.5 rounded-[8px] border px-3 py-2.5 text-sm font-semibold transition-colors hover:bg-pink-50"
                        style={{ borderColor: '#FBCFE8', color: '#BE185D' }}>
                        <ShieldX className="h-4 w-4" /> Robado
                      </button>
                    </div>
                  )}
                  {canReactivate && (
                    <button onClick={() => setReactivateOpen(true)}
                      className="flex w-full items-center justify-center gap-2 rounded-[8px] px-4 py-2.5 text-sm font-semibold text-white transition-opacity hover:opacity-90"
                      style={{ background: BRAND }}>
                      <RotateCcw className="h-4 w-4" /> Reactivar activo
                    </button>
                  )}
                </div>
                {isStuck && !canReactivate && (
                  <p className="mt-3 rounded-lg bg-bg p-2.5 text-[12px] text-ink2">
                    Activo inactivo. Solo un administrador puede reactivarlo.
                  </p>
                )}
              </section>
            )}
          </div>
        </div>
      </div>

      {movement && (
        <MovementModal open={!!movement} kind={movement} asset={a} onClose={() => setMovement(null)} onDone={onMovementDone} />
      )}

      {/* Reactivación (solo admin) */}
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

      {/* Lightbox: foto en grande al tocar */}
      {lightbox !== null && photos[lightbox] && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/85 p-4"
          onClick={() => setLightbox(null)}
        >
          <button
            type="button"
            onClick={() => setLightbox(null)}
            className="absolute right-4 top-4 flex h-10 w-10 items-center justify-center rounded-full bg-white/10 text-white hover:bg-white/20"
            aria-label="Cerrar"
          >
            <X className="h-5 w-5" />
          </button>

          {photos.length > 1 && (
            <>
              <button
                type="button"
                onClick={(e) => { e.stopPropagation(); setLightbox((lightbox - 1 + photos.length) % photos.length) }}
                className="absolute left-4 flex h-11 w-11 items-center justify-center rounded-full bg-white/10 text-white hover:bg-white/20"
                aria-label="Anterior"
              >
                <ChevronLeft className="h-6 w-6" />
              </button>
              <button
                type="button"
                onClick={(e) => { e.stopPropagation(); setLightbox((lightbox + 1) % photos.length) }}
                className="absolute right-4 top-1/2 flex h-11 w-11 -translate-y-1/2 items-center justify-center rounded-full bg-white/10 text-white hover:bg-white/20"
                aria-label="Siguiente"
              >
                <ChevronRight className="h-6 w-6" />
              </button>
            </>
          )}

          <figure className="flex max-h-full max-w-4xl flex-col items-center gap-3" onClick={(e) => e.stopPropagation()}>
            <img
              src={photos[lightbox].url}
              alt={`Foto ${lightbox + 1} de ${a.internalCode}`}
              className="max-h-[80vh] w-auto rounded-lg object-contain"
            />
            <figcaption className="flex items-center gap-3 text-[12px] text-white/80">
              <span>{lightbox + 1} / {photos.length}</span>
              <button
                type="button"
                onClick={() => downloadPhoto(photos[lightbox].id, photos[lightbox].fileName)}
                disabled={downloading === photos[lightbox].id}
                className="inline-flex items-center gap-1.5 rounded-[8px] bg-white/10 px-3 py-1.5 font-medium text-white hover:bg-white/20 disabled:opacity-50"
              >
                {downloading === photos[lightbox].id ? <Loader2 className="h-4 w-4 animate-spin" /> : <Download className="h-4 w-4" />}
                Descargar
              </button>
            </figcaption>
          </figure>
        </div>
      )}
    </div>
  )
}
