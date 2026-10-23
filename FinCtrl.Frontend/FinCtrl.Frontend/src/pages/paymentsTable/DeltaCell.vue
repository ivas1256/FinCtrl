<template>
    <td class="hm-delta" :class="[`hm-delta-${view.tone}`, { 'hm-delta-total': total }]">
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
import { ComparisonState, deltaCellView } from '../../utils/periodComparison'
import { formatSignedRoundedCurrency, formatDeltaPercent } from '../../utils/format'

// Ячейка колонки «Δ к прошлому периоду» (СР §2.4); суммы Т и П — node.comparison из ответа api/PaymentsApi
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
    }
})

const view = computed(() => deltaCellView(props.node, props.semantics))
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
</style>
