import { describe, it, expect } from 'vitest'
import { formatPeriodHeader, GroupingType } from './periodHeader'

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
