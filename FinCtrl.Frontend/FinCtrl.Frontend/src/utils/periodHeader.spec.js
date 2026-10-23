import { describe, it, expect } from 'vitest'
import {
  formatPeriodHeader,
  formatComparisonHeader,
  formatComparisonCaption,
  formatComparisonTitle,
  GroupingType
} from './periodHeader'

// Период приходит с backend как { from, to } в ISO без зоны; to — исключающая граница
const period = (from, to) => ({ from: `${from}T00:00:00`, to: `${to}T00:00:00` })

describe('formatPeriodHeader', () => {
  it('месяц — «Окт 2025»', () => {
    expect(formatPeriodHeader(period('2025-10-01', '2025-11-01'), GroupingType.Month)).toBe('Окт 2025')
  })

  it('неделя — диапазон дат с понедельника по воскресенье', () => {
    expect(formatPeriodHeader(period('2025-10-13', '2025-10-20'), GroupingType.Week)).toBe('13.10 – 19.10.2025')
  })

  it('неделя на стыке лет — с годом у обеих дат', () => {
    expect(formatPeriodHeader(period('2025-12-29', '2026-01-05'), GroupingType.Week)).toBe('29.12.2025 – 04.01.2026')
  })

  it('квартал — «I кв. 2026»', () => {
    expect(formatPeriodHeader(period('2026-01-01', '2026-04-01'), GroupingType.Quarter)).toBe('I кв. 2026')
  })

  it('IV квартал — «IV кв. 2025»', () => {
    expect(formatPeriodHeader(period('2025-10-01', '2026-01-01'), GroupingType.Quarter)).toBe('IV кв. 2025')
  })

  it('год — «2025»', () => {
    expect(formatPeriodHeader(period('2025-01-01', '2026-01-01'), GroupingType.Year)).toBe('2025')
  })

  it('день — «15.10.2025»', () => {
    expect(formatPeriodHeader(period('2025-10-15', '2025-10-15'), GroupingType.Day)).toBe('15.10.2025')
  })

  it('тип группировки строкой (из select) обрабатывается так же', () => {
    expect(formatPeriodHeader(period('2025-01-01', '2026-01-01'), '4')).toBe('2025')
  })

  it('группировка не выбрана или неизвестна — диапазон «с – по» без вычитания дня', () => {
    expect(formatPeriodHeader(period('2025-10-01', '2025-11-01'), GroupingType.NotSelected)).toBe('01.10.2025 – 01.11.2025')
    expect(formatPeriodHeader(period('2025-10-01', '2025-11-01'), undefined)).toBe('01.10.2025 – 01.11.2025')
  })

  it('неделя на стыке месяцев внутри года — без года у начала', () => {
    expect(formatPeriodHeader(period('2025-09-29', '2025-10-06'), GroupingType.Week)).toBe('29.09 – 05.10.2025')
  })

  it('неделя, кончающаяся 31 декабря, — без перехода года', () => {
    expect(formatPeriodHeader(period('2029-12-24', '2030-01-01'), GroupingType.Week)).toBe('24.12 – 31.12.2029')
  })

  it('период с временем в дате — время отбрасывается', () => {
    expect(formatPeriodHeader({ from: '2025-10-15T13:45:00', to: '2025-10-16T00:00:00' }, GroupingType.Day)).toBe('15.10.2025')
  })
})

// Блок comparison из ответа api/PaymentsApi (СР §5.2); to — исключающая граница
const iso = (d) => `${d}T00:00:00`
const comparison = ({ asOf, cur, prev, curEnd, partial = true }) => ({
  asOf: iso(asOf),
  current: { key: 'k', from: iso(cur[0]), to: iso(cur[1]) },
  previous: { key: 'p', from: iso(prev[0]), to: iso(prev[1]) },
  currentPeriodTo: iso(curEnd),
  isCurrentPartial: partial
})

// СР §2.1, D = 21.10.2026
const month = comparison({ asOf: '2026-10-21', cur: ['2026-10-01', '2026-10-22'], prev: ['2026-09-01', '2026-10-01'], curEnd: '2026-11-01' })
const week = comparison({ asOf: '2026-10-21', cur: ['2026-10-19', '2026-10-22'], prev: ['2026-10-12', '2026-10-19'], curEnd: '2026-10-26' })
const quarter = comparison({ asOf: '2026-10-21', cur: ['2026-10-01', '2026-10-22'], prev: ['2026-07-01', '2026-10-01'], curEnd: '2027-01-01' })
const year = comparison({ asOf: '2026-10-21', cur: ['2026-01-01', '2026-10-22'], prev: ['2025-01-01', '2026-01-01'], curEnd: '2027-01-01' })
const day = comparison({ asOf: '2026-10-21', cur: ['2026-10-21', '2026-10-22'], prev: ['2026-10-20', '2026-10-21'], curEnd: '2026-10-22', partial: false })

