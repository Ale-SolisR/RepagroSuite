import { useMemo } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { useQuery, useQueries, useMutation, useQueryClient } from '@tanstack/react-query'
import { ArrowLeft, Loader2, FileText, Download, Ban, ShieldCheck, Wrench, Link2, CornerUpLeft, CornerDownRight, RefreshCw } from 'lucide-react'
import toast from 'react-hot-toast'
import { format, parseISO } from 'date-fns'
import { es } from 'date-fns/locale'

import { itTicketsApi } from '@/api/itTickets'
import { itAssetsApi } from '@/api/itAssets'
import { qk, staleTimes, invalidate } from '@/lib/queryKeys'
import { useAuthStore } from '@/store/authStore'
import { extractApiError } from '@/utils'
import Chip from '@/components/ui/Chip'
import AssetMovements from '@/components/ti/AssetMovements'
import { useConfirm } from '@/components/ui/ConfirmDialog'
import { downloadBlob, filenameFromContentDisposition } from '@/lib/download'
import { ticketStatusChipVariant } from '@/components/ti/itStatus'
import type { ItAssetDto } from '@/types'

const BRAND = '#0E6B4B'

export default function ItTicketDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const qc = useQueryClient()
  const { hasPermission } = useAuthStore()
  const confirm = useConfirm()

  const { data: t, isLoading } = useQuery({
    queryKey: qk.ti.ticket(id ?? ''),
    queryFn: () => itTicketsApi.getById(id!).then(r => r.data.data!),
    enabled: !!id,
    staleTime: staleTimes.ti,
  })

  // Activos de la boleta: cargamos su ficha actual para ofrecer las mismas acciones de
  // movimiento que en el inventario, según el estado REAL de cada activo hoy.
  const assetIds = useMemo(
    () => [...new Set((t?.lines ?? []).map(l => l.assetId).filter((x): x is string => !!x))],
    [t],
  )
  const assetQueries = useQueries({
    queries: assetIds.map(aid => ({
      queryKey: qk.ti.asset(aid),
      queryFn: () => itAssetsApi.getById(aid).then(r => r.data.data!),
      enabled: !!aid,
      staleTime: staleTimes.ti,
    })),
  })
  const assets = assetQueries.map(q => q.data).filter((x): x is ItAssetDto => !!x)

  const pdfMut = useMutation({
    mutationFn: () => itTicketsApi.getPdf(id!),
    onSuccess: (res) => {
      const blob = res.data as Blob
      // Nombre identificativo desde el backend; si no, lo armamos con el número de boleta.
      const fileName = filenameFromContentDisposition(res.headers?.['content-disposition'])
        ?? `${t?.ticketNumber ?? 'boleta'}.pdf`
      downloadBlob(blob, fileName)
    },
    onError: (e) => toast.error(extractApiError(e)),
  })

  const regenMut = useMutation({
    mutationFn: () => itTicketsApi.regeneratePdf(id!).then(r => r.data.data!),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: qk.ti.ticket(id!) })
      toast.success('PDF regenerado con el formato actual.')
      pdfMut.mutate()   // descarga el PDF nuevo (con cédulas y condición en español)
    },
    onError: (e) => toast.error(extractApiError(e)),
  })

  const voidMut = useMutation({
    mutationFn: (reason: string) => itTicketsApi.void(id!, { reason }).then(r => r.data.data!),
    onSuccess: () => {
      invalidate.ti(qc)
      qc.invalidateQueries({ queryKey: qk.ti.ticket(id!) })
      toast.success('Boleta anulada.')
    },
    onError: (e) => toast.error(extractApiError(e)),
  })

  async function handleVoid() {
    const reason = await confirm({
      title: `Anular boleta ${t?.ticketNumber ?? ''}`.trim(),
      message: 'La boleta quedará anulada y la acción se registra en la auditoría. Indica el motivo.',
      variant: 'danger',
      confirmText: 'Anular boleta',
      input: { label: 'Motivo de anulación', required: true, multiline: true, placeholder: 'Describe por qué se anula esta boleta…' },
    })
    if (!reason) return
    voidMut.mutate(reason)
  }

  if (isLoading) return <div className="flex h-full items-center justify-center"><Loader2 className="h-6 w-6 animate-spin text-ink2" /></div>
  if (!t) return <div className="p-6 text-ink2">Boleta no encontrada.</div>

  return (
    <div className="flex min-h-full flex-col">
      <header className="sticky top-0 z-10 flex flex-wrap items-center gap-x-3 gap-y-2 border-b border-line bg-paper px-4 sm:px-6 py-2.5">
        <button onClick={() => navigate(-1)} className="shrink-0 rounded p-1.5 text-ink2 hover:bg-bg hover:text-ink" aria-label="Volver"><ArrowLeft className="h-5 w-5" /></button>
        <div className="min-w-0 flex-1">
          <p className="font-mono text-[11px] text-ink2 mb-0.5 leading-none">TI / Boletas / {t.ticketTypeName}</p>
          <div className="flex flex-wrap items-center gap-x-2 gap-y-1">
            <h1 className="flex min-w-0 items-center gap-1.5 text-[16px] font-semibold leading-tight tracking-tight text-ink sm:text-[18px]">
              <FileText className="h-4 w-4 shrink-0 sm:h-[18px] sm:w-[18px]" style={{ color: BRAND }} />
              <span className="break-all">{t.ticketNumber}</span>
            </h1>
            <Chip variant={ticketStatusChipVariant(t.status)} label={t.statusName} />
          </div>
        </div>
        {/* Acciones: fila propia a todo el ancho en móvil, en línea a la derecha en ≥sm */}
        <div className="flex w-full gap-2 sm:w-auto sm:shrink-0">
          {t.hasPdf && (
            <button onClick={() => pdfMut.mutate()} disabled={pdfMut.isPending}
              className="inline-flex min-h-[44px] flex-1 items-center justify-center gap-1.5 rounded-[8px] px-3 text-sm font-medium text-white transition-colors touch-manipulation hover:opacity-90 disabled:opacity-50 sm:flex-none" style={{ background: BRAND }}>
              {pdfMut.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Download className="h-4 w-4" />} PDF
            </button>
          )}
          {t.hasPdf && hasPermission('Ti.Ticket.Create') && (
            <button onClick={() => regenMut.mutate()} disabled={regenMut.isPending || pdfMut.isPending}
              title="Regenerar el PDF con el formato actual (cédulas, condición en español)"
              className="inline-flex min-h-[44px] flex-1 items-center justify-center gap-1.5 rounded-[8px] border border-line bg-paper px-3 text-sm font-medium text-ink transition-colors touch-manipulation hover:bg-bg disabled:opacity-50 sm:flex-none">
              {regenMut.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <RefreshCw className="h-4 w-4" />} Regenerar
            </button>
          )}
          {t.status !== 'Anulada' && hasPermission('Ti.Ticket.Void') && (
            <button onClick={handleVoid} disabled={voidMut.isPending}
              className="inline-flex min-h-[44px] flex-1 items-center justify-center gap-1.5 rounded-[8px] border border-red-200 bg-red-50 px-3 text-sm font-medium text-red-700 transition-colors touch-manipulation hover:bg-red-100 disabled:opacity-50 sm:flex-none">
              <Ban className="h-4 w-4" /> Anular
            </button>
          )}
        </div>
      </header>

      <div className="flex-1 p-4 sm:p-6 bg-bg">
        <div className="mx-auto max-w-3xl space-y-3.5">
          {t.status === 'Anulada' && (
            <div className="rounded-[10px] border p-4" style={{ background: '#FEF2F2', borderColor: '#FECACA' }}>
              <p className="text-sm font-medium" style={{ color: '#991B1B' }}>Boleta anulada{t.voidedAt ? ` el ${format(parseISO(t.voidedAt), 'd MMM yyyy', { locale: es })}` : ''}{t.voidedByName ? ` por ${t.voidedByName}` : ''}</p>
              {t.voidReason && <p className="text-[13px]" style={{ color: '#B91C1C' }}>Motivo: {t.voidReason}</p>}
            </div>
          )}

          <section className="rounded-[10px] border border-line bg-paper p-5 shadow-sh1">
            <div className="grid gap-x-8 sm:grid-cols-2">
              <Field label="Tipo" value={t.ticketTypeName} />
              <Field label="Emitida" value={format(parseISO(t.issuedAt), "d MMM yyyy · HH:mm", { locale: es })} />
              <Field label="Emitida por" value={t.itResponsibleName} />
              <Field label="Colaborador" value={t.employeeName} />
              <Field label="Cédula colaborador" value={t.employeeIdentification} />
            </div>
            {t.notes && <p className="mt-3 rounded-lg bg-bg p-3 text-[13px] text-ink2">{t.notes}</p>}
            {t.pdfSha256 && <p className="mt-2 break-all font-mono text-[10.5px] text-ink2">SHA-256: {t.pdfSha256}</p>}
          </section>

          {/* Cadena de trazabilidad: entrega ↔ devolución/desasignación/incidente (sin anular nada). */}
          {t.chain.length > 0 && (
            <section className="rounded-[10px] border border-line bg-paper p-5 shadow-sh1">
              <h2 className="mb-1 flex items-center gap-2 text-sm font-semibold text-ink">
                <Link2 className="h-4 w-4" style={{ color: BRAND }} /> Cadena de trazabilidad
              </h2>
              <p className="mb-3 text-[11px] text-ink2">Boletas enlazadas a este movimiento. Cada una queda válida y firmada.</p>
              <div className="space-y-2">
                {t.chain.map(c => (
                  <Link
                    key={`${c.ticketId}-${c.assetCode ?? ''}`}
                    to={`/ti/tickets/${c.ticketId}`}
                    className="flex min-h-[44px] items-center gap-3 rounded-[10px] border border-line bg-paper p-3 transition-colors touch-manipulation hover:bg-bg"
                  >
                    <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-bg" style={{ color: BRAND }}>
                      {c.relation === 'Origen' ? <CornerUpLeft className="h-4 w-4" /> : <CornerDownRight className="h-4 w-4" />}
                    </span>
                    <div className="min-w-0 flex-1">
                      <p className="text-[12px] text-ink2">
                        {c.relation === 'Origen' ? 'Respalda la entrega' : 'Cerrada por'}{c.assetCode ? ` · ${c.assetCode}` : ''}
                      </p>
                      <p className="flex flex-wrap items-center gap-x-2 text-sm font-medium text-ink">
                        <span className="font-mono">{c.ticketNumber}</span>
                        <span className="text-[13px] font-normal text-ink2">· {c.ticketTypeName}</span>
                      </p>
                    </div>
                    <Chip variant={ticketStatusChipVariant(c.status)} label={c.statusName} />
                    <FileText className="hidden h-4 w-4 shrink-0 text-ink2 sm:block" />
                  </Link>
                ))}
              </div>
            </section>
          )}

          <section className="rounded-[10px] border border-line bg-paper p-5 shadow-sh1">
            <h2 className="mb-3 text-sm font-semibold text-ink">Activos incluidos</h2>
            <table className="w-full text-sm">
              <thead><tr className="border-b border-line text-left text-[11px] uppercase tracking-wider text-ink2">
                <th className="py-2 font-medium">Código</th><th className="py-2 font-medium">Descripción</th><th className="py-2 font-medium">Serie</th><th className="py-2 font-medium">Condición</th>
              </tr></thead>
              <tbody>
                {t.lines.map((l, i) => (
                  <tr key={i} className="border-b border-line last:border-0">
                    <td className="py-2">{l.assetId ? <Link to={`/ti/assets/${l.assetId}`} className="font-mono text-ink hover:underline">{l.internalCode}</Link> : <span className="font-mono">{l.internalCode ?? '—'}</span>}</td>
                    <td className="py-2 text-ink2">{l.description ?? '—'}</td>
                    <td className="py-2 font-mono text-[12px] text-ink2">{l.serialNumber ?? '—'}</td>
                    <td className="py-2 text-ink2">{l.condition ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </section>

          {/* Acciones por activo — mismas que en el inventario, según el estado actual de cada equipo. */}
          {assets.length > 0 && (
            <div className="space-y-3.5">
              {assets.length > 1 && (
                <h2 className="flex items-center gap-2 px-1 pt-1 text-sm font-semibold text-ink">
                  <Wrench className="h-4 w-4" style={{ color: BRAND }} /> Acciones por activo
                </h2>
              )}
              {assets.map(asset => (
                <AssetMovements
                  key={asset.id}
                  asset={asset}
                  showAssetCode
                  readOnly={t.status === 'Anulada'}
                  onChanged={() => qc.invalidateQueries({ queryKey: qk.ti.ticket(id!) })}
                />
              ))}
            </div>
          )}

          {t.photos.length > 0 && (
            <section className="rounded-[10px] border border-line bg-paper p-5 shadow-sh1">
              <h2 className="mb-3 text-sm font-semibold text-ink">Evidencia fotográfica</h2>
              <div className="grid grid-cols-3 gap-2">
                {t.photos.map(p => <img key={p.id} src={p.imageBase64} alt="Evidencia" className="aspect-square w-full rounded-lg border border-line object-cover" />)}
              </div>
            </section>
          )}

          {t.signatures.length > 0 && (
            <section className="rounded-[10px] border border-line bg-paper p-5 shadow-sh1">
              <h2 className="mb-3 flex items-center gap-2 text-sm font-semibold text-ink"><ShieldCheck className="h-4 w-4" /> Firmas</h2>
              <div className="grid gap-3 sm:grid-cols-2">
                {t.signatures.map((s, i) => (
                  <div key={i} className="rounded-lg border border-line p-3">
                    <img src={s.imageBase64} alt={s.signerType} className="h-24 w-full rounded bg-white object-contain" />
                    <p className="mt-1 text-[13px] font-medium text-ink">{s.signerType === 'ResponsableTI' ? 'Responsable TI' : 'Colaborador'}</p>
                    <p className="text-[12px] text-ink2">{s.signerName ?? '—'}</p>
                    <p className="font-mono text-[11px] text-ink2">{format(parseISO(s.signedAt), 'd MMM yyyy · HH:mm', { locale: es })}</p>
                  </div>
                ))}
              </div>
              <p className="mt-2 text-[10.5px] text-ink2">Firma electrónica de evidencia, no certificada legalmente (Ley 8454 CR).</p>
            </section>
          )}
        </div>
      </div>
    </div>
  )
}

function Field({ label, value }: { label: string; value?: string | null }) {
  return (
    <div className="flex justify-between gap-4 border-b border-line py-2">
      <span className="text-[13px] text-ink2">{label}</span>
      <span className="text-[13px] font-medium text-ink text-right">{value ?? '—'}</span>
    </div>
  )
}
