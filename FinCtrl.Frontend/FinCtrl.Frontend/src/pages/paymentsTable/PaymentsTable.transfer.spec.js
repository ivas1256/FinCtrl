import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { SAVINGS_CATEGORY_ID } from '../../utils/periodComparison'

vi.mock('axios', () => ({ default: { get: vi.fn(), post: vi.fn(), put: vi.fn() } }))
// jsdom не рисует модалки Bootstrap — достаточно, чтобы show/hide не падали
vi.mock('bootstrap', () => ({ Modal: { getOrCreateInstance: () => ({ show: vi.fn(), hide: vi.fn() }) } }))

const axios = (await import('axios')).default
const PaymentsTable = (await import('./PaymentsTable.vue')).default
const PaymentList = (await import('./PaymentList.vue')).default

const FOOD = 7

const outgoing = {
    paymentId: 1,
    paymentType: 1,
    paymentSum: -500,
    paymentDate: '2026-10-02T00:00:00',
    effectiveCategory: { categoryId: FOOD },
    tags: [],
    transferId: null,
    transferPairPaymentId: null
}

const candidate = (paymentId, isSavings) => ({
    paymentId,
    paymentType: 0,
    paymentSum: 500,
    paymentDate: '2026-10-03T00:00:00',
    account: { accountId: paymentId, displayName: isSavings ? 'Вклад' : 'Карта', isSavings },
    paymentSource: { paymentSourceName: 'Перевод' }
})

const table = {
    paymentsTree: [
        { categoryId: FOOD, categoryName: 'Еда', periods: { '102026': { totalSum: -500, payments: [outgoing] } }, children: [] }
    ],
    periods: { '102026': { from: '2026-10-01T00:00:00', to: '2026-11-01T00:00:00' } }
}

const openModal = async (payment = outgoing) => {
    const wrapper = mount(PaymentsTable, {
        global: { stubs: { TagMultiSelect: true, CategoryTreeSelect: true, PaymentSourcesList: true } },
        attachTo: document.body
    })
    await flushPromises()
    await wrapper.find('td.hm-cell.interactive').trigger('click')
    await flushPromises()
    wrapper.findComponent(PaymentList).vm.$emit('edit-payment', payment)
    await flushPromises()
    return wrapper
}

const categorySelect = (wrapper) => wrapper.findComponent({ name: 'CategoryTreeSelect' })
const transferCheckbox = (wrapper) => wrapper.find('#edit-payment-is-transfer')
const pairSelect = (wrapper) => wrapper.find('#edit-payment-transfer-pair')
const savePayment = async (wrapper) => {
    await wrapper.findAll('.modal-footer button').find(b => b.text() === 'Сохранить').trigger('click')
    await flushPromises()
    return axios.put.mock.calls.at(-1)[1]
}

describe('PaymentsTable: перевод между своими счетами в модалке правки', () => {
    // jsdom не умеет прокручивать: handleCellClick вызывает scrollIntoView
    Element.prototype.scrollIntoView = vi.fn()

    // handleCellClick (при открытии ячейки и повторно после сохранения) откладывает прокрутку setTimeout'ом:
    // подменяем только setTimeout (flushPromises работает на setImmediate) и прокручиваем его до конца теста,
    // иначе таймер срабатывает после teardown jsdom
    beforeEach(() => {
        vi.useFakeTimers({ toFake: ['setTimeout'] })
    })

    afterEach(() => {
        vi.runOnlyPendingTimers()
        vi.useRealTimers()
    })

    beforeEach(() => {
        axios.get.mockReset()
        axios.put.mockReset()
        axios.put.mockResolvedValue({})
        axios.get.mockImplementation(async (url) => {
            if (url.endsWith('/transfer-candidates')) return { data: [candidate(20, true), candidate(21, false)] }
            return { data: url.endsWith('/api/PaymentsApi') ? table : [] }
        })
    })

    it('галочка показывает select второй стороны с кандидатами и пунктом «Не указывать»', async () => {
        const wrapper = await openModal()
        expect(pairSelect(wrapper).exists()).toBe(false)

        await transferCheckbox(wrapper).setValue(true)
        await flushPromises()

        expect(axios.get).toHaveBeenCalledWith(expect.stringContaining('/api/PaymentsApi/1/transfer-candidates'))
        const options = pairSelect(wrapper).findAll('option')
        expect(options.map(o => o.text())).toEqual([
            'Не указывать',
            expect.stringContaining('Вклад · Перевод'),
            expect.stringContaining('Карта · Перевод')
        ])
        wrapper.unmount()
    })

    it('вторая сторона на накопительном счёте подставляет «Накопления», снятие галочки возвращает прежнюю категорию', async () => {
        const wrapper = await openModal()
        await transferCheckbox(wrapper).setValue(true)
        await flushPromises()

        await pairSelect(wrapper).setValue('20')
        expect(categorySelect(wrapper).props('modelValue')).toBe(SAVINGS_CATEGORY_ID)

        await transferCheckbox(wrapper).setValue(false)
        expect(categorySelect(wrapper).props('modelValue')).toBe(FOOD)
        wrapper.unmount()
    })

    it('сохранение отправляет отметку и вторую сторону', async () => {
        const wrapper = await openModal()
        await transferCheckbox(wrapper).setValue(true)
        await flushPromises()
        await pairSelect(wrapper).setValue('21')

        const body = await savePayment(wrapper)

        expect(body).toMatchObject({ IsTransfer: true, TransferPairPaymentId: 21, CategoryId: FOOD })
        wrapper.unmount()
    })

    it('без изменений перевода поля перевода не отправляются', async () => {
        const wrapper = await openModal()

        const body = await savePayment(wrapper)

        expect(body).not.toHaveProperty('IsTransfer')
        expect(body).not.toHaveProperty('TransferPairPaymentId')
        wrapper.unmount()
    })

    it('у связанного перевода «Не указывать» отправляет 0, снятие галочки — только IsTransfer: false', async () => {
        const linked = { ...outgoing, paymentType: 2, transferId: 5, transferPairPaymentId: 20 }

        let wrapper = await openModal(linked)
        expect(transferCheckbox(wrapper).element.checked).toBe(true)
        expect(pairSelect(wrapper).element.value).toBe('20')
        await pairSelect(wrapper).setValue('')
        expect(await savePayment(wrapper)).toMatchObject({ TransferPairPaymentId: 0 })
        expect(axios.put.mock.calls.at(-1)[1]).not.toHaveProperty('IsTransfer')
        wrapper.unmount()

        wrapper = await openModal(linked)
        await transferCheckbox(wrapper).setValue(false)
        const body = await savePayment(wrapper)
        expect(body).toMatchObject({ IsTransfer: false })
        expect(body).not.toHaveProperty('TransferPairPaymentId')
        wrapper.unmount()
    })
})
