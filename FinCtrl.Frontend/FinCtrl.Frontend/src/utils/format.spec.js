// Часовой пояс с положительным смещением: в нём toISOString() сдвигает локальную полночь на предыдущий день
process.env.TZ = 'Asia/Yekaterinburg'

import { describe, it, expect } from 'vitest'
import {
    formatCurrency,
    formatSignedCurrency,
    formatShortAmount,
    formatSignedRoundedCurrency,
    formatSignedPercent,
    formatDelta,
    formatDeltaPercent,
    parseDate,
    formatDate,
    formatShortDate,
    formatLongDate,
    formatDateTime,
    toInputDate
} from './format'

// Intl разделяет разряды неразрывными пробелами
const normalize = (s) => s.replace(/\s/g, ' ')

describe('formatCurrency', () => {
    it('форматирует рубли с копейками', () => {
        expect(normalize(formatCurrency(1234.5))).toBe('1 234,50 ₽')
        expect(normalize(formatCurrency(-1500))).toBe('-1 500,00 ₽')
    })

    it('округляет до копеек', () => {
        expect(normalize(formatCurrency(0.129))).toBe('0,13 ₽')
    })

    it('пустое значение — ноль', () => {
        expect(normalize(formatCurrency(null))).toBe('0,00 ₽')
        expect(normalize(formatCurrency(undefined))).toBe('0,00 ₽')
    })
})

describe('formatSignedCurrency', () => {
    it('показывает знак расхода и поступления', () => {
        expect(normalize(formatSignedCurrency(-1234.5))).toBe('-1 234,50 ₽')
        expect(normalize(formatSignedCurrency(1234.5))).toBe('+1 234,50 ₽')
    })

    it('ноль и пустое значение — без знака', () => {
        expect(normalize(formatSignedCurrency(0))).toBe('0,00 ₽')
        expect(normalize(formatSignedCurrency(null))).toBe('0,00 ₽')
    })
})

describe('formatShortAmount', () => {
    it('меньше тысячи — целое число', () => {
        expect(formatShortAmount(950.6)).toBe('951')
    })

    it('тысячи и миллионы — с суффиксом и без лишнего ".0"', () => {
        expect(formatShortAmount(12000)).toBe('12к')
        expect(formatShortAmount(1500)).toBe('1.5к')
        expect(formatShortAmount(1500000)).toBe('1.5М')
        expect(formatShortAmount(2000000)).toBe('2М')
    })

    it('пустое значение и ноль — "0"', () => {
        expect(formatShortAmount(0)).toBe('0')
        expect(formatShortAmount(null)).toBe('0')
    })
})

describe('parseDate', () => {
    it('строка только с датой — локальная полночь, а не UTC', () => {
        const date = parseDate('2026-10-20')
        expect([date.getFullYear(), date.getMonth(), date.getDate(), date.getHours()]).toEqual([2026, 9, 20, 0])
    })

    it('дата-время без зоны — локальное время', () => {
        const date = parseDate('2026-10-20T14:05:00')
        expect([date.getDate(), date.getHours(), date.getMinutes()]).toEqual([20, 14, 5])
    })

    it('Date возвращается как есть', () => {
        const date = new Date(2026, 0, 1)
        expect(parseDate(date)).toBe(date)
    })

    it('пустое и некорректное значение — null', () => {
        expect(parseDate(null)).toBeNull()
        expect(parseDate('')).toBeNull()
        expect(parseDate('не дата')).toBeNull()
        expect(parseDate(new Date('x'))).toBeNull()
    })
})

describe('форматирование дат', () => {
    it('formatDate — дд.мм.гггг', () => {
        expect(formatDate('2026-09-07T00:00:00')).toBe('07.09.2026')
        expect(formatDate('2026-09-07')).toBe('07.09.2026')
    })

    it('formatShortDate — дд.мм', () => {
        expect(formatShortDate('2026-09-07')).toBe('07.09')
    })

    it('formatLongDate — с днём недели и месяцем словом', () => {
        expect(formatLongDate('2026-09-07')).toBe('понедельник, 7 сентября 2026 г.')
    })

    it('formatDateTime — дата и время', () => {
        expect(formatDateTime('2026-09-07T14:05:00')).toBe('07.09.2026 14:05')
    })

    it('пустое значение — заглушка, по умолчанию "-"', () => {
        expect(formatDate(null)).toBe('-')
        expect(formatDate('', '')).toBe('')
        expect(formatShortDate(undefined)).toBe('-')
        expect(formatLongDate(null)).toBe('-')
        expect(formatDateTime(null)).toBe('-')
    })
})

describe('toInputDate', () => {
    it('дата платежа из API не сдвигается на день раньше', () => {
        expect(toInputDate('2026-10-20T00:00:00')).toBe('2026-10-20')
    })

    it('Date в локальную полночь — тот же календарный день', () => {
        expect(toInputDate(new Date(2026, 0, 1))).toBe('2026-01-01')
    })

    it('строка только с датой возвращается без изменений', () => {
        expect(toInputDate('2026-03-09')).toBe('2026-03-09')
    })

    it('пустое значение — пустая строка', () => {
        expect(toInputDate(null)).toBe('')
        expect(toInputDate('')).toBe('')
    })
})

describe('форматтеры сравнения периодов', () => {
  it('formatSignedRoundedCurrency: знак, U+2212, NBSP, ноль без знака', () => {
    expect(formatSignedRoundedCurrency(4500)).toBe('+4\u00A0500\u00A0₽')
    expect(formatSignedRoundedCurrency(-6550)).toBe('\u22126\u00A0550\u00A0₽')
    expect(formatSignedRoundedCurrency(0)).toBe('0\u00A0₽')
    expect(formatSignedRoundedCurrency(-0.2)).toBe('0\u00A0₽')
  })

  it('formatSignedRoundedCurrency: половина округляется от нуля', () => {
    expect(formatSignedRoundedCurrency(2.5)).toBe('+3\u00A0₽')
    expect(formatSignedRoundedCurrency(-2.5)).toBe('\u22123\u00A0₽')
  })

  it('formatSignedPercent: NBSP перед «%», U+2212', () => {
    expect(formatSignedPercent(61)).toBe('+61\u00A0%')
    expect(formatSignedPercent(-21)).toBe('\u221221\u00A0%')
    expect(formatSignedPercent(0)).toBe('0\u00A0%')
    expect(formatSignedPercent(-100)).toBe('\u2212100\u00A0%')
  })

  it('formatDelta: процент, «новая», «<1 %», прочерк', () => {
    expect(formatDelta(4500, 61)).toBe('+4\u00A0500\u00A0₽ (+61\u00A0%)')
    expect(formatDelta(3800, 'new')).toBe('+3\u00A0800\u00A0₽ (новая)')
    expect(formatDelta(50, 'small')).toBe('+50\u00A0₽ (<1\u00A0%)')
    expect(formatDelta(1200, null)).toBe('+1\u00A0200\u00A0₽ (\u2014)')
  })

  it('formatDeltaPercent: часть в скобках', () => {
    expect(formatDeltaPercent(-21)).toBe('(−21 %)')
    expect(formatDeltaPercent('new')).toBe('(новая)')
    expect(formatDeltaPercent('small')).toBe('(<1 %)')
  })
})
