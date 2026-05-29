import { useRef, useState } from 'react'
import { Camera, X, ImagePlus } from 'lucide-react'

interface PhotoCaptureProps {
  value: string[]            // data URLs
  onChange: (photos: string[]) => void
  max?: number               // por defecto 3 (propuesta §7)
  disabled?: boolean
  label?: string
  hint?: string
}

const MAX_DIM = 1280
const QUALITY = 0.7

/**
 * Captura de fotos para evidencia: usa la cámara en móvil (capture="environment")
 * o selección de archivo en escritorio. Recomprime en el cliente a JPEG (~1280px)
 * antes de subir, para no inflar la red ni la BD. Devuelve data URLs base64.
 */
export default function PhotoCapture({
  value, onChange, max = 3, disabled = false,
  label = 'Fotos', hint = 'Hasta 3 fotos: frontal, número de serie y estado físico.',
}: PhotoCaptureProps) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [busy, setBusy] = useState(false)

  async function compress(file: File): Promise<string> {
    const dataUrl = await new Promise<string>((resolve, reject) => {
      const reader = new FileReader()
      reader.onload = () => resolve(reader.result as string)
      reader.onerror = reject
      reader.readAsDataURL(file)
    })
    const img = await new Promise<HTMLImageElement>((resolve, reject) => {
      const i = new Image()
      i.onload = () => resolve(i)
      i.onerror = reject
      i.src = dataUrl
    })
    const scale = Math.min(1, MAX_DIM / Math.max(img.width, img.height))
    const w = Math.round(img.width * scale)
    const h = Math.round(img.height * scale)
    const canvas = document.createElement('canvas')
    canvas.width = w
    canvas.height = h
    const ctx = canvas.getContext('2d')
    if (!ctx) return dataUrl
    ctx.drawImage(img, 0, 0, w, h)
    return canvas.toDataURL('image/jpeg', QUALITY)
  }

  async function handleFiles(files: FileList | null) {
    if (!files?.length) return
    setBusy(true)
    try {
      const room = max - value.length
      const picked = Array.from(files).slice(0, room)
      const compressed = await Promise.all(picked.map(compress))
      onChange([...value, ...compressed])
    } finally {
      setBusy(false)
      if (inputRef.current) inputRef.current.value = ''
    }
  }

  function remove(idx: number) {
    onChange(value.filter((_, i) => i !== idx))
  }

  const full = value.length >= max

  return (
    <div>
      <div className="mb-2 flex items-center justify-between">
        <span className="text-[12px] font-medium text-ink2">{label}</span>
        <span className="font-mono text-[11px] text-ink2">{value.length}/{max}</span>
      </div>

      <div className="grid grid-cols-3 gap-2">
        {value.map((src, i) => (
          <div key={i} className="group relative aspect-square overflow-hidden rounded-lg border border-line bg-white">
            <img src={src} alt={`Foto ${i + 1}`} className="h-full w-full object-cover" />
            {!disabled && (
              <button
                type="button"
                onClick={() => remove(i)}
                className="absolute right-1 top-1 flex h-6 w-6 items-center justify-center rounded-full bg-black/55 text-white transition-opacity hover:bg-black/75"
                aria-label={`Quitar foto ${i + 1}`}
              >
                <X className="h-3.5 w-3.5" />
              </button>
            )}
          </div>
        ))}

        {!full && !disabled && (
          <button
            type="button"
            onClick={() => inputRef.current?.click()}
            disabled={busy}
            className="flex aspect-square flex-col items-center justify-center gap-1 rounded-lg border border-dashed border-line bg-bg text-ink2 transition-colors hover:border-brand-400 hover:text-ink disabled:opacity-50"
          >
            {busy ? <ImagePlus className="h-5 w-5 animate-pulse" /> : <Camera className="h-5 w-5" />}
            <span className="text-[10.5px] font-medium">{busy ? 'Procesando…' : 'Agregar'}</span>
          </button>
        )}
      </div>

      <input
        ref={inputRef}
        type="file"
        accept="image/*"
        capture="environment"
        multiple
        className="hidden"
        onChange={(e) => handleFiles(e.target.files)}
      />

      <p className="mt-2 text-[10.5px] leading-snug text-ink2">{hint}</p>
    </div>
  )
}
