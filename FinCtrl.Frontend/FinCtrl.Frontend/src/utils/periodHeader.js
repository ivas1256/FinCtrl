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

const QUARTER_NAMES = ['I', 'II', 'III', 'IV']

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
        case GroupingType.Quarter:
            return `${QUARTER_NAMES[Math.floor((from.month - 1) / 3)]} кв. ${from.year}`
        case GroupingType.Year:
            return String(from.year)
        case GroupingType.Day:
            return fullDate(from)
        default:
            return `${fullDate(from)} – ${fullDate(parseDate(period.to))}`
    }
}

// ---- Подписи сравнения с прошлым периодом (СР §2.1, §2.4, §4.2) ----

const MONTHS = {
    nominative: ['январь', 'февраль', 'март', 'апрель', 'май', 'июнь', 'июль', 'август', 'сентябрь', 'октябрь', 'ноябрь', 'декабрь'],
    genitive: ['января', 'февраля', 'марта', 'апреля', 'мая', 'июня', 'июля', 'августа', 'сентября', 'октября', 'ноября', 'декабря'],
    dative: ['январю', 'февралю', 'марту', 'апрелю', 'маю', 'июню', 'июлю', 'августу', 'сентябрю', 'октябрю', 'ноябрю', 'декабрю'],
    prepositional: ['январе', 'феврале', 'марте', 'апреле', 'мае', 'июне', 'июле', 'августе', 'сентябре', 'октябре', 'ноябре', 'декабре']
}

const capitalize = (s) => s.charAt(0).toUpperCase() + s.slice(1)

const quarterOf = (d) => Math.floor((d.month - 1) / 3)

// asOf — D, last — последний день периода (to исключающая); граница to приходит как ISO без зоны
const lastDayOf = (isoTo) => previousDay(parseDate(isoTo))

const range = (from, last, withYear) =>
    withYear
        ? `${fullDate(from)}–${fullDate(last)}`
        : `${dayMonth(from)}–${dayMonth(last)}`

// Единые части подписей для пары периодов. comparison — блок comparison корня ответа api/PaymentsApi
const describe = (comparison, groupingType) => {
    const curFrom = parseDate(comparison.current.from)
    const prevFrom = parseDate(comparison.previous.from)
    const asOf = parseDate(comparison.asOf)
    const curFullLast = lastDayOf(comparison.currentPeriodTo)
    const prevLast = lastDayOf(comparison.previous.to)
    const withYears = curFrom.year !== prevFrom.year
    const grouping = Number(groupingType)
    return { grouping, curFrom, prevFrom, asOf, curFullLast, prevLast, withYears, partial: comparison.isCurrentPartial }
}

// Названия пары периодов: { cur, prev } в именительном/«исходном» виде — для заголовка карточки (§4.2)
const periodNames = (c) => {
    const { grouping, curFrom, prevFrom, curFullLast, prevLast, withYears } = c
    switch (grouping) {
        case GroupingType.Month: {
            const year = (d) => (withYears ? ` ${d.year}` : '')
            return {
                cur: capitalize(MONTHS.nominative[curFrom.month - 1]) + year(curFrom),
                prev: MONTHS.genitive[prevFrom.month - 1] + year(prevFrom)
            }
        }
        case GroupingType.Week: {
            const yearsInside = withYears || curFrom.year !== curFullLast.year || prevFrom.year !== prevLast.year
            return {
                cur: `Неделя ${range(curFrom, curFullLast, yearsInside)}`,
                prev: range(prevFrom, prevLast, yearsInside)
            }
        }
        case GroupingType.Quarter:
            return {
                cur: `${QUARTER_NAMES[quarterOf(curFrom)]} кв. ${curFrom.year}`,
                prev: `${QUARTER_NAMES[quarterOf(prevFrom)]} кв. ${prevFrom.year}`
            }
        case GroupingType.Year:
            return { cur: String(curFrom.year), prev: String(prevFrom.year) }
        default:
            return { cur: fullDate(curFrom), prev: fullDate(prevFrom) }
    }
}

