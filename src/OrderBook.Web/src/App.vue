<script setup lang="ts">
import { useOrderBook } from '@/composables/useOrderBook'
import OrderBookHeader from '@/components/order-book/OrderBookHeader.vue'
import OrderBookDepthChart from '@/components/order-book/OrderBookDepthChart.vue'
import OrderBookQuote from '@/components/order-book/OrderBookQuote.vue'

const { snapshot, status, isStale, secondsSinceLastUpdate } = useOrderBook()
</script>

<template>
    <main class="min-h-screen bg-muted/30 px-4 py-6 sm:px-6 sm:py-8">
        <div class="mx-auto max-w-[1600px] space-y-6">
            <OrderBookHeader
                :status="status"
                :is-stale="isStale"
                :seconds-since-last-update="secondsSinceLastUpdate"
            />

            <div class="grid items-start gap-6 lg:grid-cols-3">
                <OrderBookDepthChart class="lg:col-span-2" :order-book="snapshot?.orderBook ?? null" />
                <OrderBookQuote
                    :order-book="snapshot?.orderBook ?? null"
                    :is-outdated="isStale || status !== 'connected'"
                />
            </div>
        </div>
    </main>
</template>
