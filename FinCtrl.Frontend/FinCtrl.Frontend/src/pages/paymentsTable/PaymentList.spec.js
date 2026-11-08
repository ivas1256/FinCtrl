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

    it('у перевода направление — первая строка карточки, отдельного счёта нет', () => {
        const wrapper = mount(PaymentList, {
            props: { payments: [payment({ paymentType: 2, transferPairAccount: { displayName: 'Вклад' } })] }
        })
        const item = wrapper.find('.payment-item').element
        expect(item.firstElementChild.classList.contains('payment-transfer')).toBe(true)
        expect(wrapper.find('.payment-account').exists()).toBe(false)
    })

    it('у обычного платежа строки перевода нет, выводится счёт', () => {
        const wrapper = mount(PaymentList, { props: { payments: [payment()] } })
        expect(wrapper.find('.payment-transfer').exists()).toBe(false)
        expect(wrapper.find('.payment-account').text()).toBe('Счёт: Карта')
    })

    it('у платежа без счёта выводится «не указан»', () => {
        const wrapper = mount(PaymentList, { props: { payments: [payment({ account: null })] } })
        expect(wrapper.find('.payment-account').text()).toBe('Счёт: не указан')
    })
})
