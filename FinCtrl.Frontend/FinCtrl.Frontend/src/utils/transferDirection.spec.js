import { describe, it, expect } from 'vitest'
import { getTransferDirection } from './transferDirection'

const transfer = (extra) => ({
    paymentType: 2,
    paymentSum: -100,
    account: { displayName: 'Карта' },
    transferPairAccount: { displayName: 'Вклад' },
    ...extra
})

describe('getTransferDirection', () => {
    it('списание: свой счёт → счёт пары', () => {
        expect(getTransferDirection(transfer())).toBe('Карта → Вклад')
    })

    it('поступление: счёт пары → свой счёт', () => {
        expect(getTransferDirection(transfer({ paymentSum: 100 }))).toBe('Вклад → Карта')
    })

    it('без пары: «не указан» вместо счёта пары', () => {
        expect(getTransferDirection(transfer({ transferPairAccount: null }))).toBe('Карта → не указан')
    })

    it('платёж без счёта', () => {
        expect(getTransferDirection(transfer({ account: null }))).toBe('не указан → Вклад')
    })

    it('обычный платёж — null', () => {
        expect(getTransferDirection(transfer({ paymentType: 1 }))).toBeNull()
    })
})
