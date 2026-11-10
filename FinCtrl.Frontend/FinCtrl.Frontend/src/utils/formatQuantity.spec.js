import { describe, it, expect } from 'vitest'
import { formatQuantity } from './format'

describe('formatQuantity', () => {
  it('целое и дробное без лишних нулей', () => {
    expect(formatQuantity(2)).toBe('2')
    expect(formatQuantity(2.0)).toBe('2')
    expect(formatQuantity(0.624)).toBe('0,624')
    expect(formatQuantity(1.5)).toBe('1,5')
  })

  it('пустое значение — 0', () => {
    expect(formatQuantity(null)).toBe('0')
  })
})
