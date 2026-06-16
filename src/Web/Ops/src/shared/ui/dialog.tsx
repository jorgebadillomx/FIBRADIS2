import * as React from 'react'
import { Dialog as DialogPrimitive } from 'radix-ui'
import { XIcon } from 'lucide-react'
import { cn } from '@/shared/lib/utils'

function Dialog(props: React.ComponentProps<typeof DialogPrimitive.Root>) {
  return <DialogPrimitive.Root data-slot="dialog" {...props} />
}

function DialogContent({
  className,
  children,
  ...props
}: React.ComponentProps<typeof DialogPrimitive.Content>) {
  return (
    <DialogPrimitive.Portal>
      <DialogPrimitive.Overlay
        className="fixed inset-0 z-50 bg-slate-950/40 backdrop-blur-[2px] data-[state=closed]:animate-out data-[state=open]:animate-in motion-reduce:animate-none motion-reduce:transition-none"
      />
      <DialogPrimitive.Content
        className={cn(
          'fixed left-1/2 top-1/2 z-50 w-[calc(100%-2rem)] max-w-2xl -translate-x-1/2 -translate-y-1/2 rounded-2xl border border-slate-200 bg-white p-6 shadow-2xl outline-none',
          className,
        )}
        {...props}
      >
        {children}
        <DialogPrimitive.Close
          className="absolute right-4 top-4 rounded-md p-1 text-slate-400 transition hover:bg-slate-100 hover:text-slate-700"
          aria-label="Cerrar modal"
        >
          <XIcon className="h-4 w-4" />
        </DialogPrimitive.Close>
      </DialogPrimitive.Content>
    </DialogPrimitive.Portal>
  )
}

function DialogHeader(props: React.ComponentProps<'div'>) {
  return <div className={cn('space-y-1', props.className)} {...props} />
}

function DialogTitle(props: React.ComponentProps<typeof DialogPrimitive.Title>) {
  return <DialogPrimitive.Title className={cn('text-lg font-semibold text-slate-900', props.className)} {...props} />
}

function DialogDescription(props: React.ComponentProps<typeof DialogPrimitive.Description>) {
  return <DialogPrimitive.Description className={cn('text-sm text-slate-500', props.className)} {...props} />
}

function DialogFooter(props: React.ComponentProps<'div'>) {
  return <div className={cn('flex flex-col-reverse gap-2 pt-2 sm:flex-row sm:justify-end', props.className)} {...props} />
}

export { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle }
