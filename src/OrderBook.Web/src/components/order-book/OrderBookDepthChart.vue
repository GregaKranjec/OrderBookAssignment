<script setup lang="ts">
import { computed } from 'vue'
import { useMediaQuery } from '@vueuse/core'
import { VisArea, VisAxis, VisXYContainer } from '@unovis/vue'
import { ChartCrosshair, ChartTooltip, ChartTooltipContent, componentToString } from '@/components/ui/chart'
import type { ChartConfig } from '@/components/ui/chart'
import OrderBookChartCard from '@/components/order-book/OrderBookChartCard.vue'
import { useChartYDomain } from '@/composables/useChartYDomain'
import type { BitstampOrderBook } from '@/types/orderBook'
import { DEPTH_PRICE_RANGE, prepareOrderBookDepth } from '@/lib/orderBookChart'
import type { CumulativeDepthLevel } from '@/lib/orderBookChart'
import {
    formatOrderBookPrice,
    formatOrderBookQuantity,
    orderBookChartConfig,
} from '@/lib/orderBookChartPresentation'

const props = defineProps<{
    orderBook: BitstampOrderBook | null
}>()

const isMobile = useMediaQuery('(width < 40rem)')
const chart = computed(() => prepareOrderBookDepth(props.orderBook))

// chart y axis domain calc - to prevent y axis from jumping on each data update
const yDomain = useChartYDomain(
    () => Math.max(chart.value.totalBidQuantity, chart.value.totalAskQuantity),
    50,
)
const priceTickFormatter = new Intl.NumberFormat('en-IE', {
    style: 'currency', currency: 'EUR', maximumFractionDigits: 0,
})
const quantityTickFormatter = new Intl.NumberFormat('en-IE', { maximumFractionDigits: 2 })

const chartConfig = {
    bid: { ...orderBookChartConfig.bid, label: 'Cumulative bids' },
    ask: { ...orderBookChartConfig.ask, label: 'Cumulative asks' },
} satisfies ChartConfig

// sets tooltip class and content
const tooltipContent = componentToString(chartConfig, ChartTooltipContent, {
    class: 'min-w-52 [&_.flex-1]:gap-3',
    labelFormatter: price => `${formatOrderBookPrice(Number(price))} per 1 BTC`,
})

function formatPriceTick(tick: number | Date) {
    return priceTickFormatter.format(Number(tick))
}

function formatQuantityTick(tick: number | Date) {
    return quantityTickFormatter.format(Number(tick))
}

function tooltip(level: CumulativeDepthLevel) {
    // Include price so the helper distinguishes equal quantities at different levels.
    const payload = { price: level.price, [level.side]: `${formatOrderBookQuantity(level.cumulativeQuantity)} BTC` }
    return tooltipContent?.(payload, level.price) ?? ''
}
</script>

<template>
    <OrderBookChartCard
        v-slot="{ duration }"
        title="Cumulative market depth"
        :description="`All ${chart.bids.length.toLocaleString()} bid and ${chart.asks.length.toLocaleString()} ask levels within ±${DEPTH_PRICE_RANGE * 100}% of the mid-price.`"
        :config="chartConfig"
        :has-data="chart.levels.length > 0"
        :is-waiting="orderBook === null"
        :cursor="true"
    >
        <VisXYContainer
            :duration="duration"
            :y-domain="yDomain"
            :x-domain="chart.priceDomain"
            :padding="{ top: 12, right: 12, bottom: 0, left: 12 }"
        >
            <!-- Each side has its own data so the chart leaves the spread between them empty. -->
            <VisArea
                v-if="chart.bids.length > 0"
                :data="chart.bids"
                :x="(level: CumulativeDepthLevel) => level.price"
                :y="(level: CumulativeDepthLevel) => level.cumulativeQuantity"
                curve-type="stepBefore"
                :color="chartConfig.bid.color"
                :opacity="0.15"
                :line="true"
            />
            <VisArea
                v-if="chart.asks.length > 0"
                :data="chart.asks"
                :x="(level: CumulativeDepthLevel) => level.price"
                :y="(level: CumulativeDepthLevel) => level.cumulativeQuantity"
                curve-type="stepAfter"
                :color="chartConfig.ask.color"
                :opacity="0.15"
                :line="true"
            />
            <VisAxis
                type="x"
                label="Price per 1 BTC (EUR)"
                :tick-format="formatPriceTick"
                :tick-spacing="isMobile ? 30 : 75"
                :tick-text-angle="isMobile ? -90 : 0"
                :tick-text-align="'right'"
                :tick-text-width="90"
                :tick-text-adaptive-sets="true"
                :tick-text-hide-overlapping="!isMobile"
                :grid-line="false"
            />
            <VisAxis
                type="y"
                label="Cumulative BTC"
                :tick-format="formatQuantityTick"
                :num-ticks="4"
                :tick-line="false"
                :domain-line="false"
            />
            <ChartTooltip />
            <ChartCrosshair
                :data="chart.levels"
                :x="(level: CumulativeDepthLevel) => level.price"
                :y="(level: CumulativeDepthLevel) => level.cumulativeQuantity"
                :color="(level: CumulativeDepthLevel) => chartConfig[level.side].color"
                :template="tooltip"
                :hide-when-far-from-pointer="false"
            />
        </VisXYContainer>
    </OrderBookChartCard>
</template>
