import { Link } from 'react-router'

interface Props { ticker: string }

export function FibraNotFound({ ticker }: Props) {
  return (
    <div className="container mx-auto px-4 py-16 text-center">
      <h1 className="text-2xl font-semibold mb-2">FIBRA no encontrada</h1>
      <p className="text-muted-foreground mb-6">
        No existe una FIBRA con ticker <span className="font-mono font-medium">{ticker.toUpperCase()}</span> en el catálogo.
      </p>
      <Link to="/" className="text-sm text-primary hover:underline">← Volver a la Home</Link>
    </div>
  )
}
