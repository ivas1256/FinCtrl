// Карточка сравнения: строки таблицы, топ-3, сортировка, масштаб полос (СР §4.4–4.5, §4.7).
// Входы — узлы дерева ответа GET api/PaymentsApi (node.comparison {current, previous}, знаковые суммы).

import {
    Semantics,
    getSemantics,
    magnitude,
    deltaCellView,
    ComparisonState,
    SAVINGS_CATEGORY_ID
} from './periodComparison'
import { TRANSFERS_ID, UNCATEGORIZED_ID } from './heatmap'

export const SortMode = Object.freeze({
    ByCategory: 'category',
    ByChange: 'change'
})

// Допуск «собственных платежей»: полкопейки, чтобы погрешность сумм не давала пустую строку
const OWN_EPSILON = 0.005
const ZERO = 1e-9

const sumOf = (node, key) => (node.comparison && node.comparison[key]) || 0

// Строка карточки: { key, name, level, semantics, isService, current, previous, sPrev, sCur, view, children }.
// current/previous — знаковые суммы из API; sPrev/sCur — величины S (положительные «в основную сторону»)
const makeRow = ({ key, name, level, semantics, isService = false, current, previous, children = [] }) => {
    const sPrev = magnitude(semantics, previous)
    const sCur = magnitude(semantics, current)
    return {
        key,
        name,
        level,
        semantics,
        isService,
        current,
        previous,
        sPrev,
        sCur,
        view: deltaCellView({ comparison: { current, previous } }, semantics),
        children
    }
}

const isEmptyRow = (row) => Math.abs(row.sPrev) < ZERO && Math.abs(row.sCur) < ZERO

// Строки детей узла + строка «{Имя} (без подкатегории)». Пустые (S_П = S_Т = 0) отбрасываются
const buildChildRows = (node, semantics, level) => {
    const children = node.children || []
    const rows = []
    let childrenCurrent = 0
    let childrenPrevious = 0

    for (const child of children) {
        // Доход и «Накопления» наследуются потомками
        const childSemantics = semantics === Semantics.Expense ? getSemantics(child) : semantics
        const current = sumOf(child, 'current')
        const previous = sumOf(child, 'previous')
        childrenCurrent += current
        childrenPrevious += previous
        const row = makeRow({
            key: String(child.categoryId),
            name: child.categoryName,
            level,
            semantics: childSemantics,
            current,
            previous,
            children: buildChildRows(child, childSemantics, level + 1)
        })
        if (!isEmptyRow(row)) rows.push(row)
    }

    if (children.length > 0) {
        const ownCurrent = sumOf(node, 'current') - childrenCurrent
        const ownPrevious = sumOf(node, 'previous') - childrenPrevious
        if (Math.abs(ownCurrent) >= OWN_EPSILON || Math.abs(ownPrevious) >= OWN_EPSILON) {
            rows.push(makeRow({
                key: `own-${node.categoryId}`,
                name: `${node.categoryName} (без подкатегории)`,
                level,
                semantics,
                isService: true,
                current: ownCurrent,
                previous: ownPrevious
            }))
        }
    }
    return rows
}

// Модель карточки для охвата. scope: { node, semantics } — категория, либо { treeRows } — «Итого»
// (корни дерева без «ВСЕГО»). Возвращает { hasSubcategories, rows }
export const buildComparisonRows = (scope) => {
    if (scope.treeRows) {
        const roots = scope.treeRows.filter(n =>
            !n.isOnlyIncome && n.categoryId !== SAVINGS_CATEGORY_ID && n.categoryId !== TRANSFERS_ID)
        const rows = []
        for (const node of roots) {
            const isUncategorized = node.categoryId === UNCATEGORIZED_ID
            const row = makeRow({
                key: String(node.categoryId),
                name: node.categoryName,
                level: 0,
                semantics: Semantics.Expense,
                isService: isUncategorized,
                current: sumOf(node, 'current'),
                previous: sumOf(node, 'previous'),
                children: isUncategorized ? [] : buildChildRows(node, Semantics.Expense, 1)
            })
            if (!isEmptyRow(row)) rows.push(row)
        }
        // «Без категории» — в конце
        rows.sort((a, b) => Number(a.isService) - Number(b.isService))
        return { hasSubcategories: roots.length > 0, rows }
    }

    const { node, semantics } = scope
    const hasSubcategories = (node.children || []).length > 0
    if (!hasSubcategories) return { hasSubcategories, rows: [] }
    return {
        hasSubcategories,
        rows: buildChildRows(node, semantics, 0)
    }
}

const pctWeight = (pct) => {
    if (pct === 'new') return Infinity
    if (typeof pct === 'number') return Math.abs(pct)
    return 0
}

const byMagnitudeThenName = (a, b) =>
    pctWeight(b.view.pct) - pctWeight(a.view.pct) || a.name.localeCompare(b.name, 'ru')

// Топ-3 по непосредственным строкам охвата (СР §4.4): { growth, decline }
export const topChanges = (rows, limit = 3) => {
    const growth = rows
        .filter(r => r.view.state !== ComparisonState.None && r.view.rub > 0 && r.view.state !== ComparisonState.Flat)
        .sort((a, b) => b.view.rub - a.view.rub || byMagnitudeThenName(a, b))
    const decline = rows
        .filter(r => r.view.state !== ComparisonState.None && r.view.rub < 0 && r.view.state !== ComparisonState.Flat)
        .sort((a, b) => a.view.rub - b.view.rub || byMagnitudeThenName(a, b))
    return { growth: growth.slice(0, limit), decline: decline.slice(0, limit) }
}

// Сортировка строк (новый массив). «По категориям» — порядок дерева, служебные строки в конце;
// «По изменению» — по убыванию Δ₽ на каждом уровне. Порядок равных сохраняется (сортировка стабильна)
export const sortComparisonRows = (rows, mode) => {
    const sorted = rows.map(r => ({ ...r, children: sortComparisonRows(r.children, mode) }))
    if (mode === SortMode.ByChange) {
        return sorted.sort((a, b) => b.view.rub - a.view.rub)
    }
    return sorted.sort((a, b) => Number(a.isService) - Number(b.isService))
}

const flatten = (rows) => rows.flatMap(r => [r, ...flatten(r.children)])

// M = max |Δ₽| по всем строкам, включая внуков (СР §4.5)
export const barScale = (rows) =>
    flatten(rows).reduce((max, r) => Math.max(max, Math.abs(r.view.rub)), 0)

// Доля полосы 0..1; при M = 0 — 0
export const barShare = (row, scale) =>
    scale > 0 ? Math.abs(row.view.rub) / scale : 0