// Заголовок карточки: "Октябрь против сентября", "Январь 2027 против декабря 2026"
export const formatComparisonTitle = (comparison, groupingType) => {
    const names = periodNames(describe(comparison, groupingType))
    return `${names.cur} против ${names.prev}`
}

// Шапка колонки Δ: { text, title } — "Δ к сентябрю", "Δ к пред. неделе", "Δ к III кв.", "Δ к 2025", "Δ к 20.10"
export const formatComparisonHeader = (comparison, groupingType) => {
    const c = describe(comparison, groupingType)
    const { grouping, prevFrom, prevLast, withYears } = c
    let text
    switch (grouping) {
        case GroupingType.Month:
            text = `Δ к ${MONTHS.dative[prevFrom.month - 1]}${withYears ? ` ${prevFrom.year}` : ''}`
            break
        case GroupingType.Week:
            text = 'Δ к пред. неделе'
            break
        case GroupingType.Quarter:
            text = `Δ к ${QUARTER_NAMES[quarterOf(prevFrom)]} кв.${withYears ? ` ${prevFrom.year}` : ''}`
            break
        case GroupingType.Year:
            text = `Δ к ${prevFrom.year}`
            break
        default:
            text = `Δ к ${withYears ? fullDate(prevFrom) : dayMonth(prevFrom)}`
    }
    const caption = formatComparisonCaption(comparison, groupingType)
    const body = caption.replace(/^Δ — /, '')
    const title = grouping === GroupingType.Week
        ? `${range(prevFrom, prevLast, withYears || prevFrom.year !== prevLast.year)}. Изменение трат: ${body}`
        : `Изменение трат: ${body}`
    return { text, title }
}

// Первая строка подписи под таблицей (§2.8): "Δ — октябрь по 21 число против всего сентября"
export const formatComparisonCaption = (comparison, groupingType) => {
    const c = describe(comparison, groupingType)
    const { grouping, curFrom, prevFrom, asOf, partial, withYears } = c
    const names = periodNames(c)
    const lower = (s) => s.charAt(0).toLowerCase() + s.slice(1)
    const upTo = `${asOf.day} ${MONTHS.genitive[asOf.month - 1]}`

    switch (grouping) {
        case GroupingType.Month: {
            const cur = lower(names.cur)
            return partial
                ? `Δ — ${cur} по ${asOf.day} число против всего ${names.prev}`
                : `Δ — ${cur} против ${names.prev}, оба месяца целиком`
        }
        case GroupingType.Week:
            return partial
                ? `Δ — ${lower(names.cur)} по ${dayMonth(asOf)} против всей недели ${names.prev}`
                : `Δ — ${lower(names.cur)} против недели ${names.prev}, обе недели целиком`
        case GroupingType.Quarter:
        case GroupingType.Year: {
            const cur = grouping === GroupingType.Quarter && !withYears ? `${QUARTER_NAMES[quarterOf(curFrom)]} кв.` : names.cur
            const prev = grouping === GroupingType.Quarter && !withYears ? `${QUARTER_NAMES[quarterOf(prevFrom)]} кв.` : names.prev
            return partial
                ? `Δ — ${cur} по ${upTo} против всего ${prev}`
                : `Δ — ${cur} против ${prev}, оба периода целиком`
        }
        default:
            return `Δ — ${names.cur} против ${names.prev}`
    }
}

// ---- Подписи карточки сравнения (СР §3.3, §4.2, §4.3) ----

const lowerFirst = (s) => s.charAt(0).toLowerCase() + s.slice(1)

// Название периода в именительном падеже, с заглавной: "Сентябрь", "Неделя 12.10–18.10", "III кв. 2026", "2025", "20.10.2026"
const nominativeNames = (c) => {
    const { grouping, curFrom, prevFrom, withYears } = c
    if (grouping === GroupingType.Month) {
        const name = (d) => capitalize(MONTHS.nominative[d.month - 1]) + (withYears ? ` ${d.year}` : '')
        return { cur: name(curFrom), prev: name(prevFrom) }
    }
    if (grouping === GroupingType.Week) {
        const names = periodNames(c)
        return { cur: names.cur, prev: `Неделя ${names.prev}` }
    }
    return periodNames(c)
}

