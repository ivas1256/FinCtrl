import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import CategoryNode from './CategoryNode.vue'

const cafe = { categoryId: 2, categoryName: 'Кафе', color: '#ff0000', orderIndex: 2, children: [] }
const food = { categoryId: 1, categoryName: 'Еда', color: '', orderIndex: 1, children: [cafe] }

const names = (wrapper) => wrapper.findAll('.node-name').map((el) => el.text())

describe('CategoryNode', () => {
    it('выводит узел и его детей рекурсивно с отступом по глубине', () => {
        const wrapper = mount(CategoryNode, { props: { node: food } })

        expect(names(wrapper)).toEqual(['Еда', 'Кафе'])
        const nodes = wrapper.findAll('.category-node')
        expect(nodes[0].attributes('style')).toContain('padding-left: 0px')
        expect(nodes[1].attributes('style')).toContain('padding-left: 20px')
        expect(wrapper.text()).toContain('#1')
    })

    it('кнопка сворачивает и разворачивает детей; у листа кнопки нет', async () => {
        const wrapper = mount(CategoryNode, { props: { node: food } })
        const toggle = wrapper.find('.expand-btn')
        expect(wrapper.findAll('.expand-btn')).toHaveLength(1)
        expect(toggle.text()).toBe('▼')

        await toggle.trigger('click')
        expect(names(wrapper)).toEqual(['Еда'])
        expect(toggle.text()).toBe('▶')

        await toggle.trigger('click')
        expect(names(wrapper)).toEqual(['Еда', 'Кафе'])
    })

    it('действия узла отдают сам узел', async () => {
        const wrapper = mount(CategoryNode, { props: { node: cafe } })

        await wrapper.find('[title="Добавить подкатегорию"]').trigger('click')
        await wrapper.find('[title="Редактировать"]').trigger('click')
        await wrapper.find('[title="Удалить"]').trigger('click')

        expect(wrapper.emitted('add-child')).toEqual([[cafe]])
        expect(wrapper.emitted('edit')).toEqual([[cafe]])
        expect(wrapper.emitted('delete')).toEqual([[cafe]])
    })

    it('действия дочернего узла всплывают до корня с дочерним узлом', async () => {
        const wrapper = mount(CategoryNode, { props: { node: food } })
        const child = wrapper.findAll('.category-node')[1]

        await child.find('[title="Редактировать"]').trigger('click')
        await child.find('[title="Удалить"]').trigger('click')
        await child.find('[title="Добавить подкатегорию"]').trigger('click')

        expect(wrapper.emitted('edit')).toEqual([[cafe]])
        expect(wrapper.emitted('delete')).toEqual([[cafe]])
        expect(wrapper.emitted('add-child')).toEqual([[cafe]])
    })

    it('цвет категории, по умолчанию — серый', () => {
        const wrapper = mount(CategoryNode, { props: { node: food } })
        const dots = wrapper.findAll('.node-color-dot')

        // jsdom приводит цвет к rgb(): #808080 и #ff0000
        expect(dots[0].element.style.background).toBe('rgb(128, 128, 128)')
        expect(dots[1].element.style.background).toBe('rgb(255, 0, 0)')
    })

    it('доходная категория помечена бейджем', () => {
        const income = mount(CategoryNode, { props: { node: { ...cafe, isOnlyIncome: true } } })
        const spending = mount(CategoryNode, { props: { node: cafe } })

        expect(income.text()).toContain('Только доход')
        expect(spending.text()).not.toContain('Только доход')
    })
})
