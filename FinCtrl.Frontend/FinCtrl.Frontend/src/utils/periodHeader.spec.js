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

  it('год — «2025»', () => {
    expect(formatPeriodHeader(period('2025-01-01', '2026-01-01'), GroupingType.Year)).toBe('2025')
  })

  it('день — «15.10.2025»', () => {
    expect(formatPeriodHeader(period('2025-10-15', '2025-10-15'), GroupingType.Day)).toBe('15.10.2025')
  })

  it('тип группировки строкой (из select) обрабатывается так же', () => {
    expect(formatPeriodHeader(period('2025-01-01', '2026-01-01'), '4')).toBe('2025')
  })
})
