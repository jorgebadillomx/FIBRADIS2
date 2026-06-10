import { useRef, useState } from 'react'
import { useNavigate } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import { Command, CommandList, CommandItem, CommandEmpty } from '@/shared/ui/command'
import { Popover, PopoverAnchor, PopoverContent } from '@/shared/ui/popover'
import { Input } from '@/shared/ui/input'
import { fetchAllFibras } from '@/api/fibrasApi'
import { filterFibrasByQuery } from './global-search'
import { cn } from '@/shared/lib/utils'

interface GlobalSearchProps {
  onSelect?: (ticker: string) => void
  className?: string
}

export function GlobalSearch({ onSelect, className }: GlobalSearchProps) {
  const [open, setOpen] = useState(false)
  const [query, setQuery] = useState('')
  const navigate = useNavigate()
  const inputRef = useRef<HTMLInputElement>(null)

  const { data: fibras = [], isLoading, isError } = useQuery({
    queryKey: ['fibras', 'all'],
    queryFn: fetchAllFibras,
    staleTime: 5 * 60 * 1000,
  })

  const filtered = filterFibrasByQuery(fibras, query)

  function handleSelect(ticker: string) {
    setOpen(false)
    setQuery('')
    onSelect?.(ticker)
    navigate(`/fibras/${encodeURIComponent(ticker)}`)
  }

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverAnchor asChild>
        <Input
          ref={inputRef}
          role="combobox"
          aria-label="Buscar FIBRA por ticker o nombre"
          aria-expanded={open}
          aria-haspopup="listbox"
          aria-controls="global-search-listbox"
          aria-autocomplete="list"
          placeholder="Buscar FIBRA por ticker o nombre..."
          value={query}
          onChange={(e) => {
            const val = e.target.value
            setQuery(val)
            setOpen(val.length >= 1)
          }}
          onFocus={() => { if (query.length >= 1) setOpen(true) }}
          className={cn('h-9 w-full max-w-[16rem] lg:max-w-[24rem]', className)}
        />
      </PopoverAnchor>
      <PopoverContent
        className="w-64 lg:w-96 p-0"
        align="start"
        onOpenAutoFocus={(e) => e.preventDefault()}
        onInteractOutside={(e) => {
          if (inputRef.current?.contains(e.target as Node)) {
            e.preventDefault()
          }
        }}
      >
        <Command shouldFilter={false}>
          <CommandList id="global-search-listbox" role="listbox" aria-label="Resultados de búsqueda">
            {isLoading && (
              <div role="status" aria-live="polite" className="py-6 text-center text-sm text-muted-foreground">
                Cargando catálogo...
              </div>
            )}
            {isError && (
              <div role="status" aria-live="polite" className="py-6 text-center text-sm text-muted-foreground">
                Error al cargar el catálogo
              </div>
            )}
            {!isLoading && !isError && query.length >= 1 && filtered.length === 0 && (
              <CommandEmpty role="status" aria-live="polite">Sin resultados encontrados.</CommandEmpty>
            )}
            {!isLoading && !isError && filtered.map((f) => (
              <CommandItem key={f.ticker} onSelect={() => handleSelect(f.ticker)}>
                <span className="font-medium">{f.ticker}</span>
                <span className="ml-2 text-sm text-muted-foreground">{f.fullName}</span>
              </CommandItem>
            ))}
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  )
}
