import { describe, it, expect } from 'vitest'
import { groupPaymentsBySource, sourceNamesCaption, NO_SOURCE_NAME } from './paymentSourceGroups'

const payment = (paymentSum, paymentSourceName, classification = null) => ({
  paymentSum,
  paymentSource: paymentSourceName === undefined ? null : { paymentSourceName, classification }
})

describe('groupPaymentsBySource', () => {
  it('объединяет источники с одной классификацией и собирает их имена', () => {
    const groups = groupPaymentsBySource([
      payment(-100, 'PYATEROCHKA 123', 'Продукты'),
      payment(-50, 'MAGNIT MM', 'Продукты'),
      payment(-30, 'PYATEROCHKA 123', 'Продукты')
    ])

    expect(groups).toEqual([
      { name: 'Продукты', sourceNames: ['PYATEROCHKA 123', 'MAGNIT MM'], totalSum: -180, count: 3 }
    ])
  })

  it('источник без классификации группируется по своему имени', () => {
    const groups = groupPaymentsBySource([
      payment(-10, 'YANDEX TAXI'),
      payment(-20, 'YANDEX TAXI', '  '),
      payment(-5, 'SHOP', 'Магазины')
    ])

    expect(groups.map(g => [g.name, g.sourceNames, g.totalSum])).toEqual([
      ['YANDEX TAXI', ['YANDEX TAXI'], -30],
      ['Магазины', ['SHOP'], -5]
    ])
  })

  it('платежи без источника — группа «Без источника»', () => {
    const groups = groupPaymentsBySource([payment(-10, undefined), payment(-15, undefined)])

    expect(groups).toEqual([{ name: NO_SOURCE_NAME, sourceNames: [], totalSum: -25, count: 2 }])
  })

  it('обрезает пробелы в классификации', () => {
    const groups = groupPaymentsBySource([payment(-1, 'A', ' Такси '), payment(-2, 'B', 'Такси')])

    expect(groups).toHaveLength(1)
    expect(groups[0].name).toBe('Такси')
  })
})

describe('sourceNamesCaption', () => {
  it('перечисляет имена через запятую', () => {
    expect(sourceNamesCaption({ name: 'Продукты', sourceNames: ['A', 'B'] })).toBe('A, B')
  })

  it('пусто, если единственное имя совпадает с названием группы', () => {
    expect(sourceNamesCaption({ name: 'Кафе', sourceNames: ['Кафе'] })).toBe('')
  })
})
