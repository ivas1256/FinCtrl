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
                        class="btn btn-sm btn-outline-secondary me-1 expand-btn"
                    >
                        {{ isExpanded ? '−' : '+' }}
                    </button>
                    <span v-else class="expand-btn-placeholder me-1"></span>
                    <span
                        class="hm-dot"
                        :class="{ 'hm-dot-dim': level > 0 }"
                        :style="{ background: categoryColor(category) }"
                    ></span>
                    <span :class="level === 0 ? 'hm-cat-name-root' : 'hm-cat-name-child'">
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
        </tr>

        <!-- Дочерние категории -->
        <template v-if="category.children && category.children.length > 0 && isExpanded">
            <payment-category-row
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
                @cell-click="handleCellClick"
            />
        </template>
    </template>
</template>

<script>
import { categoryColor, heatClass, heatStyle } from '../../utils/heatmap'
import { formatSignedCurrency } from '../../utils/format'

export default {
    name: 'PaymentCategoryRow',
    props: {
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
        }
    },
    emits: ['cell-click'],
    data() {
        return {
            isExpanded: true
        }
    },
    computed: {
        shouldShow() {
            return this.level === 0 || this.parentExpanded
        }
    },
    methods: {
        toggleExpand() {
            this.isExpanded = !this.isExpanded
        },
        handleCellClick(payments, categoryName, periodName, totalSum, categoryId, periodKey) {
            this.$emit('cell-click', payments, categoryName, periodName, totalSum, categoryId, periodKey)
        },
        isSelected(periodKey) {
            return this.selectedCategoryId === this.category.categoryId && this.selectedPeriodKey === periodKey
        },
        categoryColor,
        heatClass,
        heatStyle,
        formatSignedCurrency
    }
}
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

.hm-cat-cell { padding: 7px 14px; border-top: 1px solid #dee2e6; vertical-align: middle; }
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
.hm-cat-name-child { font-size: 12.5px; color: #6c757d; white-space: nowrap; }

.hm-cell {
    text-align: right;
    padding: 7px 12px;
    border-top: 1px solid #dee2e6;
    vertical-align: middle;
    white-space: nowrap;
    cursor: pointer;
    transition: filter 0.1s;
}
.hm-cell:hover { filter: brightness(0.90); }
.hm-cell.hm-zero:hover { filter: none; }

.hm-cell.selected {
    outline: 2px solid #0d6efd;
    outline-offset: -2px;
}

.hm-cell          { color: #c84c4e; }
.hm-cell.hm-zero  { color: #aab4c2; cursor: default; }
.hm-cell.hm-income { color: #198754; }
.hm-cell.hm-hi    { color: #6e1517; }

@media (prefers-color-scheme: dark) {
    :root:not([data-theme="light"]) .hm-cell.hm-hi { color: #ffcdd2; }
}
:root[data-theme="dark"] .hm-cell.hm-hi { color: #ffcdd2; }
</style>
