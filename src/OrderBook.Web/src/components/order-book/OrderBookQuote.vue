<script setup lang="ts">
import { computed, ref } from 'vue'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { calculateOrderBookQuote } from '@/lib/orderBookQuote'
import type { BitstampOrderBook } from '@/types/orderBook'

const props = defineProps<{
    orderBook: BitstampOrderBook | null
    isOutdated: boolean
}>()

const amount = ref('')
const quote = computed(() => calculateOrderBookQuote(amount.value, props.orderBook?.asks ?? []))

const BTC_INPUT_PATTERN = /^\d*(?:[.,]\d{0,8})?$/

function preventInvalidInput(event: InputEvent | ClipboardEvent) {
    const input = event.target as HTMLInputElement

  // take input from clipboard if pasted
    const text = event instanceof ClipboardEvent
        ? event.clipboardData?.getData('text')
        : event.data

    // allow for clearing the input field
    if (text === null || text === undefined) return

    // prevent next input from being invalid
    const start = input.selectionStart ?? input.value.length
    const end = input.selectionEnd ?? start
    const nextValue = input.value.slice(0, start) + text + input.value.slice(end)

    if (!BTC_INPUT_PATTERN.test(nextValue)) {
        event.preventDefault()
    }
}
</script>

<template>
    <Card>
        <CardHeader>
            <CardTitle id="purchase-quote-title" class="uppercase">Purchase quote</CardTitle>
        </CardHeader>

        <CardContent class="space-y-5">
            <div class="max-w-sm space-y-2">
                <Label for="btc-amount">Amount (BTC)</Label>
                <Input
                    id="btc-amount"
                    v-model="amount"
                    type="text"
                    inputmode="decimal"
                    autocomplete="off"
                    placeholder="0.00"
                    @beforeinput="preventInvalidInput"
                    @paste="preventInvalidInput"
                />
            </div>

            <p v-if="quote.status === 'invalid'" id="quote-message" class="text-sm text-destructive">
                {{ quote.message }}
            </p>
            <p v-else-if="quote.status === 'empty'" id="quote-message" class="text-sm text-muted-foreground">
                Enter a BTC amount to see its estimated cost.
            </p>
            <p v-else-if="orderBook === null" id="quote-message" class="text-sm text-muted-foreground">
                Waiting for order-book data...
            </p>
            <p v-else-if="quote.status === 'unavailable'" id="quote-message" class="text-sm text-muted-foreground">
                Unable to calculate a quote at the moment.
            </p>
            <p v-else-if="quote.status === 'amount-too-large'" id="quote-message" class="text-sm text-muted-foreground">
                Amount too large, insufficient liquidity.
            </p>
            <div v-else-if="quote.status === 'quoted'" class="space-y-2">
                <dl class="space-y-2">
                    <div>
                        <dt class="text-sm text-muted-foreground">Estimated total (EUR)</dt>
                        <dd class="break-words text-3xl font-medium tabular-nums">€{{ quote.total.toFixed(2) }}</dd>
                    </div>
                    <div class="text-sm">
                        <dt class="inline text-muted-foreground">Average price per BTC: </dt>
                        <dd class="inline tabular-nums">€{{ quote.averagePrice.toFixed(2) }}</dd>
                    </div>
                </dl>
            </div>

            <p v-if="orderBook !== null && isOutdated && quote.status !== 'empty' && quote.status !== 'invalid'"
                class="text-sm text-amber-700">
                Order-book data is outdated. The estimate will update once fresh data arrives.
            </p>
        </CardContent>
    </Card>
</template>
