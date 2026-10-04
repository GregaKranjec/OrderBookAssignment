/**
 * One price level from Bitstamp.
 * Values remain strings to preserve their decimal representation.
 */
export type OrderBookPriceLevel = [price: string, quantity: string]

export interface BitstampOrderBook {
    timestamp: string
    microtimestamp?: string
    bids: OrderBookPriceLevel[]
    asks: OrderBookPriceLevel[]
}

export interface OrderBookSnapshot {
    snapshotId: string
    sequence: number
    market: string
    acquiredAtUtc: string
    orderBook: BitstampOrderBook
}