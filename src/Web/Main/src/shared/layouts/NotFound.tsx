import { Link } from 'react-router'

export function NotFound() {
  return (
    <div className="container mx-auto px-4 py-16 text-center">
      <h1 className="text-2xl font-semibold mb-2">Página no encontrada</h1>
      <p className="text-muted-foreground mb-6">Esta sección estará disponible próximamente.</p>
      <Link to="/" className="text-sm text-primary hover:underline">← Volver a la Home</Link>
    </div>
  )
}
