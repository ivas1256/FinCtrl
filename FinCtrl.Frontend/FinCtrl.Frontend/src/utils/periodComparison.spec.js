import { describe, it, expect } from 'vitest'
import {
  compare,
  compareNode,
  getSemantics,
  isIncomeSemantics,
  magnitude,
  Semantics,
  ComparisonState
} from './periodComparison'
import { formatDelta } from './format'

// Примеры из СР §7: S_П → S_Т
const cell = (sPrev, sCur) => {
  const r = compare(sPrev, sCur)
  return formatDelta(r.deltaRub, r.pct)
}

describe('compare — примеры СР §7', () => {
  it('Продукты 31 200 → 24 650: снижение', () => {
    const r = compare(31200, 24650)
    expect(r.state).toBe(ComparisonState.Down)
    expect(cell(31200, 24650)).toBe('−6\u00A0550\u00A0₽ (−21\u00A0%)')
  })

  it('Кафе 7 400 → 11 900: рост', () => {
    expect(compare(7400, 11900).state).toBe(ComparisonState.Up)
    expect(cell(7400, 11900)).toBe('+4\u00A0500\u00A0₽ (+61\u00A0%)')
  })

  it('Подарки 0 → 3 800: новая', () => {
    expect(compare(0, 3800).state).toBe(ComparisonState.New)
    expect(cell(0, 3800)).toBe('+3\u00A0800\u00A0₽ (новая)')
  })

  it('Здоровье 1 900 → 0: −100 %', () => {
    expect(compare(1900, 0).pct).toBe(-100)
    expect(cell(1900, 0)).toBe('−1\u00A0900\u00A0₽ (−100\u00A0%)')
  })

  it('Итого 51 600 → 52 450: +2 %', () => {
    expect(cell(51600, 52450)).toBe('+850\u00A0₽ (+2\u00A0%)')
  })

  it('Рынок 3 300 → 3 350: +50 ₽ (+2 %), округление 1,52 вверх', () => {
    expect(cell(3300, 3350)).toBe('+50\u00A0₽ (+2\u00A0%)')
  })

  it('нетто-возврат в П (S_П < 0): процент от модуля, знак совпадает с Δ₽', () => {
    const r = compare(-500, 700)
    expect(r.pct).toBe(240)
    expect(r.direction).toBe(1)
    expect(cell(-500, 700)).toBe('+1\u00A0200\u00A0₽ (+240\u00A0%)')
  })

  it('S_П < 0, Δ₽ < 0: процент отрицательный', () => {
    const r = compare(-500, -700)
    expect(r.direction).toBe(-1)
    expect(r.pct).toBe(-40)
  })
})

describe('compare — состояния', () => {
  it('нет данных в обоих периодах', () => {
    expect(compare(0, 0)).toMatchObject({ state: ComparisonState.None, pct: null, direction: 0 })
    expect(compare(undefined, null).state).toBe(ComparisonState.None)
  })

  it('|Δ₽| < 0,5 — без изменений', () => {
    expect(compare(1000, 1000.4)).toMatchObject({ state: ComparisonState.Flat, pct: 0, direction: 0 })
    expect(cell(1000, 1000)).toBe('0\u00A0₽ (0\u00A0%)')
  })

  it('Δ₽ = 0,5 округляется от нуля — это уже изменение', () => {
    expect(compare(1000, 1000.5).direction).toBe(1)
  })

  it('мелкое изменение: Δ₽ ≠ 0, |Δ%| < 0,5 — «<1 %» по знаку Δ₽', () => {
    expect(compare(100000, 100050)).toMatchObject({ state: ComparisonState.Up, pct: 'small' })
    expect(cell(100000, 99950)).toBe('−50\u00A0₽ (<1\u00A0%)')
  })

  it('граница 0,5 % — уже «1 %»', () => {
    expect(compare(10000, 10050).pct).toBe(1)
  })

  it('новая с отрицательной суммой (нетто-возврат) — направление по знаку', () => {
    expect(compare(0, -300)).toMatchObject({ state: ComparisonState.New, direction: -1 })
  })
})

describe('magnitude / семантика', () => {
  it('траты: S = −v, возврат уменьшает S', () => {
    expect(magnitude(Semantics.Expense, -31200)).toBe(31200)
    expect(magnitude(Semantics.Expense, 500)).toBe(-500)
  })

  it('доходная категория: S = +v', () => {
    expect(magnitude(Semantics.Income, 100000)).toBe(100000)
  })

  it('«Накопления»: S = −v', () => {
    expect(magnitude(Semantics.Savings, -20000)).toBe(20000)
  })

  it('getSemantics: доход, «Накопления» (id 33) и потомки накоплений', () => {
    expect(getSemantics({ categoryId: 5, isOnlyIncome: false })).toBe(Semantics.Expense)
    expect(getSemantics({ categoryId: 7, isOnlyIncome: true })).toBe(Semantics.Income)
    expect(getSemantics({ categoryId: 33, isOnlyIncome: false })).toBe(Semantics.Savings)
    expect(getSemantics({ categoryId: 90, isOnlyIncome: false }, true)).toBe(Semantics.Savings)
  })

  it('доходная семантика цвета — у доходов и «Накоплений»', () => {
    expect(isIncomeSemantics(Semantics.Income)).toBe(true)
    expect(isIncomeSemantics(Semantics.Savings)).toBe(true)
    expect(isIncomeSemantics(Semantics.Expense)).toBe(false)
  })
})

describe('compareNode', () => {
  it('траты: суммы из node.comparison со знаком API', () => {
    const node = { categoryId: 5, isOnlyIncome: false, comparison: { current: -24650, previous: -31200 } }
    expect(compareNode(node, Semantics.Expense)).toMatchObject({ state: ComparisonState.Down, pct: -21 })
  })

  it('доход: рост поступлений — рост', () => {
    const node = { comparison: { current: 110000, previous: 100000 } }
    expect(compareNode(node, Semantics.Income)).toMatchObject({ state: ComparisonState.Up, pct: 10 })
  })

  it('нет блока comparison — нет данных', () => {
    expect(compareNode({}, Semantics.Expense).state).toBe(ComparisonState.None)
  })
})
