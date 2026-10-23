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

    it('«Без категории» выводится приглушённым именем обычного веса', () => {
        const uncategorized = { categoryId: 2147483646, categoryName: 'Без категории', periods: {}, children: [] }
        const wrapper = mountRow({ category: uncategorized })
        expect(wrapper.find('.hm-cat-name-muted').text()).toBe('Без категории')
        expect(wrapper.find('.hm-cat-name-root').exists()).toBe(false)
    })

    it('без comparison колонки Δ нет', () => {
        expect(mountRow().find('.hm-delta').exists()).toBe(false)
    })

    it('колонка Δ есть у родителя и ребёнка, родитель — по своим суммам', () => {
        const withComparison = {
            ...tree,
            comparison: { previous: -4600, current: -3100 },
            children: [{ ...tree.children[0], comparison: { previous: -3100, current: -2300 } }]
        }
        const wrapper = mountRow({ category: withComparison, showDelta: true })
        const deltas = wrapper.findAll('.hm-delta')
        expect(deltas).toHaveLength(2)
        expect(deltas[0].find('.hm-delta-rub').text()).toBe('−1 500 ₽')
        expect(deltas[1].find('.hm-delta-rub').text()).toBe('−800 ₽')
    })

    it('свёрнутый родитель показывает свою Δ', async () => {
        const wrapper = mountRow({ category: { ...tree, comparison: { previous: -4600, current: -3100 } }, showDelta: true })
        await wrapper.find('.expand-btn').trigger('click')
        expect(wrapper.findAll('.hm-delta')).toHaveLength(1)
        expect(wrapper.find('.hm-delta-rub').text()).toBe('−1 500 ₽')
    })

    it('доходная строка и потомки «Накоплений» — инвертированный цвет Δ', () => {
        const income = { categoryId: 9, categoryName: 'Зарплата', isOnlyIncome: true, periods: {}, children: [],
            comparison: { previous: 100000, current: 110000 } }
        expect(mountRow({ category: income, showDelta: true }).find('.hm-delta').classes()).toContain('hm-delta-good')

        const savings = { categoryId: 33, categoryName: 'Накопления', periods: {}, comparison: { previous: -1000, current: -2000 },
            children: [{ categoryId: 34, categoryName: 'Вклад', periods: {}, children: [], comparison: { previous: -1000, current: -2000 } }] }
        const deltas = mountRow({ category: savings, showDelta: true }).findAll('.hm-delta')
        expect(deltas.map(d => d.classes().includes('hm-delta-good'))).toEqual([true, true])
    })

    it('клик по ячейке месяца по-прежнему открывает платежи при колонке Δ', async () => {
        const wrapper = mountRow({ showDelta: true })
        await wrapper.findAll('tr')[1].findAll('.hm-cell')[0].trigger('click')
        expect(wrapper.emitted('cell-click')).toHaveLength(1)
    })
})
