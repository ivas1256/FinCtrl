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
                    :key="source.paymentSourceId"
                    class="list-group-item d-flex justify-content-between align-items-center p-0 border-0"
                >
                    <div class="py-2">
                        <div class="fw-medium">{{ source.paymentSourceName || 'Без источника' }}</div>
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

<script>
import { formatCurrency } from '../../utils/format'

export default {
    name: 'PaymentSourcesList',
    props: {
        payments: {
            type: Array,
            required: true,
            default: () => []
        }
    },
    data() {
        return {
            sortOrder: 'asc' // 'asc' или 'desc'
        }
    },
    computed: {
        // Группируем платежи по источникам
        sourcesWithAmounts() {
            const sourcesMap = {}
            
            this.payments.forEach(payment => {
                const sourceId = payment.paymentSource?.paymentSourceId || 'unknown'
                const sourceName = payment.paymentSource?.paymentSourceName || 'Без источника'
                
                if (!sourcesMap[sourceId]) {
                    sourcesMap[sourceId] = {
                        paymentSourceId: sourceId,
                        paymentSourceName: sourceName,
                        totalSum: 0,
                        count: 0
                    }
                }
                
                sourcesMap[sourceId].totalSum += payment.paymentSum
                sourcesMap[sourceId].count += 1
            })
            
            return Object.values(sourcesMap)
        },
        
        // Отсортированные источники
        sortedSources() {
            const sources = [...this.sourcesWithAmounts]
            
            return sources.sort((a, b) => {
                if (this.sortOrder === 'desc') {
                    return b.totalSum - a.totalSum // по убыванию
                } else {
                    return a.totalSum - b.totalSum // по возрастанию
                }
            })
        },
        
        // Общая сумма по всем источникам
        totalAllSources() {
            return this.sourcesWithAmounts.reduce((sum, source) => sum + source.totalSum, 0)
        }
    },
    methods: {
        formatCurrency,
        
        toggleSortOrder() {
            // Переключаем между убывающим и возрастающим порядком
            this.sortOrder = this.sortOrder === 'desc' ? 'asc' : 'desc'
        }
    }
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

.payment-amount {
    font-size: 1.1rem;
    font-weight: 600;
    color: #0d6efd;
}
</style>