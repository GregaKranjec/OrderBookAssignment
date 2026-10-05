import type { ChartConfig } from '@/components/ui/chart'

export const orderBookChartConfig = {
    bid: { label: 'Bids', color: 'var(--order-book-bid)' },
    ask: { label: 'Asks', color: 'var(--order-book-ask)' },
} satisfies ChartConfig

// number formatting
const priceFormatter = new Intl.NumberFormat('en-IE', { style: 'currency', currency: 'EUR' })
const quantityFormatter = new Intl.NumberFormat('en-IE', { maximumFractionDigits: 8 })

export function formatOrderBookPrice(price: number | null) {
    return price === null ? '—' : priceFormatter.format(price)
}

export function formatOrderBookQuantity(quantity: number) {
    return quantityFormatter.format(quantity)
}
