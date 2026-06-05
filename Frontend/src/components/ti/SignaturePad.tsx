import { useRef, useState, useEffect, useCallback } from 'react'
import { Eraser, Check, RotateCcw, Maximize2, X, RotateCw, PenLine } from 'lucide-react'

interface SignaturePadProps {
  /** Firma confirmada como data URL PNG. */
  value?: string | null
  onConfirm: (dataUrl: string) => void
  onClear?: () => void
  label?: string
  /** Aviso legal mostrado bajo el lienzo. */
  legalNotice?: string
  disabled?: boolean
}

const BRAND = '#0E6B4B'

/**
 * Firma en pantalla sobre <canvas> usando Pointer Events: un solo código cubre
 * mouse, touchpad, dedo (celular/tablet) y pantalla táctil. Exporta PNG base64.
 *
 * En móvil/tablet ofrece un modo a PANTALLA COMPLETA pensado para firmar con el
 * teléfono en horizontal: el lienzo es enorme, la barra superior con el rótulo
 * marca claramente el lado "arriba" y hay una línea de firma "✗ ____" abajo,
 * para que la firma nunca quede de cabeza.
 *
 * IMPORTANTE: es una firma electrónica de evidencia, NO una firma digital
 * certificada legalmente (Ley 8454 CR).
 */
export default function SignaturePad({
  value, onConfirm, onClear, label = 'Firma', legalNotice, disabled = false,
}: SignaturePadProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const drawing = useRef(false)
  const last = useRef<{ x: number; y: number } | null>(null)
  const [hasInk, setHasInk] = useState(false)
  const [confirmed, setConfirmed] = useState(!!value)
  const [fullscreen, setFullscreen] = useState(false)

  // ─── Lienzo en línea (escritorio) ───────────────────────────────────────────
  const setupCanvas = useCallback((canvas: HTMLCanvasElement | null) => {
    if (!canvas) return
    const ratio = window.devicePixelRatio || 1
    const rect = canvas.getBoundingClientRect()
    if (rect.width === 0) return
    canvas.width = rect.width * ratio
    canvas.height = rect.height * ratio
    const ctx = canvas.getContext('2d')
    if (!ctx) return
    ctx.scale(ratio, ratio)
    ctx.lineCap = 'round'
    ctx.lineJoin = 'round'
    ctx.lineWidth = 2.4
    ctx.strokeStyle = '#13211C'
  }, [])

  useEffect(() => {
    setupCanvas(canvasRef.current)
    const onResize = () => setupCanvas(canvasRef.current)
    window.addEventListener('resize', onResize)
    return () => window.removeEventListener('resize', onResize)
  }, [setupCanvas])

  function pos(e: React.PointerEvent<HTMLCanvasElement>) {
    const rect = e.currentTarget.getBoundingClientRect()
    return { x: e.clientX - rect.left, y: e.clientY - rect.top }
  }
  function start(e: React.PointerEvent<HTMLCanvasElement>) {
    if (disabled || confirmed) return
    e.currentTarget.setPointerCapture(e.pointerId)
    drawing.current = true
    last.current = pos(e)
  }
  function move(e: React.PointerEvent<HTMLCanvasElement>) {
    if (!drawing.current || disabled || confirmed) return
    e.preventDefault()
    const ctx = canvasRef.current?.getContext('2d')
    if (!ctx || !last.current) return
    const p = pos(e)
    ctx.beginPath(); ctx.moveTo(last.current.x, last.current.y); ctx.lineTo(p.x, p.y); ctx.stroke()
    last.current = p
    if (!hasInk) setHasInk(true)
  }
  function end() { drawing.current = false; last.current = null }

  function clear() {
    const canvas = canvasRef.current
    const ctx = canvas?.getContext('2d')
    if (canvas && ctx) ctx.clearRect(0, 0, canvas.width, canvas.height)
    setHasInk(false); setConfirmed(false); onClear?.()
  }
  function confirm() {
    const canvas = canvasRef.current
    if (!canvas || !hasInk) return
    onConfirm(canvas.toDataURL('image/png')); setConfirmed(true)
  }

  return (
    <div className="rounded-[10px] border border-line bg-paper p-3">
      <div className="mb-2 flex items-center justify-between gap-2">
        <span className="text-[12px] font-medium text-ink2">{label}</span>
        <div className="flex items-center gap-2">
          {confirmed && (
            <span className="inline-flex items-center gap-1 text-[11px] font-medium text-emerald-700">
              <Check className="h-3 w-3" /> Confirmada
            </span>
          )}
          {!disabled && (
            <button
              type="button"
              onClick={() => setFullscreen(true)}
              className="inline-flex items-center gap-1 rounded-md border border-line px-2 py-1 text-[11px] font-medium text-ink2 hover:bg-bg hover:text-ink"
              title="Firmar en pantalla completa"
            >
              <Maximize2 className="h-3.5 w-3.5" /> Ampliar
            </button>
          )}
        </div>
      </div>

      {confirmed && value ? (
        <img src={value} alt={`${label} confirmada`} className="h-36 w-full rounded-lg border border-line bg-white object-contain" />
      ) : (
        <canvas
          ref={canvasRef}
          onPointerDown={start}
          onPointerMove={move}
          onPointerUp={end}
          onPointerLeave={end}
          className="h-36 w-full touch-none rounded-lg border border-dashed border-line bg-white"
          style={{ touchAction: 'none', cursor: disabled ? 'not-allowed' : 'crosshair' }}
          aria-label={`Área de ${label}`}
        />
      )}

      <div className="mt-2 flex items-center gap-2">
        <button
          type="button" onClick={clear} disabled={disabled || (!hasInk && !confirmed)}
          className="inline-flex flex-1 items-center justify-center gap-1.5 rounded-[8px] border border-line px-3 py-2 text-[13px] font-medium text-ink transition-colors hover:bg-bg disabled:opacity-40"
        >
          {confirmed ? <RotateCcw className="h-4 w-4" /> : <Eraser className="h-4 w-4" />}
          {confirmed ? 'Volver a firmar' : 'Limpiar'}
        </button>
        {!confirmed && (
          <button
            type="button" onClick={confirm} disabled={disabled || !hasInk}
            className="inline-flex flex-1 items-center justify-center gap-1.5 rounded-[8px] px-3 py-2 text-[13px] font-medium text-white transition-colors hover:opacity-90 disabled:opacity-40"
            style={{ background: BRAND }}
          >
            <Check className="h-4 w-4" /> Confirmar firma
          </button>
        )}
      </div>

      <p className="mt-2 text-[10.5px] leading-snug text-ink2">
        {legalNotice ?? 'Firma electrónica de evidencia, no certificada legalmente (Ley 8454 CR). Se registra fecha/hora, IP y usuario.'}
      </p>

      {fullscreen && (
        <FullscreenSign
          label={label}
          onClose={() => setFullscreen(false)}
          onConfirm={(dataUrl) => { onConfirm(dataUrl); setConfirmed(true); setHasInk(true); setFullscreen(false) }}
        />
      )}
    </div>
  )
}

