<template>
    <div class="cmp-wrap">
        <table class="cmp-table">
            <thead>
                <tr>
                    <th class="cmp-col-cat" scope="col">Категория</th>
                    <th class="cmp-col-sum" scope="col">{{ labels.previous }}</th>
                    <th class="cmp-col-sum" scope="col" :title="labels.currentTitle || undefined">{{ labels.current }}</th>
                    <th class="cmp-col-bar d-none d-md-table-cell" scope="col" aria-hidden="true">
                        <span class="cmp-bar-legend">
                            <span>меньше</span>
                            <span>больше</span>
                        </span>
                    </th>
                    <th class="cmp-col-delta" scope="col">Δ</th>
                </tr>
            </thead>
            <tbody>
                <tr v-for="item in visibleRows" :key="item.row.key" :data-row-key="item.row.key">
                    <td class="cmp-cat-cell">
                        <div class="cmp-cat" :style="item.row.level > 0 ? { paddingLeft: (item.row.level * 15) + 'px' } : {}">
                            <button
                                v-if="item.row.children.length"
                                type="button"
                                class="cmp-toggle"
                                :aria-expanded="item.expanded ? 'true' : 'false'"
                                @click="toggle(item.row.key)"
                            >
                                <span class="cmp-chevron" aria-hidden="true">{{ item.expanded ? '▾' : '▸' }}</span>
                                <span :class="nameClass(item.row)">{{ item.row.name }}</span>
                            </button>
                            <template v-else>
                                <span class="cmp-chevron" aria-hidden="true"></span>
                                <span :class="nameClass(item.row)">{{ item.row.name }}</span>
                            </template>
                        </div>
                    </td>
                    <td class="cmp-sum cmp-sum-prev" :class="{ 'cmp-zero': isZero(item.row.sPrev) }">
                        {{ isZero(item.row.sPrev) ? '—' : formatCurrency(item.row.sPrev) }}
                    </td>
                    <td class="cmp-sum" :class="{ 'cmp-zero': isZero(item.row.sCur) }">
                        {{ isZero(item.row.sCur) ? '—' : formatCurrency(item.row.sCur) }}
                    </td>
                    <td class="cmp-bar-cell d-none d-md-table-cell" aria-hidden="true">
                        <div class="cmp-bar-track">
                            <span class="cmp-axis"></span>
                            <span
                                v-if="barStyle(item.row)"
                                class="cmp-bar"
                                :class="`cmp-bar-${item.row.view.tone}`"
                                :style="barStyle(item.row)"
                            ></span>
                        </div>
                    </td>
                    <DeltaCell :node="deltaNode(item.row)" :semantics="item.row.semantics" />
                </tr>
            </tbody>
        </table>
    </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { formatCurrency } from '../../utils/format'
import { barShare } from '../../utils/comparisonCard'
import DeltaCell from './DeltaCell.vue'

// Таблица подкатегорий карточки сравнения (СР §4.5). Строки — модель comparisonCard (уже отсортированная);
// внуки развёрнуты по умолчанию, шеврон их сворачивает. Свёрнутость сбрасывается пересозданием компонента (key охвата)
const props = defineProps({
    rows: {
        type: Array,
        required: true
    },
    // M = max |Δ₽| по всем строкам, включая свёрнутые (barScale) — длины полос не зависят от свёрнутости
    scale: {
        type: Number,
        required: true
    },
    // Шапки П и Т: formatComparisonColumnLabels
    labels: {
        type: Object,
        required: true
    }
})

const collapsed = ref(new Set())

const toggle = (key) => {
    const next = new Set(collapsed.value)
    if (next.has(key)) next.delete(key)
    else next.add(key)
    collapsed.value = next
}

// Видимые строки в порядке дерева: { row, expanded }
const visibleRows = computed(() => {
    const result = []
    const walk = (rows) => {
        for (const row of rows) {
            const expanded = !collapsed.value.has(row.key)
            result.push({ row, expanded })
            if (expanded) walk(row.children)
        }
    }
    walk(props.rows)
    return result
})

const ZERO = 0.005
const isZero = (value) => Math.abs(value) < ZERO

