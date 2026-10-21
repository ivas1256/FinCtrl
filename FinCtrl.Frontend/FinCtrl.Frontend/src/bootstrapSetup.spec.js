import { describe, it, expect } from 'vitest'
import { readFileSync, readdirSync, statSync } from 'node:fs'
import { join, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'

// Bootstrap должен подключаться одной копией из npm (задача 0020):
// без CDN в index.html и без глобального window.bootstrap в компонентах.
const srcDir = dirname(fileURLToPath(import.meta.url))
const rootDir = join(srcDir, '..')

const collectVueFiles = (dir) =>
    readdirSync(dir).flatMap(name => {
        const path = join(dir, name)
        if (statSync(path).isDirectory()) return collectVueFiles(path)
        return path.endsWith('.vue') ? [path] : []
    })

describe('подключение Bootstrap', () => {
    it('index.html не загружает Bootstrap с CDN', () => {
        const html = readFileSync(join(rootDir, 'index.html'), 'utf8')
        expect(html).not.toMatch(/bootstrap/i)
    })

    it('main.js импортирует CSS Bootstrap из npm раньше собственных стилей', () => {
        const main = readFileSync(join(srcDir, 'main.js'), 'utf8')
        const bootstrapCss = main.indexOf("import 'bootstrap/dist/css/bootstrap.min.css'")
        const ownCss = main.indexOf("import './assets/main.css'")
        expect(bootstrapCss).toBeGreaterThanOrEqual(0)
        expect(bootstrapCss).toBeLessThan(ownCss)
    })

    it('компоненты не используют глобальный bootstrap.*', () => {
        const offenders = collectVueFiles(srcDir)
            .filter(file => /\bbootstrap\.\w/.test(readFileSync(file, 'utf8')))
        expect(offenders).toEqual([])
    })
})
