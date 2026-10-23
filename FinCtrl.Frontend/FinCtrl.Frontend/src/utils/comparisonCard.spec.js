import { describe, it, expect } from 'vitest'
import {
  buildComparisonRows,
  topChanges,
  sortComparisonRows,
  barScale,
  barShare,
  SortMode
} from './comparisonCard'
import { Semantics, SAVINGS_CATEGORY_ID } from './periodComparison'
import { UNCATEGORIZED_ID } from './heatmap'

// Узел дерева; суммы знаковые (расход < 0): S_П → previous, S_Т → current
const node = (categoryId, categoryName, sPrev, sCur, children = [], extra = {}) => ({
  categoryId,
  categoryName,
  comparison: { current: -sCur, previous: -sPrev },
  children,
  ...extra
})

// Данные СР §7
const products = node(1, 'Продукты', 31200, 24650, [
  node(11, 'Супермаркеты', 27900, 21300),
  node(12, 'Рынок', 3300, 3350)
])
const cafe = node(2, 'Кафе и рестораны', 7400, 11900)
const transport = node(3, 'Транспорт', 4600, 3100, [
  node(31, 'Такси', 3100, 2300),
  node(32, 'Общественный транспорт', 1500, 800)
])
const sport = node(4, 'Спорт', 6500, 9000)
const gifts = node(5, 'Подарки', 0, 3800)
const health = node(6, 'Здоровье', 1900, 0)
const uncategorized = node(UNCATEGORIZED_ID, 'Без категории', 800, 1500)
const income = {
  categoryId: 50,
  categoryName: 'Зарплата',
  isOnlyIncome: true,
  comparison: { current: 110000, previous: 100000 },
  children: []
}
const savings = {
  categoryId: SAVINGS_CATEGORY_ID,
  categoryName: 'Накопления',
  comparison: { current: 5000, previous: 0 },
  children: []
}
const treeRows = [products, cafe, transport, sport, gifts, health, income, savings, uncategorized]

const names = (rows) => rows.map(r => r.name)

describe('buildComparisonRows — охват «Продукты»', () => {
  const model = buildComparisonRows({ node: products, semantics: Semantics.Expense })

  it('строки Супермаркеты и Рынок с Δ (СР 25)', () => {
    expect(names(model.rows)).toEqual(['Супермаркеты', 'Рынок'])
    expect(model.rows[0].view.rub).toBe(-6600)
    expect(model.rows[0].view.pct).toBe(-24)
    expect(model.rows[1].view.rub).toBe(50)
    expect(model.rows[1].view.pct).toBe(2)
  })

  it('топ: рост — Рынок, снижение — Супермаркеты (СР 25)', () => {
    const top = topChanges(model.rows)
    expect(names(top.growth)).toEqual(['Рынок'])
    expect(names(top.decline)).toEqual(['Супермаркеты'])
  })

  it('без собственных платежей родителя служебной строки нет', () => {
    expect(model.hasSubcategories).toBe(true)
    expect(model.rows.some(r => r.isService)).toBe(false)
  })
})

describe('buildComparisonRows — собственные платежи родителя', () => {
  const parent = node(1, 'Дом', 1000, 1500, [node(11, 'Ремонт', 600, 700)])
  const model = buildComparisonRows({ node: parent, semantics: Semantics.Expense })

  it('добавляет «{Родитель} (без подкатегории)» в конец (СР 29)', () => {
    expect(names(model.rows)).toEqual(['Ремонт', 'Дом (без подкатегории)'])
    const own = model.rows[1]
    expect(own.isService).toBe(true)
    expect(own.sPrev).toBe(400)
    expect(own.sCur).toBe(800)
  })

  it('сумма строк уровня равна сумме родителя', () => {
    const sum = (key) => model.rows.reduce((s, r) => s + r[key], 0)
    expect(sum('sPrev')).toBe(1000)
    expect(sum('sCur')).toBe(1500)
  })

  it('погрешность плавающей точки не даёт строку «(без подкатегории)»', () => {
    const p = node(1, 'Р', 0.3, 0.3, [node(11, 'А', 0.1, 0.1), node(12, 'Б', 0.2, 0.2)])
    const m = buildComparisonRows({ node: p, semantics: Semantics.Expense })
    expect(names(m.rows)).toEqual(['А', 'Б'])
  })

  it('«(без подкатегории)» есть и у внуков-родителей', () => {
    const root = node(1, 'Корень', 300, 300, [
      node(11, 'Середина', 300, 300, [node(111, 'Лист', 200, 250)])
    ])
    const m = buildComparisonRows({ node: root, semantics: Semantics.Expense })
    expect(names(m.rows[0].children)).toEqual(['Лист', 'Середина (без подкатегории)'])
    expect(m.rows[0].children[1].sCur).toBe(50)
  })
})

