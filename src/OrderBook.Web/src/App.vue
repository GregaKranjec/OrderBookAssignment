<script setup lang="ts">
import { useOrderBook } from '@/composables/useOrderBook'
import OrderBookHeader from '@/components/order-book/OrderBookHeader.vue'
import OrderBookChart from '@/components/order-book/OrderBookChart.vue'
import OrderBookDepthChart from '@/components/order-book/OrderBookDepthChart.vue'
import { Card } from '@/components/ui/card'

const { snapshot, status, isStale, secondsSinceLastUpdate } = useOrderBook()
</script>

<template>
    <main class="min-h-screen bg-muted/30 px-4 py-6 sm:px-6 sm:py-8">
        <div class="mx-auto max-w-7xl space-y-6">
            <OrderBookHeader
                :status="status"
                :is-stale="isStale"
                :seconds-since-last-update="secondsSinceLastUpdate"
            />

            <div class="grid gap-6">
                <OrderBookChart :order-book="snapshot?.orderBook ?? null" />
                <OrderBookDepthChart :order-book="snapshot?.orderBook ?? null" />
                <Card class="min-h-64" />
            </div>
        </div>
    </main>
</template>
