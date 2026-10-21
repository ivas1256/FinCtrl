// Текст ошибки API для пользователя.
// Backend отвечает ProblemDetails (RFC 7807): { title, detail, status }, а ошибки валидации модели —
// ValidationProblemDetails с полем errors: { Поле: ['сообщение', ...] }.

const validationMessages = (errors) =>
    Object.values(errors ?? {})
        .flat()
        .filter((message) => typeof message === 'string' && message.trim() !== '')

export const getErrorMessage = (error, fallback) => {
    const data = error?.response?.data

    if (typeof data === 'string' && data.trim() !== '') {
        return data
    }

    if (data && typeof data === 'object') {
        const messages = validationMessages(data.errors)
        if (messages.length > 0) {
            return messages.join('\n')
        }
        if (data.detail) {
            return data.detail
        }
        if (data.title) {
            return data.title
        }
    }

    return fallback
}