describe('buildComparisonRows — особые состояния', () => {
  it('лист — нет подкатегорий, строк нет (СР 30)', () => {
    const model = buildComparisonRows({ node: cafe, semantics: Semantics.Expense })
    expect(model.hasSubcategories).toBe(false)
    expect(model.rows).toEqual([])
    expect(topChanges(model.rows)).toEqual({ growth: [], decline: [] })
  })

  it('все дети пустые — единственная строка «(без подкатегории)»', () => {
    const parent = node(1, 'Дом', 100, 200, [node(11, 'Пусто', 0, 0)])
    const model = buildComparisonRows({ node: parent, semantics: Semantics.Expense })
    expect(names(model.rows)).toEqual(['Дом (без подкатегории)'])
    expect(topChanges(model.rows).growth).toHaveLength(1)
  })

  it('строки без данных в обоих периодах не включаются', () => {
    const parent = node(1, 'Дом', 100, 100, [node(11, 'Пусто', 0, 0), node(12, 'Есть', 100, 100)])
    const model = buildComparisonRows({ node: parent, semantics: Semantics.Expense })
    expect(names(model.rows)).toEqual(['Есть'])
  })

  it('семантика дохода наследуется подстроками', () => {
    const inc = {
      categoryId: 50,
      categoryName: 'Доход',
      isOnlyIncome: true,
      comparison: { current: 110, previous: 100 },
      children: [{ categoryId: 51, categoryName: 'Зарплата', comparison: { current: 110, previous: 100 }, children: [] }]
    }
    const model = buildComparisonRows({ node: inc, semantics: Semantics.Income })
    expect(model.rows[0].semantics).toBe(Semantics.Income)
    expect(model.rows[0].sCur).toBe(110)
    expect(model.rows[0].view.tone).toBe('good')
  })

  it('потомки «Накоплений» остаются «Накоплениями»', () => {
    const sv = { ...savings, children: [{ categoryId: 700, categoryName: 'Вклад', comparison: { current: 5000, previous: 0 }, children: [] }] }
    const model = buildComparisonRows({ node: sv, semantics: Semantics.Savings })
    expect(model.rows[0].semantics).toBe(Semantics.Savings)
  })
})

