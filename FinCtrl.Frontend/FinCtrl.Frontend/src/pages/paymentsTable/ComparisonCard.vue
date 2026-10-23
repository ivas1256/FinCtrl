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
        </div>
    </section>
</template>

<script setup>
import { ref, computed } from 'vue'
import { categoryColor } from '../../utils/heatmap'
import { formatCurrency, formatSignedRoundedCurrency, formatDeltaPercent } from '../../utils/format'
import { formatComparisonTitle, formatComparisonPeriodNote, formatComparisonTileLabels } from '../../utils/periodHeader'
import { ComparisonState, deltaCellView, magnitude } from '../../utils/periodComparison'

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
    }
})

const emit = defineEmits(['close'])

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
.cmp-diff-bad     { color: var(--fc-danger-text); }
.cmp-diff-good    { color: var(--fc-success-text); }
.cmp-diff-neutral { color: var(--fc-text-secondary-on-gray); }
</style>
