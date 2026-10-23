import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import ComparisonCard from './ComparisonCard.vue'
import { GroupingType } from '../../utils/periodHeader'
import { Semantics } from '../../utils/periodComparison'

const NBSP = '\u00A0'
// Intl разделяет разряды неразрывными пробелами; пробелы между элементами схлопываем
const normalize = (s) => s.replace(/\s+/g, ' ')

// СР §5.2, D = 21.10.2026
const comparison = {
    asOf: '2026-10-21T00:00:00',
    current: { key: '102026', from: '2026-10-01T00:00:00', to: '2026-10-22T00:00:00' },
    previous: { key: '092026', from: '2026-09-01T00:00:00', to: '2026-10-01T00:00:00' },
    currentPeriodTo: '2026-11-01T00:00:00',
    isCurrentPartial: true
}

const products = { categoryId: 5, categoryName: 'Продукты', color: '#ff8800', comparison: { previous: -31200, current: -24650 } }

const mountCard = (props = {}) =>
    mount(ComparisonCard, {
        props: {
            id: 'comparison-card',
            comparison,
            groupingType: GroupingType.Month,
            node: products,
            semantics: Semantics.Expense,
            ...props
        },
        attachTo: document.body
    })

// Плитка: [подпись, значение]
const tiles = (wrapper) => wrapper.findAll('.cmp-tile').map(t => [t.find('.small').text(), normalize(t.find('.small + div').text())])

describe('ComparisonCard', () => {
    it('заголовок «Октябрь против сентября · Продукты», точка цвета, section связан с h5', () => {
        const wrapper = mountCard()
        const h5 = wrapper.find('h5')
        expect(normalize(h5.text())).toBe('Октябрь против сентября · Продукты')
        expect(h5.attributes('tabindex')).toBe('-1')
        expect(wrapper.find('section').attributes('id')).toBe('comparison-card')
        expect(wrapper.find('section').attributes('aria-labelledby')).toBe(h5.attributes('id'))
        expect(wrapper.find('.cmp-dot').attributes('style')).toContain('background')
        wrapper.unmount()
    })

    it('плитки и подпись «Продукты» — пример СР 24', () => {
        const wrapper = mountCard()
        expect(wrapper.find('.card-body p').text()).toBe('Октябрь — по 21 число, сентябрь — целиком')
        expect(tiles(wrapper)).toEqual([
            ['Октябрь, по 21.10', '24 650,00 ₽'],
            ['Сентябрь', '31 200,00 ₽'],
            ['Разница', '▼снижение −6 550 ₽(−21 %)']
        ])
        expect(wrapper.find('.cmp-diff .cmp-arrow').attributes('aria-hidden')).toBe('true')
        expect(normalize(wrapper.find('.cmp-diff-pct').text())).toBe('(−21 %)')
        expect(wrapper.find('.cmp-diff').classes()).toContain('cmp-diff-good')
        wrapper.unmount()
    })

    it('дочерняя категория — «Родитель / Подкатегория»', () => {
        const wrapper = mountCard({ node: { ...products, categoryName: 'Рынок' }, parent: { categoryId: 1, categoryName: 'Продукты' } })
        expect(normalize(wrapper.find('.cmp-scope').text())).toBe('Продукты / Рынок')
        expect(wrapper.find('.cmp-scope-parent').exists()).toBe(true)
        wrapper.unmount()
    })

    it('«Итого» — «Все траты» без точки', () => {
        const wrapper = mountCard({ node: { categoryId: 2147483647, categoryName: 'ВСЕГО потрачено:', comparison: { previous: -51600, current: -52450 } }, isTotal: true })
        expect(normalize(wrapper.find('h5').text())).toBe('Октябрь против сентября · Все траты')
        expect(wrapper.find('.cmp-dot').exists()).toBe(false)
        expect(wrapper.find('.cmp-diff').classes()).toContain('cmp-diff-bad')
        wrapper.unmount()
    })

    it('доходная строка: величина поступления, рост — зелёный', () => {
        const wrapper = mountCard({ node: { categoryId: 9, categoryName: 'Зарплата', isOnlyIncome: true, comparison: { previous: 100000, current: 110000 } }, semantics: Semantics.Income })
        expect(tiles(wrapper)[0]).toEqual(['Октябрь, по 21.10', '110 000,00 ₽'])
        expect(wrapper.find('.cmp-diff').classes()).toContain('cmp-diff-good')
        expect(wrapper.find('.cmp-arrow').text()).toBe('▲')
        wrapper.unmount()
    })

    it('с тегами — подпись дополнена', () => {
        const wrapper = mountCard({ withTags: true })
        expect(wrapper.find('.card-body p').text()).toBe('Октябрь — по 21 число, сентябрь — целиком, с учётом выбранных тегов')
        wrapper.unmount()
    })

    it('«Скрыть» и Esc эмитят close', async () => {
        const wrapper = mountCard()
        await wrapper.find('button').trigger('click')
        await wrapper.find('h5').trigger('keydown', { key: 'Escape' })
        expect(wrapper.emitted('close')).toHaveLength(2)
        wrapper.unmount()
    })

    it('reveal: фокус на заголовок, прокрутка карточки nearest; при reduced motion — без плавности', () => {
        const wrapper = mountCard()
        const section = wrapper.find('section').element
        section.scrollIntoView = vi.fn()
        window.matchMedia = vi.fn(() => ({ matches: false }))
        wrapper.vm.reveal()
        expect(document.activeElement).toBe(wrapper.find('h5').element)
        expect(section.scrollIntoView).toHaveBeenCalledWith({ behavior: 'smooth', block: 'nearest' })

        window.matchMedia = vi.fn(() => ({ matches: true }))
        wrapper.vm.reveal()
        expect(section.scrollIntoView).toHaveBeenLastCalledWith({ behavior: 'auto', block: 'nearest' })
        delete window.matchMedia
        wrapper.unmount()
    })
})

