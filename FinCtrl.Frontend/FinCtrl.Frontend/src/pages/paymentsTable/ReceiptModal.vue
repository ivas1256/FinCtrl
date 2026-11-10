<template>
    <div class="modal fade" ref="modalEl" tabindex="-1" aria-hidden="true">
        <div class="modal-dialog">
            <div class="modal-content receipt">
                <div class="modal-header">
                    <div>
                        <h6 class="modal-title mb-0">Кассовый чек · #{{ payment?.paymentId }}</h6>
                        <div class="small text-muted">{{ formatDateTime(payment?.paymentDate) }}</div>
                    </div>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Закрыть"></button>
                </div>
                <div class="modal-body receipt-body">
                    <AlertMessage :message="error" @close="clearAlerts" />

                    <div v-if="loading" class="text-center py-4">
                        <div class="spinner-border spinner-border-sm text-primary" role="status">
                            <span class="visually-hidden">Загрузка...</span>
                        </div>
                    </div>

                    <div v-else-if="receipt && receipt.items.length === 0" class="text-center py-4 text-muted">
                        У этой операции нет чека
                    </div>

                    <template v-else-if="receipt">
                        <div v-for="item in receipt.items" :key="item.receiptItemId" class="receipt-item mb-2">
                            <div class="receipt-name">{{ item.itemName }}</div>
                            <div class="d-flex align-items-baseline text-muted small">
                                <span>{{ formatQuantity(item.quantity) }} {{ item.unitName }} × {{ formatCurrency(item.price) }}</span>
                                <span class="receipt-leader flex-grow-1 mx-1"></span>
                                <span class="receipt-item-total">{{ formatCurrency(item.totalPrice) }}</span>
                            </div>
                        </div>
                        <div class="receipt-total d-flex justify-content-between fw-bold pt-2 mt-3">
                            <span>ИТОГ</span>
                            <span class="receipt-total-sum">{{ formatCurrency(receipt.totalSum) }}</span>
                        </div>
                        <div v-if="sumMismatch" class="receipt-payment-sum small text-muted mt-1">
                            Сумма операции: {{ formatCurrency(payment.paymentSum) }}
                        </div>
                    </template>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-outline-secondary btn-sm" data-bs-dismiss="modal">Закрыть</button>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import axios from 'axios'
import { Modal } from 'bootstrap'
import { APP_CONFIG } from '../../config'
import { getErrorMessage } from '../../utils/apiError'
import { formatCurrency, formatDateTime, formatQuantity } from '../../utils/format'
import AlertMessage from '../../components/AlertMessage.vue'
import { useAlerts } from '../../composables/useAlerts'

const modalEl = ref(null)
const payment = ref(null)
const receipt = ref(null)
const loading = ref(false)
const { error, showError, clearAlerts } = useAlerts()

let requestId = 0

const sumMismatch = computed(() =>
    !!receipt.value && receipt.value.items.length > 0 && !!payment.value &&
    Math.abs(payment.value.paymentSum) !== receipt.value.totalSum)

// Открывает модалку и заново запрашивает чек платежа
const open = async (p) => {
    const id = ++requestId
    payment.value = p
    receipt.value = null
    loading.value = true
    clearAlerts()
    Modal.getOrCreateInstance(modalEl.value).show()
    try {
        const response = await axios.get(`${APP_CONFIG.apiBaseUrl}/api/PaymentsApi/${p.paymentId}/receipt`)
        if (id !== requestId) return
        receipt.value = response.data
    } catch (e) {
        if (id !== requestId) return
        console.error(e)
        showError(getErrorMessage(e, 'Ошибка загрузки чека'))
    } finally {
        if (id === requestId) loading.value = false
    }
}

defineExpose({ open })
</script>

<style scoped>
.receipt-body {
    font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, 'Liberation Mono', monospace;
}

.receipt-leader {
    border-bottom: 1px dotted #adb5bd;
    transform: translateY(-0.25em);
}

.receipt-total {
    border-top: 1px dashed #6c757d;
}
</style>
