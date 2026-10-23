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
