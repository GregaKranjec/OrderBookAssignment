import { ChartTooltipContent, componentToString } from '@/components/ui/chart'
import type { ChartConfig } from '@/components/ui/chart'
import type { CumulativeDepthLevel } from '@/lib/orderBookDepth'

const priceTickFormatter = new Intl.NumberFormat('en-IE', {
    style: 'currency', currency: 'EUR', maximumFractionDigits: 0,
})
const quantityTickFormatter = new Intl.NumberFormat('en-IE', { maximumFractionDigits: 2 })
const priceFormatter = new Intl.NumberFormat('en-IE', { style: 'currency', currency: 'EUR' })
const quantityFormatter = new Intl.NumberFormat('en-IE', { maximumFractionDigits: 8 })

export const depthChartConfig = {
    bid: { label: 'Cumulative bids', color: 'var(--order-book-bid)' },
    ask: { label: 'Cumulative asks', color: 'var(--order-book-ask)' },
} satisfies ChartConfig

export function formatPriceTick(tick: number | Date) {
    return priceTickFormatter.format(Number(tick))
}

export function formatQuantityTick(tick: number | Date) {
    return quantityTickFormatter.format(Number(tick))
}

export function depthColor(level: CumulativeDepthLevel) {
    return depthChartConfig[level.side].color
}

// Create this during component setup because the chart helper uses Vue's component context.
export function createDepthTooltip() {
    const tooltipContent = componentToString(depthChartConfig, ChartTooltipContent, {
        class: 'min-w-52 [&_.flex-1]:gap-3',
        labelFormatter: price => `${priceFormatter.format(Number(price))} per 1 BTC`,
    })

    return (level: CumulativeDepthLevel) => {
        // Include price so the helper distinguishes equal quantities at different levels.
        const payload = { price: level.price, [level.side]: `${quantityFormatter.format(level.cumulativeQuantity)} BTC` }
        return tooltipContent?.(payload, level.price) ?? ''
    }
}
