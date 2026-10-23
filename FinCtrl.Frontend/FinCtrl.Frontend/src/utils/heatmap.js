// Тепловая карта таблицы расходов — см. .ai-docs/project/payments-table-heatmap.md

export const PALETTE = [
  '#4e79a7', '#f28e2b', '#e15759', '#76b7b2',
  '#59a14f', '#edc948', '#b07aa1', '#ff9da7',
  '#9c755f', '#bab0ac'
]

export const catColor = (index) => PALETTE[(index || 0) % PALETTE.length]

// Id фейковой категории «Без категории» (CategoryIds.FakeEmpty на backend = int.MaxValue - 1)
export const UNCATEGORIZED_ID = 2147483646

export const DEFAULT_CATEGORY_COLOR = '#808080'

// Цвет категории задаётся на странице "Категории"; по умолчанию — серый
export const categoryColor = (category) => category?.color || DEFAULT_CATEGORY_COLOR

// maxV — максимальный |totalSum| по всем ячейкам выборки
// v    — значение конкретной ячейки (может быть отрицательным)
// Шкала строится только по расходам — поступления (v > 0) в неё не входят
export const heatAlpha = (v, maxV) => {
  if (!v || v > 0 || !maxV) return 0
  return 0.04 + (Math.abs(v) / maxV) * 0.32 // диапазон: 0.04 – 0.36, ненавязчивый фон
}

export const heatStyle = (value, maxV) => {
  const alpha = heatAlpha(value, maxV)
  if (!alpha) return {}
  return { background: `rgba(200, 76, 78, ${alpha.toFixed(2)})` }
}

export const heatClass = (value, maxV) => {
  if (!value) return 'hm-zero'
  if (value > 0) return 'hm-income'
  // value < 0 — расход: нейтральный текст, интенсивность передаёт только фон
  return ''
}

// Пересчитывается каждый раз при загрузке данных, включает родительские и дочерние строки
export const computeMaxValue = (tree) => {
  let max = 0
  const traverse = (nodes) => (nodes || []).forEach(node => {
    Object.values(node.periods || {}).forEach(p => {
      const abs = Math.abs(p.totalSum || 0)
      if (abs > max) max = abs
    })
    if (node.children?.length) traverse(node.children)
  })
  traverse(tree)
  return max
}

// Id фейковой категории «ВСЕГО» (CategoryIds.FakeTotal на backend = int.MaxValue)
export const TOTAL_CATEGORY_ID = 2147483647

// «ВСЕГО» считает backend: в тело таблицы и в шкалу тепла оно не входит, из него берётся «Итого»
export const splitTotalNode = (tree) => {
  const nodes = tree || []
  return {
    rows: nodes.filter(n => n.categoryId !== TOTAL_CATEGORY_ID),
    totalNode: nodes.find(n => n.categoryId === TOTAL_CATEGORY_ID) || null
  }
}

// Есть ли хоть одна ненулевая сумма в узлах и их потомках
export const hasAnySum = (tree) => (tree || []).some(node =>
  Object.values(node.periods || {}).some(p => p.totalSum) || hasAnySum(node.children)
)
