<script setup lang="ts">
import { useOrderBook } from '@/composables/useOrderBook'
import OrderBookHeader from '@/components/order-book/OrderBookHeader.vue'
import { Card } from '@/components/ui/card'

const { status, isStale, secondsSinceLastUpdate } = useOrderBook()
</script>

<template>
    <main class="min-h-screen bg-muted/30 px-4 py-6 sm:px-6 sm:py-8">
        <div class="mx-auto max-w-6xl space-y-6">
            <OrderBookHeader
                :status="status"
                :is-stale="isStale"
                :seconds-since-last-update="secondsSinceLastUpdate"
            />

            <div class="grid items-start gap-6 md:grid-cols-[minmax(0,1fr)_280px]">
                <Card class="min-h-80 md:min-h-112" />
                <Card class="min-h-64 md:min-h-112" />
            </div>
        </div>
    </main>
</template>
