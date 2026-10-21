import { describe, it, expect } from 'vitest'
import { readFileSync, readdirSync, statSync, existsSync } from 'node:fs'
import { join, dirname, relative } from 'node:path'
import { fileURLToPath } from 'node:url'

// Сторожа стайлгайда (.ai-docs/workflow/styleguide.md, задача 0022)
const srcDir = dirname(fileURLToPath(import.meta.url))

const collectFiles = (dir, extensions) =>
    readdirSync(dir).flatMap(name => {
        const path = join(dir, name)
        if (statSync(path).isDirectory()) return collectFiles(path, extensions)
        return extensions.some(ext => path.endsWith(ext)) && !path.endsWith('.spec.js') ? [path] : []
    })

const offenders = (extensions, pattern) =>
    collectFiles(srcDir, extensions)
        .filter(file => pattern.test(readFileSync(file, 'utf8')))
        .map(file => relative(srcDir, file).replaceAll('\\', '/'))

const read = (path) => readFileSync(join(srcDir, path), 'utf8')

describe('стайлгайд frontend', () => {
    it('все компоненты на <script setup>, без Options API', () => {
        expect(offenders(['.vue'], /export\s+default\s*\{|<script>/)).toEqual([])
    })

    it('нет alert() — ошибки и сообщения показываются через AlertMessage', () => {
        expect(offenders(['.vue', '.js'], /\balert\s*\(/)).toEqual([])
    })

    it('нет шаблонного base.css и стилей тёмной темы', () => {
        expect(existsSync(join(srcDir, 'assets', 'base.css'))).toBe(false)
        expect(offenders(['.vue', '.css', '.js'], /prefers-color-scheme|base\.css/)).toEqual([])
    })

    it('модалки правки на главной и в «Операциях» — modal-lg', () => {
        const editModals = [
            ['pages/paymentsTable/PaymentsTable.vue', 'editPaymentModal'],
            ['pages/payments/Payments.vue', 'bulkTagModal']
        ]
        for (const [file, modalId] of editModals) {
            const source = read(file)
            const afterModal = source.slice(source.indexOf(`id="${modalId}"`))
            expect(afterModal, `${file}#${modalId}`).toMatch(/^[^>]*>\s*<div class="modal-dialog modal-lg">/)
        }
    })

    it('на главной есть спиннер загрузки по стайлгайду', () => {
        expect(read('pages/paymentsTable/PaymentsTable.vue')).toMatch(/class="spinner-border text-primary" role="status"/)
    })
})
