// «Распределение по источникам» на главной: группировка платежей по классификации источника.
// Входы — платежи GET api/PaymentsApi (paymentSource {paymentSourceName, classification}).

export const NO_SOURCE_NAME = 'Без источника'

const trimmed = (value) => (typeof value === 'string' ? value.trim() : '')

// Группа: { name, sourceNames, totalSum, count }.
// name — классификация источника; без неё — имя самого источника (как будто он классифицирован своим
// именем). sourceNames — уникальные оригинальные имена в порядке появления.
export function groupPaymentsBySource(payments) {
    const groups = new Map()

    for (const payment of payments) {
        const sourceName = trimmed(payment.paymentSource?.paymentSourceName)
        const name = trimmed(payment.paymentSource?.classification) || sourceName || NO_SOURCE_NAME

        let group = groups.get(name)
        if (!group) {
            group = { name, sourceNames: [], totalSum: 0, count: 0 }
            groups.set(name, group)
        }

        if (sourceName && !group.sourceNames.includes(sourceName)) group.sourceNames.push(sourceName)
        group.totalSum += payment.paymentSum
        group.count += 1
    }

    return [...groups.values()]
}

// Подпись под названием группы: оригинальные имена, если они что-то добавляют к названию.
export function sourceNamesCaption(group) {
    if (group.sourceNames.length === 1 && group.sourceNames[0] === group.name) return ''
    return group.sourceNames.join(', ')
}
