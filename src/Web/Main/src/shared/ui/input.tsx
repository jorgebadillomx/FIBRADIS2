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

function Input({ className, type, id, name, ...props }: React.ComponentProps<"input">) {
  const autoId = React.useId().replace(/:/g, '')
  const resolvedId = id ?? `input-${autoId}`
  const labelSeed =
    typeof props['aria-label'] === 'string'
      ? props['aria-label']
      : typeof props.placeholder === 'string'
        ? props.placeholder
        : resolvedId
  const resolvedName = name ?? buildFieldName(labelSeed)

  return (
    <input
      id={resolvedId}
      name={resolvedName}
      type={type}
      data-slot="input"
      className={cn(
        "h-8 w-full min-w-0 rounded-lg border border-input bg-transparent px-2.5 py-1 text-base transition-colors outline-none file:inline-flex file:h-6 file:border-0 file:bg-transparent file:text-sm file:font-medium file:text-foreground placeholder:text-muted-foreground focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:pointer-events-none disabled:cursor-not-allowed disabled:bg-input/50 disabled:opacity-50 aria-invalid:border-destructive aria-invalid:ring-3 aria-invalid:ring-destructive/20 md:text-sm dark:bg-input/30 dark:disabled:bg-input/80 dark:aria-invalid:border-destructive/50 dark:aria-invalid:ring-destructive/40",
        className
      )}
      {...props}
    />
  )
}

export { Input }
