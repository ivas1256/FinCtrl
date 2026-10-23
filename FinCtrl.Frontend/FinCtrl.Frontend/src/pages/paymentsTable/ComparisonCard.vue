<template>
    <section
        :id="id"
        ref="root"
        class="card border-0 shadow-sm mt-4 cmp-card"
        :aria-labelledby="`${id}-title`"
        @keydown.esc="emit('close')"
    >
        <div class="card-header d-flex justify-content-between align-items-center gap-3">
            <h5 :id="`${id}-title`" ref="heading" class="mb-0 cmp-title" tabindex="-1">
                <span>{{ title }}</span>
                <span class="cmp-sep">{{ ' · ' }}</span>
                <span class="cmp-scope">
                    <span v-if="!isTotal" class="cmp-dot" :style="{ background: categoryColor(node) }"></span>
                    <template v-if="parent">
                        <span class="cmp-scope-parent">{{ parent.categoryName }} /</span>
                    </template>
                    {{ scopeName }}
                </span>
            </h5>
            <button type="button" class="btn btn-sm btn-outline-secondary" @click="emit('close')">Скрыть</button>
        </div>
        <div class="card-body">
            <p class="small text-muted mb-3">{{ periodNote }}</p>

            <div class="row g-3 mb-4">
                <div class="col-12 col-md-4">
                    <div class="cmp-tile">
                        <div class="small text-muted">{{ tileLabels.current }}</div>
                        <div class="fs-4 fw-bold cmp-amount">{{ formatCurrency(currentValue) }}</div>
                    </div>
                </div>
                <div class="col-12 col-md-4">
                    <div class="cmp-tile">
                        <div class="small text-muted">{{ tileLabels.previous }}</div>
                        <div class="fs-4 fw-bold cmp-amount">{{ formatCurrency(previousValue) }}</div>
                    </div>
                </div>
                <div class="col-12 col-md-4">
                    <div class="cmp-tile">
                        <div class="small text-muted">Разница</div>
                        <div class="cmp-diff" :class="`cmp-diff-${delta.tone}`">
                            <template v-if="delta.state === ComparisonState.None">
                                <span class="fs-4 fw-bold" aria-hidden="true">—</span>
                                <span class="visually-hidden">{{ delta.hiddenText }}</span>
                            </template>
                            <template v-else>
                                <span class="fs-4 fw-bold cmp-amount">
                                    <span class="cmp-arrow" aria-hidden="true">{{ delta.arrow }}</span>
                                    <span class="visually-hidden">{{ delta.hiddenText }} </span>
                                    {{ formatSignedRoundedCurrency(delta.rub) }}
                                </span>
                                <span class="cmp-diff-pct">{{ formatDeltaPercent(delta.pct) }}</span>
                            </template>
                        </div>
                    </div>
                </div>
            </div>

            <p v-if="!hasRows" class="text-muted small py-2 mb-0 cmp-empty">
                У категории нет подкатегорий — сравнение только по категории целиком.
            </p>
            <template v-else>
                <!-- Топ-3 роста и снижения (СР §4.4) -->
                <div class="row g-4 mb-4">
                    <div v-for="block in topBlocks" :key="block.key" class="col-12 col-md-6">
                        <div :id="`${id}-${block.key}`" class="small text-muted fw-semibold cmp-top-title">{{ block.title }}</div>
                        <ul v-if="block.rows.length" class="list-unstyled mb-0" :aria-labelledby="`${id}-${block.key}`">
                            <li v-for="row in block.rows" :key="row.key" class="d-flex justify-content-between gap-3 py-2 cmp-top-row">
                                <span class="cmp-top-name">{{ row.name }}</span>
                                <span class="cmp-top-delta" :class="`cmp-diff-${row.view.tone}`">
                                    <span class="cmp-arrow" aria-hidden="true">{{ row.view.arrow }}</span>
                                    <span class="visually-hidden">{{ row.view.hiddenText }} </span>
                                    {{ formatSignedRoundedCurrency(row.view.rub) }} · {{ formatDeltaPercentPlain(row.view.pct) }}
                                </span>
                            </li>
                        </ul>
                        <div v-else class="small text-muted py-2">{{ block.empty }}</div>
                    </div>
                </div>

                <!-- Таблица подкатегорий и сортировка (СР §4.5) -->
                <div class="d-flex justify-content-end mb-2">
                    <div class="btn-group btn-group-sm" role="group" aria-label="Сортировка">
                        <button
                            v-for="option in SORT_OPTIONS"
                            :key="option.mode"
                            type="button"
                            class="btn"
                            :class="sortMode === option.mode ? 'btn-primary' : 'btn-outline-secondary'"
                            :aria-pressed="sortMode === option.mode ? 'true' : 'false'"
                            @click="emit('update:sortMode', option.mode)"
                        >
                            {{ option.label }}
                        </button>
                    </div>
                </div>
                <ComparisonTable :key="scopeKey" :rows="sortedRows" :scale="scale" :labels="columnLabels" />
                <p v-if="showUncategorizedNote" class="small text-muted mt-2 mb-0 cmp-note">
                    «Без категории» не входит в итог трат — разберите эти платежи на странице «Операции».
                </p>
            </template>
        </div>
    </section>
