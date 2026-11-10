<template>
    <div class="payment-list">
        <!-- Группировка по дате -->
        <div v-for="(payments, date) in groupedPayments" :key="date" class="payment-date-group mb-3">
            <h6 class="date-header border-bottom pb-2 mb-3">
                {{ formatLongDate(date) }}
                <small class="text-muted ms-2">
                    {{ formatCurrency(getDateTotal(payments)) }}
                </small>
            </h6>
            
            <!-- Список платежей -->
            <div v-for="payment in payments" :key="payment.paymentId" class="payment-item mb-2 p-2 border rounded">
                <!-- Направление перевода между своими счетами — первой строкой -->
                <div v-if="getTransferDirection(payment)" class="payment-transfer small fw-semibold mb-1">
                    {{ getTransferDirection(payment) }}
                </div>

                <!-- Источник, сумма, кнопка -->
                <div class="d-flex justify-content-between align-items-center mb-1" :data-id="payment.paymentId">
                    <div class="payment-source fw-medium">
                        {{ payment.paymentSource?.paymentSourceName || 'Не указан' }}
                        {{ payment.paymentSource?.description }}
                    </div>
                    <div class="d-flex align-items-center gap-2">
                        <span :class="['payment-amount', payment.paymentSum < 0 ? 'text-danger' : 'text-success']">
                            {{ formatCurrency(payment.paymentSum) }}
                        </span>
                        <button
                            class="btn btn-sm btn-outline-secondary"
                            @click="$emit('open-receipt', payment)"
                            :disabled="!payment.hasReceipt"
                            :title="payment.hasReceipt ? 'Открыть чек' : 'Чека нет'"
                        >
                            🧾
                        </button>
                        <button 
                            class="btn btn-sm btn-outline-primary"
                            @click="$emit('edit-payment', payment)"
                            title="Редактировать платеж"
                        >
                            ✏️
                        </button>
                    </div>
                </div>
                
                <!-- Категория, счёт (у перевода он уже в первой строке) и описание -->
                <div class="d-flex justify-content-between align-items-start">
                    <div class="payment-category small text-muted">
                        Категория: <strong>{{ getCategoryName(payment.effectiveCategory) }}</strong>
                        <span v-if="!getTransferDirection(payment)" class="payment-account ms-2">
                            Счёт: <strong>{{ getAccountName(payment) }}</strong>
                        </span>
                    </div>
                    <div class="payment-description small text-muted flex-grow-1 ms-3 text-end">
                        {{ payment.paymentDescription || 'Нет описания' }}
                    </div>
                </div>

                <!-- Теги -->
                <div v-if="payment.tags && payment.tags.length > 0" class="mt-1">
                    <span
                        v-for="tag in payment.tags"
                        :key="tag.tagId"
                        class="badge bg-secondary me-1"
                    >{{ tag.tagName }}</span>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { computed } from 'vue'
import { formatCurrency, formatLongDate } from '../../utils/format'
import { getTransferDirection } from '../../utils/transferDirection'

const props = defineProps({
    payments: {
        type: Array,
        required: true
    },
    categories: {
        type: Array,
        default: () => []
    }
})

defineEmits(['edit-payment', 'open-receipt'])

// Платежи по дням (YYYY-MM-DD) в порядке поступления
const groupedPayments = computed(() => {
    const grouped = {}
    props.payments.forEach(payment => {
        const date = payment.paymentDate.split('T')[0]
        if (!grouped[date]) {
            grouped[date] = []
        }
        grouped[date].push(payment)
    })
    return grouped
})

const getCategoryName = (category) => category?.categoryName || 'Без категории'

const getAccountName = (payment) => payment.account?.displayName || 'не указан'

const getDateTotal = (payments) => payments.reduce((sum, payment) => sum + payment.paymentSum, 0)
</script>

<style scoped>
.payment-list {
    font-size: 0.9rem;
}

.date-header {
    font-size: 0.95rem;
    font-weight: 600;
}

.payment-item {
    background-color: #f8f9fa;
    transition: background-color 0.2s;
}

.payment-item:hover {
    background-color: #e9ecef;
}

.payment-source {
    font-size: 0.9rem;
}

.payment-amount {
    font-weight: 600;
    font-size: 0.9rem;
}

.payment-category {
    font-size: 0.8rem;
}

.payment-description {
    font-size: 0.8rem;
    max-width: 60%;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.btn-sm {
    padding: 0.15rem 0.4rem;
    font-size: 0.75rem;
}
</style>