import { useRef, useState } from 'react'
import { Input } from '@/shared/ui/input'

interface EditableCellProps {
  value: number
  format: (v: number) => string
  validate: (raw: string) => string | null
  parse: (raw: string) => number
  onSave: (newValue: number) => Promise<void>
  className?: string
}

export function EditableCell({ value, format, validate, parse, onSave, className }: EditableCellProps) {
  const [editing, setEditing] = useState(false)
  const [draft, setDraft] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const inputRef = useRef<HTMLInputElement>(null)

  function startEditing() {
    setDraft(String(value))
    setError(null)
    setEditing(true)
    setTimeout(() => inputRef.current?.select(), 0)
  }

  function cancel() {
    setEditing(false)
    setError(null)
  }

  async function save() {
    const trimmed = draft.trim()
    const err = validate(trimmed)
    if (err) {
      setError(err)
      return
    }
    const parsed = parse(trimmed)
    if (parsed === value) {
      setEditing(false)
      setError(null)
      return
    }
    setSaving(true)
    try {
      await onSave(parsed)
      setEditing(false)
      setError(null)
    } catch {
      setError('Error al guardar. Intenta de nuevo.')
    } finally {
      setSaving(false)
    }
  }

  if (!editing) {
    return (
      <span
        className={`cursor-text select-none rounded px-1 hover:bg-muted/60 ${className ?? ''}`}
        onDoubleClick={startEditing}
        title="Doble clic para editar"
      >
        {format(value)}
      </span>
    )
  }

  return (
    <span className="relative inline-flex flex-col gap-0.5">
      <Input
        ref={inputRef}
        value={draft}
        onChange={(e) => { setDraft(e.target.value); setError(null) }}
        onKeyDown={(e) => {
          if (e.key === 'Enter') { e.preventDefault(); void save() }
          if (e.key === 'Escape') cancel()
        }}
        onBlur={() => void save()}
        disabled={saving}
        className="h-7 w-28 text-right tabular-nums text-sm"
        autoFocus
      />
      {error && (
        <span className="absolute top-full left-0 z-10 mt-0.5 whitespace-nowrap rounded bg-destructive px-2 py-0.5 text-xs text-destructive-foreground shadow">
          {error}
        </span>
      )}
    </span>
  )
}