describe('ComparisonCard: топ-3, таблица, сортировка (СР §4.4–4.7)', () => {
    // Узел дерева; суммы знаковые (расход < 0). Данные СР §7
    const tree = (categoryId, categoryName, sPrev, sCur, children = []) => ({
        categoryId, categoryName, comparison: { current: -sCur, previous: -sPrev }, children
    })
    const productsTree = tree(1, 'Продукты', 31200, 24650, [tree(11, 'Супермаркеты', 27900, 21300), tree(12, 'Рынок', 3300, 3350)])
    const treeRows = [
        productsTree,
        tree(2, 'Кафе и рестораны', 7400, 11900),
        tree(3, 'Транспорт', 4600, 3100, [tree(31, 'Такси', 3100, 2300), tree(32, 'Общественный транспорт', 1500, 800)]),
        tree(4, 'Спорт', 6500, 9000),
        tree(5, 'Подарки', 0, 3800),
        tree(6, 'Здоровье', 1900, 0),
        tree(2147483646, 'Без категории', 800, 1500)
    ]
    const total = { categoryId: 2147483647, categoryName: 'ВСЕГО потрачено:', comparison: { previous: -51600, current: -52450 } }

    // Колонки топ-3: [заголовок, [[имя, изменение], …] | текст пустого состояния]
    const top = (wrapper) => wrapper.findAll('.row.g-4 > div').map(col => [
        col.find('.cmp-top-title').text(),
        col.find('ul').exists()
            ? col.findAll('li').map(li => [li.find('.cmp-top-name').text(), normalize(li.find('.cmp-top-delta').text())])
            : col.find('.small.py-2').text()
    ])
    const sortButtons = (wrapper) => wrapper.findAll('[aria-label="Сортировка"] button')
    const tableRootNames = (wrapper) => wrapper.findAll('.cmp-name-root, .cmp-name-muted').map(n => n.text())

    it('«Продукты»: топ роста — Рынок, снижения — Супермаркеты (СР 25)', () => {
        const wrapper = mountCard({ node: productsTree })
        expect(top(wrapper)).toEqual([
            ['Выросли сильнее всего', [['Рынок', '▲рост +50 ₽ · +2 %']]],
            ['Снизились сильнее всего', [['Супермаркеты', '▼снижение −6 600 ₽ · −24 %']]]
        ])
        expect(wrapper.find('.cmp-top-delta').classes()).toContain('cmp-diff-bad')
        expect(wrapper.find('.cmp-top-delta .cmp-arrow').attributes('aria-hidden')).toBe('true')
        expect(wrapper.findAll('.row.g-4 > div').every(c => c.classes().includes('col-12') && c.classes().includes('col-md-6'))).toBe(true)
        expect(wrapper.findAll('.cmp-table tbody tr')).toHaveLength(2)
        expect(wrapper.find('.cmp-note').exists()).toBe(false)
        wrapper.unmount()
    })

    it('«Итого»: топ-3 по СР 26, «Без категории» в таблице и сноска', () => {
        const wrapper = mountCard({ node: total, isTotal: true, treeRows })
        const [growth, decline] = top(wrapper)
        expect(growth[1].map(r => r[0])).toEqual(['Кафе и рестораны', 'Подарки', 'Спорт'])
        expect(decline[1].map(r => r[0])).toEqual(['Продукты', 'Здоровье', 'Транспорт'])
        expect(tableRootNames(wrapper)).toContain('Без категории')
        expect(wrapper.find('.cmp-note').text())
            .toBe('«Без категории» не входит в итог трат — разберите эти платежи на странице «Операции».')
        wrapper.unmount()
    })

    it('нет снижения — «Снижения нет»', () => {
        const node = tree(1, 'Дом', 100, 300, [tree(11, 'Ремонт', 100, 300)])
        const wrapper = mountCard({ node })
        expect(top(wrapper)[1]).toEqual(['Снизились сильнее всего', 'Снижения нет'])
        wrapper.unmount()
    })

    it('лист — только плитки и текст «нет подкатегорий» (СР 30)', () => {
        const wrapper = mountCard({ node: { ...products, categoryName: 'Спорт', children: [] } })
        expect(wrapper.find('.cmp-empty').text()).toBe('У категории нет подкатегорий — сравнение только по категории целиком.')
        expect(wrapper.findAll('.cmp-tile')).toHaveLength(3)
        expect(wrapper.find('.row.g-4').exists()).toBe(false)
        expect(wrapper.find('table').exists()).toBe(false)
        expect(wrapper.find('[aria-label="Сортировка"]').exists()).toBe(false)
        wrapper.unmount()
    })

    it('сортировка: «По категориям» по умолчанию, клик эмитит update:sortMode, порядок по пропу (СР 28)', async () => {
        const wrapper = mountCard({ node: total, isTotal: true, treeRows })
        const [byCategory, byChange] = sortButtons(wrapper)
        expect(byCategory.text()).toBe('По категориям')
        expect(byCategory.attributes('aria-pressed')).toBe('true')
        expect(byCategory.classes()).toContain('btn-primary')
        expect(byChange.attributes('aria-pressed')).toBe('false')
        expect(byChange.classes()).toContain('btn-outline-secondary')
        expect(tableRootNames(wrapper)).toEqual(['Продукты', 'Кафе и рестораны', 'Транспорт', 'Спорт', 'Подарки', 'Здоровье', 'Без категории'])

        await byChange.trigger('click')
        expect(wrapper.emitted('update:sortMode')).toEqual([['change']])

        await wrapper.setProps({ sortMode: 'change' })
        expect(sortButtons(wrapper)[1].attributes('aria-pressed')).toBe('true')
        expect(tableRootNames(wrapper)).toEqual(['Кафе и рестораны', 'Подарки', 'Спорт', 'Без категории', 'Транспорт', 'Здоровье', 'Продукты'])
        wrapper.unmount()
    })

    it('смена охвата сбрасывает свёрнутость внуков', async () => {
        const wrapper = mountCard({ node: total, isTotal: true, treeRows })
        await wrapper.find('.cmp-table button[aria-expanded="true"]').trigger('click')
        expect(wrapper.find('.cmp-table button[aria-expanded="false"]').exists()).toBe(true)

        await wrapper.setProps({ node: treeRows[2], isTotal: false })
        await wrapper.setProps({ node: total, isTotal: true })
        expect(wrapper.find('.cmp-table button[aria-expanded="false"]').exists()).toBe(false)
        wrapper.unmount()
    })
})
