import { useParams, Link } from 'react-router'

export function FichaPlaceholder() {
  const { ticker } = useParams<{ ticker: string }>()

  return (
    <div className="container mx-auto px-4 py-12">
      <h1 className="text-2xl font-semibold mb-2">{ticker?.toUpperCase()}</h1>
      <p className="text-muted-foreground mb-6">Ficha pública — disponible en Historia 2.3</p>
      <Link to="/" className="text-sm text-primary hover:underline">← Volver a la Home</Link>
    </div>
  )
}
