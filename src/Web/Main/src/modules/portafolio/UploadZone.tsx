import { useRef, useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import type { components } from '@fibradis/shared-api-client'
import { apiClient } from '@/api/fibrasApi'
import { Button } from '@/shared/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/shared/ui/dialog'
import { ErrorTable } from '@/modules/portafolio/ErrorTable'

interface RowError {
  rowNumber: number
  ticker: string
  message: string
}

interface UploadZoneProps {
  currentPositionCount: number
  onUploadSuccess: (positionCount: number) => void
}

type PortfolioUploadResponseDto = components['schemas']['PortfolioUploadResponseDto']

export function UploadZone({ currentPositionCount, onUploadSuccess }: UploadZoneProps) {
  const queryClient = useQueryClient()
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [isDragging, setIsDragging] = useState(false)
  const [uploadMode, setUploadMode] = useState<'replace' | 'merge'>('replace')
  const [showModeDialog, setShowModeDialog] = useState(false)
  const [showDuplicateDialog, setShowDuplicateDialog] = useState(false)
  const [errors, setErrors] = useState<RowError[]>([])
  const [isUploading, setIsUploading] = useState(false)

  function resetFileSelection() {
    setSelectedFile(null)
    if (fileInputRef.current) fileInputRef.current.value = ''
  }

  async function doUpload(fileArg?: File, mode: 'replace' | 'merge' = uploadMode, force = false) {
    const file = fileArg ?? selectedFile
    if (!file) return

    setIsUploading(true)
    setErrors([])

    const formData = new FormData()
    formData.append('file', file)

    const { data, error, response } = await apiClient.POST('/api/v1/portfolio/upload', {
      params: { query: { mode, force } },
      body: formData as unknown as { file: string },
      bodySerializer: () => formData,
    })

    setIsUploading(false)

    if (response.ok && data) {
      if ((data as PortfolioUploadResponseDto).duplicateDetected) {
        setShowDuplicateDialog(true)
        return
      }

      await queryClient.invalidateQueries({ queryKey: ['portfolio'] })
      const positionCount = Number((data as PortfolioUploadResponseDto).positionCount)
      setShowModeDialog(false)
      setShowDuplicateDialog(false)
      resetFileSelection()
      onUploadSuccess(positionCount)
      return
    }

    if (error && 'errors' in error) {
      setErrors((error.errors as RowError[]) ?? [])
    } else {
      setErrors([{ rowNumber: 0, ticker: '', message: 'Error inesperado al subir el archivo.' }])
    }
  }

  function handleFileSelect(file: File) {
    setErrors([])
    setSelectedFile(file)

    if (currentPositionCount > 0) {
      setUploadMode('replace')
      setShowModeDialog(true)
      return
    }

    void doUpload(file, 'replace')
  }

  function handleInputChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (file) handleFileSelect(file)
  }

  function handleDrop(e: React.DragEvent) {
    e.preventDefault()
    setIsDragging(false)
    const file = e.dataTransfer.files[0]
    if (file && (file.name.endsWith('.xlsx') || file.name.endsWith('.csv'))) {
      handleFileSelect(file)
    }
  }

  function handleConfirmMode() {
    setShowModeDialog(false)
    void doUpload(undefined, uploadMode)
  }

  function handleCancelMode() {
    setShowModeDialog(false)
    resetFileSelection()
  }

  function handleConfirmDuplicate() {
    setShowDuplicateDialog(false)
    void doUpload(undefined, 'merge', true)
  }

  function handleCancelDuplicate() {
    setShowDuplicateDialog(false)
    resetFileSelection()
  }

  return (
    <div className="space-y-4">
      <div
        className={[
          'border-2 border-dashed rounded-xl p-10 flex flex-col items-center gap-4 transition-colors cursor-pointer',
          isDragging ? 'border-primary bg-primary/5' : 'border-border hover:border-primary/50',
        ].join(' ')}
        onDragOver={(e) => { e.preventDefault(); setIsDragging(true) }}
        onDragLeave={() => setIsDragging(false)}
        onDrop={handleDrop}
        onClick={() => fileInputRef.current?.click()}
      >
        <div className="text-4xl text-muted-foreground">📂</div>
        <div className="text-center">
          <p className="font-medium">Arrastra tu archivo aquí o haz clic para seleccionar</p>
          <p className="text-sm text-muted-foreground mt-1">Formatos aceptados: .xlsx, .csv</p>
          <p className="text-sm text-muted-foreground">Columnas requeridas: Ticker, Qty, AvgCost</p>
        </div>
        <input
          ref={fileInputRef}
          id="portfolio-file"
          name="portfolioFile"
          type="file"
          accept=".xlsx,.csv"
          className="hidden"
          aria-label="Seleccionar archivo de portafolio (.xlsx o .csv)"
          onChange={handleInputChange}
        />
      </div>

      {selectedFile && !showModeDialog && !showDuplicateDialog && !isUploading && (
        <div className="flex items-center gap-3 rounded-lg border border-border bg-muted/50 p-3">
          <span className="flex-1 truncate text-sm text-muted-foreground">{selectedFile.name}</span>
          <Button onClick={() => void doUpload()} size="sm">
            Cargar portafolio
          </Button>
        </div>
      )}

      {errors.length > 0 && (
        <div className="space-y-2">
          <p className="text-sm font-medium text-destructive">
            Se encontraron {errors.length} {errors.length === 1 ? 'error' : 'errores'}. Corrígelos y vuelve a subir el archivo.
          </p>
          <ErrorTable errors={errors} />
        </div>
      )}

      <Dialog open={showModeDialog} onOpenChange={(open) => { if (!open) handleCancelMode() }}>
        <DialogContent showCloseButton={false}>
          <DialogHeader>
            <DialogTitle>¿Cómo quieres subir este archivo?</DialogTitle>
            <DialogDescription>
              Elige si quieres reemplazar tu portafolio actual o sumar las posiciones del archivo.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-3">
            <button
              type="button"
              className={[
                'w-full rounded-xl border p-4 text-left transition-colors',
                uploadMode === 'replace'
                  ? 'border-primary bg-primary/5'
                  : 'border-border hover:border-primary/40',
              ].join(' ')}
              onClick={() => setUploadMode('replace')}
            >
              <div className="font-medium">Actualizar portafolio</div>
              <div className="mt-1 text-sm text-muted-foreground">
                Reemplaza todo con el contenido del archivo. Se guardará un respaldo.
              </div>
            </button>
            <button
              type="button"
              className={[
                'w-full rounded-xl border p-4 text-left transition-colors',
                uploadMode === 'merge'
                  ? 'border-primary bg-primary/5'
                  : 'border-border hover:border-primary/40',
              ].join(' ')}
              onClick={() => setUploadMode('merge')}
            >
              <div className="font-medium">Agregar al portafolio</div>
              <div className="mt-1 text-sm text-muted-foreground">
                Suma los títulos a los existentes y promedia el costo. Útil si tienes varios portafolios en GBM.
              </div>
            </button>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={handleCancelMode} disabled={isUploading}>Cancelar</Button>
            <Button onClick={handleConfirmMode} disabled={isUploading}>Continuar</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={showDuplicateDialog} onOpenChange={(open) => { if (!open) handleCancelDuplicate() }}>
        <DialogContent showCloseButton={false}>
          <DialogHeader>
            <DialogTitle>Este archivo ya está en tu portafolio</DialogTitle>
            <DialogDescription>
              Las posiciones ya existen con los mismos valores. ¿Quieres cargarlo de todas formas?
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={handleCancelDuplicate} disabled={isUploading}>
              Cancelar
            </Button>
            <Button onClick={handleConfirmDuplicate} disabled={isUploading}>
              Cargar de todas formas
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
