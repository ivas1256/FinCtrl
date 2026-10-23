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
    wrapper.findAll('.hm-cat-cell').map(cell => cell.text().replace(/[▸▾]/g, '').trim())

describe('PaymentCategoryRow', () => {
    it('рендерит строку категории и, рекурсивно, развёрнутых детей', () => {
        const wrapper = mountRow()
        expect(categoryNames(wrapper)).toEqual(['Еда', 'Продукты'])
        expect(wrapper.findAll('tr')[0].findAll('.hm-cell')).toHaveLength(2)
    })

    it('по умолчанию раскрыто; клик по имени сворачивает и разворачивает детей, шеврон и aria-expanded меняются', async () => {
        const wrapper = mountRow()
        const toggle = wrapper.find('.hm-cat-toggle')
        expect(toggle.attributes('aria-expanded')).toBe('true')
        expect(toggle.find('.hm-chevron').text()).toBe('▾')
        expect(toggle.text()).toContain('Еда')

        await toggle.trigger('click')
        expect(categoryNames(wrapper)).toEqual(['Еда'])
        expect(toggle.attributes('aria-expanded')).toBe('false')
        expect(toggle.find('.hm-chevron').text()).toBe('▸')

        await toggle.trigger('click')
        expect(categoryNames(wrapper)).toEqual(['Еда', 'Продукты'])
    })

    it('у листа нет кнопки, есть пустой плейсхолдер шеврона', () => {
        const wrapper = mountRow()
        const leafCell = wrapper.findAll('tr')[1].find('.hm-cat-cell')
        expect(leafCell.find('button').exists()).toBe(false)
        expect(leafCell.find('.hm-chevron').text()).toBe('')
        expect(leafCell.find('.hm-chevron').attributes('aria-hidden')).toBe('true')
    })

    it('клик по ячейке дочерней строки всплывает cell-click с данными ячейки', async () => {
        const wrapper = mountRow()
        await wrapper.findAll('tr')[1].findAll('.hm-cell')[0].trigger('click')
        expect(wrapper.emitted('cell-click')).toEqual([
            [childPayments, 'Продукты', periods[0].name, -300, 2, 'p1']
        ])
    })

    it('пустая ячейка — «—», без клика, фокуса и роли кнопки', async () => {
        const wrapper = mountRow()
        const cell = wrapper.findAll('tr')[1].findAll('.hm-cell')[1]
        expect(cell.text()).toBe('—')
        expect(cell.attributes('aria-label')).toBe('нет трат')
        expect(cell.attributes('tabindex')).toBeUndefined()
        expect(cell.attributes('role')).toBeUndefined()
        expect(cell.attributes('title')).toBeUndefined()
        await cell.trigger('click')
        await cell.trigger('keydown', { key: 'Enter' })
        expect(wrapper.emitted('cell-click')).toBeUndefined()
    })

    it('непустая ячейка — кнопка с aria-label и aria-pressed; Enter и Space открывают платежи', async () => {
        const wrapper = mountRow()
        const cell = wrapper.findAll('tr')[1].findAll('.hm-cell')[0]
        expect(cell.attributes('tabindex')).toBe('0')
        expect(cell.attributes('role')).toBe('button')
        expect(cell.attributes('aria-pressed')).toBe('false')
        expect(cell.attributes('aria-label').replace(/\s+/g, ' ')).toBe(`Продукты, ${periods[0].name}: -300,00 ₽`)
        await cell.trigger('keydown', { key: 'Enter' })
        await cell.trigger('keydown', { key: ' ' })
        expect(wrapper.emitted('cell-click')).toHaveLength(2)
    })

    it('выбранная ячейка — aria-pressed="true"', () => {
        const wrapper = mountRow({ selectedCategoryId: 1, selectedPeriodKey: 'p2' })
        const cells = wrapper.findAll('tr')[0].findAll('.hm-cell')
        expect(cells[0].attributes('aria-pressed')).toBe('false')
        expect(cells[1].attributes('aria-pressed')).toBe('true')
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
        await wrapper.find('.hm-cat-toggle').trigger('click')
        expect(wrapper.findAll('.hm-delta')).toHaveLength(1)
        expect(wrapper.find('.hm-delta-rub').text()).toBe('−1 500 ₽')
    })

    it('доходная строка и потомки «Накоплений» — инвертированный цвет Δ', () => {
        const income = { categoryId: 9, categoryName: 'Зарплата', isOnlyIncome: true, periods: {}, children: [],
            comparison: { previous: 100000, current: 110000 } }
        expect(mountRow({ category: income, showDelta: true }).find('.hm-delta').classes()).toContain('hm-delta-good')

        const savings = { categoryId: 33, categoryName: 'Накопления', periods: {}, comparison: { previous: 1000, current: 2000 },
            children: [{ categoryId: 34, categoryName: 'Вклад', periods: {}, children: [], comparison: { previous: 1000, current: 2000 } }] }
        const deltas = mountRow({ category: savings, showDelta: true }).findAll('.hm-delta')
        expect(deltas.map(d => d.classes().includes('hm-delta-good'))).toEqual([true, true])
    })

    it('клик по ячейке месяца по-прежнему открывает платежи при колонке Δ', async () => {
        const wrapper = mountRow({ showDelta: true })
        await wrapper.findAll('tr')[1].findAll('.hm-cell')[0].trigger('click')
        expect(wrapper.emitted('cell-click')).toHaveLength(1)
    })
})
