import { HubConnectionBuilder } from '@microsoft/signalr'
import { computed, onMounted, onUnmounted, ref, shallowRef } from 'vue'
import type { OrderBookSnapshot } from '@/types/orderBook'

// constants
const SNAPSHOT_EVENT = 'OrderBookUpdated' // OrderBookUpdated as defined on the API OrderBookHub.cs
const RETRY_DELAY_MS = 5000
const STALE_THRESHOLD_MS = 3000  // stale status defind at 3s for now

// for UI
export type ConnectionStatus =
    | 'connecting'
    | 'connected'
    | 'reconnecting'
    | 'disconnected'

export function useOrderBook() {
    const snapshot = shallowRef<OrderBookSnapshot | null>(null)
    const status = ref<ConnectionStatus>('connecting')
    const error = ref<string | null>(null)
    const now = ref(Date.now())

    let lastSequence = 0
    let disposed = false
    let retryTimer: number | undefined
    let clockTimer: number | undefined

    // connect to the hub
    const connection = new HubConnectionBuilder()
        .withUrl('/hubs/order-book')
        .withAutomaticReconnect()
        .build()

    connection.on(SNAPSHOT_EVENT, (incoming: OrderBookSnapshot) => {
        // do not update if disposed or old/duplicate data
        if (disposed || incoming.sequence <= lastSequence) {
            return
        }

        lastSequence = incoming.sequence
        snapshot.value = incoming
        now.value = Date.now()
    })

    function scheduleRetry() {
        if (disposed || retryTimer !== undefined) {
            return
        }

        retryTimer = window.setTimeout(() => {
            retryTimer = undefined
            void start()
        }, RETRY_DELAY_MS)
    }

    async function start() {
        if (disposed) {
            return
        }

        lastSequence = 0
        status.value = 'connecting'
        error.value = null

        try {
            await connection.start()

            if (!disposed) {
                status.value = 'connected'
                error.value = null
            }
        } catch (exception) {
            if (disposed) {
                return
            }

            status.value = 'disconnected'
            error.value = exception instanceof Error
                ? exception.message
                : 'Unable to connect to the order book.'

            scheduleRetry()
        }
    }

    // updates status to reconnecting on reconnect
    connection.onreconnecting(() => {
        if (disposed) {
            return
        }

        // api restart causes a sequence reset - set it to 0
        lastSequence = 0
        status.value = 'reconnecting'
        error.value = 'Connection lost. Reconnecting…'
    })

    // updates status to connected on reconnect
    connection.onreconnected(() => {
        if (disposed) {
            return
        }

        status.value = 'connected'
        error.value = null
    })

    // updates status to disconnected on close
    connection.onclose(() => {
        if (disposed) {
            return
        }

        status.value = 'disconnected'
        error.value = 'Connection closed. Retrying…'
        scheduleRetry()
    })

    const secondsSinceLastUpdate = computed(() => {
        if (snapshot.value === null) {
            return null
        }

        const acquiredAt = Date.parse(snapshot.value.acquiredAtUtc)
        return Math.max(0, Math.floor((now.value - acquiredAt) / 1000))
    })

    // returns stale status
    const isStale = computed(() => {
        if (snapshot.value === null) {
            return false
        }

        const acquiredAt = Date.parse(snapshot.value.acquiredAtUtc)
        return now.value - acquiredAt > STALE_THRESHOLD_MS
    })

    // timer setup on mount
    onMounted(() => {
        clockTimer = window.setInterval(() => {
            now.value = Date.now()
        }, 1000)

        void start()
    })

    // cleanup
    onUnmounted(() => {
        disposed = true

        if (retryTimer !== undefined) {
            window.clearTimeout(retryTimer)
        }

        if (clockTimer !== undefined) {
            window.clearInterval(clockTimer)
        }

        void connection.stop().catch((exception) => {
            console.error('Failed to stop the order-book connection.', exception)
        })
    })

    return {
        snapshot,
        status,
        error,
        isStale,
        secondsSinceLastUpdate,
    }
}
