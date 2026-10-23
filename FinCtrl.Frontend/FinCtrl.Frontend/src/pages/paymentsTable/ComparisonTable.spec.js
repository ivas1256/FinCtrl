import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import ComparisonTable from './ComparisonTable.vue'
import { buildComparisonRows, sortComparisonRows, barScale, SortMode } from '../../utils/comparisonCard'
import { Semantics } from '../../utils/periodComparison'
import { UNCATEGORIZED_ID } from '../../utils/heatmap'

const normalize = (s) => s.replace(/\s+/g, ' ').trim()

// Узел дерева; суммы знаковые (расход < 0)
const node = (categoryId, categoryName, sPrev, sCur, children = []) => ({
    categoryId,
    categoryName,
    comparison: { current: -sCur, previous: -sPrev },
    children
})

// Данные СР §7
const treeRows = [
    node(1, 'Продукты', 31200, 24650, [node(11, 'Супермаркеты', 27900, 21300), node(12, 'Рынок', 3300, 3350)]),
    node(2, 'Кафе и рестораны', 7400, 11900),
    node(3, 'Транспорт', 4600, 3100, [node(31, 'Такси', 3100, 2300), node(32, 'Общественный транспорт', 1500, 800)]),
    node(4, 'Спорт', 6500, 9000),
    node(5, 'Подарки', 0, 3800),
    node(6, 'Здоровье', 1900, 0),
    node(UNCATEGORIZED_ID, 'Без категории', 800, 1500)
]

const labels = { previous: 'Сентябрь', current: 'Октябрь', currentTitle: 'по 21.10' }

const mountTable = (mode = SortMode.ByCategory, model = buildComparisonRows({ treeRows })) =>
    mount(ComparisonTable, {
        props: { rows: sortComparisonRows(model.rows, mode), scale: barScale(model.rows), labels },
        attachTo: document.body
    })

const rowNames = (wrapper) => wrapper.findAll('tbody tr').map(tr => tr.find('[class^="cmp-name"]').text())
const row = (wrapper, key) => wrapper.find(`tbody tr[data-row-key="${key}"]`)
const bar = (wrapper, key) => row(wrapper, key).find('.cmp-bar')

describe('ComparisonTable', () => {
    it('шапка: Категория / П / Т с подсказкой / полоса скрыта на узких / Δ', () => {
        const wrapper = mountTable()
        const th = wrapper.findAll('thead th')
        expect(th.map(t => normalize(t.text()))).toEqual(['Категория', 'Сентябрь', 'Октябрь', 'меньшебольше', 'Δ'])
        expect(th[2].attributes('title')).toBe('по 21.10')
        expect(th[3].classes()).toEqual(expect.arrayContaining(['d-none', 'd-md-table-cell']))
        expect(th[3].attributes('aria-hidden')).toBe('true')
        expect(wrapper.find('.cmp-wrap').exists()).toBe(true)
        wrapper.unmount()
    })

    it('«По категориям»: порядок дерева, внуки развёрнуты, «Без категории» в конце', () => {
        const wrapper = mountTable()
        expect(rowNames(wrapper)).toEqual([
            'Продукты', 'Супермаркеты', 'Рынок', 'Кафе и рестораны', 'Транспорт', 'Такси', 'Общественный транспорт',
            'Спорт', 'Подарки', 'Здоровье', 'Без категории'
        ])
        wrapper.unmount()
    })

    it('«По изменению» — порядок СР 28', () => {
        const wrapper = mountTable(SortMode.ByChange)
        const roots = wrapper.findAll('tbody tr').filter(tr => !tr.find('.cmp-name-child').exists()).map(tr => tr.find('[class^="cmp-name"]').text())
        expect(roots).toEqual(['Кафе и рестораны', 'Подарки', 'Спорт', 'Без категории', 'Транспорт', 'Здоровье', 'Продукты'])
        wrapper.unmount()
    })

    it('суммы П и Т без знака, нули — «—»; служебная строка — вторичный цвет; Δ — общая разметка ячейки', () => {
        const wrapper = mountTable()
        const gifts = row(wrapper, '5').findAll('td')
        expect(normalize(gifts[1].text())).toBe('—')
        expect(gifts[1].classes()).toContain('cmp-zero')
        expect(normalize(gifts[2].text())).toBe('3 800,00 ₽')
        expect(gifts[4].find('.hm-delta-arrow').text()).toBe('▲')
        expect(normalize(gifts[4].find('.hm-delta-rub').text())).toBe('+3 800 ₽')
        expect(gifts[4].text()).toContain('(новая)')
        expect(gifts[4].classes()).toContain('hm-delta-bad')
        expect(gifts[4].attributes('tabindex')).toBeUndefined()

        expect(row(wrapper, String(UNCATEGORIZED_ID)).find('.cmp-name-muted').exists()).toBe(true)
        expect(row(wrapper, '11').find('.cmp-name-child').exists()).toBe(true)
        expect(row(wrapper, '1').find('.cmp-name-root').exists()).toBe(true)
        wrapper.unmount()
    })

    it('полосы: самая длинная — «Супермаркеты» влево, «Кафе» ≈ 68 % половины вправо (СР 27)', () => {
        const wrapper = mountTable()
        const supermarkets = bar(wrapper, '11')
        expect(supermarkets.attributes('style')).toContain('right: 50%')
        expect(supermarkets.attributes('style')).toContain('width: 50%')
        expect(supermarkets.classes()).toContain('cmp-bar-good')

        const cafe = bar(wrapper, '2')
        expect(cafe.attributes('style')).toContain('left: 50%')
        expect(parseFloat(cafe.element.style.width)).toBeCloseTo(4500 / 6600 * 50, 5)
        expect(cafe.classes()).toContain('cmp-bar-bad')
        expect(row(wrapper, '2').find('.cmp-bar-cell').attributes('aria-hidden')).toBe('true')
        wrapper.unmount()
    })

    it('шеврон сворачивает внуков, длины полос не меняются (СР 27, 37)', async () => {
        const wrapper = mountTable()
        const before = bar(wrapper, '2').element.style.width
        const toggle = row(wrapper, '1').find('button')
        expect(toggle.attributes('aria-expanded')).toBe('true')

        await toggle.trigger('click')
        expect(toggle.attributes('aria-expanded')).toBe('false')
        expect(row(wrapper, '11').exists()).toBe(false)
        expect(row(wrapper, '12').exists()).toBe(false)
        expect(bar(wrapper, '2').element.style.width).toBe(before)

        await toggle.trigger('click')
        expect(row(wrapper, '11').exists()).toBe(true)
        wrapper.unmount()
    })

    it('Δ₽ = 0 и M = 0 — полос нет', () => {
        const flat = { node: node(1, 'Дом', 500, 500, [node(11, 'Ремонт', 500, 500)]), semantics: Semantics.Expense }
        const wrapper = mountTable(SortMode.ByCategory, buildComparisonRows(flat))
        expect(rowNames(wrapper)).toEqual(['Ремонт'])
        expect(wrapper.find('.cmp-bar').exists()).toBe(false)
        expect(wrapper.find('.cmp-axis').exists()).toBe(true)
        wrapper.unmount()
    })
})
