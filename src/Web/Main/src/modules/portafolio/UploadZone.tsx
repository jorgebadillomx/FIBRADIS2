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
  const [showConfirm, setShowConfirm] = useState(false)
  const [errors, setErrors] = useState<RowError[]>([])
  const [isUploading, setIsUploading] = useState(false)

  function handleFileSelect(file: File) {
    setErrors([])
    setSelectedFile(file)
    if (currentPositionCount > 0) {
      setShowConfirm(true)
    }
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

  async function doUpload() {
    if (!selectedFile) return
    setIsUploading(true)
    setErrors([])

    const formData = new FormData()
    formData.append('file', selectedFile)

    const { data, error, response } = await apiClient.POST('/api/v1/portfolio/upload', {
      body: formData as unknown as { file: string },
      bodySerializer: () => formData,
    })

    setIsUploading(false)

    if (response.ok && data) {
      await queryClient.invalidateQueries({ queryKey: ['portfolio'] })
      setSelectedFile(null)
      const positionCount = Number((data as PortfolioUploadResponseDto).positionCount)
      onUploadSuccess(positionCount)
      return
    }

    if (error && 'errors' in error) {
      setErrors((error.errors as RowError[]) ?? [])
    } else {
      setErrors([{ rowNumber: 0, ticker: '', message: 'Error inesperado al subir el archivo.' }])
    }
  }

  function handleConfirm() {
    setShowConfirm(false)
    void doUpload()
  }

  function handleCancelConfirm() {
    setShowConfirm(false)
    setSelectedFile(null)
    if (fileInputRef.current) fileInputRef.current.value = ''
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
          type="file"
          accept=".xlsx,.csv"
          className="hidden"
          aria-label="Seleccionar archivo de portafolio (.xlsx o .csv)"
          onChange={handleInputChange}
        />
      </div>

      {selectedFile && !showConfirm && (
        <div className="flex items-center gap-3 p-3 rounded-lg bg-muted/50 border border-border">
          <span className="text-sm text-muted-foreground flex-1 truncate">{selectedFile.name}</span>
          <Button
            onClick={doUpload}
            disabled={isUploading}
            size="sm"
          >
            {isUploading ? 'Cargando...' : 'Cargar portafolio'}
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

      <Dialog open={showConfirm} onOpenChange={(open) => { if (!open) handleCancelConfirm() }}>
        <DialogContent showCloseButton={false}>
          <DialogHeader>
            <DialogTitle>Reemplazar portafolio</DialogTitle>
            <DialogDescription>
              Esto reemplazará tus {currentPositionCount} posiciones actuales. ¿Continuar?
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={handleCancelConfirm}>Cancelar</Button>
            <Button onClick={handleConfirm}>Continuar</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
