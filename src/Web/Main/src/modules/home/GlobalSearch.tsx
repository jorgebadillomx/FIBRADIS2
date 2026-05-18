import { useRef, useState } from 'react'
import { useNavigate } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import { Command, CommandList, CommandItem, CommandEmpty } from '@/shared/ui/command'
import { Popover, PopoverAnchor, PopoverContent } from '@/shared/ui/popover'
import { Input } from '@/shared/ui/input'
import { fetchAllFibras } from '@/api/fibrasApi'

export function GlobalSearch() {
  const [open, setOpen] = useState(false)
  const [query, setQuery] = useState('')
  const navigate = useNavigate()
  const inputRef = useRef<HTMLInputElement>(null)

  const { data: fibras = [], isLoading, isError } = useQuery({
    queryKey: ['fibras', 'all'],
    queryFn: fetchAllFibras,
    staleTime: 5 * 60 * 1000,
  })

  const filtered = query.length >= 1
    ? fibras
        .filter(f =>
          (f.ticker ?? '').toLowerCase().includes(query.toLowerCase()) ||
          (f.fullName ?? '').toLowerCase().includes(query.toLowerCase())
        )
        .slice(0, 8)
    : []

  function handleSelect(ticker: string) {
    setOpen(false)
    setQuery('')
    navigate(`/fibras/${encodeURIComponent(ticker)}`)
  }

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverAnchor asChild>
        <Input
          ref={inputRef}
          placeholder="Buscar FIBRA por ticker o nombre..."
          value={query}
          onChange={(e) => {
            const val = e.target.value
            setQuery(val)
            setOpen(val.length >= 1)
          }}
          onFocus={() => { if (query.length >= 1) setOpen(true) }}
          className="w-full max-w-[16rem] lg:max-w-[24rem] h-9"
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
          <CommandList>
            {isLoading && (
              <div className="py-6 text-center text-sm text-muted-foreground">Cargando catálogo...</div>
            )}
            {isError && (
              <div className="py-6 text-center text-sm text-muted-foreground">Error al cargar el catálogo</div>
            )}
            {!isLoading && !isError && query.length >= 1 && filtered.length === 0 && <CommandEmpty>Sin resultados encontrados.</CommandEmpty>}
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
