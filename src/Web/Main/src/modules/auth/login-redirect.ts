export const DEFAULT_LOGIN_REDIRECT = '/portafolio'

export function resolveLoginRedirect(rawRedirect: string | null | undefined): string {
  const redirect = rawRedirect?.trim()
  if (!redirect) return DEFAULT_LOGIN_REDIRECT

  // Rechaza cualquier carácter de control (CR/LF/TAB, etc.) que podría alterar el destino.
  const hasControlChar = [...redirect].some((ch) => ch.charCodeAt(0) < 0x20)

  // Solo rutas internas absolutas. Rechaza destinos externos / open redirect:
  // - que no empiezan con '/'
  // - protocol-relative '//host' y la variante con backslash '/\host'
  //   (algunos navegadores normalizan '\' -> '/', volviéndola protocol-relative)
  if (
    !redirect.startsWith('/') ||
    redirect[1] === '/' ||
    redirect[1] === '\\' ||
    hasControlChar
  ) {
    return DEFAULT_LOGIN_REDIRECT
  }

  return redirect
}
