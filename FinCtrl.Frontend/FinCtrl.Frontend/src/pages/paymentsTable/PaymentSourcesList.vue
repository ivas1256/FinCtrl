<!-- PaymentSourcesList.vue -->
<template>
    <div class="payment-sources-section">
        <div class="d-flex justify-content-between align-items-center mb-3">
            <h6 class="mb-0">По источникам</h6>
            <button 
                class="btn btn-sm btn-outline-secondary"
                @click="toggleSortOrder"
                title="Изменить порядок сортировки"
            >
                <template v-if="sortOrder === 'desc'">
                    ↓ По сумме
                </template>
                <template v-else>
                    ↑ По сумме
                </template>
            </button>
        </div>
        
        <div v-if="sourcesWithAmounts.length === 0" class="text-muted">
            Нет данных по источникам
        </div>
        
        <div v-else>
            <div class="list-group">
                <div 
                    v-for="source in sortedSources"
                    :key="source.name"
                    class="list-group-item d-flex justify-content-between align-items-center p-0 border-0"
                >
                    <div class="py-2">
                        <div class="fw-medium">{{ source.name }}</div>
                        <div v-if="sourceNamesCaption(source)" class="text-muted small source-names">
                            {{ sourceNamesCaption(source) }}
                        </div>
                        <div class="text-muted small">
                            {{ source.count }} платеж(ей)
                        </div>
                    </div>
                    <div class="payment-amount">
                        {{ formatCurrency(source.totalSum) }}
                    </div>
                </div>
            </div>
            
            <div class="mt-3 pt-2 border-top">
                <div class="d-flex justify-content-between fw-bold fs-6">
                    <span>Итого:</span>
                    <span>{{ formatCurrency(totalAllSources) }}</span>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { formatCurrency } from '../../utils/format'
import { groupPaymentsBySource, sourceNamesCaption } from '../../utils/paymentSourceGroups'

const props = defineProps({
    payments: {
        type: Array,
        default: () => []
    }
})

const sortOrder = ref('asc') // 'asc' или 'desc'

// Группируем платежи по классификации источника (без неё — по имени источника)
const sourcesWithAmounts = computed(() => groupPaymentsBySource(props.payments))

// Отсортированные по сумме источники
const sortedSources = computed(() =>
    [...sourcesWithAmounts.value].sort((a, b) =>
        sortOrder.value === 'desc' ? b.totalSum - a.totalSum : a.totalSum - b.totalSum
    )
)

// Общая сумма по всем источникам
const totalAllSources = computed(() =>
    sourcesWithAmounts.value.reduce((sum, source) => sum + source.totalSum, 0)
)

const toggleSortOrder = () => {
    sortOrder.value = sortOrder.value === 'desc' ? 'asc' : 'desc'
}
</script>

<style scoped>
.payment-sources-section {
    background-color: #f8f9fa;
    border-radius: 0.375rem;
    padding: 1rem;
    margin-top: 1rem;
}

.list-group-item {
    background-color: transparent;
}

.source-names {
    overflow-wrap: anywhere;
}

.payment-amount {
    font-size: 1.1rem;
    font-weight: 600;
    color: #0d6efd;
}
</style>