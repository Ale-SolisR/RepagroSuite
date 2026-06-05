import repagroLogoFull from '@/assets/repagro-logo-full.png'
import { Layers, ShieldCheck, Activity } from 'lucide-react'

const BENEFITS = [
  { icon: Layers, text: 'Gestión operativa centralizada' },
  { icon: ShieldCheck, text: 'Control de acceso seguro por usuario' },
  { icon: Activity, text: 'Información disponible en tiempo real' },
]

/**
 * Panel de marca (solo escritorio ≥lg). En móvil/tablet se reemplaza por un
 * encabezado compacto dentro de la columna del formulario.
 */
export default function BrandPanel() {
  return (
    <div
      className="relative hidden lg:flex flex-col justify-between overflow-hidden p-10 xl:p-12"
      style={{ background: 'linear-gradient(150deg, #073D31 0%, #0A5037 55%, #0E6B4B 100%)' }}
    >
      {/* ── Decoración dorada (sutil) ── */}
      <div className="absolute top-0 right-0 h-[340px] w-[340px] pointer-events-none" aria-hidden="true">
        <div
          className="absolute rounded-full"
          style={{
            top: -80, right: -80, width: 280, height: 280,
            background: 'radial-gradient(circle, rgba(201,178,107,.28), transparent 65%)',
            filter: 'blur(20px)',
          }}
        />
        <svg viewBox="0 0 340 340" className="absolute inset-0 h-full w-full" xmlns="http://www.w3.org/2000/svg">
          <defs>
            <linearGradient id="goldShine" x1="0%" y1="0%" x2="100%" y2="0%">
              <stop offset="0%" stopColor="#EFE5C2" stopOpacity="0" />
              <stop offset="30%" stopColor="#D9C58A" stopOpacity="1" />
              <stop offset="55%" stopColor="#EFE5C2" stopOpacity="1" />
              <stop offset="80%" stopColor="#C9B26B" stopOpacity="1" />
              <stop offset="100%" stopColor="#A89248" stopOpacity="1" />
            </linearGradient>
            <linearGradient id="goldShineThin" x1="0%" y1="0%" x2="100%" y2="0%">
              <stop offset="0%" stopColor="#EFE5C2" stopOpacity="0" />
              <stop offset="100%" stopColor="#C9B26B" stopOpacity="1" />
            </linearGradient>
          </defs>
          <rect x="52" y="180" width="288" height="14" rx="2" fill="url(#goldShine)" />
          <rect x="92" y="202" width="248" height="6" rx="1" fill="url(#goldShineThin)" />
          <rect x="172" y="214" width="168" height="3" rx="1" fill="url(#goldShineThin)" opacity="0.85" />
          <rect x="252" y="222" width="88" height="2" rx="1" fill="#D9C58A" opacity="0.7" />
          <rect x="320" y="120" width="3" height="118" rx="1" fill="#C9B26B" opacity="0.85" />
          <rect x="280" y="140" width="2.5" height="60" rx="1" fill="#D9C58A" opacity="0.55" transform="rotate(28 280 140)" />
          <circle cx="290" cy="155" r="3" fill="#C9B26B" />
          <circle cx="308" cy="168" r="2" fill="#D9C58A" />
          <circle cx="275" cy="172" r="1.5" fill="#EFE5C2" />
        </svg>
        <div className="absolute rounded-full" style={{ top: 110, right: 30, width: 160, height: 160, border: '1.5px solid rgba(201,178,107,.35)' }} />
        <div className="absolute rounded-full" style={{ top: 136, right: 56, width: 108, height: 108, border: '1px solid rgba(239,229,194,.20)' }} />
      </div>

      {/* ── Logo ── */}
      <div className="relative z-10 flex items-center">
        <img src={repagroLogoFull} alt="Repagro App" className="h-16 xl:h-20 w-auto max-w-full" />
      </div>

      {/* ── Mensaje principal + beneficios ── */}
      <div className="relative z-10 max-w-md">
        <p className="font-mono text-[12px] tracking-[.16em] uppercase mb-4" style={{ color: '#D9C58A' }}>
          Repagro App
        </p>
        <h1 className="text-[34px] xl:text-[40px] font-medium leading-[1.15] text-white mb-4">
          Una plataforma corporativa para centralizar tus operaciones.
        </h1>
        <p className="text-[15px] leading-relaxed mb-8" style={{ color: 'rgba(255,255,255,.72)' }}>
          Salas, activos TI, boletas, inventario y trazabilidad — todo en un solo lugar, seguro y en tiempo real.
        </p>

        <ul className="space-y-3">
          {BENEFITS.map(({ icon: Icon, text }) => (
            <li key={text} className="flex items-center gap-3">
              <span
                className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg ring-1"
                style={{ background: 'rgba(201,178,107,.14)', ['--tw-ring-color' as string]: 'rgba(201,178,107,.35)' }}
              >
                <Icon className="h-4 w-4" style={{ color: '#E6D9A8' }} strokeWidth={1.9} />
              </span>
              <span className="text-[14px] text-white/85">{text}</span>
            </li>
          ))}
        </ul>
      </div>

      {/* ── Footer: versión + sello de seguridad ── */}
      <div className="relative z-10 flex items-center justify-between gap-4">
        <span className="font-mono text-[12px]" style={{ color: 'rgba(255,255,255,.5)' }}>
          © 2026 Repagro · v6.1
        </span>
        <span
          className="inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-[11px] font-medium text-white/80 ring-1"
          style={{ background: 'rgba(255,255,255,.06)', ['--tw-ring-color' as string]: 'rgba(255,255,255,.14)' }}
        >
          <ShieldCheck className="h-3.5 w-3.5" style={{ color: '#D9C58A' }} />
          Acceso seguro
        </span>
      </div>
    </div>
  )
}
