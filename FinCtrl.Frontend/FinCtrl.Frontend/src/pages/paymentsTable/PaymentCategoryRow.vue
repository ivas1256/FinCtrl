<template>
    <template v-if="shouldShow">
        <!-- Основная строка категории -->
        <tr>
            <td class="hm-cat-cell">
                <div
                    :class="level === 0 ? 'hm-cat-root' : 'hm-cat-child'"
                    :style="level > 0 ? { paddingLeft: (level * 15) + 'px' } : {}"
                >
                    <button
                        v-if="category.children && category.children.length > 0"
                        @click="toggleExpand"
                        class="btn btn-sm btn-outline-secondary expand-btn"
                    >
                        {{ isExpanded ? '−' : '+' }}
                    </button>
                    <span v-else class="expand-btn-placeholder"></span>
                    <span
                        class="hm-dot"
                        :class="{ 'hm-dot-dim': level > 0 }"
                        :style="{ background: categoryColor(category) }"
                    ></span>
                    <span :class="nameClass">
                        {{ category.categoryName }}
                    </span>
                </div>
            </td>
            <td
                v-for="period in periods"
                :key="period.key"
                class="hm-cell"
                :class="[
                    heatClass(category.periods[period.key]?.totalSum, maxValue),
                    { selected: isSelected(period.key) }
                ]"
                :style="heatStyle(category.periods[period.key]?.totalSum, maxValue)"
                @click="handleCellClick(
                    category.periods[period.key]?.payments || [],
                    category.categoryName,
                    period.name,
                    category.periods[period.key]?.totalSum || 0,
                    category.categoryId,
                    period.key
                )"
                :title="`Кликните для просмотра ${(category.periods[period.key]?.payments || []).length} платежей`"
            >
                {{ formatSignedCurrency(category.periods[period.key]?.totalSum || 0) }}
            </td>
            <DeltaCell v-if="showDelta" :node="category" :semantics="semantics" />
        </tr>

        <!-- Дочерние категории -->
        <template v-if="category.children && category.children.length > 0 && isExpanded">
            <PaymentCategoryRow
                v-for="child in category.children"
                :key="child.categoryId"
                :category="child"
                :periods="periods"
                :level="level + 1"
                :parent-expanded="isExpanded"
                :max-value="maxValue"
                :cat-index="catIndex"
                :selected-category-id="selectedCategoryId"
                :selected-period-key="selectedPeriodKey"
                :show-delta="showDelta"
                :in-savings="semantics === Semantics.Savings"
                @cell-click="handleCellClick"
            />
        </template>
    </template>
</template>

<script setup>
import { ref, computed } from 'vue'
import { categoryColor, heatClass, heatStyle, UNCATEGORIZED_ID } from '../../utils/heatmap'
import { formatSignedCurrency } from '../../utils/format'
import { getSemantics, Semantics } from '../../utils/periodComparison'
import DeltaCell from './DeltaCell.vue'

// Компонент рекурсивный: в шаблоне ссылается сам на себя по имени
defineOptions({ name: 'PaymentCategoryRow' })

const props = defineProps({
    category: {
        type: Object,
        required: true
    },
    periods: {
        type: Array,
        required: true
    },
    level: {
        type: Number,
        default: 0
    },
    parentExpanded: {
        type: Boolean,
        default: true
    },
    maxValue: {
        type: Number,
        default: 0
    },
    catIndex: {
        type: Number,
        default: 0
    },
    selectedCategoryId: {
        type: [Number, String],
        default: null
    },
    selectedPeriodKey: {
        type: String,
        default: null
    },
    // Колонка «Δ к прошлому периоду»: в ответе есть comparison
    showDelta: {
        type: Boolean,
        default: false
    },
    // Строка внутри ветки «Накоплений» — семантика Δ как у дохода
    inSavings: {
        type: Boolean,
        default: false
    }
})

const emit = defineEmits(['cell-click'])

const isExpanded = ref(true)

const semantics = computed(() => getSemantics(props.category, props.inSavings))

const shouldShow = computed(() => props.level === 0 || props.parentExpanded)

// «Без категории» — приглушённое имя обычного веса
const nameClass = computed(() => {
    if (props.category.categoryId === UNCATEGORIZED_ID) return 'hm-cat-name-muted'
    return props.level === 0 ? 'hm-cat-name-root' : 'hm-cat-name-child'
})

const toggleExpand = () => {
    isExpanded.value = !isExpanded.value
}

const handleCellClick = (payments, categoryName, periodName, totalSum, categoryId, periodKey) => {
    emit('cell-click', payments, categoryName, periodName, totalSum, categoryId, periodKey)
}

const isSelected = (periodKey) =>
    props.selectedCategoryId === props.category.categoryId && props.selectedPeriodKey === periodKey
</script>

<style scoped>
.expand-btn {
    width: 22px;
    height: 22px;
    padding: 0;
    font-size: 12px;
    line-height: 1;
}

.expand-btn-placeholder {
    display: inline-block;
    width: 22px;
    height: 22px;
    flex-shrink: 0;
}

.hm-cat-cell { padding: 7px 14px; border-top: 1px solid var(--fc-border); vertical-align: middle; }
.hm-cat-root  { display: flex; align-items: center; gap: 8px; }
.hm-cat-child { display: flex; align-items: center; gap: 8px; }

.hm-dot {
    width: 10px;
    height: 10px;
    border-radius: 50%;
    flex-shrink: 0;
}
.hm-dot-dim { opacity: 0.35; }

.hm-cat-name-root  { font-size: 13px; font-weight: 600; white-space: nowrap; }
.hm-cat-name-child { font-size: 12.5px; color: var(--fc-text-secondary); white-space: nowrap; }
.hm-cat-name-muted { font-size: 13px; font-weight: 400; color: var(--fc-text-secondary); white-space: nowrap; }

.hm-cell {
    text-align: right;
    padding: 7px 12px;
    border-top: 1px solid var(--fc-border);
    vertical-align: middle;
    white-space: nowrap;
    cursor: pointer;
    transition: filter 0.1s;
}
.hm-cell:hover { filter: brightness(0.90); }
.hm-cell.hm-zero:hover { filter: none; }

.hm-cell.selected {
    outline: 2px solid var(--fc-accent);
    outline-offset: -2px;
}

.hm-cell          { color: var(--fc-cell-text); }
.hm-cell.hm-zero  { color: var(--fc-cell-text-zero); cursor: default; }
.hm-cell.hm-income { color: var(--fc-cell-text-income); }
.hm-cell.hm-hi    { color: var(--fc-cell-text-hi); }
</style>
