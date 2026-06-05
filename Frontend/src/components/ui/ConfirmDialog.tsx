import {
  createContext, useCallback, useContext, useEffect, useRef, useState,
  type ReactNode,
} from 'react'
import { AlertTriangle, HelpCircle, Loader2 } from 'lucide-react'

/**
 * Sistema de diálogos imperativo basado en promesas para reemplazar los
 * `window.confirm` / `window.prompt` nativos (feos, no temáticos y bloqueantes).
 *
 * Uso:
 *   const confirm = useConfirm()
 *   if (await confirm({ title, message, variant: 'danger' })) { ... }       // -> boolean
 *   const motivo = await confirm({ title, input: { required: true } })       // -> string | null
 *
 * Características: bottom-sheet en móvil / tarjeta centrada en escritorio, foco automático,
 * ESC y clic fuera para cancelar, objetivos táctiles ≥44px, inputs a 16px en móvil.
 */

type Variant = 'danger' | 'primary'

interface InputSpec {
  label?: string
  placeholder?: string
  required?: boolean
  multiline?: boolean
  initialValue?: string
}

interface BaseOptions {
  title: string
  message?: ReactNode
  confirmText?: string
  cancelText?: string
  variant?: Variant
}

export interface ConfirmOptions extends BaseOptions { input?: undefined }
export interface PromptOptions extends BaseOptions { input: InputSpec }

interface ConfirmFn {
  (options: ConfirmOptions): Promise<boolean>
  (options: PromptOptions): Promise<string | null>
}

type AnyOptions = BaseOptions & { input?: InputSpec }
type Resolver = (result: boolean | string | null) => void

const ConfirmContext = createContext<ConfirmFn | null>(null)

const ACCENT: Record<Variant, { ring: string; tint: string; icon: typeof AlertTriangle; btn: string }> = {
  danger: { ring: '#BE123C', tint: '#FFF1F2', icon: AlertTriangle, btn: 'bg-danger hover:bg-red-700' },
  primary: { ring: '#0E6B4B', tint: '#ECFDF5', icon: HelpCircle, btn: 'bg-brand-600 hover:bg-brand-700' },
}

export function ConfirmDialogProvider({ children }: { children: ReactNode }) {
  const [opts, setOpts] = useState<AnyOptions | null>(null)
  const [value, setValue] = useState('')
  const resolverRef = useRef<Resolver | null>(null)
  const inputRef = useRef<HTMLTextAreaElement & HTMLInputElement>(null)
  const confirmBtnRef = useRef<HTMLButtonElement>(null)

  const confirm = useCallback(((options: AnyOptions) => {
    setOpts(options)
    setValue(options.input?.initialValue ?? '')
    return new Promise<boolean | string | null>((resolve) => { resolverRef.current = resolve })
  }) as ConfirmFn, [])

  const settle = useCallback((result: boolean | string | null) => {
    resolverRef.current?.(result)
    resolverRef.current = null
    setOpts(null)
    setValue('')
  }, [])

  const onCancel = useCallback(() => settle(opts?.input ? null : false), [settle, opts])
  const onConfirm = useCallback(() => settle(opts?.input ? value.trim() : true), [settle, opts, value])

  // Foco automático y bloqueo de scroll del fondo mientras el diálogo está abierto.
  useEffect(() => {
    if (!opts) return
    document.body.style.overflow = 'hidden'
    const t = setTimeout(() => {
      if (opts.input) inputRef.current?.focus()
      else confirmBtnRef.current?.focus()
    }, 50)
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onCancel()
      // Enter envía en inputs de una línea; en textarea se respeta el salto de línea.
      if (e.key === 'Enter' && (!opts.input || !opts.input.multiline) && canConfirm()) {
        e.preventDefault(); onConfirm()
      }
    }
    window.addEventListener('keydown', onKey)
    return () => { document.body.style.overflow = ''; clearTimeout(t); window.removeEventListener('keydown', onKey) }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [opts, value])

  function canConfirm() {
    if (opts?.input?.required) return value.trim().length > 0
    return true
  }

  return (
    <ConfirmContext.Provider value={confirm}>
      {children}
      {opts && (() => {
        const variant = opts.variant ?? 'primary'
        const accent = ACCENT[variant]
        const Icon = accent.icon
        const ready = canConfirm()
        return (
          <div
            className="fixed inset-0 z-[1100] flex items-end justify-center bg-black/55 sm:items-center sm:p-4"
            onMouseDown={(e) => { if (e.target === e.currentTarget) onCancel() }}
            role="dialog" aria-modal="true" aria-label={opts.title}
          >
            <div className="w-full rounded-t-2xl bg-paper p-5 shadow-2xl sm:max-w-md sm:rounded-2xl">
              <div className="mb-3 flex items-start gap-3">
                <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full"
                  style={{ background: accent.tint, color: accent.ring }}>
                  <Icon className="h-5 w-5" />
                </span>
                <div className="min-w-0 flex-1 pt-0.5">
                  <h2 className="text-[15px] font-semibold leading-tight text-ink">{opts.title}</h2>
                  {opts.message && (
                    <div className="mt-1 text-[13px] leading-relaxed text-ink2">{opts.message}</div>
                  )}
                </div>
              </div>

              {opts.input && (
                <div className="mb-4">
                  {opts.input.label && (
                    <label className="mb-1 block text-[12px] font-medium text-ink2">
                      {opts.input.label}{opts.input.required && ' *'}
                    </label>
                  )}
                  {opts.input.multiline ? (
                    <textarea
                      ref={inputRef}
                      className="min-h-[88px] w-full rounded-[8px] border border-line bg-paper px-3 py-2 text-[16px] text-ink focus:border-brand-400 focus:outline-none sm:text-sm"
                      value={value} onChange={e => setValue(e.target.value)}
                      placeholder={opts.input.placeholder}
                    />
                  ) : (
                    <input
                      ref={inputRef}
                      className="h-11 w-full rounded-[8px] border border-line bg-paper px-3 text-[16px] text-ink focus:border-brand-400 focus:outline-none sm:text-sm"
                      value={value} onChange={e => setValue(e.target.value)}
                      placeholder={opts.input.placeholder}
                    />
                  )}
                </div>
              )}

              <div className="flex flex-col-reverse gap-2.5 sm:flex-row sm:justify-end">
                <button type="button" onClick={onCancel}
                  className="min-h-[44px] rounded-[8px] border border-line bg-paper px-4 text-sm font-medium text-ink transition-colors hover:bg-bg touch-manipulation">
                  {opts.cancelText ?? 'Cancelar'}
                </button>
                <button ref={confirmBtnRef} type="button" onClick={onConfirm} disabled={!ready}
                  className={`inline-flex min-h-[44px] items-center justify-center gap-2 rounded-[8px] px-4 text-sm font-semibold text-white transition-colors touch-manipulation disabled:opacity-50 ${accent.btn}`}>
                  {opts.confirmText ?? 'Confirmar'}
                </button>
              </div>
            </div>
          </div>
        )
      })()}
    </ConfirmContext.Provider>
  )
}

// Loader exportado por conveniencia para callers que muestren estado tras confirmar.
export { Loader2 as ConfirmLoader }

export function useConfirm(): ConfirmFn {
  const ctx = useContext(ConfirmContext)
  if (!ctx) throw new Error('useConfirm debe usarse dentro de <ConfirmDialogProvider>')
  return ctx
}
