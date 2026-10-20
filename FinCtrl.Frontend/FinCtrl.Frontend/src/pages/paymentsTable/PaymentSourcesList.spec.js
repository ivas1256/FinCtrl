import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import PaymentSourcesList from './PaymentSourcesList.vue'

const source = (id, name) => ({ paymentSourceId: id, paymentSourceName: name })

const payments = [
    { paymentSum: -100, paymentSource: source(1, 'Магазин') },
    { paymentSum: -300, paymentSource: source(2, 'Заправка') },
    { paymentSum: -50, paymentSource: source(1, 'Магазин') },
    { paymentSum: -20, paymentSource: null }
]

const names = (wrapper) => wrapper.findAll('.list-group-item .fw-medium').map(el => el.text())
const normalize = (text) => text.replace(/\s+/g, ' ').trim()

describe('PaymentSourcesList', () => {
    it('без платежей показывает пустое состояние', () => {
        const wrapper = mount(PaymentSourcesList, { props: { payments: [] } })
        expect(wrapper.text()).toContain('Нет данных по источникам')
    })

    it('группирует платежи по источнику: сумма и количество', () => {
        const wrapper = mount(PaymentSourcesList, { props: { payments } })
        const items = wrapper.findAll('.list-group-item')
        expect(items).toHaveLength(3)
        const shop = items.find(item => item.text().includes('Магазин'))
        expect(shop.text()).toContain('2 платеж(ей)')
        expect(normalize(shop.find('.payment-amount').text())).toBe('-150,00 ₽')
        expect(names(wrapper)).toContain('Без источника')
    })

    it('по умолчанию сортирует по возрастанию суммы, кнопка переключает порядок', async () => {
        const wrapper = mount(PaymentSourcesList, { props: { payments } })
        expect(names(wrapper)).toEqual(['Заправка', 'Магазин', 'Без источника'])
        expect(wrapper.find('button').text()).toContain('↑')

        await wrapper.find('button').trigger('click')
        expect(names(wrapper)).toEqual(['Без источника', 'Магазин', 'Заправка'])
        expect(wrapper.find('button').text()).toContain('↓')
    })

    it('показывает итог по всем источникам', () => {
        const wrapper = mount(PaymentSourcesList, { props: { payments } })
        expect(normalize(wrapper.find('.border-top .fw-bold span:last-child').text())).toBe('-470,00 ₽')
    })
})
