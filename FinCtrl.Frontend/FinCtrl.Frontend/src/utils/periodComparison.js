// Сравнение с прошлым периодом: величина строки и Δ (СР §5.3). Суммы Т и П приходят с backend
// (узел.comparison.current/previous, знаковые: расход < 0, поступление > 0).

import { roundHalfAwayFromZero } from './format'

// id категории «Накопления» (CategoryIds.Savings на backend)
export const SAVINGS_CATEGORY_ID = 33

export const Semantics = Object.freeze({
    Expense: 'expense',
    Income: 'income',
    Savings: 'savings'
})

// Семантика строки. Потомки «Накоплений» наследуют её — inSavings передаёт вызывающий при обходе дерева
export const getSemantics = (node, inSavings = false) => {
    if (inSavings || node.categoryId === SAVINGS_CATEGORY_ID) return Semantics.Savings
    return node.isOnlyIncome ? Semantics.Income : Semantics.Expense
}

// Доходная семантика цвета (СР О12): у доходов и «Накоплений» рост — хорошо
export const isIncomeSemantics = (semantics) =>
    semantics === Semantics.Income || semantics === Semantics.Savings

// Величина S: положительное число «в основную сторону» строки. Доходы — +v, траты и «Накопления» — −v
export const magnitude = (semantics, value) => {
    const v = value || 0
    return semantics === Semantics.Income ? v : -v
}

const ZERO = 1e-9

export const ComparisonState = Object.freeze({
    Up: 'up',
    Down: 'down',
    Flat: 'flat',
    New: 'new',
    None: 'none'
})

// Δ по величинам прошлого и текущего периода.
// Возвращает { state, direction, deltaRub, pct }: deltaRub не округлён, direction — знак округлённого Δ₽ (−1/0/1),
// pct — для formatDelta: число (целые проценты, знак = знаку Δ₽), 'new', 'small' (<1 %) или null (нет данных)
export const compare = (sPrev, sCur) => {
    const prev = sPrev || 0
    const cur = sCur || 0
    const deltaRub = cur - prev
    const rounded = roundHalfAwayFromZero(deltaRub)
    const direction = Math.sign(rounded)
    const prevIsZero = Math.abs(prev) < ZERO

    if (prevIsZero && Math.abs(cur) < ZERO) {
        return { state: ComparisonState.None, direction: 0, deltaRub: 0, pct: null }
    }
    if (rounded === 0) {
        return { state: ComparisonState.Flat, direction: 0, deltaRub, pct: 0 }
    }
    if (prevIsZero) {
        return { state: ComparisonState.New, direction, deltaRub, pct: 'new' }
    }

    const pctExact = (deltaRub / Math.abs(prev)) * 100
    const state = direction > 0 ? ComparisonState.Up : ComparisonState.Down
    if (Math.abs(pctExact) < 0.5) return { state, direction, deltaRub, pct: 'small' }
    return { state, direction, deltaRub, pct: roundHalfAwayFromZero(pctExact) }
}

// Δ узла: сумма Т и П из node.comparison {current, previous}
export const compareNode = (node, semantics) => {
    const { current, previous } = node.comparison || {}
    return compare(magnitude(semantics, previous), magnitude(semantics, current))
}