const nameClass = (row) => {
    if (row.isService) return 'cmp-name-muted'
    return row.level === 0 ? 'cmp-name-root' : 'cmp-name-child'
}

// Рост — вправо от оси, снижение — влево; длина — доля от половины колонки, минимум 2px (в CSS)
const barStyle = (row) => {
    const share = barShare(row, props.scale)
    if (share === 0) return null
    const width = `${share * 50}%`
    return row.view.rub > 0 ? { left: '50%', width } : { right: '50%', width }
}

// DeltaCell без подписей — не интерактивна; ключ с префиксом, чтобы не совпасть с Δ-ячейками тепловой карты
const deltaNode = (row) => ({
    categoryId: `cmp-${row.key}`,
    categoryName: row.name,
    comparison: { current: row.current, previous: row.previous }
})
</script>

<style scoped>
.cmp-wrap {
    overflow-x: auto;
}

/* Оформление — как у тепловой карты (ДТ §2, §4, §5.1), без тепла */
.cmp-table {
    width: 100%;
    border-collapse: collapse;
    font-variant-numeric: tabular-nums;
    font-size: 12.5px;
}
.cmp-table thead th {
    background-color: var(--fc-card-header-bg);
    font-size: 11px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.06em;
    color: var(--fc-text-secondary-on-gray);
    padding: 9px 14px;
    border-bottom: 1px solid var(--fc-border);
    white-space: nowrap;
}
.cmp-col-cat   { text-align: left; min-width: 200px; }
.cmp-col-sum   { text-align: right; width: 132px; }
.cmp-col-bar   { width: 200px; }
.cmp-col-delta { text-align: right; width: 176px; border-left: 1px solid var(--fc-border); }

.cmp-bar-legend {
    display: flex;
    justify-content: space-between;
    text-transform: none;
    letter-spacing: 0;
    font-weight: 400;
}

.cmp-cat-cell {
    padding: 7px 14px;
    border-top: 1px solid var(--fc-border);
    vertical-align: middle;
}
.cmp-cat { display: flex; align-items: center; gap: 8px; }

.cmp-toggle {
    display: flex;
    align-items: center;
    gap: 8px;
    width: 100%;
    min-height: 22px;
    padding: 0;
    border: 0;
    background: none;
    font: inherit;
    color: inherit;
    text-align: left;
    cursor: pointer;
}
.cmp-toggle:focus-visible {
    outline: 2px solid var(--fc-accent-text);
    outline-offset: 2px;
    border-radius: 3px;
}
.cmp-chevron {
    display: inline-block;
    width: 22px;
    flex-shrink: 0;
    font-size: 12px;
    line-height: 1;
    text-align: center;
    color: var(--fc-text-secondary);
}

.cmp-name-root  { font-size: 13px; font-weight: 600; color: var(--fc-text); white-space: nowrap; }
.cmp-name-child { font-size: 12.5px; color: var(--fc-text-secondary); white-space: nowrap; }
.cmp-name-muted { font-size: 13px; font-weight: 400; color: var(--fc-text-secondary); white-space: nowrap; }

.cmp-sum {
    text-align: right;
    padding: 7px 12px;
    border-top: 1px solid var(--fc-border);
    vertical-align: middle;
    white-space: nowrap;
    color: var(--fc-text);
}
.cmp-sum-prev { color: var(--fc-text-secondary-on-gray); }
.cmp-sum.cmp-zero { color: var(--fc-text-secondary); }

.cmp-bar-cell {
    padding: 0;
    border-top: 1px solid var(--fc-border);
    vertical-align: middle;
}
.cmp-bar-track {
    position: relative;
    height: 36px;
}
.cmp-axis {
    position: absolute;
    top: 0;
    bottom: 0;
    left: 50%;
    width: 1px;
    background-color: var(--fc-axis);
}
.cmp-bar {
    position: absolute;
    top: 50%;
    height: 10px;
    min-width: 2px;
    margin-top: -5px;
    border-radius: 3px;
}
.cmp-bar-bad     { background-color: var(--fc-bar-bad); }
.cmp-bar-good    { background-color: var(--fc-bar-good); }
.cmp-bar-neutral { background-color: var(--fc-axis); }
</style>
