import { describe, it, expect, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'

vi.mock('axios', () => ({
    default: {
        get: vi.fn(async (url) => ({ data: url.includes('/api/PaymentsApi') ? { paymentsTree: [], periods: {} } : [] })),
        post: vi.fn()
    }
}))

const PaymentsTable = (await import('./PaymentsTable.vue')).default

const mountPage = async () => {
    const wrapper = mount(PaymentsTable, { global: { stubs: { TagMultiSelect: true, CategoryTreeSelect: true } } })
    await flushPromises()
    return wrapper
}

const quickButtons = (wrapper) => wrapper.findAll('.btn-group button')

describe('PaymentsTable: панель фильтров', () => {
    it('быстрый период помечается aria-pressed, ручная смена даты снимает отметку', async () => {
        const wrapper = await mountPage()
        expect(quickButtons(wrapper).every(b => b.attributes('aria-pressed') === 'false')).toBe(true)

        await quickButtons(wrapper)[2].trigger('click')
        await flushPromises()
        expect(quickButtons(wrapper)[2].attributes('aria-pressed')).toBe('true')
        expect(quickButtons(wrapper)[2].classes()).toContain('btn-primary')

        await wrapper.find('input[type="date"]').setValue('2026-01-05')
        expect(quickButtons(wrapper).every(b => b.attributes('aria-pressed') === 'false')).toBe(true)
        expect(quickButtons(wrapper).some(b => b.classes().includes('btn-primary'))).toBe(false)
    })

    it('кнопка «Отправить» прижата вправо', async () => {
        const wrapper = await mountPage()
        expect(wrapper.find('button[type="submit"]').classes()).toContain('ms-auto')
    })
})

describe('PaymentsTable: колонка Δ к прошлому периоду', () => {
    const comparison = {
        asOf: '2026-10-21T00:00:00',
        current: { key: '102026', from: '2026-10-01T00:00:00', to: '2026-10-22T00:00:00' },
        previous: { key: '092026', from: '2026-09-01T00:00:00', to: '2026-10-01T00:00:00' },
        currentPeriodTo: '2026-11-01T00:00:00',
        isCurrentPartial: true
    }
    const response = (withComparison) => ({
        paymentsTree: [
            { categoryId: 5, categoryName: 'Кафе', periods: { '102026': { totalSum: -11900, payments: [] } }, children: [],
                comparison: { previous: -7400, current: -11900 } },
            { categoryId: 2147483647, categoryName: 'ВСЕГО потрачено:', periods: { '102026': { totalSum: -52450, payments: [] } },
                children: [], comparison: { previous: -51600, current: -52450 } }
        ],
        periods: { '102026': { from: '2026-10-01T00:00:00', to: '2026-11-01T00:00:00' } },
        ...(withComparison ? { comparison } : {})
    })

    const mountWith = async (data) => {
        const axios = (await import('axios')).default
        axios.get.mockImplementation(async (url) => ({ data: url.includes('/api/PaymentsApi') ? data : [] }))
        return mountPage()
    }

    it('шапка «Δ к сентябрю» с title, Δ в строках и в «Итого» по узлу «ВСЕГО», подпись под таблицей', async () => {
        const wrapper = await mountWith(response(true))
        const header = wrapper.find('th.hm-col-delta')
        expect(header.text()).toBe('Δ к сентябрю')
        expect(header.attributes('scope')).toBe('col')
        expect(header.attributes('title')).toBe('Изменение трат: октябрь по 21 число против всего сентября')

        const deltas = wrapper.findAll('td.hm-delta')
        expect(deltas).toHaveLength(2)
        expect(deltas[0].text()).toContain('(+61 %)')
        expect(deltas[1].classes()).toContain('hm-delta-total')
        expect(deltas[1].find('.hm-delta-rub').text()).toBe('+850 ₽')

        expect(wrapper.find('.hm-footnote').text()).toContain('Δ — октябрь по 21 число против всего сентября')
        expect(wrapper.find('.hm-footnote').text()).not.toContain('с учётом выбранных тегов')
    })

    it('при выбранных тегах они уходят в запрос, подпись дополнена «с учётом выбранных тегов»', async () => {
        const wrapper = await mountWith(response(true))
        const axios = (await import('axios')).default
        axios.get.mockClear()

        wrapper.findComponent({ name: 'TagMultiSelect' }).vm.$emit('update:modelValue', [7])
        await flushPromises()

        const [, config] = axios.get.mock.calls.find(([url]) => url.endsWith('/api/PaymentsApi'))
        expect(config.params.getAll('tagIds')).toEqual(['7'])
        expect(wrapper.find('.hm-footnote').text())
            .toContain('Δ — октябрь по 21 число против всего сентября, с учётом выбранных тегов')
    })

    it('без comparison в ответе колонки и подписи нет', async () => {
        const wrapper = await mountWith(response(false))
        expect(wrapper.find('th.hm-col-delta').exists()).toBe(false)
        expect(wrapper.find('td.hm-delta').exists()).toBe(false)
        expect(wrapper.find('.hm-footnote').text()).not.toContain('Δ —')
    })
})

describe('PaymentsTable: карточка сравнения по клику на Δ (СР §3)', () => {
    // jsdom не умеет прокручивать: handleCellClick и карточка вызывают scrollIntoView
    Element.prototype.scrollIntoView = vi.fn()

    const comparison = {
        asOf: '2026-10-21T00:00:00',
        current: { key: '102026', from: '2026-10-01T00:00:00', to: '2026-10-22T00:00:00' },
        previous: { key: '092026', from: '2026-09-01T00:00:00', to: '2026-10-01T00:00:00' },
        currentPeriodTo: '2026-11-01T00:00:00',
        isCurrentPartial: true
    }
    const data = {
        paymentsTree: [
            { categoryId: 5, categoryName: 'Продукты', periods: { '102026': { totalSum: -24650, payments: [{ paymentId: 1, paymentDate: '2026-10-02T00:00:00', paymentSum: -24650 }] } },
                children: [], comparison: { previous: -31200, current: -24650 } },
            { categoryId: 6, categoryName: 'Транспорт', periods: { '102026': { totalSum: -3100, payments: [] } },
                children: [], comparison: { previous: -4600, current: -3100 } },
            { categoryId: 2147483647, categoryName: 'ВСЕГО потрачено:', periods: { '102026': { totalSum: -27750, payments: [] } },
                children: [], comparison: { previous: -35800, current: -27750 } }
        ],
        periods: { '102026': { from: '2026-10-01T00:00:00', to: '2026-11-01T00:00:00' } },
        comparison
    }

    const mountWith = async () => {
        const axios = (await import('axios')).default
        axios.get.mockImplementation(async (url) => ({ data: url.includes('/api/PaymentsApi') ? data : [] }))
        const wrapper = mount(PaymentsTable, {
            global: { stubs: { TagMultiSelect: true, CategoryTreeSelect: true, PaymentList: true, PaymentSourcesList: true } },
            attachTo: document.body
        })
        await flushPromises()
        return wrapper
    }

    const delta = (wrapper, key) => wrapper.find(`td.hm-delta[data-delta-key="${key}"]`)
    const card = (wrapper) => wrapper.find('#comparison-card')
    const paymentsBlock = (wrapper) => wrapper.findAll('h5').find(h => h.text().startsWith('Платежи:'))

    it('клик по Δ открывает карточку, повторный — закрывает', async () => {
        const wrapper = await mountWith()
        expect(card(wrapper).exists()).toBe(false)

        await delta(wrapper, 5).trigger('click')
        await flushPromises()
        expect(card(wrapper).find('h5').text()).toContain('Октябрь против сентября')
        expect(card(wrapper).find('.cmp-scope').text()).toBe('Продукты')
        expect(delta(wrapper, 5).attributes('aria-expanded')).toBe('true')
        expect(delta(wrapper, 5).classes()).toContain('selected')
        expect(document.activeElement).toBe(card(wrapper).find('h5').element)

        await delta(wrapper, 5).trigger('click')
        await flushPromises()
        expect(card(wrapper).exists()).toBe(false)
        expect(delta(wrapper, 5).attributes('aria-expanded')).toBe('false')
        wrapper.unmount()
    })

    it('клик по Δ другой категории перестраивает карточку и переносит выделение', async () => {
        const wrapper = await mountWith()
        await delta(wrapper, 5).trigger('click')
        await delta(wrapper, 6).trigger('click')
        await flushPromises()
        expect(card(wrapper).find('.cmp-scope').text()).toBe('Транспорт')
        expect(delta(wrapper, 5).attributes('aria-expanded')).toBe('false')
        expect(delta(wrapper, 6).attributes('aria-expanded')).toBe('true')
        wrapper.unmount()
    })

    it('Δ и блок платежей взаимоисключают друг друга', async () => {
        const wrapper = await mountWith()
        const periodCell = wrapper.find('td.hm-cell.interactive')

        await periodCell.trigger('click')
        await flushPromises()
        expect(paymentsBlock(wrapper)).toBeTruthy()

        await delta(wrapper, 5).trigger('click')
        await flushPromises()
        expect(card(wrapper).exists()).toBe(true)
        expect(paymentsBlock(wrapper)).toBeUndefined()
        expect(wrapper.find('td.hm-cell.selected').exists()).toBe(false)

        await wrapper.find('td.hm-cell.interactive').trigger('click')
        await flushPromises()
        expect(card(wrapper).exists()).toBe(false)
        expect(paymentsBlock(wrapper)).toBeTruthy()
        expect(wrapper.find('td.hm-delta.selected').exists()).toBe(false)
        wrapper.unmount()
    })

    it('«Скрыть» и Esc закрывают карточку, фокус возвращается на Δ-ячейку', async () => {
        const wrapper = await mountWith()
        await delta(wrapper, 5).trigger('click')
        await flushPromises()
        await card(wrapper).find('button').trigger('click')
        await flushPromises()
        expect(card(wrapper).exists()).toBe(false)
        expect(document.activeElement).toBe(delta(wrapper, 5).element)

        await delta(wrapper, 6).trigger('keydown', { key: 'Enter' })
        await flushPromises()
        await card(wrapper).find('h5').trigger('keydown', { key: 'Escape' })
        await flushPromises()
        expect(card(wrapper).exists()).toBe(false)
        expect(document.activeElement).toBe(delta(wrapper, 6).element)
        wrapper.unmount()
    })

    it('Δ «Итого» открывает карточку «Все траты» по узлу «ВСЕГО»', async () => {
        const wrapper = await mountWith()
        await delta(wrapper, 'total').trigger('click')
        await flushPromises()
        expect(card(wrapper).find('.cmp-scope').text()).toBe('Все траты')
        expect(card(wrapper).findAll('.cmp-tile .small + div')[1].text().replace(/\s/g, ' ')).toBe('35 800,00 ₽')
        wrapper.unmount()
    })

    it('перезагрузка таблицы («Отправить») закрывает карточку', async () => {
        const wrapper = await mountWith()
        await delta(wrapper, 5).trigger('click')
        await flushPromises()
        await wrapper.find('form').trigger('submit')
        await flushPromises()
        expect(card(wrapper).exists()).toBe(false)
        expect(wrapper.find('td.hm-delta.selected').exists()).toBe(false)
        wrapper.unmount()
    })
})

describe('PaymentsTable: сортировка карточки сравнения (СР §4.5)', () => {
    Element.prototype.scrollIntoView = vi.fn()

    const comparison = {
        asOf: '2026-10-21T00:00:00',
        current: { key: '102026', from: '2026-10-01T00:00:00', to: '2026-10-22T00:00:00' },
        previous: { key: '092026', from: '2026-09-01T00:00:00', to: '2026-10-01T00:00:00' },
        currentPeriodTo: '2026-11-01T00:00:00',
        isCurrentPartial: true
    }
    const cat = (categoryId, categoryName, sPrev, sCur, children = []) => ({
        categoryId,
        categoryName,
        periods: { '102026': { totalSum: -sCur, payments: [] } },
        comparison: { previous: -sPrev, current: -sCur },
        children
    })
    const data = {
        paymentsTree: [
            cat(1, 'Продукты', 31200, 24650, [cat(11, 'Супермаркеты', 27900, 21300), cat(12, 'Рынок', 3300, 3350)]),
            cat(3, 'Транспорт', 4600, 3100, [cat(31, 'Такси', 3100, 2300), cat(32, 'Общественный транспорт', 1500, 800)]),
            cat(2147483647, 'ВСЕГО потрачено:', 35800, 27750)
        ],
        periods: { '102026': { from: '2026-10-01T00:00:00', to: '2026-11-01T00:00:00' } },
        comparison
    }

    it('выбор «По изменению» сохраняется при смене охвата', async () => {
        const axios = (await import('axios')).default
        axios.get.mockImplementation(async (url) => ({ data: url.includes('/api/PaymentsApi') ? data : [] }))
        const wrapper = mount(PaymentsTable, {
            global: { stubs: { TagMultiSelect: true, CategoryTreeSelect: true, PaymentList: true, PaymentSourcesList: true } },
            attachTo: document.body
        })
        await flushPromises()
        const card = () => wrapper.find('#comparison-card')
        const names = () => card().findAll('.cmp-table tbody tr').map(tr => tr.find('[class^="cmp-name"]').text())

        await wrapper.find('td.hm-delta[data-delta-key="1"]').trigger('click')
        await flushPromises()
        expect(names()).toEqual(['Супермаркеты', 'Рынок'])

        await card().findAll('[aria-label="Сортировка"] button')[1].trigger('click')
        expect(names()).toEqual(['Рынок', 'Супермаркеты'])

        await wrapper.find('td.hm-delta[data-delta-key="3"]').trigger('click')
        await flushPromises()
        expect(card().findAll('[aria-label="Сортировка"] button')[1].attributes('aria-pressed')).toBe('true')
        expect(names()).toEqual(['Общественный транспорт', 'Такси'])
        wrapper.unmount()
    })
})
