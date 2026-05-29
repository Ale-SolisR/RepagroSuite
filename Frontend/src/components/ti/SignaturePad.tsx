import { useRef, useState, useEffect, useCallback } from 'react'
import { Eraser, Check, RotateCcw } from 'lucide-react'

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

/**
 * Firma en pantalla sobre <canvas> usando Pointer Events: un solo código cubre
 * mouse, touchpad, dedo (celular/tablet) y pantalla táctil. Exporta PNG base64.
 *
 * IMPORTANTE: es una firma electrónica de evidencia, NO una firma digital
 * certificada legalmente (Ley 8454 CR). Para validez legal plena se requiere
 * integrar certificado digital / proveedor autorizado.
 */
export default function SignaturePad({
  value, onConfirm, onClear, label = 'Firma', legalNotice, disabled = false,
}: SignaturePadProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const drawing = useRef(false)
  const last = useRef<{ x: number; y: number } | null>(null)
  const [hasInk, setHasInk] = useState(false)
  const [confirmed, setConfirmed] = useState(!!value)

  // Escala el lienzo al ancho real respetando devicePixelRatio (trazo nítido).
  const setupCanvas = useCallback(() => {
    const canvas = canvasRef.current
    if (!canvas) return
    const ratio = window.devicePixelRatio || 1
    const rect = canvas.getBoundingClientRect()
    canvas.width = rect.width * ratio
    canvas.height = rect.height * ratio
    const ctx = canvas.getContext('2d')
    if (!ctx) return
    ctx.scale(ratio, ratio)
    ctx.lineCap = 'round'
    ctx.lineJoin = 'round'
    ctx.lineWidth = 2.2
    ctx.strokeStyle = '#13211C'
  }, [])

  useEffect(() => {
    setupCanvas()
    const onResize = () => setupCanvas()
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
    ctx.beginPath()
    ctx.moveTo(last.current.x, last.current.y)
    ctx.lineTo(p.x, p.y)
    ctx.stroke()
    last.current = p
    if (!hasInk) setHasInk(true)
  }

  function end() {
    drawing.current = false
    last.current = null
  }

  function clear() {
    const canvas = canvasRef.current
    const ctx = canvas?.getContext('2d')
    if (canvas && ctx) ctx.clearRect(0, 0, canvas.width, canvas.height)
    setHasInk(false)
    setConfirmed(false)
    onClear?.()
  }

  function confirm() {
    const canvas = canvasRef.current
    if (!canvas || !hasInk) return
    onConfirm(canvas.toDataURL('image/png'))
    setConfirmed(true)
  }

  return (
    <div className="rounded-[10px] border border-line bg-paper p-3">
      <div className="mb-2 flex items-center justify-between">
        <span className="text-[12px] font-medium text-ink2">{label}</span>
        {confirmed && (
          <span className="inline-flex items-center gap-1 text-[11px] font-medium text-emerald-700">
            <Check className="h-3 w-3" /> Confirmada
          </span>
        )}
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
          type="button"
          onClick={clear}
          disabled={disabled || (!hasInk && !confirmed)}
          className="inline-flex flex-1 items-center justify-center gap-1.5 rounded-[8px] border border-line px-3 py-2 text-[13px] font-medium text-ink transition-colors hover:bg-bg disabled:opacity-40"
        >
          {confirmed ? <RotateCcw className="h-4 w-4" /> : <Eraser className="h-4 w-4" />}
          {confirmed ? 'Volver a firmar' : 'Limpiar'}
        </button>
        {!confirmed && (
          <button
            type="button"
            onClick={confirm}
            disabled={disabled || !hasInk}
            className="inline-flex flex-1 items-center justify-center gap-1.5 rounded-[8px] px-3 py-2 text-[13px] font-medium text-white transition-colors hover:opacity-90 disabled:opacity-40"
            style={{ background: '#0E6B4B' }}
          >
            <Check className="h-4 w-4" /> Confirmar firma
          </button>
        )}
      </div>

      <p className="mt-2 text-[10.5px] leading-snug text-ink2">
        {legalNotice ?? 'Firma electrónica de evidencia, no certificada legalmente (Ley 8454 CR). Se registra fecha/hora, IP y usuario.'}
      </p>
    </div>
  )
}
