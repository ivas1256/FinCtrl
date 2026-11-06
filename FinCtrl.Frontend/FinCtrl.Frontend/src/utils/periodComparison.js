// Сравнение с прошлым периодом: величина строки и Δ (СР §5.3). Суммы Т и П приходят с backend
// (узел.comparison.current/previous, знаковые: расход < 0, поступление > 0).

import { roundHalfAwayFromZero, formatDeltaSpoken } from './format'
import { TRANSFERS_ID } from './heatmap'

// id категории «Накопления» (CategoryIds.Savings на backend)
export const SAVINGS_CATEGORY_ID = 33

export const Semantics = Object.freeze({
    Expense: 'expense',
    Income: 'income',
    Savings: 'savings',
    // «Переводы»: не траты и не доходы — Δ всегда серая
    Transfer: 'transfer'
})

// Семантика строки. Потомки «Накоплений» наследуют её — inSavings передаёт вызывающий при обходе дерева
export const getSemantics = (node, inSavings = false) => {
    if (node.categoryId === TRANSFERS_ID) return Semantics.Transfer
    if (inSavings || node.categoryId === SAVINGS_CATEGORY_ID) return Semantics.Savings
    return node.isOnlyIncome ? Semantics.Income : Semantics.Expense
}

// Доходная семантика цвета (СР О12): у доходов и «Накоплений» рост — хорошо
export const isIncomeSemantics = (semantics) =>
    semantics === Semantics.Income || semantics === Semantics.Savings

// Величина S: положительное число «в основную сторону» строки. Доходы и «Накопления» — +v, траты — −v
// («Накопления» на dev-базе приходят поступлениями, v > 0 — проверено в задаче 0028)
export const magnitude = (semantics, value) => {
    const v = value || 0
    return isIncomeSemantics(semantics) ? v : -v
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

// Тон цвета ячейки Δ: bad — красный (траты выросли / доход упал), good — зелёный, neutral — серый
export const DeltaTone = Object.freeze({
    Bad: 'bad',
    Good: 'good',
    Neutral: 'neutral'
})

const HIDDEN_TEXT = {
    [ComparisonState.Up]: 'рост',
    [ComparisonState.Down]: 'снижение',
    [ComparisonState.Flat]: 'без изменений',
    [ComparisonState.New]: 'новая трата',
    [ComparisonState.None]: 'нет данных в обоих периодах'
}

// «Переводы» — не траты: вместо «новая трата»
const TRANSFER_NEW_TEXT = 'новые переводы'

// Представление ячейки Δ (СР §2.4): { state, arrow, rub, pct, tone, hiddenText, semantics }.
// У «Переводов» tone всегда neutral: рост переводов — ни хорошо, ни плохо.
// rub/pct — для formatSignedRoundedCurrency/formatDeltaPercent; при state = none показывается «—»
export const deltaCellView = (node, semantics) => {
    const result = compareNode(node, semantics)
    const arrow = result.state === ComparisonState.None
        ? ''
        : result.direction > 0 ? '▲' : result.direction < 0 ? '▼' : '•'
    const isTransfer = semantics === Semantics.Transfer
    let tone = DeltaTone.Neutral
    if (result.direction !== 0 && !isTransfer) {
        const grew = result.direction > 0
        tone = grew !== isIncomeSemantics(semantics) ? DeltaTone.Bad : DeltaTone.Good
    }
    return {
        state: result.state,
        arrow,
        rub: result.deltaRub,
        pct: result.pct,
        tone,
        hiddenText: isTransfer && result.state === ComparisonState.New ? TRANSFER_NEW_TEXT : HIDDEN_TEXT[result.state],
        semantics
    }
}

// Охват карточки сравнения по categoryId: { node, parent, semantics } или null.
// parent — непосредственный родитель (для подписи «Родитель / Подкатегория»), у корня — null
export const findComparisonScope = (tree, categoryId, parent = null, inSavings = false) => {
    for (const node of tree || []) {
        const semantics = getSemantics(node, inSavings)
        if (node.categoryId === categoryId) return { node, parent, semantics }
        const found = findComparisonScope(node.children, categoryId, node, semantics === Semantics.Savings)
        if (found) return found
    }
    return null
}

// Ячейка Δ кликабельна, если есть данные хотя бы в одном периоде (СР §3.1)
export const isDeltaInteractive = (view) => view.state !== ComparisonState.None

const SPOKEN_STATE = {
    [ComparisonState.Up]: 'рост на',
    [ComparisonState.Down]: 'снижение на',
    [ComparisonState.New]: 'новая трата на'
}

// aria-label ячейки Δ (СР §2.4): "Продукты: октябрь против сентября, снижение на 6 550 ₽, минус 21 %. Открыть сравнение".
// phrase — formatComparisonPhrase из periodHeader
export const deltaAriaLabel = (name, phrase, view) => {
    const spokenState = view.semantics === Semantics.Transfer && view.state === ComparisonState.New
        ? `${TRANSFER_NEW_TEXT} на`
        : SPOKEN_STATE[view.state]
    const change = view.state === ComparisonState.Flat
        ? 'без изменений'
        : `${spokenState} ${formatDeltaSpoken(view.rub, view.pct)}`
    return `${name}: ${phrase}, ${change}. Открыть сравнение`
}
