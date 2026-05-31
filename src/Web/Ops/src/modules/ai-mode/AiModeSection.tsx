import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { fetchAiMode, fetchAiProvider, setAiConfig, setAiMode, setAiProvider } from '@/api/aiModeApi'

export function AiModeSection() {
  const queryClient = useQueryClient()
  const [selected, setSelected] = useState<'Off' | 'On' | null>(null)
  const [selectedProvider, setSelectedProvider] = useState<string | null>(null)
  const [selectedModel, setSelectedModel] = useState<string | null>(null)
  const [pendingMinBodyLength, setPendingMinBodyLength] = useState<number | null>(null)

  const modeQuery = useQuery({
    queryKey: ['ai-mode'],
    queryFn: fetchAiMode,
    retry: false,
  })

  const saveMutation = useMutation({
    mutationFn: setAiMode,
    onSuccess: async () => {
      setSelected(null)
      await queryClient.invalidateQueries({ queryKey: ['ai-mode'] })
    },
  })

  const saveThresholdMutation = useMutation({
    mutationFn: (value: number) => setAiConfig({ minBodyTextLengthForAi: value }),
    onSuccess: async () => {
      setPendingMinBodyLength(null)
      await queryClient.invalidateQueries({ queryKey: ['ai-mode'] })
    },
  })

  const providerQuery = useQuery({
    queryKey: ['ai-provider'],
    queryFn: fetchAiProvider,
    retry: false,
  })

  const saveProviderMutation = useMutation({
    mutationFn: ({ provider, modelId }: { provider: string; modelId: string }) =>
      setAiProvider(provider, modelId),
    onSuccess: async () => {
      setSelectedProvider(null)
      setSelectedModel(null)
      await queryClient.invalidateQueries({ queryKey: ['ai-provider'] })
    },
  })

  const currentProvider = providerQuery.data?.provider
  const currentModel = providerQuery.data?.modelId
  const pendingProvider = selectedProvider ?? currentProvider
  const availableModels =
    providerQuery.data?.availableProviders.find((p) => p.provider === pendingProvider)?.models ?? []
  const pendingModel = selectedModel ?? (pendingProvider === currentProvider ? currentModel : availableModels[0])

  const providerChanged = selectedProvider !== null && selectedProvider !== currentProvider
  const modelChanged = selectedModel !== null && selectedModel !== currentModel
  const providerOrModelChanged = providerChanged || modelChanged

  const currentMode = modeQuery.data?.mode as 'Off' | 'On' | undefined
  const pendingMode = selected ?? currentMode

  return (
    <section className="rounded-2xl border border-border/80 bg-white/90 p-6 shadow-sm">
      <div className="flex flex-col gap-2">
        <h2 className="text-lg font-semibold tracking-tight">Modo AI</h2>
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

          {selected !== null && selected !== currentMode ? (
            <div className="mt-4 flex items-center gap-3">
              <button
                className="h-10 rounded-xl bg-teal-700 px-5 text-sm font-medium text-white transition hover:bg-teal-800 disabled:cursor-not-allowed disabled:bg-teal-400"
                disabled={saveMutation.isPending}
                onClick={() => saveMutation.mutate(selected)}
                type="button"
              >
                {saveMutation.isPending ? 'Guardando...' : 'Guardar cambio'}
              </button>
              <button
                className="text-sm text-muted-foreground hover:text-foreground"
                disabled={saveMutation.isPending}
                onClick={() => setSelected(null)}
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
              <h3 className="text-sm font-semibold tracking-tight">Umbral mínimo de cuerpo del artículo</h3>
              <p className="text-sm text-muted-foreground">
                Mínimo de caracteres que debe tener el body_text para enviar el artículo a la IA. Artículos por debajo del umbral se guardan como Partial sin llamar al proveedor.
              </p>
            </div>
            {modeQuery.data ? (
              <div className="mt-4 flex items-center gap-3">
                <input
                  type="number"
                  min={0}
                  max={10000}
                  step={50}
                  aria-label="Umbral mínimo de caracteres para análisis IA"
                  className="w-28 rounded-xl border border-border px-3 py-2 text-sm"
                  value={pendingMinBodyLength ?? modeQuery.data.minBodyTextLengthForAi}
                  onChange={(e) => {
                    const v = parseInt(e.target.value, 10)
                    if (!isNaN(v)) setPendingMinBodyLength(v)
                  }}
                  disabled={saveThresholdMutation.isPending}
                />
                {pendingMinBodyLength !== null &&
                  pendingMinBodyLength !== modeQuery.data.minBodyTextLengthForAi ? (
                  <>
                    <button
                      className="h-10 rounded-xl bg-teal-700 px-5 text-sm font-medium text-white transition hover:bg-teal-800 disabled:cursor-not-allowed disabled:bg-teal-400"
                      disabled={saveThresholdMutation.isPending}
                      onClick={() => saveThresholdMutation.mutate(pendingMinBodyLength)}
                      type="button"
                    >
                      {saveThresholdMutation.isPending ? 'Guardando...' : 'Guardar umbral'}
                    </button>
                    <button
                      className="text-sm text-muted-foreground hover:text-foreground"
                      disabled={saveThresholdMutation.isPending}
                      onClick={() => setPendingMinBodyLength(null)}
                      type="button"
                    >
                      Cancelar
                    </button>
                  </>
                ) : null}
              </div>
            ) : null}
            {saveThresholdMutation.isError ? (
              <p className="mt-3 text-sm text-destructive">{saveThresholdMutation.error.message}</p>
            ) : null}
            {saveThresholdMutation.isSuccess ? (
              <p className="mt-3 text-sm text-teal-700">Umbral actualizado correctamente.</p>
            ) : null}
          </div>

          <div className="mt-6 rounded-xl border border-border/80 bg-slate-50/80 p-4">
            <div className="flex flex-col gap-2">
              <h3 className="text-sm font-semibold tracking-tight">Proveedor de IA</h3>
              <p className="text-sm text-muted-foreground">
                Selecciona el proveedor y modelo que usará el pipeline y los disparos manuales.
              </p>
            </div>

            {providerQuery.isLoading ? (
              <p className="mt-4 text-sm text-muted-foreground">Cargando configuración...</p>
            ) : providerQuery.isError ? (
              <p className="mt-4 text-sm text-destructive">{providerQuery.error.message}</p>
            ) : (
              <>
                <div className="mt-4 flex flex-col gap-3 md:flex-row">
                  <div className="flex flex-col gap-1">
                    <label className="text-xs font-medium text-muted-foreground">Proveedor</label>
                    <div className="flex gap-2">
                      {providerQuery.data?.availableProviders.map((p) => (
                        <button
                          key={p.provider}
                          type="button"
                          className={[
                            'rounded-xl border px-4 py-2 text-sm font-medium transition',
                            pendingProvider === p.provider
                              ? 'border-teal-700 bg-teal-700 text-white'
                              : 'border-border bg-white text-slate-700 hover:border-teal-600',
                          ].join(' ')}
                          disabled={saveProviderMutation.isPending}
                          onClick={() => {
                            setSelectedProvider(p.provider)
                            setSelectedModel(null)
                          }}
                        >
                          {p.provider}
                        </button>
                      ))}
                    </div>
                  </div>

                  <div className="flex flex-col gap-1">
                    <label className="text-xs font-medium text-muted-foreground">Modelo</label>
                    <div className="flex gap-2">
                      {availableModels.map((m) => (
                        <button
                          key={m}
                          type="button"
                          className={[
                            'rounded-xl border px-4 py-2 text-sm font-medium transition',
                            pendingModel === m
                              ? 'border-teal-700 bg-teal-700 text-white'
                              : 'border-border bg-white text-slate-700 hover:border-teal-600',
                          ].join(' ')}
                          disabled={saveProviderMutation.isPending}
                          onClick={() => setSelectedModel(m)}
                        >
                          {m}
                        </button>
                      ))}
                    </div>
                  </div>
                </div>

                {providerOrModelChanged ? (
                  <div className="mt-4 flex items-center gap-3">
                    <button
                      className="h-10 rounded-xl bg-teal-700 px-5 text-sm font-medium text-white transition hover:bg-teal-800 disabled:cursor-not-allowed disabled:bg-teal-400"
                      disabled={saveProviderMutation.isPending}
                      onClick={() =>
                        saveProviderMutation.mutate({
                          provider: pendingProvider ?? '',
                          modelId: pendingModel ?? '',
                        })
                      }
                      type="button"
                    >
                      {saveProviderMutation.isPending ? 'Guardando...' : 'Guardar proveedor'}
                    </button>
                    <button
                      className="text-sm text-muted-foreground hover:text-foreground"
                      disabled={saveProviderMutation.isPending}
                      onClick={() => {
                        setSelectedProvider(null)
                        setSelectedModel(null)
                        saveProviderMutation.reset()
                      }}
                      type="button"
                    >
                      Cancelar
                    </button>
                  </div>
                ) : null}

                {saveProviderMutation.isError ? (
                  <p className="mt-3 text-sm text-destructive">{saveProviderMutation.error.message}</p>
                ) : null}

                {saveProviderMutation.isSuccess ? (
                  <p className="mt-3 text-sm text-teal-700">Proveedor actualizado correctamente.</p>
                ) : null}

                {providerQuery.data ? (
                  <p className="mt-3 text-xs text-muted-foreground">
                    Activo: {providerQuery.data.provider} · {providerQuery.data.modelId}
                    {providerQuery.data.updatedBy ? ` · por ${providerQuery.data.updatedBy}` : ''}
                  </p>
                ) : null}
              </>
            )}
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
