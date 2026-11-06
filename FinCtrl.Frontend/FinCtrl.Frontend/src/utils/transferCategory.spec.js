import { describe, it, expect } from 'vitest'
import { resolveTransferCategory } from './transferCategory'
import { SAVINGS_CATEGORY_ID } from './periodComparison'

const FOOD = 7

describe('resolveTransferCategory', () => {
    it('перевод на накопительный счёт подставляет «Накопления» и запоминает прежнюю категорию', () => {
        expect(resolveTransferCategory({ isTransfer: true, pairIsSavings: true, categoryId: FOOD, auto: null }))
            .toEqual({ categoryId: SAVINGS_CATEGORY_ID, auto: { previousCategoryId: FOOD } })
    })

    it('без категории подставляет «Накопления» и запоминает пустую категорию', () => {
        expect(resolveTransferCategory({ isTransfer: true, pairIsSavings: true, categoryId: '', auto: null }))
            .toEqual({ categoryId: SAVINGS_CATEGORY_ID, auto: { previousCategoryId: '' } })
    })

    it('если «Накопления» уже выбраны, подстановки нет — откатывать будет нечего', () => {
        expect(resolveTransferCategory({ isTransfer: true, pairIsSavings: true, categoryId: SAVINGS_CATEGORY_ID, auto: null }))
            .toEqual({ categoryId: SAVINGS_CATEGORY_ID, auto: null })
    })

    it('вторая сторона не на накопительном счёте — категория не меняется', () => {
        expect(resolveTransferCategory({ isTransfer: true, pairIsSavings: false, categoryId: FOOD, auto: null }))
            .toEqual({ categoryId: FOOD, auto: null })
    })

    it('повторный вызов после подстановки ничего не меняет', () => {
        const auto = { previousCategoryId: FOOD }
        expect(resolveTransferCategory({ isTransfer: true, pairIsSavings: true, categoryId: SAVINGS_CATEGORY_ID, auto }))
            .toEqual({ categoryId: SAVINGS_CATEGORY_ID, auto })
    })

    it('снятие галочки возвращает категорию, бывшую до подстановки', () => {
        expect(resolveTransferCategory({
            isTransfer: false, pairIsSavings: true, categoryId: SAVINGS_CATEGORY_ID, auto: { previousCategoryId: FOOD }
        })).toEqual({ categoryId: FOOD, auto: null })
    })

    it('выбор второй стороны не на накопительном счёте возвращает прежнюю категорию', () => {
        expect(resolveTransferCategory({
            isTransfer: true, pairIsSavings: false, categoryId: SAVINGS_CATEGORY_ID, auto: { previousCategoryId: FOOD }
        })).toEqual({ categoryId: FOOD, auto: null })
    })

    it('категорию, выбранную вручную после подстановки, снятие галочки не трогает', () => {
        expect(resolveTransferCategory({ isTransfer: false, pairIsSavings: true, categoryId: 12, auto: { previousCategoryId: FOOD } }))
            .toEqual({ categoryId: 12, auto: null })
    })

    it('снятие галочки без подстановки не трогает «Накопления», выбранные пользователем', () => {
        expect(resolveTransferCategory({ isTransfer: false, pairIsSavings: true, categoryId: SAVINGS_CATEGORY_ID, auto: null }))
            .toEqual({ categoryId: SAVINGS_CATEGORY_ID, auto: null })
    })
})
