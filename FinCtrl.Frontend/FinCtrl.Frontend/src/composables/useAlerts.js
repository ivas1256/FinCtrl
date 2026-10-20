import { ref } from 'vue'

// Сообщения страницы вместо системных диалогов браузера: ошибка и результат успешного действия.
// Показываются компонентом AlertMessage; одновременно видно только одно из двух.
export const useAlerts = () => {
    const error = ref(null)
    const success = ref(null)

    const showError = (message) => {
        success.value = null
        error.value = message
    }

    const showSuccess = (message) => {
        error.value = null
        success.value = message
    }

    const clearAlerts = () => {
        error.value = null
        success.value = null
    }

    return { error, success, showError, showSuccess, clearAlerts }
}
