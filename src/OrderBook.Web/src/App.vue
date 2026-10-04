<script setup lang="ts">
import { useOrderBook } from '@/composables/useOrderBook'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'

const { snapshot, status, error, isStale } = useOrderBook()
</script>

<template>
  <main class="mx-auto p-6">
    <Card>
      <CardContent class="space-y-4">
        <div class="flex flex-wrap gap-2">
          <Badge variant="outline">Connection: {{ status }}</Badge>
          <Badge v-if="isStale" variant="destructive">Stale data</Badge>
        </div>
        <dl v-if="snapshot" class="grid grid-cols-2">
          <div>
            <dt>Snapshot sequence</dt>
            <dd>{{ snapshot.sequence }}</dd>
          </div>
          <div>
            <dt>Acquired at (UTC)</dt>
            <dd>{{ snapshot.acquiredAtUtc }}</dd>
          </div>
          <div>
            <dt>Bid levels</dt>
            <dd>{{ snapshot.orderBook.bids.length }}</dd>
          </div>
          <div>
            <dt>Ask levels</dt>
            <dd>{{ snapshot.orderBook.asks.length }}</dd>
          </div>
        </dl>

        <p v-else>Waiting for the first snapshot…</p>
        <p v-if="error">{{ error }}</p>
      </CardContent>
    </Card>
  </main>
</template>
