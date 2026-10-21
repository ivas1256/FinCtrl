import { describe, it, expect } from 'vitest'
import { readFileSync, readdirSync, statSync } from 'node:fs'
import { join, dirname, relative } from 'node:path'
import { fileURLToPath } from 'node:url'

// Суммы и даты форматируются только через src/utils/format.js (задача 0021):
// без локальных копий форматтеров в компонентах.
const srcDir = dirname(fileURLToPath(import.meta.url))

const collectVueFiles = (dir) =>
    readdirSync(dir).flatMap(name => {
        const path = join(dir, name)
        if (statSync(path).isDirectory()) return collectVueFiles(path)
        return path.endsWith('.vue') ? [path] : []
    })

const offenders = (pattern) =>
    collectVueFiles(srcDir)
        .filter(file => pattern.test(readFileSync(file, 'utf8')))
        .map(file => relative(srcDir, file))

describe('общие форматтеры', () => {
    it('в компонентах нет локальных formatCurrency/formatDate', () => {
        expect(offenders(/(const|function)\s+format(Currency|Date)\b|\bformat(Currency|Date)\s*\(\s*\w*\s*\)\s*\{/)).toEqual([])
    })

    it('в компонентах нет прямого форматирования сумм и дат через Intl/toLocale*', () => {
        expect(offenders(/Intl\.NumberFormat|toLocaleDateString|toLocaleTimeString|toLocaleString/)).toEqual([])
    })

    it('в компонентах нет даты для input через toISOString().split', () => {
        expect(offenders(/toISOString\(\)\.split/)).toEqual([])
    })
})
