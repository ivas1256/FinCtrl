import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import DeltaCell from './DeltaCell.vue'
import { Semantics } from '../../utils/periodComparison'

const NBSP = ' '

const node = (previous, current) => ({ categoryId: 5, isOnlyIncome: false, comparison: { previous, current } })

// Ячейка — <td>, монтируем внутрь строки таблицы
const mountCell = (n, semantics = Semantics.Expense, extra = {}) =>
    mount(DeltaCell, {
        props: { node: n, semantics, ...extra },
        attachTo: document.createElement('tr')
    })

describe('DeltaCell', () => {
    it('рост трат: «▲ +4 500 ₽ (+61 %)», красный, скрытый текст «рост», стрелка скрыта от скринридеров', () => {
        const wrapper = mountCell(node(-7400, -11900))
        expect(wrapper.find('.hm-delta-arrow').text()).toBe('▲')
        expect(wrapper.text()).toContain(`(+61${NBSP}%)`)
        expect(wrapper.classes()).toContain('hm-delta-bad')
        expect(wrapper.find('.hm-delta-arrow').attributes('aria-hidden')).toBe('true')
        expect(wrapper.find('.visually-hidden').text()).toBe('рост')
        expect(wrapper.find('.hm-delta-rub').text()).toBe(`+4${NBSP}500${NBSP}₽`)
    })

    it('снижение трат: «▼ −6 550 ₽ (−21 %)», зелёный', () => {
        const wrapper = mountCell(node(-31200, -24650))
        expect(wrapper.find('.hm-delta-arrow').text()).toBe('▼')
        expect(wrapper.find('.hm-delta-rub').text()).toBe(`−6${NBSP}550${NBSP}₽`)
        expect(wrapper.text()).toContain(`(−21${NBSP}%)`)
        expect(wrapper.classes()).toContain('hm-delta-good')
        expect(wrapper.find('.visually-hidden').text()).toBe('снижение')
    })

    it('новая категория: «(новая)», скрытый текст «новая трата»', () => {
        const wrapper = mountCell(node(0, -3800))
        expect(wrapper.text()).toContain('(новая)')
        expect(wrapper.classes()).toContain('hm-delta-bad')
        expect(wrapper.find('.visually-hidden').text()).toBe('новая трата')
    })

    it('исчезнувшая: «(−100 %)»', () => {
        const wrapper = mountCell(node(-1900, 0))
        expect(wrapper.text()).toContain(`(−100${NBSP}%)`)
        expect(wrapper.classes()).toContain('hm-delta-good')
    })

    it('без изменений: «•», серый', () => {
        const wrapper = mountCell(node(-1000, -1000))
        expect(wrapper.find('.hm-delta-arrow').text()).toBe('•')
        expect(wrapper.classes()).toContain('hm-delta-neutral')
        expect(wrapper.find('.visually-hidden').text()).toBe('без изменений')
    })

    it('нет данных: «—» серым, без стрелки', () => {
        const wrapper = mountCell({ categoryId: 5 })
        expect(wrapper.find('.hm-delta-arrow').exists()).toBe(false)
        expect(wrapper.find('[aria-hidden="true"]').text()).toBe('—')
        expect(wrapper.classes()).toContain('hm-delta-neutral')
    })

    it('доходная строка: рост — зелёный ▲, падение — красный ▼', () => {
        const up = mountCell({ ...node(100000, 110000), isOnlyIncome: true }, Semantics.Income)
        expect(up.find('.hm-delta-arrow').text()).toBe('▲')
        expect(up.classes()).toContain('hm-delta-good')

        const down = mountCell({ ...node(100000, 90000), isOnlyIncome: true }, Semantics.Income)
        expect(down.find('.hm-delta-arrow').text()).toBe('▼')
        expect(down.classes()).toContain('hm-delta-bad')
    })

    it('ячейка «Итого» помечена своим классом', () => {
        expect(mountCell(node(-51600, -52450), Semantics.Expense, { total: true }).classes()).toContain('hm-delta-total')
    })

    describe('кликабельность (СР §3)', () => {
        const labels = {
            phrase: 'октябрь против сентября',
            title: 'Сравнить октябрь и сентябрь по подкатегориям',
            cardId: 'comparison-card'
        }
        const products = { ...node(-31200, -24650), categoryName: 'Продукты' }

        it('ячейка с данными — кнопка: tabindex, role, title, aria-label, aria-controls, aria-expanded', () => {
            const td = mountCell(products, Semantics.Expense, { labels })
            expect(td.attributes('tabindex')).toBe('0')
            expect(td.attributes('role')).toBe('button')
            expect(td.attributes('title')).toBe(labels.title)
            expect(td.attributes('aria-controls')).toBe('comparison-card')
            expect(td.attributes('aria-expanded')).toBe('false')
            expect(td.attributes('aria-label'))
                .toBe(`Продукты: октябрь против сентября, снижение на 6${NBSP}550${NBSP}₽, минус 21${NBSP}%. Открыть сравнение`)
            expect(td.attributes('data-delta-key')).toBe('5')
            expect(td.classes()).toContain('interactive')
        })

        it('клик, Enter и Space эмитят select', async () => {
            const td = mountCell(products, Semantics.Expense, { labels })
            await td.trigger('click')
            await td.trigger('keydown', { key: 'Enter' })
            await td.trigger('keydown', { key: ' ' })
            expect(td.emitted('select')).toHaveLength(3)
        })

        it('выбранная: aria-expanded="true" и класс selected', () => {
            const td = mountCell(products, Semantics.Expense, { labels, selected: true })
            expect(td.attributes('aria-expanded')).toBe('true')
            expect(td.classes()).toContain('selected')
        })

        it('«Итого»: имя охвата из name, ключ total', () => {
            const td = mountCell(node(-51600, -52450), Semantics.Expense, { labels, total: true, name: 'Все траты' })
            expect(td.attributes('aria-label')).toMatch(/^Все траты: октябрь против сентября, рост на 850/)
            expect(td.attributes('data-delta-key')).toBe('total')
        })

        it('«—» не фокусируется и не реагирует на клик (СР 11)', async () => {
            const td = mountCell({ categoryId: 5, categoryName: 'Пусто' }, Semantics.Expense, { labels, selected: true })
            expect(td.attributes('tabindex')).toBeUndefined()
            expect(td.attributes('role')).toBeUndefined()
            expect(td.classes()).not.toContain('interactive')
            expect(td.classes()).not.toContain('selected')
            await td.trigger('click')
            await td.trigger('keydown', { key: 'Enter' })
            expect(td.emitted('select')).toBeUndefined()
        })
    })
})
