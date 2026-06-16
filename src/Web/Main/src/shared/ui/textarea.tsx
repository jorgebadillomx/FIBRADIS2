import * as React from "react"

import { cn } from "@/shared/lib/utils"

function buildFieldName(seed: string) {
  const normalized = seed
    .normalize('NFKD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')

  return normalized || 'field'
}

function Textarea({ className, id, name, ...props }: React.ComponentProps<"textarea">) {
  const autoId = React.useId().replace(/:/g, '')
  const resolvedId = id ?? `textarea-${autoId}`
  const labelSeed =
    typeof props['aria-label'] === 'string'
      ? props['aria-label']
      : typeof props.placeholder === 'string'
        ? props.placeholder
        : resolvedId
  const resolvedName = name ?? buildFieldName(labelSeed)

  return (
    <textarea
      id={resolvedId}
      name={resolvedName}
      data-slot="textarea"
      className={cn(
        "flex field-sizing-content min-h-16 w-full rounded-lg border border-input bg-transparent px-2.5 py-2 text-base transition-colors outline-none placeholder:text-muted-foreground focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:cursor-not-allowed disabled:bg-input/50 disabled:opacity-50 aria-invalid:border-destructive aria-invalid:ring-3 aria-invalid:ring-destructive/20 md:text-sm dark:bg-input/30 dark:disabled:bg-input/80 dark:aria-invalid:border-destructive/50 dark:aria-invalid:ring-destructive/40",
        className
      )}
      {...props}
    />
  )
}

export { Textarea }
