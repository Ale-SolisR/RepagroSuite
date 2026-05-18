import repagroLogo from '@/assets/repagro-logo.png'

export default function BrandPanel() {
  return (
    <div
      className="hidden lg:flex flex-col justify-between p-12 relative overflow-hidden"
      style={{ background: 'linear-gradient(135deg, #003E2D 0%, #005947 55%, #006F55 100%)' }}
      aria-hidden="true"
    >
      <div className="absolute top-0 right-0 w-[340px] h-[340px] pointer-events-none">
        <div
          className="absolute rounded-full"
          style={{
            top: -80, right: -80, width: 280, height: 280,
            background: 'radial-gradient(circle, rgba(245,197,24,.28), transparent 65%)',
            filter: 'blur(20px)',
          }}
        />
        <svg
          viewBox="0 0 340 340"
          className="absolute inset-0 w-full h-full"
          xmlns="http://www.w3.org/2000/svg"
        >
          <defs>
            <linearGradient id="goldShine" x1="0%" y1="0%" x2="100%" y2="0%">
              <stop offset="0%"   stopColor="#FFEFA8" stopOpacity="0" />
              <stop offset="30%"  stopColor="#FFD84D" stopOpacity="1" />
              <stop offset="55%"  stopColor="#FFF6D6" stopOpacity="1" />
              <stop offset="80%"  stopColor="#F5C518" stopOpacity="1" />
              <stop offset="100%" stopColor="#D9A800" stopOpacity="1" />
            </linearGradient>
            <linearGradient id="goldShineThin" x1="0%" y1="0%" x2="100%" y2="0%">
              <stop offset="0%"   stopColor="#FFEFA8" stopOpacity="0" />
              <stop offset="100%" stopColor="#F5C518" stopOpacity="1" />
            </linearGradient>
          </defs>
          <rect x="52"  y="180" width="288" height="14" rx="2" fill="url(#goldShine)" />
          <rect x="92"  y="202" width="248" height="6"  rx="1" fill="url(#goldShineThin)" />
          <rect x="172" y="214" width="168" height="3"  rx="1" fill="url(#goldShineThin)" opacity="0.85" />
          <rect x="252" y="222" width="88"  height="2"  rx="1" fill="#FFD84D" opacity="0.7" />
          <rect x="320" y="120" width="3"   height="118" rx="1" fill="#F5C518" opacity="0.85" />
          <rect x="280" y="140" width="2.5" height="60"  rx="1" fill="#FFD84D" opacity="0.55"
            transform="rotate(28 280 140)" />
          <circle cx="290" cy="155" r="3"   fill="#F5C518" />
          <circle cx="308" cy="168" r="2"   fill="#FFD84D" />
          <circle cx="275" cy="172" r="1.5" fill="#FFEFA8" />
        </svg>
        <div
          className="absolute rounded-full"
          style={{
            top: 110, right: 30,
            width: 160, height: 160,
            border: '1.5px solid rgba(245,197,24,.35)',
          }}
        />
        <div
          className="absolute rounded-full"
          style={{
            top: 136, right: 56,
            width: 108, height: 108,
            border: '1px solid rgba(255,239,168,.20)',
          }}
        />
      </div>

      <div className="relative z-10 flex items-center">
        <div className="flex items-center justify-center rounded-xl bg-white px-5 py-3 shrink-0">
          <img src={repagroLogo} alt="Repagro" className="h-16 w-auto" />
        </div>
      </div>

      <div className="relative z-10">
        <p
          className="font-mono text-[13px] tracking-[.14em] uppercase mb-5"
          style={{ color: '#FFD84D' }}
        >
          Repagro Suite
        </p>
        <h1
          className="text-[40px] font-medium leading-[1.18] text-white mb-4"
          style={{ maxWidth: 380 }}
        >
          Una plataforma<br />para tu trabajo<br />diario.
        </h1>
        <p className="text-[15px]" style={{ color: 'rgba(255,255,255,.70)', maxWidth: 380 }}>
          Salas, inventario, activos TI, boletas y más.<br />Todo en un solo lugar.
        </p>
      </div>

      <div className="relative z-10 flex items-center justify-between">
        <span className="font-mono text-[12px]" style={{ color: 'rgba(255,255,255,.5)' }}>
          © 2026 Repagro · v6.1
        </span>
        <div className="flex items-center gap-1.5">
          <div
            className="h-px w-14"
            style={{ background: 'linear-gradient(to right, transparent, #F5C518)' }}
          />
          <div
            className="h-2 w-2 rounded-full shrink-0"
            style={{ background: '#F5C518', boxShadow: '0 0 8px 3px rgba(245,197,24,.55)' }}
          />
          <div
            className="h-px w-14"
            style={{ background: 'linear-gradient(to left, transparent, #F5C518)' }}
          />
        </div>
      </div>
    </div>
  )
}
