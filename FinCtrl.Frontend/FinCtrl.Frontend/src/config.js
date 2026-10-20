// Конфигурация frontend. Адрес API задаётся переменной VITE_API_BASE_URL (например, в .env.local —
// он не коммитится); без неё используется адрес backend по умолчанию для локальной разработки.
export const DEFAULT_API_BASE_URL = 'http://localhost:5000'

// Завершающий "/" отбрасывается: вызовы строятся как apiBaseUrl + '/api/...'
export const resolveApiBaseUrl = (env) =>
    (env?.VITE_API_BASE_URL?.trim() || DEFAULT_API_BASE_URL).replace(/\/+$/, '')

export const APP_CONFIG = {
    apiBaseUrl: resolveApiBaseUrl(import.meta.env)
}
