import { useState } from 'react'
import { PlayCircle } from 'lucide-react'
import type { VariantProps } from 'class-variance-authority'
import { Button } from '@/shared/ui/button'
import type { buttonVariants } from '@/shared/ui/button-variants'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/shared/ui/dialog'
import { PORTAFOLIO_VIDEO_ID, youtubeEmbedUrl } from '@/modules/portafolio/youtube-embed'

type ButtonVariantProps = VariantProps<typeof buttonVariants>

type VideoAyudaDialogProps = {
  triggerLabel?: string
  triggerVariant?: ButtonVariantProps['variant']
  triggerSize?: ButtonVariantProps['size']
  triggerClassName?: string
  videoId?: string
  title?: string
  description?: string
}

/**
 * Botón + modal que reproduce el video guía de la sección privada de portafolio.
 * El <iframe> se monta SOLO cuando el modal está abierto, por lo que no se cargan
 * recursos ni cookies de YouTube hasta que el usuario hace clic, y la reproducción
 * se detiene al cerrar (el Dialog desmonta su contenido).
 */
export function VideoAyudaDialog({
  triggerLabel = 'Ver video: cómo usar tu portafolio',
  triggerVariant = 'outline',
  triggerSize = 'lg',
  triggerClassName,
  videoId = PORTAFOLIO_VIDEO_ID,
  title = 'Cómo usar tu portafolio',
  description = 'Recorrido rápido de la sección privada: cargar posiciones, leer KPIs y el calendario de distribuciones.',
}: VideoAyudaDialogProps) {
  const [open, setOpen] = useState(false)

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button variant={triggerVariant} size={triggerSize} className={triggerClassName}>
          <PlayCircle className="size-4" aria-hidden="true" />
          {triggerLabel}
        </Button>
      </DialogTrigger>
      <DialogContent className="max-h-[calc(100dvh-2rem)] overflow-y-auto sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription>{description}</DialogDescription>
        </DialogHeader>
        <div className="aspect-video w-full overflow-hidden rounded-lg bg-black">
          {open && (
            <iframe
              className="h-full w-full"
              src={youtubeEmbedUrl(videoId)}
              title={`${title} | Fibras Inmobiliarias`}
              allow="accelerometer; autoplay; clipboard-write; encrypted-media; fullscreen; gyroscope; picture-in-picture; web-share"
              referrerPolicy="strict-origin-when-cross-origin"
              allowFullScreen
            />
          )}
        </div>
      </DialogContent>
    </Dialog>
  )
}
