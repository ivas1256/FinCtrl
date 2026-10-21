import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import PaymentCategoryRow from './PaymentCategoryRow.vue'

const periods = [
    { key: 'p1', name: '2026-09-01 - 2026-10-01' },
    { key: 'p2', name: '2026-10-01 - 2026-11-01' }
]

const childPayments = [{ paymentId: 7 }]
const tree = {
    categoryId: 1,
    categoryName: 'Еда',
    periods: { p1: { totalSum: -300, payments: [] }, p2: { totalSum: -100, payments: [] } },
    children: [
        {
            categoryId: 2,
            categoryName: 'Продукты',
            periods: { p1: { totalSum: -300, payments: childPayments } },
            children: []
        }
    ]
}

// Компонент рендерит фрагмент из <tr> — монтируем внутрь <tbody>
const mountRow = (props = {}) =>
    mount(PaymentCategoryRow, {
        props: { category: tree, periods, maxValue: 300, ...props },
        attachTo: document.createElement('tbody')
    })

const categoryNames = (wrapper) =>
    wrapper.findAll('.hm-cat-cell').map(cell => cell.text().replace(/[+−]/g, '').trim())

describe('PaymentCategoryRow', () => {
    it('рендерит строку категории и, рекурсивно, развёрнутых детей', () => {
        const wrapper = mountRow()
        expect(categoryNames(wrapper)).toEqual(['Еда', 'Продукты'])
        expect(wrapper.findAll('tr')[0].findAll('.hm-cell')).toHaveLength(2)
    })

    it('кнопка сворачивает и разворачивает детей', async () => {
        const wrapper = mountRow()
        const toggle = wrapper.find('.expand-btn')
        expect(toggle.text()).toBe('−')

        await toggle.trigger('click')
        expect(categoryNames(wrapper)).toEqual(['Еда'])
        expect(toggle.text()).toBe('+')

        await toggle.trigger('click')
        expect(categoryNames(wrapper)).toEqual(['Еда', 'Продукты'])
    })

    it('клик по ячейке дочерней строки всплывает cell-click с данными ячейки', async () => {
        const wrapper = mountRow()
        await wrapper.findAll('tr')[1].findAll('.hm-cell')[0].trigger('click')
        expect(wrapper.emitted('cell-click')).toEqual([
            [childPayments, 'Продукты', periods[0].name, -300, 2, 'p1']
        ])
    })

    it('ячейка без данных отдаёт пустой список и нулевую сумму', async () => {
        const wrapper = mountRow()
        await wrapper.findAll('tr')[1].findAll('.hm-cell')[1].trigger('click')
        expect(wrapper.emitted('cell-click')[0]).toEqual([[], 'Продукты', periods[1].name, 0, 2, 'p2'])
    })

    it('подсвечивает выбранную ячейку и раскрашивает суммы', () => {
        const wrapper = mountRow({ selectedCategoryId: 1, selectedPeriodKey: 'p2' })
        const cells = wrapper.findAll('tr')[0].findAll('.hm-cell')
        expect(cells[0].classes()).not.toContain('selected')
        expect(cells[1].classes()).toContain('selected')
        expect(cells[0].classes()).toContain('hm-hi') // 300 из 300 — насыщенный расход
        expect(cells[0].text().replace(/\s+/g, ' ')).toBe('-300,00 ₽')
    })
})