describe('подписи сравнения — шапка колонки', () => {
  it('месяц', () => expect(formatComparisonHeader(month, GroupingType.Month).text).toBe('Δ к сентябрю'))
  it('неделя: title содержит диапазон прошлой недели', () => {
    const header = formatComparisonHeader(week, GroupingType.Week)
    expect(header.text).toBe('Δ к пред. неделе')
    expect(header.title.startsWith('12.10–18.10')).toBe(true)
  })
  it('квартал', () => expect(formatComparisonHeader(quarter, GroupingType.Quarter).text).toBe('Δ к III кв.'))
  it('год', () => expect(formatComparisonHeader(year, GroupingType.Year).text).toBe('Δ к 2025'))
  it('день', () => expect(formatComparisonHeader(day, GroupingType.Day).text).toBe('Δ к 20.10'))
  it('title месяца', () =>
    expect(formatComparisonHeader(month, GroupingType.Month).title)
      .toBe('Изменение трат: октябрь по 21 число против всего сентября'))

  it('переход года: январь против декабря — с годом', () => {
    const c = comparison({ asOf: '2027-01-15', cur: ['2027-01-01', '2027-01-16'], prev: ['2026-12-01', '2027-01-01'], curEnd: '2027-02-01' })
    expect(formatComparisonHeader(c, GroupingType.Month).text).toBe('Δ к декабрю 2026')
  })
})

describe('подписи сравнения — подпись под таблицей', () => {
  it('месяц', () =>
    expect(formatComparisonCaption(month, GroupingType.Month)).toBe('Δ — октябрь по 21 число против всего сентября'))
  it('неделя', () =>
    expect(formatComparisonCaption(week, GroupingType.Week))
      .toBe('Δ — неделя 19.10–25.10 по 21.10 против всей недели 12.10–18.10'))
  it('квартал', () =>
    expect(formatComparisonCaption(quarter, GroupingType.Quarter)).toBe('Δ — IV кв. по 21 октября против всего III кв.'))
  it('год', () =>
    expect(formatComparisonCaption(year, GroupingType.Year)).toBe('Δ — 2026 по 21 октября против всего 2025'))
  it('день', () =>
    expect(formatComparisonCaption(day, GroupingType.Day)).toBe('Δ — 21.10.2026 против 20.10.2026'))

  it('D — последний день месяца: оба месяца целиком', () => {
    const c = comparison({ asOf: '2026-10-31', cur: ['2026-10-01', '2026-11-01'], prev: ['2026-09-01', '2026-10-01'], curEnd: '2026-11-01', partial: false })
    expect(formatComparisonCaption(c, GroupingType.Month)).toBe('Δ — октябрь против сентября, оба месяца целиком')
  })
})

describe('подписи сравнения — заголовок карточки', () => {
  it('месяц', () => expect(formatComparisonTitle(month, GroupingType.Month)).toBe('Октябрь против сентября'))
  it('неделя', () => expect(formatComparisonTitle(week, GroupingType.Week)).toBe('Неделя 19.10–25.10 против 12.10–18.10'))
  it('квартал', () => expect(formatComparisonTitle(quarter, GroupingType.Quarter)).toBe('IV кв. 2026 против III кв. 2026'))
  it('год', () => expect(formatComparisonTitle(year, GroupingType.Year)).toBe('2026 против 2025'))
  it('день', () => expect(formatComparisonTitle(day, GroupingType.Day)).toBe('21.10.2026 против 20.10.2026'))

  it('январь против декабря — с годами', () => {
    const c = comparison({ asOf: '2027-01-15', cur: ['2027-01-01', '2027-01-16'], prev: ['2026-12-01', '2027-01-01'], curEnd: '2027-02-01' })
    expect(formatComparisonTitle(c, GroupingType.Month)).toBe('Январь 2027 против декабря 2026')
  })

  it('I кв. против IV кв. прошлого года', () => {
    const c = comparison({ asOf: '2027-02-10', cur: ['2027-01-01', '2027-02-11'], prev: ['2026-10-01', '2027-01-01'], curEnd: '2027-04-01' })
    expect(formatComparisonTitle(c, GroupingType.Quarter)).toBe('I кв. 2027 против IV кв. 2026')
  })

  it('неделя на переходе года — диапазоны с годами', () => {
    const c = comparison({ asOf: '2027-01-02', cur: ['2026-12-28', '2027-01-03'], prev: ['2026-12-21', '2026-12-28'], curEnd: '2027-01-04' })
    expect(formatComparisonTitle(c, GroupingType.Week))
      .toBe('Неделя 28.12.2026–03.01.2027 против 21.12.2026–27.12.2026')
  })
})