</template>

<script setup>
import { ref, computed } from 'vue'
import { categoryColor, UNCATEGORIZED_ID } from '../../utils/heatmap'
import { formatCurrency, formatSignedRoundedCurrency, formatDeltaPercent, formatDeltaPercentPlain } from '../../utils/format'
import {
    formatComparisonTitle,
    formatComparisonPeriodNote,
    formatComparisonTileLabels,
    formatComparisonColumnLabels
} from '../../utils/periodHeader'
import { ComparisonState, deltaCellView, magnitude } from '../../utils/periodComparison'
import { buildComparisonRows, topChanges, sortComparisonRows, barScale, SortMode } from '../../utils/comparisonCard'
import ComparisonTable from './ComparisonTable.vue'

// Карточка «{Т} против {П}» под таблицей (СР §4): заголовок с охватом, подпись-период и три плитки.
// Охват — узел дерева ответа api/PaymentsApi (категория или «ВСЕГО» для «Итого»)
const props = defineProps({
    id: {
        type: String,
        required: true
    },
    // Блок comparison корня ответа и группировка/теги на момент запроса
    comparison: {
        type: Object,
        required: true
    },
    groupingType: {
        type: [Number, String],
        required: true
    },
    withTags: {
        type: Boolean,
        default: false
    },
    node: {
        type: Object,
        required: true
    },
    // Непосредственный родитель охвата (для подписи «Родитель / Подкатегория»), у корня — null
    parent: {
        type: Object,
        default: null
    },
    // Semantics из periodComparison
    semantics: {
        type: String,
        required: true
    },
    // Карточка по строке «Итого» — охват «Все траты»
    isTotal: {
        type: Boolean,
        default: false
    },
    // Корни дерева без «ВСЕГО» — строки таблицы для охвата «Итого» (СР §4.5)
    treeRows: {
        type: Array,
        default: () => []
    },
    // SortMode; хранится на странице, чтобы выбор переживал смену охвата (v-model:sort-mode)
    sortMode: {
        type: String,
        default: SortMode.ByCategory
    }
})

const emit = defineEmits(['close', 'update:sortMode'])

const SORT_OPTIONS = [
    { mode: SortMode.ByCategory, label: 'По категориям' },
    { mode: SortMode.ByChange, label: 'По изменению' }
]

const root = ref(null)
const heading = ref(null)

const title = computed(() => formatComparisonTitle(props.comparison, props.groupingType))
const periodNote = computed(() => formatComparisonPeriodNote(props.comparison, props.groupingType, props.withTags))
const tileLabels = computed(() => formatComparisonTileLabels(props.comparison, props.groupingType))
const scopeName = computed(() => (props.isTotal ? 'Все траты' : props.node.categoryName))

