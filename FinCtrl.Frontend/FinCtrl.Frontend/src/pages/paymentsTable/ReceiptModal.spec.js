import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'

vi.mock('axios', () => ({ default: { get: vi.fn() } }))
vi.mock('bootstrap', () => ({ Modal: { getOrCreateInstance: () => ({ show: vi.fn(), hide: vi.fn() }) } }))

const axios = (await import('axios')).default
const ReceiptModal = (await import('./ReceiptModal.vue')).default

const payment = { paymentId: 5, paymentSum: -300, paymentDate: '2026-10-02T12:30:00' }

const receipt = {
    paymentId: 5,
    totalSum: 300,
    items: [
        { receiptItemId: 1, itemName: 'Бананы', price: 100, quantity: 0.624, unitId: 1, unitName: 'кг', totalPrice: 62.4 },
        { receiptItemId: 2, itemName: 'Молоко', price: 118.8, quantity: 2, unitId: 2, unitName: 'шт', totalPrice: 237.6 }
    ]
}

const openWith = async (data, p = payment) => {
    axios.get.mockResolvedValueOnce({ data })
    const wrapper = mount(ReceiptModal)
    wrapper.vm.open(p)
    await flushPromises()
    return wrapper
}

describe('ReceiptModal', () => {
    beforeEach(() => axios.get.mockReset())

    it('рисует позиции, количество с единицей и ИТОГ', async () => {
        const wrapper = await openWith(receipt)
        expect(axios.get.mock.calls[0][0]).toContain('/api/PaymentsApi/5/receipt')
        const items = wrapper.findAll('.receipt-item')
        expect(items).toHaveLength(2)
        expect(items[0].text()).toContain('Бананы')
        expect(items[0].text()).toContain('0,624 кг ×')
        expect(items[1].text()).toContain('2 шт ×')
        expect(wrapper.find('.receipt-total').text()).toContain('ИТОГ')
        expect(wrapper.find('.receipt-total-sum').text()).toContain('300,00')
        expect(wrapper.text()).toContain('Кассовый чек · #5')
    })

    it('при совпадении суммы строки «Сумма операции» нет, при расхождении — есть', async () => {
        const same = await openWith(receipt)
        expect(same.find('.receipt-payment-sum').exists()).toBe(false)

        const diff = await openWith(receipt, { ...payment, paymentSum: -350 })
        expect(diff.find('.receipt-payment-sum').text()).toContain('Сумма операции')
        expect(diff.find('.receipt-payment-sum').text()).toContain('350,00')
    })

    it('пустой чек — текст «У этой операции нет чека»', async () => {
        const wrapper = await openWith({ paymentId: 5, totalSum: 0, items: [] })
        expect(wrapper.text()).toContain('У этой операции нет чека')
        expect(wrapper.find('.receipt-total').exists()).toBe(false)
    })

    it('404 — detail показывается в алерте', async () => {
        axios.get.mockRejectedValueOnce({ response: { status: 404, data: { detail: 'Операция 5 не найдена' } } })
        const wrapper = mount(ReceiptModal)
        wrapper.vm.open(payment)
        await flushPromises()
        expect(wrapper.find('.alert').text()).toContain('Операция 5 не найдена')
    })

    it('устаревший ответ игнорируется', async () => {
        let resolveFirst
        axios.get.mockReturnValueOnce(new Promise(r => { resolveFirst = r }))
        axios.get.mockResolvedValueOnce({ data: { paymentId: 6, totalSum: 0, items: [] } })
        const wrapper = mount(ReceiptModal)
        wrapper.vm.open(payment)
        wrapper.vm.open({ ...payment, paymentId: 6 })
        await flushPromises()
        resolveFirst({ data: receipt })
        await flushPromises()
        expect(wrapper.text()).toContain('У этой операции нет чека')
        expect(wrapper.findAll('.receipt-item')).toHaveLength(0)
    })
})
