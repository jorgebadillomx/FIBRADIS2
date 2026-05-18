import { PriceCarousel } from './PriceCarousel'
import { TopMovers } from './TopMovers'
import { QuickRanking } from './QuickRanking'
import { NewsSection } from './NewsSection'

export function HomePage() {
  return (
    <div className="container mx-auto px-4 py-6 space-y-8">
      <PriceCarousel />
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2">
          <TopMovers />
        </div>
        <div>
          <NewsSection />
        </div>
      </div>
      <QuickRanking />
    </div>
  )
}