// Плитки — величина S без знака (СР §4.3, О4), по семантике строки
const currentValue = computed(() => magnitude(props.semantics, props.node.comparison?.current))
const previousValue = computed(() => magnitude(props.semantics, props.node.comparison?.previous))
const delta = computed(() => deltaCellView(props.node, props.semantics))

// Строки таблицы, топ-3 и масштаб полос (СР §4.4–4.5); модель — utils/comparisonCard
const model = computed(() => buildComparisonRows(props.isTotal
    ? { treeRows: props.treeRows }
    : { node: props.node, semantics: props.semantics }))
const hasRows = computed(() => model.value.hasSubcategories && model.value.rows.length > 0)
const sortedRows = computed(() => sortComparisonRows(model.value.rows, props.sortMode))
const scale = computed(() => barScale(model.value.rows))
const columnLabels = computed(() => formatComparisonColumnLabels(props.comparison, props.groupingType))
const scopeKey = computed(() => (props.isTotal ? 'total' : String(props.node.categoryId)))

const topBlocks = computed(() => {
    const top = topChanges(model.value.rows)
    return [
        { key: 'growth', title: 'Выросли сильнее всего', empty: 'Роста нет', rows: top.growth },
        { key: 'decline', title: 'Снизились сильнее всего', empty: 'Снижения нет', rows: top.decline }
    ]
})

// Сноска СР §4.6: «Без категории» есть среди строк «Итого»
const showUncategorizedNote = computed(() =>
    props.isTotal && model.value.rows.some(r => r.key === String(UNCATEGORIZED_ID)))

// Фокус на заголовок и прокрутка карточки в зону видимости (СР §3.3, §3.4)
const reveal = () => {
    heading.value?.focus({ preventScroll: true })
    const reduceMotion = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches
    root.value?.scrollIntoView?.({ behavior: reduceMotion ? 'auto' : 'smooth', block: 'nearest' })
}

defineExpose({ reveal })
</script>

<style scoped>
.cmp-card .card-header {
    background-color: var(--fc-card-header-bg);
    border-bottom: 1px solid var(--fc-border);
}

.cmp-title {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 4px 8px;
}
.cmp-title:focus { outline: none; }
.cmp-title:focus-visible {
    outline: 2px solid var(--fc-accent-text);
    outline-offset: 2px;
    border-radius: 3px;
}
.cmp-sep { color: var(--fc-text-secondary); }
.cmp-scope {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    font-size: 13px;
    font-weight: 600;
    color: var(--fc-text);
}
.cmp-scope-parent { color: var(--fc-text-secondary); }
.cmp-dot {
    width: 10px;
    height: 10px;
    border-radius: 50%;
    flex-shrink: 0;
}

.cmp-tile {
    height: 100%;
    background-color: var(--fc-card-header-bg);
    border-radius: var(--bs-border-radius);
    padding: 12px 16px;
}
.cmp-tile .small { font-size: 13px; }
.cmp-amount {
    font-variant-numeric: tabular-nums;
    color: var(--fc-text);
    white-space: nowrap;
}
.cmp-diff .cmp-amount { color: inherit; }
.cmp-arrow {
    font-size: 0.75em;
    margin-right: 4px;
}
.cmp-diff-pct {
    margin-left: 4px;
    font-size: 16px;
    font-weight: 600;
    white-space: nowrap;
}
.cmp-top-title { font-size: 13px; }
.cmp-top-row { border-bottom: 1px solid var(--fc-border); font-size: 14px; }
.cmp-top-name { color: var(--fc-text); }
.cmp-top-delta {
    font-weight: 600;
    font-variant-numeric: tabular-nums;
    white-space: nowrap;
}

.cmp-diff-bad     { color: var(--fc-danger-text); }
.cmp-diff-good    { color: var(--fc-success-text); }
.cmp-diff-neutral { color: var(--fc-text-secondary-on-gray); }
</style>