// Слова-названия (месяц, неделя) внутри фразы пишутся со строчной; "IV кв.", годы и даты — как есть
const inPhrase = (c, s) =>
    c.grouping === GroupingType.Month || c.grouping === GroupingType.Week ? lowerFirst(s) : s

// Заголовок карточки внутри фразы: "октябрь против сентября" (для aria-label ячейки Δ, СР §2.4)
export const formatComparisonPhrase = (comparison, groupingType) => {
    const c = describe(comparison, groupingType)
    return inPhrase(c, formatComparisonTitle(comparison, groupingType))
}

// Подсказка ячейки Δ (СР §3.3): "Сравнить октябрь и сентябрь по подкатегориям"
export const formatComparisonCellTitle = (comparison, groupingType) => {
    const c = describe(comparison, groupingType)
    const names = nominativeNames(c)
    // винительный падеж «неделю»; у остальных названий он совпадает с именительным
    const accusative = (s) => inPhrase(c, s).replace(/^неделя /, 'неделю ')
    return `Сравнить ${accusative(names.cur)} и ${accusative(names.prev)} по подкатегориям`
}

const BOTH_FULL = {
    [GroupingType.Month]: 'Оба месяца целиком',
    [GroupingType.Week]: 'Обе недели целиком',
    [GroupingType.Quarter]: 'Оба квартала целиком',
    [GroupingType.Year]: 'Оба года целиком',
    [GroupingType.Day]: 'Оба дня целиком'
}

// "по 21 число" для месяца, "по 21.10" для недели, "по 21 октября" для квартала и года
const upToText = (c) => {
    const { grouping, asOf } = c
    if (grouping === GroupingType.Month) return `по ${asOf.day} число`
    if (grouping === GroupingType.Week) return `по ${dayMonth(asOf)}`
    return `по ${asOf.day} ${MONTHS.genitive[asOf.month - 1]}`
}

// Подпись-период под заголовком карточки (СР §4.2): "Октябрь — по 21 число, сентябрь — целиком" / "Оба месяца целиком";
// withTags — в запросе были выбраны теги
export const formatComparisonPeriodNote = (comparison, groupingType, withTags = false) => {
    const c = describe(comparison, groupingType)
    const names = nominativeNames(c)
    const note = c.partial && c.grouping !== GroupingType.Day
        ? `${names.cur} — ${upToText(c)}, ${inPhrase(c, names.prev)} — целиком`
        : BOTH_FULL[c.grouping] || 'Оба периода целиком'
    return withTags ? `${note}, с учётом выбранных тегов` : note
}

// Подписи плиток Т и П (СР §4.3): { current: "Октябрь, по 21.10", previous: "Сентябрь" }
export const formatComparisonTileLabels = (comparison, groupingType) => {
    const c = describe(comparison, groupingType)
    const names = nominativeNames(c)
    const partial = c.partial && c.grouping !== GroupingType.Day
    return {
        current: partial ? `${names.cur}, по ${dayMonth(c.asOf)}` : names.cur,
        previous: names.prev
    }
}

// Шапки колонок П и Т таблицы карточки (СР §4.5): { previous: "Сентябрь", current: "Октябрь", currentTitle: "по 21.10" };
// у недели — без слова «Неделя» ("12.10–18.10"); currentTitle — null, если текущий период полный
export const formatComparisonColumnLabels = (comparison, groupingType) => {
    const c = describe(comparison, groupingType)
    const names = nominativeNames(c)
    const short = (s) => s.replace(/^Неделя /, '')
    const partial = c.partial && c.grouping !== GroupingType.Day
    return {
        previous: short(names.prev),
        current: short(names.cur),
        currentTitle: partial ? `по ${dayMonth(c.asOf)}` : null
    }
}
