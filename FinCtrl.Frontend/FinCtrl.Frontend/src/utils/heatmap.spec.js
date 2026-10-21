import { describe, it, expect } from 'vitest'
import {
  PALETTE,
  catColor,
  categoryColor,
  DEFAULT_CATEGORY_COLOR,
  heatAlpha,
  heatStyle,
  heatClass,
  computeMaxValue
} from './heatmap'

describe('catColor', () => {
  it('берёт цвет палитры по индексу и зацикливается', () => {
    expect(catColor(0)).toBe(PALETTE[0])
    expect(catColor(PALETTE.length + 1)).toBe(PALETTE[1])
  })

  it('без индекса возвращает первый цвет', () => {
    expect(catColor(undefined)).toBe(PALETTE[0])
  })
})

describe('categoryColor', () => {
  it('возвращает цвет категории', () => {
    expect(categoryColor({ color: '#123456' })).toBe('#123456')
  })

  it('без цвета или категории возвращает серый по умолчанию', () => {
    expect(categoryColor({})).toBe(DEFAULT_CATEGORY_COLOR)
    expect(categoryColor(null)).toBe(DEFAULT_CATEGORY_COLOR)
  })
})

describe('heatAlpha', () => {
  it('для нуля, поступлений и пустого максимума — 0', () => {
    expect(heatAlpha(0, 100)).toBe(0)
    expect(heatAlpha(50, 100)).toBe(0)
    expect(heatAlpha(-50, 0)).toBe(0)
  })

  it('для расхода растёт линейно от 0.04 до 0.36', () => {
    expect(heatAlpha(-100, 100)).toBeCloseTo(0.36)
    expect(heatAlpha(-50, 100)).toBeCloseTo(0.2)
    expect(heatAlpha(-1, 1000)).toBeCloseTo(0.04032)
  })
})

describe('heatStyle', () => {
  it('для расхода задаёт фон с прозрачностью', () => {
    expect(heatStyle(-100, 100)).toEqual({ background: 'rgba(200, 76, 78, 0.36)' })
  })

  it('без подсветки возвращает пустой объект', () => {
    expect(heatStyle(0, 100)).toEqual({})
    expect(heatStyle(10, 100)).toEqual({})
  })
})

describe('heatClass', () => {
  it('ноль и пустое значение — hm-zero', () => {
    expect(heatClass(0, 100)).toBe('hm-zero')
    expect(heatClass(undefined, 100)).toBe('hm-zero')
  })

  it('поступление — hm-income', () => {
    expect(heatClass(10, 100)).toBe('hm-income')
  })

  it('крупный расход (больше 52% максимума) — hm-hi, остальные без класса', () => {
    expect(heatClass(-53, 100)).toBe('hm-hi')
    expect(heatClass(-52, 100)).toBe('')
  })
})

describe('computeMaxValue', () => {
  it('ищет максимум |totalSum| по всем периодам, включая дочерние строки', () => {
    const tree = [
      {
        periods: { a: { totalSum: -10 }, b: { totalSum: 5 } },
        children: [{ periods: { a: { totalSum: -70 } } }]
      },
      { periods: { a: { totalSum: 30 } } }
    ]

    expect(computeMaxValue(tree)).toBe(70)
  })

  it('для пустого дерева — 0', () => {
    expect(computeMaxValue([])).toBe(0)
    expect(computeMaxValue(null)).toBe(0)
  })

  it('узлы без periods и ячейки без totalSum не ломают расчёт', () => {
    const tree = [
      { children: [{ periods: { a: {} } }] },
      { periods: { a: { totalSum: null }, b: { totalSum: -12.5 } }, children: [] }
    ]

    expect(computeMaxValue(tree)).toBe(12.5)
  })
})

describe('граничные случаи шкалы', () => {
  it('heatAlpha: пустое значение — 0, расход больше максимума не ограничивается', () => {
    expect(heatAlpha(undefined, 100)).toBe(0)
    expect(heatAlpha(null, 100)).toBe(0)
    expect(heatAlpha(-200, 100)).toBeCloseTo(0.68)
  })

  it('heatClass: при нулевом максимуме расход — hm-hi (деление на 0 даёт Infinity)', () => {
    // На практике maxV = 0 только когда все ячейки нулевые, и до расхода дело не доходит.
    expect(heatClass(-1, 0)).toBe('hm-hi')
    expect(heatClass(0, 0)).toBe('hm-zero')
  })

  it('heatStyle округляет прозрачность до сотых', () => {
    expect(heatStyle(-1, 3)).toEqual({ background: 'rgba(200, 76, 78, 0.15)' })
  })
})
