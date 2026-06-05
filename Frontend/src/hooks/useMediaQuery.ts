import { useEffect, useState } from 'react'

/**
 * Devuelve true cuando la media query coincide. Reactivo a cambios de tamaño/orientación.
 * Ej: const isMobile = useMediaQuery('(max-width: 1023px)')
 */
export function useMediaQuery(query: string): boolean {
  const [matches, setMatches] = useState(() =>
    typeof window !== 'undefined' ? window.matchMedia(query).matches : false,
  )

  useEffect(() => {
    const mql = window.matchMedia(query)
    const onChange = () => setMatches(mql.matches)
    onChange()
    mql.addEventListener('change', onChange)
    return () => mql.removeEventListener('change', onChange)
  }, [query])

  return matches
}

/** Atajo: < lg (1024px) → móvil/tablet vertical. */
export function useIsMobile(): boolean {
  return useMediaQuery('(max-width: 1023px)')
}
