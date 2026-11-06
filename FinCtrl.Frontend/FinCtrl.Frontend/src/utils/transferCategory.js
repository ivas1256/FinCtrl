// Автоподстановка категории «Накопления» в модалке правки платежа при отметке перевода между своими счетами.
// «Накопления» подставляются, если вторая сторона перевода на накопительном счёте (account.isSavings): так в «Накопления»
// попадает только сторона на обычном счёте (карта → вклад: списание с карты; вклад → карта: поступление на карту).

import { SAVINGS_CATEGORY_ID } from './periodComparison'

// auto — состояние автоподстановки: null — не подставляли, { previousCategoryId } — подставили, запомнив прежнюю категорию.
// Возвращает новые { categoryId, auto }:
// - перевод со второй стороной на накопительном счёте → «Накопления» (если ещё не они), прежняя категория запоминается;
// - иначе, если «Накопления» подставлены автоматически и пользователь их не менял, → прежняя категория;
// - категорию, выбранную пользователем вручную после подстановки, не трогаем — только забываем подстановку.
export const resolveTransferCategory = ({ isTransfer, pairIsSavings, categoryId, auto }) => {
    const needSavings = isTransfer && pairIsSavings

    if (needSavings) {
        if (auto || categoryId === SAVINGS_CATEGORY_ID) return { categoryId, auto }
        return { categoryId: SAVINGS_CATEGORY_ID, auto: { previousCategoryId: categoryId } }
    }

    if (!auto) return { categoryId, auto }
    if (categoryId === SAVINGS_CATEGORY_ID) return { categoryId: auto.previousCategoryId, auto: null }
    return { categoryId, auto: null }
}
