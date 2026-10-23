<template>
    <td
        class="hm-delta"
        :class="[`hm-delta-${view.tone}`, { 'hm-delta-total': total, interactive, selected: interactive && selected }]"
        :data-delta-key="deltaKey"
        v-bind="cellAttrs"
        @click="interactive && emit('select')"
        @keydown.enter.prevent="interactive && $event.currentTarget.click()"
        @keydown.space.prevent="interactive && $event.currentTarget.click()"
    >
        <template v-if="view.state === ComparisonState.None">
            <span aria-hidden="true">—</span>
            <span class="visually-hidden">{{ view.hiddenText }}</span>
        </template>
        <template v-else>
            <span class="hm-delta-arrow" aria-hidden="true">{{ view.arrow }}</span>
            <span class="visually-hidden">{{ view.hiddenText }} </span>
            <span class="hm-delta-rub">{{ formatSignedRoundedCurrency(view.rub) }}</span>
            {{ formatDeltaPercent(view.pct) }}
        </template>
    </td>
</template>

<script setup>
import { computed } from 'vue'
import { ComparisonState, deltaCellView, isDeltaInteractive, deltaAriaLabel } from '../../utils/periodComparison'
import { formatSignedRoundedCurrency, formatDeltaPercent } from '../../utils/format'

// Ячейка колонки «Δ к прошлому периоду» (СР §2.4); суммы Т и П — node.comparison из ответа api/PaymentsApi.
// Клик открывает карточку сравнения (СР §3): ячейка с «—» не интерактивна
const props = defineProps({
    node: {
        type: Object,
        required: true
    },
    // Semantics из periodComparison: expense / income / savings
    semantics: {
        type: String,
        required: true
    },
    // Ячейка строки «Итого»
    total: {
        type: Boolean,
        default: false
    },
    // По этой ячейке открыта карточка сравнения
    selected: {
        type: Boolean,
        default: false
    },
    // Подписи пары периодов: { phrase, title, cardId } (periodHeader: formatComparisonPhrase, formatComparisonCellTitle)
    labels: {
        type: Object,
        default: null
    },
    // Имя охвата для aria-label; по умолчанию — имя категории
    name: {
        type: String,
        default: null
    }
})

const emit = defineEmits(['select'])

const view = computed(() => deltaCellView(props.node, props.semantics))

const interactive = computed(() => !!props.labels && isDeltaInteractive(view.value))

// Ключ для возврата фокуса после закрытия карточки
const deltaKey = computed(() => (props.total ? 'total' : String(props.node.categoryId)))

const cellAttrs = computed(() => {
    if (!interactive.value) return {}
    return {
        tabindex: 0,
        role: 'button',
        title: props.labels.title,
        'aria-label': deltaAriaLabel(props.name || props.node.categoryName, props.labels.phrase, view.value),
        'aria-expanded': props.selected ? 'true' : 'false',
        'aria-controls': props.labels.cardId
    }
})
</script>

<style scoped>
.hm-delta {
    width: 176px;
    text-align: right;
    padding: 7px 12px;
    border-top: 1px solid var(--fc-border);
    border-left: 1px solid var(--fc-border);
    vertical-align: middle;
    white-space: nowrap;
    cursor: default;
}
.hm-delta-total { padding: 9px 12px; }

.hm-delta-arrow {
    font-size: 0.75em;
    margin-right: 4px;
}
.hm-delta-rub { font-weight: 600; }

.hm-delta-bad     { color: var(--fc-danger-text); }
.hm-delta-good    { color: var(--fc-success-text); }
.hm-delta-neutral { color: var(--fc-text-secondary); }
.hm-delta-total.hm-delta-neutral { color: var(--fc-text-secondary-on-gray); }

/* Состояния кликабельной ячейки — как у ячеек периода (ДТ §5.4) */
.hm-delta.interactive { cursor: pointer; }
.hm-delta.interactive:hover { outline: 1px solid var(--fc-text-secondary); outline-offset: -1px; }
.hm-delta.selected,
.hm-delta.selected:hover {
    outline: 2px solid var(--fc-accent-text);
    outline-offset: -2px;
}
.hm-delta.interactive:focus-visible {
    outline: 2px solid var(--fc-accent-text);
    outline-offset: -2px;
}
.hm-delta.selected:focus-visible { box-shadow: inset 0 0 0 3px var(--fc-card-bg); }
</style>
