import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import AlertMessage from './AlertMessage.vue'

describe('AlertMessage', () => {
    it('без сообщения ничего не рендерит', () => {
        const wrapper = mount(AlertMessage, { props: { message: null } })
        expect(wrapper.find('.alert').exists()).toBe(false)
    })

    it('по умолчанию — ошибка (alert-danger) с текстом сообщения', () => {
        const wrapper = mount(AlertMessage, { props: { message: 'Ошибка загрузки данных' } })
        const alert = wrapper.find('[role="alert"]')
        expect(alert.classes()).toContain('alert-danger')
        expect(alert.text()).toBe('Ошибка загрузки данных')
    })

    it('тип success даёт alert-success', () => {
        const wrapper = mount(AlertMessage, { props: { message: 'Готово', type: 'success' } })
        expect(wrapper.find('.alert').classes()).toContain('alert-success')
    })

    it('многострочное сообщение (ошибки валидации) сохраняет переводы строк', () => {
        const wrapper = mount(AlertMessage, { props: { message: 'Поле 1\nПоле 2' } })
        expect(wrapper.find('.alert-text').element.textContent).toBe('Поле 1\nПоле 2')
    })

    it('крестик генерирует close', async () => {
        const wrapper = mount(AlertMessage, { props: { message: 'Ошибка' } })
        await wrapper.find('.btn-close').trigger('click')
        expect(wrapper.emitted('close')).toHaveLength(1)
    })
})
