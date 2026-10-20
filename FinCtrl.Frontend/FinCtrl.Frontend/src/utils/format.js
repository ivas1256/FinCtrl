// Дата для <input type="date"> (YYYY-MM-DD)
export const toInputDate = (value) => {
    if (!value) return ''
    const date = new Date(value)
    return date.toISOString().split('T')[0]
}
