import assert from 'node:assert/strict'
import { test } from 'node:test'
import { calculateOrderBookQuote } from '../src/lib/orderBookQuote.ts'
import type { OrderBookPriceLevel } from '../src/types/orderBook.ts'

// run in OrderBook.web as npm test

test('Calculates a purchase within a single ask level', () => {
    const quote = calculateOrderBookQuote('0.5', [['60000.00', '1']])

    assert.ok(quote.status === 'quoted')
    assert.equal(quote.total.toString(), '30000')
    assert.equal(quote.averagePrice.toString(), '60000')
})

test('Buys the cheapest asks first and uses only the needed part of the final level', () => {
    const asks: OrderBookPriceLevel[] = [['60010.00', '1'], ['60000.00', '0.3']]
    const quote = calculateOrderBookQuote('0.5', asks)

    assert.ok(quote.status === 'quoted')
    assert.equal(quote.total.toString(), '30002')
    assert.equal(quote.averagePrice.toString(), '60004')
    assert.deepEqual(asks, [['60010.00', '1'], ['60000.00', '0.3']])
})

test('Reports insufficient liquidity instead of returning a partial quote', () => {
    assert.deepEqual(calculateOrderBookQuote('1', [['60000', '0.5']]), { status: 'amount-too-large' })
    assert.deepEqual(calculateOrderBookQuote('1', []), { status: 'amount-too-large' })
})

test('Handles empty input, invalid amounts and a decimal comma', () => {
    assert.deepEqual(calculateOrderBookQuote(' ', []), { status: 'empty' })

    for (const amount of ['0', '-1', 'abc', 'NaN', 'Infinity', '1e3', '0.000000001', '1,2.3']) {
        assert.equal(calculateOrderBookQuote(amount, []).status, 'invalid', amount)
    }

    const quote = calculateOrderBookQuote('0,5', [['60000', '1']])
    assert.ok(quote.status === 'quoted')
    assert.equal(quote.total.toString(), '30000')
})

test('Rejects unusable ask values rather than producing a misleading quote', () => {
    for (const price of ['a-random-string', 'NaN', 'Infinity', '0', '-1']) {
        assert.deepEqual(calculateOrderBookQuote('1', [[price, '1']]), { status: 'unavailable' })
    }

    assert.deepEqual(calculateOrderBookQuote('1', [['60000', 'NaN']]), { status: 'unavailable' })
})
