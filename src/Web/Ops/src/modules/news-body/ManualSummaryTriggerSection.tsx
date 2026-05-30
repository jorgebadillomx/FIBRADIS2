import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { triggerAiSummary } from '@/api/aiModeApi'
import type { OpsNewsPage } from '@/api/newsApi'

const UUID_REGEX = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

export function ManualSummaryTriggerSection() {
  const [articleId, setArticleId] = useState('')
  const queryClient = useQueryClient()

  const triggerMutation = useMutation({
    mutationFn: triggerAiSummary,
    onSuccess: (_, generatedId) => {
      setArticleId('')
      queryClient.setQueriesData(
        { queryKey: ['ops-news-list'], exact: false },
        (old: OpsNewsPage | undefined) =>
          old
            ? {
                ...old,
                items: old.items.map((item) =>
                  item.id === generatedId ? { ...item, hasAiSummary: true } : item,
                ),
              }
            : old,
      )
    },
  })

  const trimmedArticleId = articleId.trim()
  const isValidUuid = UUID_REGEX.test(trimmedArticleId)

  return (
    <div className="rounded-2xl border border-border/80 bg-slate-50/80 p-4">
      <div className="flex flex-col gap-2">
        <h3 className="text-sm font-semibold tracking-tight">Generación manual de resumen</h3>
        <p className="text-sm text-muted-foreground">
          Usa el id del artículo para regenerar el resumen de una noticia específica, sin importar el modo actual.
        </p>
      </div>

      <div className="mt-4 flex flex-col gap-3 md:flex-row">
        <input
          className="h-11 flex-1 rounded-xl border border-border bg-white px-4 text-sm outline-none ring-0 transition focus:border-teal-600"
          disabled={triggerMutation.isPending}
          onChange={(event) => {
            setArticleId(event.target.value)
            triggerMutation.reset()
          }}
          placeholder="GUID del artículo de noticias"
          value={articleId}
        />
        <button
          className="h-11 rounded-xl bg-slate-900 px-5 text-sm font-medium text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:bg-slate-400"
          disabled={!isValidUuid || triggerMutation.isPending}
          onClick={() => triggerMutation.mutate(trimmedArticleId)}
          type="button"
        >
          {triggerMutation.isPending ? 'Generando...' : 'Generar resumen'}
        </button>
      </div>

      {trimmedArticleId.length > 0 && !isValidUuid ? (
        <p className="mt-2 text-sm text-destructive">El ID debe ser un GUID válido (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx).</p>
      ) : null}

      {triggerMutation.isError ? (
        <p className="mt-3 text-sm text-destructive">{triggerMutation.error.message}</p>
      ) : null}

      {triggerMutation.isSuccess ? (
        <p className="mt-3 text-sm text-teal-700">Resumen solicitado correctamente.</p>
      ) : null}
    </div>
  )
}
