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
