import { useParams, useNavigate, Link } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { ArrowLeft, Loader2, FileText, Download, Ban, ShieldCheck } from 'lucide-react'
import toast from 'react-hot-toast'
import { format, parseISO } from 'date-fns'
import { es } from 'date-fns/locale'

import { itTicketsApi } from '@/api/itTickets'
import { qk, staleTimes, invalidate } from '@/lib/queryKeys'
import { useAuthStore } from '@/store/authStore'
import { extractApiError } from '@/utils'
import Chip from '@/components/ui/Chip'
import { ticketStatusChipVariant } from '@/components/ti/itStatus'

const BRAND = '#0E6B4B'

export default function ItTicketDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const qc = useQueryClient()
  const { hasPermission } = useAuthStore()

  const { data: t, isLoading } = useQuery({
    queryKey: qk.ti.ticket(id ?? ''),
    queryFn: () => itTicketsApi.getById(id!).then(r => r.data.data!),
    enabled: !!id,
    staleTime: staleTimes.ti,
  })

  const pdfMut = useMutation({
    mutationFn: () => itTicketsApi.getPdf(id!).then(r => r.data as Blob),
    onSuccess: (blob) => {
      const url = URL.createObjectURL(blob)
      window.open(url, '_blank')
      setTimeout(() => URL.revokeObjectURL(url), 60_000)
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

  function handleVoid() {
    const reason = window.prompt('Motivo de anulación (obligatorio):')?.trim()
    if (!reason) return
    voidMut.mutate(reason)
  }

  if (isLoading) return <div className="flex h-full items-center justify-center"><Loader2 className="h-6 w-6 animate-spin text-ink2" /></div>
  if (!t) return <div className="p-6 text-ink2">Boleta no encontrada.</div>

  return (
    <div className="flex min-h-full flex-col">
      <header className="sticky top-0 z-10 flex flex-wrap items-center gap-3 border-b border-line bg-paper px-4 sm:px-6 py-3" style={{ minHeight: 64 }}>
        <button onClick={() => navigate(-1)} className="rounded p-1.5 text-ink2 hover:bg-bg hover:text-ink" aria-label="Volver"><ArrowLeft className="h-5 w-5" /></button>
        <div className="min-w-0 flex-1">
          <p className="font-mono text-[12px] text-ink2 mb-0.5 leading-none">TI / Boletas / {t.ticketTypeName}</p>
          <h1 className="text-[18px] font-semibold text-ink leading-tight tracking-tight flex items-center gap-2">
            <FileText className="h-4.5 w-4.5" style={{ color: BRAND }} /> {t.ticketNumber}
            <Chip variant={ticketStatusChipVariant(t.status)} label={t.statusName} />
          </h1>
        </div>
        <div className="flex items-center gap-2 shrink-0">
          {t.hasPdf && (
            <button onClick={() => pdfMut.mutate()} disabled={pdfMut.isPending}
              className="inline-flex items-center gap-1.5 rounded-[8px] px-3 py-2 text-sm font-medium text-white hover:opacity-90 disabled:opacity-50" style={{ background: BRAND }}>
              {pdfMut.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Download className="h-4 w-4" />} PDF
            </button>
          )}
          {t.status !== 'Anulada' && hasPermission('Ti.Ticket.Void') && (
            <button onClick={handleVoid} disabled={voidMut.isPending}
              className="inline-flex items-center gap-1.5 rounded-[8px] border border-red-200 bg-red-50 px-3 py-2 text-sm font-medium text-red-700 hover:bg-red-100 disabled:opacity-50">
              <Ban className="h-4 w-4" /> Anular
            </button>
          )}
        </div>
      </header>

      <div className="flex-1 p-4 sm:p-6 bg-bg">
        <div className="mx-auto max-w-3xl space-y-3.5">
          {t.status === 'Anulada' && (
            <div className="rounded-[10px] border p-4" style={{ background: '#FEF2F2', borderColor: '#FECACA' }}>
              <p className="text-sm font-medium" style={{ color: '#991B1B' }}>Boleta anulada{t.voidedAt ? ` el ${format(parseISO(t.voidedAt), 'd MMM yyyy', { locale: es })}` : ''}</p>
              {t.voidReason && <p className="text-[13px]" style={{ color: '#B91C1C' }}>Motivo: {t.voidReason}</p>}
            </div>
          )}

          <section className="rounded-[10px] border border-line bg-paper p-5 shadow-sh1">
            <div className="grid gap-x-8 sm:grid-cols-2">
              <Field label="Tipo" value={t.ticketTypeName} />
              <Field label="Emitida" value={format(parseISO(t.issuedAt), "d MMM yyyy · HH:mm", { locale: es })} />
              <Field label="Colaborador" value={t.employeeName} />
              <Field label="Responsable TI" value={t.itResponsibleName} />
            </div>
            {t.notes && <p className="mt-3 rounded-lg bg-bg p-3 text-[13px] text-ink2">{t.notes}</p>}
            {t.pdfSha256 && <p className="mt-2 break-all font-mono text-[10.5px] text-ink2">SHA-256: {t.pdfSha256}</p>}
          </section>

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
