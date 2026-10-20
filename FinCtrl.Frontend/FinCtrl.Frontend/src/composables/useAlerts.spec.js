import { describe, it, expect } from 'vitest'
import { useAlerts } from './useAlerts'

describe('useAlerts', () => {
    it('изначально сообщений нет', () => {
        const { error, success } = useAlerts()
        expect(error.value).toBeNull()
        expect(success.value).toBeNull()
    })

    it('ошибка заменяет сообщение об успехе', () => {
        const { error, success, showError, showSuccess } = useAlerts()
        showSuccess('Сохранено')
        showError('Ошибка сохранения')
        expect(error.value).toBe('Ошибка сохранения')
        expect(success.value).toBeNull()
    })

    it('сообщение об успехе заменяет ошибку', () => {
        const { error, success, showError, showSuccess } = useAlerts()
        showError('Ошибка')
        showSuccess('Успешно обновлено операций: 2')
        expect(success.value).toBe('Успешно обновлено операций: 2')
        expect(error.value).toBeNull()
    })

    it('clearAlerts сбрасывает оба сообщения', () => {
        const { error, success, showError, clearAlerts } = useAlerts()
        showError('Ошибка')
        clearAlerts()
        expect(error.value).toBeNull()
        expect(success.value).toBeNull()
    })

    it('у каждого вызова своё состояние', () => {
        const page = useAlerts()
        const other = useAlerts()
        page.showError('Ошибка')
        expect(other.error.value).toBeNull()
    })
})
