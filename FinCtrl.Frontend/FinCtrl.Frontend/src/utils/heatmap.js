// Тепловая карта таблицы расходов — см. .ai-docs/payments-table-heatmap.md

export const PALETTE = [
  '#4e79a7', '#f28e2b', '#e15759', '#76b7b2',
  '#59a14f', '#edc948', '#b07aa1', '#ff9da7',
  '#9c755f', '#bab0ac'
]

export const catColor = (index) => PALETTE[(index || 0) % PALETTE.length]

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
  // value < 0 — расход
  return Math.abs(value) / maxV > 0.52 ? 'hm-hi' : ''
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

// Знак суммы отображается как есть (без Math.abs) — расход отрицательный, поступление положительное
export const formatHeatCurrency = (value) => {
  return new Intl.NumberFormat('ru-RU', {
    style: 'currency',
    currency: 'RUB',
    minimumFractionDigits: 2,
    signDisplay: 'exceptZero'
  }).format(value || 0)
}
