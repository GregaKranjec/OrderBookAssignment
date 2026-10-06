<script setup lang="ts">
import { computed, ref } from 'vue'
import { useMediaQuery } from '@vueuse/core'
import { VisArea, VisAxis, VisXYContainer } from '@unovis/vue'
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { ChartContainer, ChartCrosshair, ChartLegendContent, ChartTooltip } from '@/components/ui/chart'
import { useChartYDomain } from '@/composables/useChartYDomain'
import type { BitstampOrderBook } from '@/types/orderBook'
import { prepareOrderBookDepth } from '@/lib/orderBookDepth'
import type { CumulativeDepthLevel } from '@/lib/orderBookDepth'
import {
    depthChartConfig as chartConfig,
    depthColor,
    createDepthTooltip,
    formatPriceTick,
    formatQuantityTick,
} from '@/lib/orderBookDepthPresentation'

const props = defineProps<{
    orderBook: BitstampOrderBook | null
}>()

const isMobile = useMediaQuery('(width < 40rem)')
const priceRanges = [0.01, 0.05, 0.10, 1] // small values are most useful - keeping 100% for the sake of showing the whole data range
const priceRange = ref(0.05)
const chart = computed(() => prepareOrderBookDepth(props.orderBook, priceRange.value))
const tooltip = createDepthTooltip()
const duration = 200 // animation length

// chart y axis domain calc - to prevent y axis from jumping on each data update
const { yDomain, resetYDomain } = useChartYDomain(
    () => Math.max(chart.value.totalBidQuantity, chart.value.totalAskQuantity),
    50,
)

function changePriceRange(range: number) {
    priceRange.value = range
    resetYDomain()
}
</script>

<template>
    <Card class="order-book-depth-card min-w-0 gap-3 rounded-none sm:rounded-xl">
        <CardHeader class="flex flex-row flex-wrap items-start justify-between gap-3">
            <div class="space-y-1">
                <CardTitle class="uppercase">Cumulative market depth</CardTitle>
                <CardDescription>
                    All {{ chart.bids.length.toLocaleString() }} bid and {{ chart.asks.length.toLocaleString() }} ask levels within ±{{ priceRange * 100 }}% of the mid-price.
                </CardDescription>
            </div>

            <ChartContainer :config="chartConfig" class="block h-auto w-auto aspect-auto">
                <ChartLegendContent class="pt-0" />
            </ChartContainer>

        </CardHeader>

        <CardContent class="min-w-0 px-3 sm:px-6">
            <ChartContainer v-if="chart.levels.length > 0" :config="chartConfig" :cursor="true" class="h-80 aspect-auto sm:h-96 lg:h-112">
                <VisXYContainer
                    :duration="duration"
                    :y-domain="yDomain"
                    :x-domain="chart.priceDomain"
                    :padding="{ top: 4, right: 12, bottom: 0, left: 12 }"
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
                        :tick-text-align="isMobile ? 'right' : 'center'"
                        :tick-text-angle="isMobile ? -90 : 0"
                        :tick-spacing="isMobile ? 25 : 75"
                        :min-max-ticks-only-when-width-is-less="0"
                        :tick-text-width="90"
                        :tick-text-adaptive-sets="!isMobile"
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
                        :color="depthColor"
                        :template="tooltip"
                        :hide-when-far-from-pointer="false"
                    />
                </VisXYContainer>
            </ChartContainer>
            <p v-else class="flex h-80 items-center justify-center text-sm text-muted-foreground sm:h-96 lg:h-112">
                {{ orderBook === null ? 'Waiting for data' : 'No price levels available.' }}
            </p>
        </CardContent>
        <CardFooter class="flex-wrap justify-end gap-2">
            <span class="text-xs text-muted-foreground">Range around mid-price (±)</span>
            <div class="flex gap-1" role="group" aria-label="Price range around the mid-price">
                <Button
                    v-for="range in priceRanges"
                    :key="range"
                    type="button"
                    variant="link"
                    size="sm"
                    :class="priceRange === range ? 'underline underline-offset-4' : 'text-muted-foreground'"
                    :aria-pressed="priceRange === range"
                    @click="changePriceRange(range)"
                >
                    {{ range * 100 }}%
                </Button>
            </div>
        </CardFooter>
    </Card>
</template>

<style scoped>
.order-book-depth-card {
    --order-book-bid: var(--color-emerald-600);
    --order-book-ask: var(--color-red-600);
    --vis-axis-label-color: var(--foreground);
    --vis-axis-tick-label-color: var(--muted-foreground);
    --vis-axis-grid-color: var(--border);
    --vis-axis-domain-color: var(--border);
}
</style>
