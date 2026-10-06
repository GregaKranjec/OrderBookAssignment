import type { BitstampOrderBook, OrderBookPriceLevel } from '@/types/orderBook'

export const DEPTH_PRICE_RANGE = 0.25 // Display prices within 25% of the mid-price.

interface PriceLevel {
    price: number
    quantity: number
    side: 'bid' | 'ask'
}

export interface CumulativeDepthLevel extends PriceLevel {
    cumulativeQuantity: number
}

// Parses prices | quantities on each level and filters out invalid values
function parseLevels(levels: OrderBookPriceLevel[], side: PriceLevel['side']): PriceLevel[] {
    return levels
        .map(([price, quantity]) => ({ price: Number(price), quantity: Number(quantity), side }))
        .filter(level =>
            Number.isFinite(level.price) && level.price > 0
            && Number.isFinite(level.quantity) && level.quantity > 0,
        )
}

// Convert all price level values into cumulative BTC quantities
function accumulateLevels(levels: PriceLevel[]): CumulativeDepthLevel[] {
    let cumulativeQuantity = 0

    return levels.map(level => {
        cumulativeQuantity += level.quantity
        return { ...level, cumulativeQuantity }
    })
}

// Accumulates from the best price outwards, then shows every level within the price range.
export function prepareOrderBookDepth(orderBook: BitstampOrderBook | null) {
    const bidLevels = parseLevels(orderBook?.bids ?? [], 'bid')
        .sort((left, right) => right.price - left.price)
    const askLevels = parseLevels(orderBook?.asks ?? [], 'ask')
        .sort((left, right) => left.price - right.price)

    // Both sides start with their best price, before reversing bids for display.
    const bestBid = bidLevels[0]
    const bestAsk = askLevels[0]
    const bestLevel = bestBid ?? bestAsk

    // No snapshot, an empty book, or only invalid levels all produce an empty chart.
    if (bestLevel === undefined) {
        return {
            bids: [],
            asks: [],
            priceDomain: undefined,
            levels: [],
            totalBidQuantity: 0,
            totalAskQuantity: 0,
        }
    }

    // Use the available side's best price if the other side is empty.
    const midpoint = bestBid !== undefined && bestAsk !== undefined
        ? (bestBid.price + bestAsk.price) / 2
        : bestLevel.price
    const priceDomain: [number, number] = [
        midpoint * (1 - DEPTH_PRICE_RANGE),
        midpoint * (1 + DEPTH_PRICE_RANGE),
    ]

    // Convert all bids and asks into cumulative BTC quantities.
    const allBids = accumulateLevels(bidLevels).reverse()
    const allAsks = accumulateLevels(askLevels)
    const inRange = (level: CumulativeDepthLevel) =>
        level.price >= priceDomain[0] && level.price <= priceDomain[1]
    const bids = allBids.filter(inRange)
    const asks = allAsks.filter(inRange)

    return {
        bids,
        asks,
        priceDomain,
        levels: [...bids, ...asks].sort((left, right) => left.price - right.price),
        totalBidQuantity: bids[0]?.cumulativeQuantity ?? 0,
        totalAskQuantity: asks.at(-1)?.cumulativeQuantity ?? 0,
    }
}
