import { describe, it, expect } from 'vitest'
import { getErrorMessage } from './apiError'

const withData = (data) => ({ response: { data } })

describe('getErrorMessage', () => {
  it('ProblemDetails — берёт detail', () => {
    const error = withData({ status: 409, title: 'Конфликт данных', detail: 'Тег «Поездка» уже существует' })
    expect(getErrorMessage(error, 'Ошибка')).toBe('Тег «Поездка» уже существует')
  })

  it('ProblemDetails без detail — берёт title', () => {
    expect(getErrorMessage(withData({ status: 404, title: 'Not Found' }), 'Ошибка')).toBe('Not Found')
  })

  it('ошибки валидации модели — склеивает сообщения полей', () => {
    const error = withData({
      title: 'One or more validation errors occurred.',
      errors: {
        ExcelAccountName: ['Название счёта в excel-файле обязательно'],
        DisplayName: ['Отображаемое название счёта обязательно']
      }
    })
    expect(getErrorMessage(error, 'Ошибка')).toBe(
      'Название счёта в excel-файле обязательно\nОтображаемое название счёта обязательно')
  })

  it('пустой errors — откатывается на detail', () => {
    expect(getErrorMessage(withData({ errors: {}, detail: 'Подробности' }), 'Ошибка')).toBe('Подробности')
  })

  it('строковый ответ — возвращает его', () => {
    expect(getErrorMessage(withData('Файл не выбран'), 'Ошибка')).toBe('Файл не выбран')
  })

  it('нет ответа (сетевая ошибка) или пустое тело — fallback', () => {
    expect(getErrorMessage(new Error('Network Error'), 'Ошибка сети')).toBe('Ошибка сети')
    expect(getErrorMessage(withData(''), 'Ошибка')).toBe('Ошибка')
    expect(getErrorMessage(withData({}), 'Ошибка')).toBe('Ошибка')
    expect(getErrorMessage(undefined, 'Ошибка')).toBe('Ошибка')
  })
})
