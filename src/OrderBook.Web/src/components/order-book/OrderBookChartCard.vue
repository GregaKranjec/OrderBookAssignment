<script setup lang="ts">
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { ChartContainer, ChartLegendContent } from '@/components/ui/chart'
import type { ChartConfig } from '@/components/ui/chart'

defineProps<{
    title: string
    description: string
    config: ChartConfig
    hasData: boolean
    isWaiting: boolean
    cursor?: boolean
}>()

const duration = 200 // animation length
</script>

<template>
    <Card class="min-w-0">
        <CardHeader class="flex flex-row flex-wrap items-start justify-between gap-3">
            <div class="space-y-1">
                <CardTitle class="uppercase">{{ title }}</CardTitle>
                <CardDescription>{{ description }}</CardDescription>
            </div>

            <ChartContainer :config="config" class="block h-auto w-auto aspect-auto">
                <ChartLegendContent class="pt-0" />
            </ChartContainer>
        </CardHeader>

        <CardContent class="min-w-0 px-3 sm:px-6">
            <ChartContainer v-if="hasData" :config="config" :cursor="cursor" class="h-80 aspect-auto sm:h-96 lg:h-112">
                <slot :duration="duration" />
            </ChartContainer>
            <p v-else class="flex h-80 items-center justify-center text-sm text-muted-foreground sm:h-96 lg:h-112">
                {{ isWaiting ? 'Waiting for data' : 'No price levels available.' }}
            </p>
        </CardContent>
    </Card>
</template>

<style scoped>
[data-slot='card'] {
    --order-book-bid: var(--color-emerald-600);
    --order-book-ask: var(--color-red-600);
    --vis-axis-label-color: var(--foreground);
    --vis-axis-tick-label-color: var(--muted-foreground);
    --vis-axis-grid-color: var(--border);
    --vis-axis-domain-color: var(--border);
}
</style>
