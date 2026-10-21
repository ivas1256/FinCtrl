import { describe, it, expect, vi, afterEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { nextTick } from 'vue'
import axios from 'axios'
import { APP_CONFIG } from '../config'
import CategoryTreeSelect from './CategoryTreeSelect.vue'

vi.mock('axios')

// Еда → Кафе → Кофейни; Доход — отдельный корень
const tree = [
    {
        categoryId: 1,
        categoryName: 'Еда',
        children: [
            { categoryId: 2, categoryName: 'Кафе', children: [{ categoryId: 3, categoryName: 'Кофейни', children: [] }] }
        ]
    },
    { categoryId: 4, categoryName: 'Доход' }
]

const optionTexts = (wrapper) => wrapper.findAll('option').map((option) => option.text())
const optionValues = (wrapper) => wrapper.findAll('option').map((option) => option.element.value)

describe('CategoryTreeSelect', () => {
    afterEach(() => {
        vi.restoreAllMocks()
        vi.clearAllMocks()
    })

    it('дерево из props — плоский список с отступом по уровню, без запроса к API', () => {
        const wrapper = mount(CategoryTreeSelect, { props: { categories: tree } })

        expect(optionTexts(wrapper)).toEqual(['', 'Еда', '- Кафе', '-- Кофейни', 'Доход'])
        expect(optionValues(wrapper)).toEqual(['', '1', '2', '3', '4'])
        expect(axios.get).not.toHaveBeenCalled()
    })

    it('узлы без categoryId пропускаются', () => {
        const wrapper = mount(CategoryTreeSelect, {
            props: { categories: [null, { categoryName: 'Без id' }, { categoryId: 5, categoryName: 'Быт' }] }
        })

        expect(optionTexts(wrapper)).toEqual(['', 'Быт'])
    })

    it('без props загружает дерево из API', async () => {
        axios.get.mockResolvedValue({ data: tree })
        const wrapper = mount(CategoryTreeSelect)

        await nextTick()
        expect(wrapper.find('select').element.disabled).toBe(true)
        await flushPromises()

        expect(axios.get).toHaveBeenCalledWith(APP_CONFIG.apiBaseUrl + '/api/CategoriesApi/getTree')
        expect(optionTexts(wrapper)).toEqual(['', 'Еда', '- Кафе', '-- Кофейни', 'Доход'])
        expect(wrapper.find('select').element.disabled).toBe(false)
    })

    it('autoLoad = false — не загружает дерево', () => {
        mount(CategoryTreeSelect, { props: { autoLoad: false } })

        expect(axios.get).not.toHaveBeenCalled()
    })

    it('ошибка загрузки — пустой список и разблокированный выбор', async () => {
        vi.spyOn(console, 'error').mockImplementation(() => {})
        axios.get.mockRejectedValue(new Error('Network Error'))
        const wrapper = mount(CategoryTreeSelect)

        await flushPromises()

        expect(optionTexts(wrapper)).toEqual([''])
        expect(wrapper.find('select').element.disabled).toBe(false)
    })

    it('выбор категории отдаёт id числом, пустой вариант — null', async () => {
        const wrapper = mount(CategoryTreeSelect, { props: { categories: tree, modelValue: null } })
        const select = wrapper.find('select')

        await select.setValue('2')
        await select.setValue('')

        expect(wrapper.emitted('update:modelValue')).toEqual([[2], [null]])
        expect(wrapper.emitted('change')).toEqual([[2], [null]])
    })

    it('текущее значение выбрано в списке, null — пустой вариант', async () => {
        const wrapper = mount(CategoryTreeSelect, { props: { categories: tree, modelValue: 3 } })
        expect(wrapper.find('select').element.value).toBe('3')

        await wrapper.setProps({ modelValue: null })
        expect(wrapper.find('select').element.value).toBe('')
    })

    it('размер задаёт класс Bootstrap', () => {
        expect(mount(CategoryTreeSelect, { props: { categories: [] } }).find('select').classes()).toContain('form-select-sm')
        expect(mount(CategoryTreeSelect, { props: { categories: [], size: 'lg' } }).find('select').classes()).toContain('form-select-lg')
        const md = mount(CategoryTreeSelect, { props: { categories: [], size: 'md' } }).find('select').classes()
        expect(md).not.toContain('form-select-sm')
        expect(md).not.toContain('form-select-lg')
    })
})
