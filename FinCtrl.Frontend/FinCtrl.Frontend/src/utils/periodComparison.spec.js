import { describe, it, expect } from 'vitest'
import {
  compare,
  compareNode,
  getSemantics,
  isIncomeSemantics,
  magnitude,
  Semantics,
  ComparisonState,
  deltaCellView,
  DeltaTone,
  findComparisonScope,
  isDeltaInteractive,
  deltaAriaLabel,
  SAVINGS_CATEGORY_ID
} from './periodComparison'
import { formatDelta } from './format'
import { TRANSFERS_ID } from './heatmap'

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

  it('«Накопления»: S = +v (перевод в накопления — поступление)', () => {
    expect(magnitude(Semantics.Savings, 20000)).toBe(20000)
  })

  it('getSemantics: доход, «Накопления» (id 33) и потомки накоплений', () => {
    expect(getSemantics({ categoryId: 5, isOnlyIncome: false })).toBe(Semantics.Expense)
    expect(getSemantics({ categoryId: 7, isOnlyIncome: true })).toBe(Semantics.Income)
    expect(getSemantics({ categoryId: 33, isOnlyIncome: false })).toBe(Semantics.Savings)
    expect(getSemantics({ categoryId: 90, isOnlyIncome: false }, true)).toBe(Semantics.Savings)
  })

  it('getSemantics: «Переводы» — своя семантика, не доходная', () => {
    expect(getSemantics({ categoryId: TRANSFERS_ID, isOnlyIncome: false })).toBe(Semantics.Transfer)
    expect(isIncomeSemantics(Semantics.Transfer)).toBe(false)
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

describe('deltaCellView', () => {
  const node = (previous, current, extra = {}) => ({ categoryId: 5, isOnlyIncome: false, comparison: { previous, current }, ...extra })

  it('рост трат — ▲, красный, «рост»', () => {
    const v = deltaCellView(node(-7400, -11900), Semantics.Expense)
    expect(v).toMatchObject({ arrow: '▲', tone: DeltaTone.Bad, hiddenText: 'рост', rub: 4500, pct: 61 })
  })

  it('снижение трат — ▼, зелёный, «снижение»', () => {
    const v = deltaCellView(node(-31200, -24650), Semantics.Expense)
    expect(v).toMatchObject({ arrow: '▼', tone: DeltaTone.Good, hiddenText: 'снижение', rub: -6550, pct: -21 })
  })

  it('новая — ▲, красный, «новая трата»', () => {
    expect(deltaCellView(node(0, -3800), Semantics.Expense))
      .toMatchObject({ arrow: '▲', tone: DeltaTone.Bad, hiddenText: 'новая трата', pct: 'new' })
  })

  it('исчезнувшая — ▼, зелёный, −100 %', () => {
    expect(deltaCellView(node(-1900, 0), Semantics.Expense))
      .toMatchObject({ arrow: '▼', tone: DeltaTone.Good, pct: -100 })
  })

  it('без изменений — •, серый', () => {
    expect(deltaCellView(node(-1000, -1000.3), Semantics.Expense))
      .toMatchObject({ arrow: '•', tone: DeltaTone.Neutral, hiddenText: 'без изменений', pct: 0 })
  })

  it('нет данных — без стрелки, серый, прочерк', () => {
    const v = deltaCellView({ categoryId: 5 }, Semantics.Expense)
    expect(v).toMatchObject({ state: ComparisonState.None, arrow: '', tone: DeltaTone.Neutral, pct: null })
  })

  it('доходная строка: рост — зелёный ▲, падение — красный ▼', () => {
    expect(deltaCellView(node(100000, 110000, { isOnlyIncome: true }), Semantics.Income))
      .toMatchObject({ arrow: '▲', tone: DeltaTone.Good, rub: 10000, pct: 10 })
    expect(deltaCellView(node(100000, 90000, { isOnlyIncome: true }), Semantics.Income))
      .toMatchObject({ arrow: '▼', tone: DeltaTone.Bad })
  })

  it('«Переводы»: рост и снижение — серые, новая — «новые переводы»', () => {
    expect(deltaCellView(node(-1000, -5000), Semantics.Transfer)).toMatchObject({ arrow: '▲', tone: DeltaTone.Neutral })
    expect(deltaCellView(node(-5000, -1000), Semantics.Transfer)).toMatchObject({ arrow: '▼', tone: DeltaTone.Neutral })
    expect(deltaCellView(node(0, -3800), Semantics.Transfer))
      .toMatchObject({ tone: DeltaTone.Neutral, hiddenText: 'новые переводы', pct: 'new' })
  })

  it('«Накопления»: рост — зелёный', () => {
    expect(deltaCellView(node(1000, 2000), Semantics.Savings)).toMatchObject({ arrow: '▲', tone: DeltaTone.Good })
  })
})

describe('findComparisonScope', () => {
  const tree = [
    { categoryId: 5, categoryName: 'Продукты', children: [{ categoryId: 51, categoryName: 'Рынок', children: [] }] },
    { categoryId: SAVINGS_CATEGORY_ID, categoryName: 'Накопления', children: [{ categoryId: 331, categoryName: 'Вклад', children: [] }] },
    { categoryId: 7, categoryName: 'Зарплата', isOnlyIncome: true, children: [] }
  ]

  it('корень — без родителя, семантика трат', () => {
    expect(findComparisonScope(tree, 5)).toMatchObject({ node: tree[0], parent: null, semantics: Semantics.Expense })
  })

  it('дочерняя — с непосредственным родителем', () => {
    const scope = findComparisonScope(tree, 51)
    expect(scope.node.categoryName).toBe('Рынок')
    expect(scope.parent).toBe(tree[0])
  })

  it('потомок «Накоплений» наследует семантику', () => {
    expect(findComparisonScope(tree, 331).semantics).toBe(Semantics.Savings)
  })

  it('доходная категория', () => expect(findComparisonScope(tree, 7).semantics).toBe(Semantics.Income))

  it('не найдено — null', () => expect(findComparisonScope(tree, 999)).toBeNull())
})

describe('ячейка Δ: интерактивность и aria-label', () => {
  const view = (previous, current, semantics = Semantics.Expense) =>
    deltaCellView({ categoryId: 5, comparison: { previous, current } }, semantics)
  const phrase = 'октябрь против сентября'

  it('«—» не кликабельна, остальные — кликабельны', () => {
    expect(isDeltaInteractive(view(0, 0))).toBe(false)
    expect(isDeltaInteractive(view(-1000, -1000))).toBe(true)
    expect(isDeltaInteractive(view(0, -3800))).toBe(true)
  })

  it('снижение — пример СР §2.4', () => {
    expect(deltaAriaLabel('Продукты', phrase, view(-31200, -24650)))
      .toBe('Продукты: октябрь против сентября, снижение на 6\u00A0550\u00A0₽, минус 21\u00A0%. Открыть сравнение')
  })

  it('рост, новая, мелкое изменение, без изменений', () => {
    expect(deltaAriaLabel('Кафе', phrase, view(-7400, -11900))).toContain('рост на 4\u00A0500\u00A0₽, плюс 61\u00A0%.')
    expect(deltaAriaLabel('Подарки', phrase, view(0, -3800))).toContain('новая трата на 3\u00A0800\u00A0₽.')
    expect(deltaAriaLabel('Рынок', phrase, view(-30000, -30050))).toContain('рост на 50\u00A0₽, менее 1\u00A0%.')
    expect(deltaAriaLabel('Связь', phrase, view(-1000, -1000))).toBe('Связь: октябрь против сентября, без изменений. Открыть сравнение')
    expect(deltaAriaLabel('Переводы', phrase, view(0, -3800, Semantics.Transfer))).toContain('новые переводы на 3 800 ₽.')
  })
})
