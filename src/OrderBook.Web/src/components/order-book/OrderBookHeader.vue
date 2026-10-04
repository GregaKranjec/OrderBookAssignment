<script setup lang="ts">
import { computed } from 'vue'
import { Badge } from '@/components/ui/badge'
import type { ConnectionStatus } from '@/composables/useOrderBook'

const props = defineProps<{
    status: ConnectionStatus
    isStale: boolean
    secondsSinceLastUpdate: number | null
}>()

const isLive = computed(() =>
    props.status === 'connected' && !props.isStale && props.secondsSinceLastUpdate !== null,
)

const statusLabel = computed(() => {
    switch (props.status) {
        case 'connecting':
            return 'Connecting'
        case 'reconnecting':
            return 'Reconnecting'
        case 'disconnected':
            return 'Disconnected'
        case 'connected':
            if (props.secondsSinceLastUpdate === null) {
                return 'Waiting for data'
            }

            return props.isStale ? 'Stale' : 'Live'
    }
})

const updatedLabel = computed(() => {
    const seconds = props.secondsSinceLastUpdate

    if (seconds === null) {
        return 'Waiting for the first snapshot'
    }

    if (seconds === 0) {
        return 'Updated just now'
    }

    return `Updated ${seconds} ${seconds === 1 ? 'second' : 'seconds'} ago`
})
</script>

<template>
    <header class="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
            <h1 class="text-2xl font-medium tracking-tight">BTC / EUR</h1>
            <p class="mt-1 text-sm text-muted-foreground">Order book · Bitstamp</p>
        </div>

        <div class="flex flex-wrap items-center gap-3 sm:pt-1">
            <Badge variant="outline" class="gap-2 bg-card">
                <span
                    class="size-1.5 rounded-full"
                    :class="isLive ? 'bg-emerald-600' : 'bg-amber-600'"
                />
                {{ statusLabel }}
            </Badge>
            <span class="text-xs tabular-nums text-muted-foreground">{{ updatedLabel }}</span>
        </div>
    </header>
</template>
