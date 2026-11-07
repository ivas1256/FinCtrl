import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import PaymentList from './PaymentList.vue'

const payment = (extra) => ({
    paymentId: 1,
    paymentDate: '2025-01-10T00:00:00',
    paymentSum: -100,
    paymentType: 1,
    paymentSource: { paymentSourceName: 'Магазин' },
    account: { displayName: 'Карта' },
    transferPairAccount: null,
    tags: [],
    ...extra
})

describe('PaymentList', () => {
    it('у перевода выводит «Счёт A → Счёт B»', () => {
        const wrapper = mount(PaymentList, {
            props: { payments: [payment({ paymentType: 2, transferPairAccount: { displayName: 'Вклад' } })] }
        })
        expect(wrapper.find('.payment-transfer').text()).toBe('Карта → Вклад')
    })

    it('у обычного платежа строки нет', () => {
        const wrapper = mount(PaymentList, { props: { payments: [payment()] } })
        expect(wrapper.find('.payment-transfer').exists()).toBe(false)
    })
})