describe('buildComparisonRows — охват «Итого»', () => {
  const model = buildComparisonRows({ treeRows })

  it('корни трат без доходных и «Накоплений», «Без категории» в конце служебная', () => {
    expect(names(model.rows)).toEqual([
      'Продукты', 'Кафе и рестораны', 'Транспорт', 'Спорт', 'Подарки', 'Здоровье', 'Без категории'
    ])
    expect(model.rows[6].isService).toBe(true)
  })

  it('топ-3 роста и снижения (СР 26)', () => {
    const top = topChanges(model.rows)
    expect(names(top.growth)).toEqual(['Кафе и рестораны', 'Подарки', 'Спорт'])
    expect(names(top.decline)).toEqual(['Продукты', 'Здоровье', 'Транспорт'])
  })

  it('«По изменению» (СР 28)', () => {
    const sorted = sortComparisonRows(model.rows, SortMode.ByChange)
    expect(names(sorted)).toEqual([
      'Кафе и рестораны', 'Подарки', 'Спорт', 'Без категории', 'Транспорт', 'Здоровье', 'Продукты'
    ])
  })

  it('«По категориям» — порядок дерева, служебные в конце; исходный массив не мутирован', () => {
    const before = names(model.rows)
    const byChange = sortComparisonRows(model.rows, SortMode.ByChange)
    const byCategory = sortComparisonRows(byChange, SortMode.ByCategory)
    expect(names(byCategory)).toEqual(['Кафе и рестораны', 'Подарки', 'Спорт', 'Транспорт', 'Здоровье', 'Продукты', 'Без категории'])
    expect(names(model.rows)).toEqual(before)
  })

  it('«По категориям» на исходной модели сохраняет порядок дерева', () => {
    expect(names(sortComparisonRows(model.rows, SortMode.ByCategory))).toEqual(names(model.rows))
  })

  it('масштаб полос: M = 6 600 (внук), доля «Кафе» ≈ 0,68 (СР 27)', () => {
    const scale = barScale(model.rows)
    expect(scale).toBe(6600)
    expect(barShare(model.rows[1], scale)).toBeCloseTo(4500 / 6600, 5)
  })

  it('«Итого» без трат — нет подкатегорий', () => {
    const m = buildComparisonRows({ treeRows: [income, savings] })
    expect(m.hasSubcategories).toBe(false)
    expect(m.rows).toEqual([])
  })
})

describe('topChanges', () => {
  const rowsOf = (items) => buildComparisonRows({
    node: {
      categoryId: 1,
      categoryName: 'Р',
      comparison: {
        current: items.reduce((s, i) => s + i.comparison.current, 0),
        previous: items.reduce((s, i) => s + i.comparison.previous, 0)
      },
      children: items
    },
    semantics: Semantics.Expense
  }).rows

  it('меньше трёх — сколько есть; нет роста — пустой список', () => {
    const top = topChanges(rowsOf([node(11, 'А', 100, 50), node(12, 'Б', 100, 40)]))
    expect(top.growth).toEqual([])
    expect(names(top.decline)).toEqual(['Б', 'А'])
  })

  it('при равном Δ₽ — по |Δ%| убыв., затем по имени', () => {
    const top = topChanges(rowsOf([
      node(11, 'Б', 100, 200),
      node(12, 'А', 100, 200),
      node(13, 'В', 1000, 1100)
    ]))
    // Δ₽ у всех +100; Δ%: Б 100, А 100, В 10
    expect(names(top.growth)).toEqual(['А', 'Б', 'В'])
  })

  it('«новая» при равном Δ₽ выше процентной', () => {
    const top = topChanges(rowsOf([node(11, 'Я', 100, 200), node(12, 'А', 0, 100)]))
    expect(names(top.growth)).toEqual(['А', 'Я'])
  })

  it('ограничивает тремя', () => {
    const top = topChanges(rowsOf([1, 2, 3, 4].map(i => node(10 + i, `К${i}`, 100, 100 + i * 10))))
    expect(names(top.growth)).toEqual(['К4', 'К3', 'К2'])
  })
})

describe('sortComparisonRows и barScale на вложенных строках', () => {
  const root = node(1, 'Корень', 1000, 1000, [
    node(11, 'А', 500, 400, [node(111, 'А1', 300, 100), node(112, 'А2', 200, 300)]),
    node(12, 'Б', 500, 600)
  ])
  const model = buildComparisonRows({ node: root, semantics: Semantics.Expense })

  it('«По изменению»: внуки сортируются внутри родителя', () => {
    const sorted = sortComparisonRows(model.rows, SortMode.ByChange)
    expect(names(sorted)).toEqual(['Б', 'А'])
    expect(names(sorted[1].children)).toEqual(['А2', 'А1'])
  })

  it('M учитывает внуков', () => {
    expect(barScale(model.rows)).toBe(200)
  })

  it('M = 0 — доля 0 у всех', () => {
    const flat = buildComparisonRows({ node: node(1, 'Р', 100, 100, [node(11, 'А', 100, 100)]), semantics: Semantics.Expense })
    const scale = barScale(flat.rows)
    expect(scale).toBe(0)
    expect(barShare(flat.rows[0], scale)).toBe(0)
  })
})
