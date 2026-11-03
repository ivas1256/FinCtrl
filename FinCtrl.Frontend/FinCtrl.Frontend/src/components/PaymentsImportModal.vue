<template>
    <div class="modal fade" id="paymentsImportModal" tabindex="-1" aria-labelledby="paymentsImportModalLabel" aria-hidden="true">
        <div class="modal-dialog modal-lg modal-dialog-scrollable">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title" id="paymentsImportModalLabel">Загрузка файла</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Закрыть" :disabled="uploading"></button>
                </div>
                <div class="modal-body">
                    <AlertMessage :message="error" @close="clearAlerts" />
                    <AlertMessage :message="success" type="success" @close="clearAlerts" />
                    <AlertMessage :message="classificationError" type="warning" @close="classificationError = null" />

                    <div class="d-flex gap-2 mb-3">
                        <input
                            ref="fileInput"
                            type="file"
                            class="form-control form-control-sm"
                            accept=".xlsx,.csv"
                            :disabled="uploading"
                            @change="onFileSelected"
                        >
                        <button
                            class="btn btn-primary btn-sm text-nowrap"
                            :disabled="!selectedFile || uploading"
                            @click="upload"
                        >Загрузить</button>
                    </div>

                    <div v-if="uploading" class="text-center py-5 upload-progress">
                        <div class="spinner-border text-primary" role="status">
                            <span class="visually-hidden">Загрузка...</span>
                        </div>
                        <div class="text-muted small mt-2">Загрузка файла и классификация новых источников…</div>
                    </div>

                    <template v-else-if="result">
                        <div class="btn-group btn-group-sm mb-3" role="group">
                            <button
                                type="button"
                                class="btn"
                                :class="activeTab === Tab.Payments ? 'btn-primary' : 'btn-outline-secondary'"
                                @click="activeTab = Tab.Payments"
                            >Новые платежи ({{ result.newPayments.length }})</button>
                            <button
                                type="button"
                                class="btn"
                                :class="activeTab === Tab.Sources ? 'btn-primary' : 'btn-outline-secondary'"
                                @click="activeTab = Tab.Sources"
                            >Новые источники платежей ({{ result.newPaymentSources.length }})</button>
                        </div>

                        <div v-if="activeTab === Tab.Payments" class="payments-tab">
                            <div v-if="result.newPayments.length === 0" class="text-center py-4 text-muted">
                                Новых платежей нет
                            </div>
                            <div v-else class="table-responsive">
                                <table class="table table-sm table-striped">
                                    <thead>
                                        <tr>
                                            <th>Дата</th>
                                            <th>Сумма</th>
                                            <th>Источник</th>
                                            <th>Счёт</th>
                                            <th>Описание</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        <tr v-for="payment in result.newPayments" :key="payment.paymentId">
                                            <td class="text-nowrap">{{ formatDate(payment.paymentDate) }}</td>
                                            <td class="text-nowrap" :class="payment.paymentSum >= 0 ? 'text-success' : 'text-danger'">
                                                {{ formatCurrency(payment.paymentSum) }}
                                            </td>
                                            <td>{{ payment.paymentSource?.paymentSourceName || '—' }}</td>
                                            <td>{{ payment.account?.displayName || payment.account?.excelAccountName || '—' }}</td>
                                            <td>{{ payment.paymentDescription || '—' }}</td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>
                        </div>

                        <div v-else class="sources-tab">
                            <div v-if="result.newPaymentSources.length === 0" class="text-center py-4 text-muted">
                                Новых источников нет
                            </div>
                            <div v-else class="table-responsive">
                                <table class="table table-sm table-striped">
                                    <thead>
                                        <tr>
                                            <th>Название</th>
                                            <th>Классификация</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        <tr v-for="source in result.newPaymentSources" :key="source.paymentSourceId">
                                            <td>{{ source.paymentSourceName }}</td>
                                            <td>
                                                <template v-if="source.classification">
                                                    {{ source.classification }}
                                                    <span v-if="source.isAiClassified" class="badge text-bg-info ms-1" title="Классификацию задала LLM">ИИ</span>
                                                </template>
                                                <span v-else class="text-muted">—</span>
                                            </td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>
                        </div>
                    </template>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-outline-secondary btn-sm" data-bs-dismiss="modal" :disabled="uploading">Закрыть</button>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref } from 'vue'
import axios from 'axios'
import { Modal } from 'bootstrap'
import { APP_CONFIG } from '../config'
import { getErrorMessage } from '../utils/apiError'
import { formatCurrency, formatDate } from '../utils/format'
import AlertMessage from './AlertMessage.vue'
import { useAlerts } from '../composables/useAlerts'
import { usePaymentsImport } from '../composables/usePaymentsImport'

const Tab = Object.freeze({ Payments: 'payments', Sources: 'sources' })

const { error, success, showError, showSuccess, clearAlerts } = useAlerts()
const { notifyImported } = usePaymentsImport()

const fileInput = ref(null)
const selectedFile = ref(null)
const uploading = ref(false)
const result = ref(null)
const classificationError = ref(null)
const activeTab = ref(Tab.Payments)

const resetState = () => {
    clearAlerts()
    classificationError.value = null
    result.value = null
    selectedFile.value = null
    activeTab.value = Tab.Payments
    if (fileInput.value) fileInput.value.value = ''
}

const open = () => {
    resetState()
    Modal.getOrCreateInstance(document.getElementById('paymentsImportModal')).show()
}

const onFileSelected = (event) => {
    selectedFile.value = event.target.files[0] || null
}

const upload = async () => {
    if (!selectedFile.value) return
    clearAlerts()
    classificationError.value = null
    result.value = null
    uploading.value = true
    try {
        const formData = new FormData()
        formData.append('file', selectedFile.value)
        const response = await axios.post(APP_CONFIG.apiBaseUrl + '/api/PaymentsApi/upload', formData, {
            headers: { 'Content-Type': 'multipart/form-data' }
        })
        result.value = response.data
        classificationError.value = response.data.classificationError || null
        activeTab.value = Tab.Payments
        showSuccess(`Загружено платежей: ${response.data.newPayments.length}, новых источников: ${response.data.newPaymentSources.length}`)
        notifyImported()
    } catch (err) {
        showError(getErrorMessage(err, 'Ошибка загрузки файла'))
    } finally {
        uploading.value = false
    }
}

defineExpose({ open })
</script>
