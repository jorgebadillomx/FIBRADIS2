/**
 * Identificador del video de YouTube que explica la sección privada de portafolio.
 * Centralizado aquí para que un solo cambio actualice todas las superficies.
 */
export const PORTAFOLIO_VIDEO_ID = '_nArOCSpPz4'

/**
 * Construye la URL de embed (modo privacidad, sin contenido relacionado de otros canales).
 * Usa `youtube-nocookie.com` para no fijar cookies de seguimiento hasta que el usuario
 * reproduzca el video.
 */
export function youtubeEmbedUrl(videoId: string): string {
  return `https://www.youtube-nocookie.com/embed/${encodeURIComponent(videoId.trim())}?rel=0`
}
