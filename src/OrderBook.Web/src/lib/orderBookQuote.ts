import Decimal from 'decimal.js'
import type { OrderBookPriceLevel } from '../types/orderBook'

// define a decimal for the quote
const QuoteDecimal = Decimal.clone({ precision: 40, rounding: Decimal.ROUND_HALF_EVEN })
const BTC_AMOUNT_PATTERN = /^(?:\d+(?:[.,]\d{0,8})?|[.,]\d{1,8})$/

export type OrderBookQuote =
    | { status: 'empty' }
    | { status: 'invalid'; message: string }
    | { status: 'unavailable' }
    | { status: 'amount-too-large' }
    | { status: 'quoted'; total: Decimal; averagePrice: Decimal }

// Estimates the cost of buying BTC from the snapshot's full ask side, excluding fees.
export function calculateOrderBookQuote(amount: string, asks: OrderBookPriceLevel[]): OrderBookQuote {
    const input = amount.trim()

    if (input === '') {
        return { status: 'empty' }
    }

    if (!BTC_AMOUNT_PATTERN.test(input)) {
        return { status: 'invalid', message: 'Enter a positive BTC amount with up to 8 decimal places.' }
    }

    // we allow for comma, replace it with dot for calculation
    const requestedQuantity = new QuoteDecimal(input.replace(',', '.'))

    if (requestedQuantity.isZero()) {
        return { status: 'invalid', message: 'Enter a BTC amount greater than 0.' }
    }

    // Use decimal values from the original snapshot
    let levels: { price: Decimal; quantity: Decimal }[]

    try {
        levels = asks.map(([price, quantity]) => ({
            price: new QuoteDecimal(price),
            quantity: new QuoteDecimal(quantity),
        }))
    } catch {
        return { status: 'unavailable' }
    }

    // validate prices at each level
    if (levels.some(level => !level.price.isFinite() || level.price.lte(0)
        || !level.quantity.isFinite() || level.quantity.lte(0))) {
        return { status: 'unavailable' }
    }

    levels.sort((left, right) => left.price.comparedTo(right.price))

    let remaining = requestedQuantity
    let total = new QuoteDecimal(0)

    for (const level of levels) {
        const quantity = QuoteDecimal.min(remaining, level.quantity)
        total = total.plus(quantity.times(level.price))
        remaining = remaining.minus(quantity)

        if (remaining.isZero()) {
            return {
                status: 'quoted',
                total,
                averagePrice: total.dividedBy(requestedQuantity),
            }
        }
    }

    return { status: 'amount-too-large' }
}