/**
 * Capa de firma a pantalla completa, optimizada para teléfono en horizontal.
 * El borde superior etiquetado deja claro cuál es el lado "arriba".
 */
function FullscreenSign({ label, onClose, onConfirm }: { label: string; onClose: () => void; onConfirm: (dataUrl: string) => void }) {
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const drawing = useRef(false)
  const last = useRef<{ x: number; y: number } | null>(null)
  const [hasInk, setHasInk] = useState(false)
  // Dimensiones reales del viewport (px). Si el teléfono está vertical, rotamos el escenario
  // 90° por CSS para que la firma SIEMPRE aparezca apaisada sin depender de la rotación del SO.
  const [vp, setVp] = useState(() => ({
    w: typeof window !== 'undefined' ? window.innerWidth : 0,
    h: typeof window !== 'undefined' ? window.innerHeight : 0,
  }))
  const rotated = vp.h > vp.w   // portrait → rotamos

  const setup = useCallback(() => {
    const canvas = canvasRef.current
    if (!canvas) return
    const ratio = window.devicePixelRatio || 1
    // offsetWidth/Height son el tamaño de maquetación (apaisado), inmunes al transform.
    const w = canvas.offsetWidth, h = canvas.offsetHeight
    if (w === 0 || h === 0) return
    canvas.width = w * ratio
    canvas.height = h * ratio
    const ctx = canvas.getContext('2d')
    if (!ctx) return
    ctx.scale(ratio, ratio)
    ctx.lineCap = 'round'
    ctx.lineJoin = 'round'
    ctx.lineWidth = 3
    ctx.strokeStyle = '#13211C'
    setHasInk(false)
  }, [])

  // Bloquea el scroll del fondo + configura el lienzo, y reconfigura al rotar/redimensionar.
  useEffect(() => {
    const prevOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    const onResize = () => setVp({ w: window.innerWidth, h: window.innerHeight })
    window.addEventListener('resize', onResize)
    window.addEventListener('orientationchange', onResize)
    const onKey = (e: KeyboardEvent) => { if (e.key === 'Escape') onClose() }
    window.addEventListener('keydown', onKey)
    return () => {
      window.removeEventListener('resize', onResize)
      window.removeEventListener('orientationchange', onResize)
      window.removeEventListener('keydown', onKey)
      document.body.style.overflow = prevOverflow
    }
  }, [onClose])

  // Reconfigura el lienzo cada vez que cambian las dimensiones/orientación (tras pintar).
  useEffect(() => {
    const t = window.setTimeout(setup, 40)
    return () => window.clearTimeout(t)
  }, [setup, vp.w, vp.h])

  // Mapea coordenadas de pantalla → coordenadas locales del lienzo apaisado.
  // Con rotación de 90° en sentido horario: localX = clientY - top ; localY = right - clientX.
  function pos(e: React.PointerEvent<HTMLCanvasElement>) {
    const rect = e.currentTarget.getBoundingClientRect()
    if (rotated) return { x: e.clientY - rect.top, y: rect.right - e.clientX }
    return { x: e.clientX - rect.left, y: e.clientY - rect.top }
  }
  function start(e: React.PointerEvent<HTMLCanvasElement>) {
    e.currentTarget.setPointerCapture(e.pointerId)
    drawing.current = true; last.current = pos(e)
  }
  function move(e: React.PointerEvent<HTMLCanvasElement>) {
    if (!drawing.current) return
    e.preventDefault()
    const ctx = canvasRef.current?.getContext('2d')
    if (!ctx || !last.current) return
    const p = pos(e)
    ctx.beginPath(); ctx.moveTo(last.current.x, last.current.y); ctx.lineTo(p.x, p.y); ctx.stroke()
    last.current = p
    if (!hasInk) setHasInk(true)
  }
  function end() { drawing.current = false; last.current = null }

  function clear() {
    const canvas = canvasRef.current
    const ctx = canvas?.getContext('2d')
    if (canvas && ctx) ctx.clearRect(0, 0, canvas.width, canvas.height)
    setHasInk(false)
  }
  function confirm() {
    const canvas = canvasRef.current
    if (!canvas || !hasInk) return
    onConfirm(canvas.toDataURL('image/png'))
  }

  // Escenario apaisado: si el teléfono está vertical lo rotamos 90° y lo dimensionamos
  // intercambiando ancho/alto, anclado en el borde derecho (origen top-left).
  const stageStyle: React.CSSProperties = rotated
    ? {
        position: 'absolute', top: 0, left: vp.w,
        width: vp.h, height: vp.w,
        transform: 'rotate(90deg)', transformOrigin: '0 0',
      }
    : { position: 'absolute', inset: 0 }

  return (
    <div className="fixed inset-0 z-[2000] overflow-hidden bg-white">
      <div className="flex flex-col" style={stageStyle}>
        {/* Barra superior = lado "ARRIBA" del documento de firma */}
        <div className="flex shrink-0 items-center gap-2 px-3 py-2.5 text-white" style={{ background: BRAND }}>
          <PenLine className="h-4 w-4 shrink-0" />
          <div className="min-w-0 flex-1">
            <p className="text-[10px] uppercase leading-none tracking-widest opacity-80">Arriba ↑</p>
            <p className="truncate text-[14px] font-semibold leading-tight">{label}</p>
          </div>
          <button
            type="button" onClick={onClose}
            className="flex h-9 w-9 items-center justify-center rounded-full bg-white/15 hover:bg-white/25"
            aria-label="Cerrar"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        {/* Lienzo grande (apaisado) */}
        <div className="relative flex-1">
          <canvas
            ref={canvasRef}
            onPointerDown={start}
            onPointerMove={move}
            onPointerUp={end}
            onPointerLeave={end}
            className="h-full w-full touch-none bg-white"
            style={{ touchAction: 'none', cursor: 'crosshair' }}
            aria-label={`Área de ${label} a pantalla completa`}
          />

          {/* Línea de firma "✗ ____" cerca del borde inferior: ancla la orientación */}
          <div className="pointer-events-none absolute inset-x-8 bottom-7 flex items-end gap-2">
            <span className="text-xl font-bold leading-none" style={{ color: BRAND }}>✗</span>
            <div className="flex-1 border-b-2 border-dashed" style={{ borderColor: '#C9CFCB' }} />
          </div>
          <p className="pointer-events-none absolute inset-x-0 bottom-1.5 text-center text-[11px] text-ink2">
            {label} — firme sobre la línea
          </p>

          {rotated && (
            <div className="pointer-events-none absolute right-3 top-3 inline-flex items-center gap-1 rounded-full bg-black/65 px-2.5 py-1 text-[11px] text-white">
              <RotateCw className="h-3.5 w-3.5" /> Gire el teléfono a horizontal
            </div>
          )}
        </div>

        {/* Acciones */}
        <div className="flex shrink-0 items-center gap-2.5 border-t border-line px-3 py-3">
          <button
            type="button" onClick={clear} disabled={!hasInk}
            className="inline-flex items-center justify-center gap-1.5 rounded-[10px] border border-line px-4 py-3 text-sm font-medium text-ink hover:bg-bg disabled:opacity-40"
          >
            <Eraser className="h-4 w-4" /> Limpiar
          </button>
          <button
            type="button" onClick={confirm} disabled={!hasInk}
            className="inline-flex flex-1 items-center justify-center gap-2 rounded-[10px] px-4 py-3 text-sm font-semibold text-white transition-opacity hover:opacity-90 disabled:opacity-40"
            style={{ background: BRAND }}
          >
            <Check className="h-4 w-4" /> Confirmar firma
          </button>
        </div>
      </div>
    </div>
  )
}
