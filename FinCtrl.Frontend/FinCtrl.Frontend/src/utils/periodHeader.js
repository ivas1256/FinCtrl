// Заголовок колонки периода на главной (тепловая карта)

// Значения совпадают с enum GroupingType на backend
export const GroupingType = Object.freeze({
    NotSelected: 0,
    Month: 1,
    Week: 2,
    Quarter: 3,
    Year: 4,
    Day: 5
})

const MONTH_NAMES = ['Янв', 'Фев', 'Мар', 'Апр', 'Май', 'Июн', 'Июл', 'Авг', 'Сен', 'Окт', 'Ноя', 'Дек']

// Дата приходит как ISO без зоны ("2025-10-13T00:00:00") — разбираем вручную, чтобы не зависеть от часового пояса
const parseDate = (iso) => {
    const [year, month, day] = iso.split('T')[0].split('-').map(Number)
    return { year, month, day }
}

const pad = (n) => String(n).padStart(2, '0')

const dayMonth = (d) => `${pad(d.day)}.${pad(d.month)}`

const fullDate = (d) => `${dayMonth(d)}.${d.year}`

// Последний день недели: to — исключающая граница периода
const previousDay = (d) => {
    const date = new Date(Date.UTC(d.year, d.month - 1, d.day - 1))
    return { year: date.getUTCFullYear(), month: date.getUTCMonth() + 1, day: date.getUTCDate() }
}

// period — { from, to } из ответа api/PaymentsApi, groupingType — значение GroupingType (число или строка из select)
export const formatPeriodHeader = (period, groupingType) => {
    const from = parseDate(period.from)

    switch (Number(groupingType)) {
        case GroupingType.Month:
            return `${MONTH_NAMES[from.month - 1]} ${from.year}`
        case GroupingType.Week: {
            const last = previousDay(parseDate(period.to))
            return last.year === from.year
                ? `${dayMonth(from)} – ${fullDate(last)}`
                : `${fullDate(from)} – ${fullDate(last)}`
        }
        case GroupingType.Year:
            return String(from.year)
        case GroupingType.Day:
            return fullDate(from)
        default:
            return `${fullDate(from)} – ${fullDate(parseDate(period.to))}`
    }
}
