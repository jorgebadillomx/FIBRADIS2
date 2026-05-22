import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { fetchAiMode, setAiConfig, triggerAiSummary } from '@/api/aiModeApi'

type NewsModel = 'gemini-2.5-flash' | 'gemini-2.5-pro'

export function AiModeSection() {
  const queryClient = useQueryClient()
  const [selected, setSelected] = useState<'Off' | 'On' | null>(null)
  const [selectedModel, setSelectedModel] = useState<NewsModel | null>(null)
  const [articleId, setArticleId] = useState('')

  const modeQuery = useQuery({
    queryKey: ['ai-mode'],
    queryFn: fetchAiMode,
    retry: false,
  })

  const saveMutation = useMutation({
    mutationFn: setAiConfig,
    onSuccess: async () => {
      setSelected(null)
      setSelectedModel(null)
      triggerMutation.reset()
      await queryClient.invalidateQueries({ queryKey: ['ai-mode'] })
    },
  })

  const triggerMutation = useMutation({
    mutationFn: triggerAiSummary,
    onSuccess: () => {
      setArticleId('')
    },
  })

  const currentMode = modeQuery.data?.mode as 'Off' | 'On' | undefined
  const currentModel = modeQuery.data?.newsModel as NewsModel | undefined
  const pendingMode = selected ?? currentMode
  const pendingModel = selectedModel ?? currentModel

  const modeChanged = selected !== null && selected !== currentMode
  const modelChanged = selectedModel !== null && selectedModel !== currentModel
  const hasChanges = modeChanged || modelChanged

  const UUID_REGEX = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i
  const isValidUuid = UUID_REGEX.test(articleId.trim())

  return (
    <section className="rounded-2xl border border-border/80 bg-white/90 p-6 shadow-sm">
      <div className="flex flex-col gap-2">
        <h2 className="text-lg font-semibold tracking-tight">Modo AI de Noticias</h2>
        <p className="max-w-3xl text-sm text-muted-foreground">
          Controla si se generan resúmenes de noticias. El cambio aplica en el siguiente ciclo del pipeline sin redespliegue.
        </p>
      </div>

      {modeQuery.isLoading ? (
        <p className="mt-6 text-sm text-muted-foreground">Cargando configuración...</p>
      ) : modeQuery.isError ? (
        <p className="mt-6 text-sm text-destructive">{modeQuery.error.message}</p>
      ) : (
        <>
          <div className="mt-6 flex flex-col gap-3 md:flex-row">
            {(['Off', 'On'] as const).map((mode) => (
              <button
                key={mode}
                type="button"
                className={[
                  'rounded-xl border px-5 py-3 text-sm font-medium transition',
                  pendingMode === mode
                    ? 'border-teal-700 bg-teal-700 text-white'
                    : 'border-border bg-white text-slate-700 hover:border-teal-600',
                ].join(' ')}
                disabled={saveMutation.isPending}
                onClick={() => setSelected(mode)}
              >
                {mode === 'Off' ? 'Off - sin resúmenes' : 'On - generar resumen al ingestar'}
              </button>
            ))}
          </div>

          <div className="mt-4 flex flex-col gap-2">
            <p className="text-xs font-medium text-muted-foreground">Modelo IA</p>
            <div className="flex flex-col gap-3 md:flex-row">
              {(['gemini-2.5-flash', 'gemini-2.5-pro'] as const).map((model) => (
                <button
                  key={model}
                  type="button"
                  className={[
                    'rounded-xl border px-5 py-3 text-sm font-medium transition',
                    pendingModel === model
                      ? 'border-teal-700 bg-teal-700 text-white'
                      : 'border-border bg-white text-slate-700 hover:border-teal-600',
                  ].join(' ')}
                  disabled={saveMutation.isPending}
                  onClick={() => setSelectedModel(model)}
                >
                  {model === 'gemini-2.5-flash' ? 'Flash (gemini-2.5-flash)' : 'Pro (gemini-2.5-pro)'}
                </button>
              ))}
            </div>
          </div>

          {hasChanges ? (
            <div className="mt-4 flex items-center gap-3">
              <button
                className="h-10 rounded-xl bg-teal-700 px-5 text-sm font-medium text-white transition hover:bg-teal-800 disabled:cursor-not-allowed disabled:bg-teal-400"
                disabled={saveMutation.isPending || triggerMutation.isPending}
                onClick={() =>
                  saveMutation.mutate({
                    mode: modeChanged ? (pendingMode as 'Off' | 'On') : undefined,
                    newsModel: modelChanged ? (pendingModel as NewsModel) : undefined,
                  })
                }
                type="button"
              >
                {saveMutation.isPending ? 'Guardando...' : 'Guardar cambio'}
              </button>
              <button
                className="text-sm text-muted-foreground hover:text-foreground"
                disabled={saveMutation.isPending}
                onClick={() => {
                  setSelected(null)
                  setSelectedModel(null)
                }}
                type="button"
              >
                Cancelar
              </button>
            </div>
          ) : null}

          {saveMutation.isError ? (
            <p className="mt-3 text-sm text-destructive">{saveMutation.error.message}</p>
          ) : null}

          <div className="mt-6 rounded-xl border border-border/80 bg-slate-50/80 p-4">
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
                disabled={!isValidUuid || triggerMutation.isPending || saveMutation.isPending}
                onClick={() => triggerMutation.mutate(articleId.trim())}
                type="button"
              >
                {triggerMutation.isPending ? 'Generando...' : 'Generar resumen'}
              </button>
            </div>

            {articleId.trim().length > 0 && !isValidUuid ? (
              <p className="mt-2 text-sm text-destructive">El ID debe ser un GUID válido (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx).</p>
            ) : null}

            {currentMode === 'On' ? (
              <p className="mt-3 text-sm text-muted-foreground">
                En modo On, el pipeline ya genera resúmenes automáticamente. Este disparo regenera uno específico.
              </p>
            ) : null}

            {triggerMutation.isError ? (
              <p className="mt-3 text-sm text-destructive">{triggerMutation.error.message}</p>
            ) : null}

            {triggerMutation.isSuccess ? (
              <p className="mt-3 text-sm text-teal-700">Resumen solicitado correctamente.</p>
            ) : null}
          </div>

          {modeQuery.data ? (
            <p className="mt-4 text-xs text-muted-foreground">
              Último cambio:{' '}
              {new Date(modeQuery.data.updatedAt).toLocaleString('es-MX', {
                dateStyle: 'medium',
                timeStyle: 'short',
              })}
              {modeQuery.data.updatedBy ? ` · por ${modeQuery.data.updatedBy}` : ''}
              {modeQuery.data.previousMode ? ` · anterior: ${modeQuery.data.previousMode}` : ''}
            </p>
          ) : null}
        </>
      )}
    </section>
  )
}
