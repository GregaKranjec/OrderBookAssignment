<script setup lang="ts">
import { computed } from 'vue'
import { useMediaQuery } from '@vueuse/core'
import { VisAxis, VisGroupedBar, VisGroupedBarSelectors, VisXYContainer } from '@unovis/vue'
import { ChartTooltip, ChartTooltipContent, componentToString } from '@/components/ui/chart'
import OrderBookChartCard from '@/components/order-book/OrderBookChartCard.vue'
import { useChartYDomain } from '@/composables/useChartYDomain'
import type { BitstampOrderBook } from '@/types/orderBook'
import { prepareOrderBookChart, VISIBLE_LEVELS_PER_SIDE } from '@/lib/orderBookChart'
import type { ChartPriceLevel } from '@/lib/orderBookChart'
import {
    formatOrderBookPrice,
    formatOrderBookQuantity,
    orderBookChartConfig,
} from '@/lib/orderBookChartPresentation'

const props = defineProps<{
    orderBook: BitstampOrderBook | null
}>()

const isMobile = useMediaQuery('(width < 40rem)') // Tailwind's sm breakpoint - mobile devices
const visibleLevelsPerSide = computed(() => isMobile.value ? 35 : VISIBLE_LEVELS_PER_SIDE)
const chart = computed(() => prepareOrderBookChart(props.orderBook, visibleLevelsPerSide.value))

// chart y axis domain calc - to prevent y axis from jumping on each data update
const yDomain = useChartYDomain(
    () => chart.value.levels.reduce((maximum, level) => Math.max(maximum, level.quantity), 0),
    4,
)
const chartConfig = orderBookChartConfig
const tooltipContent = componentToString(chartConfig, ChartTooltipContent, {
    class: 'min-w-52 [&_.flex-1]:gap-3',
    labelFormatter: price => `${formatOrderBookPrice(Number(price))} per 1 BTC`,
})

function formatPriceTick(tick: number | Date) {
    const level = chart.value.levels[Math.round(Number(tick))]
    return level ? formatOrderBookPrice(level.price) : ''
}

function formatQuantityTick(tick: number | Date) {
    return formatOrderBookQuantity(Number(tick))
}

function tooltip(level: ChartPriceLevel) {
    // Include price so the helper distinguishes equal quantities at different levels.
    const payload = { price: level.price, [level.side]: `${formatOrderBookQuantity(level.quantity)} BTC` }
    return tooltipContent?.(payload, level.price) ?? ''
}
</script>

<template>
    <OrderBookChartCard
        v-slot="{ duration }"
        title="Live order book"
        :description="`Nearest ${visibleLevelsPerSide} levels per side, ordered by price.`"
        :config="chartConfig"
        :has-data="chart.levels.length > 0"
        :is-waiting="orderBook === null"
    >
        <VisXYContainer
            :data="chart.levels"
            :duration="duration"
            :y-domain="yDomain"
            :padding="{ top: 12, right: 12, bottom: 0, left: 12 }"
            :prevent-empty-domain="true"
        >
            <VisGroupedBar
                :x="(_level: ChartPriceLevel, index: number) => index"
                :y="(level: ChartPriceLevel) => level.quantity"
                :color="(level: ChartPriceLevel) => chartConfig[level.side].color"
                :group-padding="0.15"
                :group-max-width="24"
                :rounded-corners="0"
                :bar-min-height="0"
            />
            <VisAxis
                type="x"
                label="Price per 1 BTC (EUR)"
                :tick-format="formatPriceTick"
                :tick-spacing="isMobile ? 30 : 75"
                :tick-text-angle="isMobile ? -90 : 0"
                :tick-text-align="'right'"
                :tick-text-width="90 "
                :tick-text-adaptive-sets="true"
                :tick-text-hide-overlapping="!isMobile"
            />
            <VisAxis
                type="y"
                label="BTC available"
                :tick-format="formatQuantityTick"
                :num-ticks="4"
                :tick-line="false"
                :domain-line="false"
            />
            <ChartTooltip :triggers="{ [VisGroupedBarSelectors.bar]: tooltip }" />
        </VisXYContainer>
    </OrderBookChartCard>
</template>
