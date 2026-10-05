import type { BitstampOrderBook, OrderBookPriceLevel } from '@/types/orderBook'

export const VISIBLE_LEVELS_PER_SIDE = 50 // number of levels to display on each side of the order book per price line depth chart
export const DEPTH_PRICE_RANGE = 0.25// price range of 10% to hide values out of range and provide a better overview on comulative chart

export interface ChartPriceLevel {
    price: number
    quantity: number
    side: 'bid' | 'ask'
}

export interface CumulativeDepthLevel extends ChartPriceLevel {
    cumulativeQuantity: number
}

// Parses prices | quantities on each level and filters out invalid values
function parseLevels(levels: OrderBookPriceLevel[], side: ChartPriceLevel['side']): ChartPriceLevel[] {
    return levels
        .map(([price, quantity]) => ({ price: Number(price), quantity: Number(quantity), side }))
        .filter(level =>
            Number.isFinite(level.price) && level.price > 0
            && Number.isFinite(level.quantity) && level.quantity > 0,
        )
}

// Prepares numeric values for display only
export function prepareOrderBookChart(orderBook: BitstampOrderBook | null, levelsPerSide = VISIBLE_LEVELS_PER_SIDE) {
    // parse levels for both and slice to show only within visible level range
    // bids and asks should come from bitstamp already sorted - but redudantly sorting through javascript shouldn't be majorly expensive
    const bids = parseLevels(orderBook?.bids ?? [], 'bid')
        .sort((left, right) => right.price - left.price)
        .slice(0, levelsPerSide)
    const asks = parseLevels(orderBook?.asks ?? [], 'ask')
        .sort((left, right) => left.price - right.price)
        .slice(0, levelsPerSide)

    return {
        levels: [...bids, ...asks].sort((left, right) => left.price - right.price),
        bestBid: bids[0]?.price ?? null,
        bestAsk: asks[0]?.price ?? null,
    }
}

// Convert all price level values into cumulative BTC quantities
function accumulateLevels(levels: ChartPriceLevel[]): CumulativeDepthLevel[] {
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
