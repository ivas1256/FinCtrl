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
