// Общие форматтеры сумм и дат (ru-RU). Использовать вместо локальных копий в компонентах.

const currencyFormat = new Intl.NumberFormat('ru-RU', {
    style: 'currency',
    currency: 'RUB',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
})

const signedCurrencyFormat = new Intl.NumberFormat('ru-RU', {
    style: 'currency',
    currency: 'RUB',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
    signDisplay: 'exceptZero'
})

// Сумма в рублях с копейками: "1 234,50 ₽"; пустое значение — как 0
export const formatCurrency = (value) => currencyFormat.format(value || 0)

// Сумма со знаком: "+1 234,50 ₽" / "-1 234,50 ₽"; ноль — без знака
export const formatSignedCurrency = (value) => signedCurrencyFormat.format(value || 0)

// Сокращённая сумма для осей и подписей графиков: 950, 12к, 1.5М
export const formatShortAmount = (value) => {
    if (!value) return '0'
    const trimZero = (s) => s.replace(/\.0$/, '')
    if (Math.abs(value) >= 1000000) return trimZero((value / 1000000).toFixed(1)) + 'М'
    if (Math.abs(value) >= 1000) return trimZero((value / 1000).toFixed(1)) + 'к'
    return Math.round(value).toString()
}

const DATE_ONLY = /^(\d{4})-(\d{2})-(\d{2})$/

// Разбор даты из API или Date. "YYYY-MM-DD" — локальная полночь (new Date() разобрал бы её как UTC),
// "YYYY-MM-DDTHH:mm:ss" без зоны — локальное время. Пустое/некорректное значение — null.
export const parseDate = (value) => {
    if (!value) return null
    if (value instanceof Date) return Number.isNaN(value.getTime()) ? null : value
    const dateOnly = typeof value === 'string' && value.match(DATE_ONLY)
    const date = dateOnly
        ? new Date(Number(dateOnly[1]), Number(dateOnly[2]) - 1, Number(dateOnly[3]))
        : new Date(value)
    return Number.isNaN(date.getTime()) ? null : date
}

const formatWith = (value, options, empty) => {
    const date = parseDate(value)
    return date ? date.toLocaleDateString('ru-RU', options) : empty
}

// Полная дата: 07.09.2026
export const formatDate = (value, empty = '-') =>
    formatWith(value, { day: '2-digit', month: '2-digit', year: 'numeric' }, empty)

// Дата без года: 07.09
export const formatShortDate = (value, empty = '-') =>
    formatWith(value, { day: '2-digit', month: '2-digit' }, empty)

// Длинная дата с днём недели: "понедельник, 7 сентября 2026 г."
export const formatLongDate = (value, empty = '-') =>
    formatWith(value, { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' }, empty)

// Дата и время: 07.09.2026 14:05
export const formatDateTime = (value, empty = '-') => {
    const date = parseDate(value)
    if (!date) return empty
    const datePart = date.toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric' })
    const timePart = date.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })
    return `${datePart} ${timePart}`
}

const pad = (n) => String(n).padStart(2, '0')

// Дата для <input type="date"> (YYYY-MM-DD) по локальному календарю — без сдвига через UTC, как у toISOString()
export const toInputDate = (value) => {
    const date = parseDate(value)
    if (!date) return ''
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
}

const MINUS = '\u2212'
const NBSP = '\u00A0'

const integerFormat = new Intl.NumberFormat('ru-RU', { maximumFractionDigits: 0, useGrouping: true })

// Округление до целого, половина — от нуля (Math.round округляет отрицательные половины к нулю)
export const roundHalfAwayFromZero = (value) => Math.sign(value) * Math.round(Math.abs(value))

const signOf = (rounded) => (rounded > 0 ? '+' : rounded < 0 ? MINUS : '')

// Целые рубли со знаком: "+4 500 ₽", "−6 550 ₽", "0 ₽" (минус — U+2212, разделитель тысяч и пробел перед ₽ — неразрывные)
export const formatSignedRoundedCurrency = (value) => {
    const rounded = roundHalfAwayFromZero(value || 0)
    return `${signOf(rounded)}${integerFormat.format(Math.abs(rounded))}${NBSP}₽`
}

// Целые проценты со знаком: "+61 %", "−21 %", "0 %" (неразрывный пробел перед %)
export const formatSignedPercent = (value) => {
    const rounded = roundHalfAwayFromZero(value || 0)
    return `${signOf(rounded)}${integerFormat.format(Math.abs(rounded))}${NBSP}%`
}

// Изменение для ячейки/плитки: "+4 500 ₽ (+61 %)". pct — число (проценты), 'new' → "(новая)",
// 'small' → "(<1 %)", иначе "(—)"
export const formatDelta = (deltaRub, pct) => {
    let tail
    if (typeof pct === 'number') tail = formatSignedPercent(pct)
    else if (pct === 'new') tail = 'новая'
    else if (pct === 'small') tail = `<1${NBSP}%`
    else tail = '\u2014'
    return `${formatSignedRoundedCurrency(deltaRub)} (${tail})`
}
