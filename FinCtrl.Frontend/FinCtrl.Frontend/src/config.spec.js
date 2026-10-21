import { describe, it, expect } from 'vitest'
import { APP_CONFIG, DEFAULT_API_BASE_URL, resolveApiBaseUrl } from './config'

describe('resolveApiBaseUrl', () => {
    it('без переменной окружения — адрес по умолчанию', () => {
        expect(resolveApiBaseUrl({})).toBe('http://localhost:5000')
        expect(resolveApiBaseUrl(undefined)).toBe(DEFAULT_API_BASE_URL)
        expect(resolveApiBaseUrl({ VITE_API_BASE_URL: '  ' })).toBe(DEFAULT_API_BASE_URL)
    })

    it('берёт адрес из VITE_API_BASE_URL и отбрасывает завершающий "/"', () => {
        expect(resolveApiBaseUrl({ VITE_API_BASE_URL: 'https://api.example.test' })).toBe('https://api.example.test')
        expect(resolveApiBaseUrl({ VITE_API_BASE_URL: 'http://localhost:5001/' })).toBe('http://localhost:5001')
    })
})

describe('APP_CONFIG', () => {
    it('apiBaseUrl вычисляется из import.meta.env', () => {
        expect(APP_CONFIG.apiBaseUrl).toBe(resolveApiBaseUrl(import.meta.env))
    })
})
