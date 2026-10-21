import { describe, it, expect, vi, afterEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import axios from 'axios'
import Multiselect from 'vue-multiselect'
import TagMultiSelect from './TagMultiSelect.vue'

vi.mock('axios')

const mountSelect = () =>
    mount(TagMultiSelect, { props: { modelValue: [1], tags: [{ tagId: 1, tagName: 'еда' }] } })

describe('TagMultiSelect', () => {
    afterEach(() => {
        vi.restoreAllMocks()
    })

    it('создаёт тег и добавляет его к выбранным', async () => {
        axios.post.mockResolvedValue({ data: { tagId: 2, tagName: 'отпуск' } })
        const wrapper = mountSelect()

        wrapper.findComponent(Multiselect).vm.$emit('tag', 'отпуск')
        await flushPromises()

        expect(wrapper.emitted('update:modelValue')).toEqual([[[1, 2]]])
        expect(wrapper.emitted('created')).toEqual([[{ tagId: 2, tagName: 'отпуск' }]])
        expect(wrapper.find('[role="alert"]').exists()).toBe(false)
    })

    it('ошибку создания тега показывает под полем, без alert()', async () => {
        const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {})
        vi.spyOn(console, 'error').mockImplementation(() => {})
        axios.post.mockRejectedValue({ response: { data: { title: 'Тег с таким названием уже существует' } } })
        const wrapper = mountSelect()

        wrapper.findComponent(Multiselect).vm.$emit('tag', 'еда')
        await flushPromises()

        expect(wrapper.find('[role="alert"]').text()).toBe('Тег с таким названием уже существует')
        expect(wrapper.emitted('update:modelValue')).toBeUndefined()
        expect(alertSpy).not.toHaveBeenCalled()
    })
})
