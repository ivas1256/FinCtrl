import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import axios from 'axios'
import PaymentsImportModal from './PaymentsImportModal.vue'
import { usePaymentsImport } from '../composables/usePaymentsImport'

vi.mock('axios', () => ({ default: { post: vi.fn() } }))
vi.mock('bootstrap', () => ({ Modal: { getOrCreateInstance: () => ({ show: vi.fn(), hide: vi.fn() }) } }))

const response = (data) => ({
    data: { newPayments: [], newPaymentSources: [], classificationError: null, ...data }
})

const selectFile = async (wrapper) => {
    const input = wrapper.find('input[type="file"]')
    Object.defineProperty(input.element, 'files', {
        value: [new File(['x'], 'выписка.xlsx')],
        configurable: true
    })
    await input.trigger('change')
}

const uploadButton = (wrapper) => wrapper.findAll('button').find(b => b.text() === 'Загрузить')

describe('PaymentsImportModal', () => {
    beforeEach(() => {
        axios.post.mockReset()
    })

    it('кнопка «Загрузить» недоступна, пока не выбран файл', async () => {
        const wrapper = mount(PaymentsImportModal)
        expect(uploadButton(wrapper).attributes('disabled')).toBeDefined()

        await selectFile(wrapper)

        expect(uploadButton(wrapper).attributes('disabled')).toBeUndefined()
    })

    it('во время загрузки показывает лоадер и блокирует кнопку', async () => {
        let resolve
        axios.post.mockReturnValue(new Promise(r => { resolve = r }))
        const wrapper = mount(PaymentsImportModal)
        await selectFile(wrapper)

        await uploadButton(wrapper).trigger('click')

        expect(wrapper.find('.upload-progress .spinner-border').exists()).toBe(true)
        expect(uploadButton(wrapper).attributes('disabled')).toBeDefined()

        resolve(response({}))
        await flushPromises()
        expect(wrapper.find('.upload-progress').exists()).toBe(false)
    })

    it('после загрузки показывает вкладки с новыми платежами и источниками', async () => {
        axios.post.mockResolvedValue(response({
            newPayments: [{ paymentId: 1, paymentSum: -250, paymentDate: '2025-10-01T00:00:00', paymentSource: { paymentSourceName: 'YANDEX TAXI' } }],
            newPaymentSources: [
                { paymentSourceId: 3, paymentSourceName: 'YANDEX TAXI', classification: 'Такси', isAiClassified: true },
                { paymentSourceId: 4, paymentSourceName: 'IP IVANOV', classification: null, isAiClassified: false }
            ]
        }))
        const { importsCompleted } = usePaymentsImport()
        const before = importsCompleted.value
        const wrapper = mount(PaymentsImportModal)
        await selectFile(wrapper)

        await uploadButton(wrapper).trigger('click')
        await flushPromises()

        const tabs = wrapper.findAll('.btn-group button')
        expect(tabs.map(t => t.text())).toEqual(['Новые платежи (1)', 'Новые источники платежей (2)'])
        expect(wrapper.find('.payments-tab').text()).toContain('YANDEX TAXI')
        expect(importsCompleted.value).toBe(before + 1)

        await tabs[1].trigger('click')
        const rows = wrapper.findAll('.sources-tab tbody tr')
        expect(rows).toHaveLength(2)
        expect(rows[0].text()).toContain('Такси')
        expect(rows[0].find('.badge').text()).toBe('ИИ')
        expect(rows[1].find('.badge').exists()).toBe(false)
    })

    it('ошибка классификации показывается предупреждением', async () => {
        axios.post.mockResolvedValue(response({ classificationError: 'Сервис классификации недоступен' }))
        const wrapper = mount(PaymentsImportModal)
        await selectFile(wrapper)

        await uploadButton(wrapper).trigger('click')
        await flushPromises()

        expect(wrapper.find('.alert-warning').text()).toContain('Сервис классификации недоступен')
    })

    it('ошибка загрузки показывается в модалке', async () => {
        axios.post.mockRejectedValue({ response: { data: { detail: 'В файле нет операций' } } })
        const wrapper = mount(PaymentsImportModal)
        await selectFile(wrapper)

        await uploadButton(wrapper).trigger('click')
        await flushPromises()

        expect(wrapper.find('.alert-danger').text()).toContain('В файле нет операций')
        expect(wrapper.find('.btn-group').exists()).toBe(false)
    })
})
