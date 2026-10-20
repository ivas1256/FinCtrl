// Часовой пояс с положительным смещением: в нём toISOString() сдвигает локальную полночь на предыдущий день
process.env.TZ = 'Asia/Yekaterinburg'

import { describe, it, expect } from 'vitest'
import { toInputDate } from './format'

describe('toInputDate', () => {
    it('дата платежа из API не сдвигается на день раньше', () => {
        expect(toInputDate('2026-10-20T00:00:00')).toBe('2026-10-20')
    })
})
