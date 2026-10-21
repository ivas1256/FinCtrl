import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import PaymentList from './PaymentList.vue'

const payment = (overrides) => ({
    paymentId: 1,
    paymentDate: '2026-10-07T12:00:00',
    paymentSum: -100,
    paymentType: 1,
    paymentSource: { paymentSourceName: 'Магазин', description: 'у дома' },
    effectiveCategory: { categoryName: 'Еда' },
    paymentDescription: 'Продукты',
    tags: [],
    ...overrides
})

const normalize = (text) => text.replace(/\s+/g, ' ').trim()

describe('PaymentList', () => {
    it('группирует платежи по дню и считает итог дня', () => {
        const wrapper = mount(PaymentList, {
            props: {
                payments: [
                    payment({ paymentId: 1, paymentSum: -100 }),
                    payment({ paymentId: 2, paymentDate: '2026-10-07T18:30:00', paymentSum: -50.5 }),
                    payment({ paymentId: 3, paymentDate: '2026-10-08T09:00:00', paymentSum: -10 })
                ]
            }
        })
        const groups = wrapper.findAll('.payment-date-group')
        expect(groups).toHaveLength(2)
        expect(groups[0].findAll('.payment-item')).toHaveLength(2)
        expect(normalize(groups[0].find('.date-header small').text())).toBe('-150,50 ₽')
        expect(groups[1].findAll('.payment-item')).toHaveLength(1)
    })

    it('показывает источник с описанием, категорию, описание и теги', () => {
        const wrapper = mount(PaymentList, {
            props: { payments: [payment({ tags: [{ tagId: 5, tagName: 'отпуск' }] })] }
        })
        expect(normalize(wrapper.find('.payment-source').text())).toBe('Магазин у дома')
        expect(wrapper.find('.payment-category').text()).toContain('Еда')
        expect(wrapper.find('.payment-description').text()).toBe('Продукты')
        expect(wrapper.find('.badge').text()).toBe('отпуск')
    })

    it('платеж без источника и категории не роняет список', () => {
        const wrapper = mount(PaymentList, {
            props: { payments: [payment({ paymentSource: null, effectiveCategory: null, paymentDescription: '' })] }
        })
        expect(normalize(wrapper.find('.payment-source').text())).toBe('Не указан')
        expect(wrapper.find('.payment-category').text()).toContain('Без категории')
        expect(wrapper.find('.payment-description').text()).toBe('Нет описания')
    })

    it('цвет суммы зависит от типа платежа', () => {
        const wrapper = mount(PaymentList, {
            props: { payments: [payment({ paymentId: 1 }), payment({ paymentId: 2, paymentType: 2, paymentSum: 500 })] }
        })
        const amounts = wrapper.findAll('.payment-amount')
        expect(amounts[0].classes()).toContain('text-danger')
        expect(amounts[1].classes()).toContain('text-success')
    })

    it('кнопка правки отдаёт платеж в edit-payment', async () => {
        const item = payment({ paymentId: 42 })
        const wrapper = mount(PaymentList, { props: { payments: [item] } })
        await wrapper.find('button').trigger('click')
        expect(wrapper.emitted('edit-payment')).toEqual([[item]])
    })
})
