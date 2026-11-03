<template>
    <div class="payment-sources-container">
        <div class="card">
            <div class="card-header d-flex justify-content-between align-items-center flex-wrap gap-2">
                <div class="d-flex align-items-center gap-2 flex-wrap">
                    <h5 class="mb-0">Источники платежей</h5>

                    <input
                        type="text"
                        class="form-control form-control-sm"
                        style="width: 250px"
                        v-model="searchQuery"
                        @keyup.enter="reload"
                        placeholder="Поиск по названию..."
                    >

                    <div class="form-check mb-0">
                        <input
                            class="form-check-input"
                            type="checkbox"
                            id="filterNoCategory"
                            v-model="filterNoCategory"
                        >
                        <label class="form-check-label" for="filterNoCategory">
                            Без категории
                        </label>
                    </div>

                    <div class="btn-group btn-group-sm" role="group">
                        <button
                            v-for="ps in pageSizeOptions"
                            :key="String(ps)"
                            class="btn"
                            :class="pageSize === ps ? 'btn-secondary' : 'btn-outline-secondary'"
                            @click="setPageSize(ps)"
                        >{{ ps === null ? 'Все' : ps }}</button>
                    </div>
                </div>

                <div class="d-flex gap-2">
                    <button
                        v-if="hasChanges"
                        class="btn btn-success btn-sm"
                        @click="saveAllChanges"
                        :disabled="saving"
                    >
                        {{ saving ? 'Сохранение...' : 'Сохранить все' }}
                    </button>
                    <button
                        class="btn btn-outline-secondary btn-sm"
                        @click="reload"
                    >
                        Обновить
                    </button>
                </div>
            </div>

            <div class="card-body">
                <AlertMessage :message="error" @close="clearAlerts" />
                <AlertMessage :message="success" type="success" @close="clearAlerts" />

                <!-- Состояние загрузки -->
                <div v-if="loading" class="text-center py-4">
                    <div class="spinner-border text-primary" role="status">
                        <span class="visually-hidden">Загрузка...</span>
                    </div>
                </div>

                <template v-else>
                    <div class="table-responsive">
                        <table class="table table-hover">
                            <thead class="table-light">
                                <tr>
                                    <th width="80">ID</th>
                                    <th>Название</th>
                                    <th>Категория</th>
                                    <th>Классификация</th>
                                    <th>Описание</th>
                                    <th width="140">Дата создания</th>
                                    <th width="100" class="text-center">Действия</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr
                                    v-for="source in paymentSources"
                                    :key="source.paymentSourceId"
                                    :class="{ 'table-warning': hasChangesForSource(source) }"
                                >
                                    <td>
                                        <span class="text-muted">{{ source.paymentSourceId }}</span>
                                    </td>
                                    <td class="source-name">
                                        <span class="text-muted">{{ source.paymentSourceName }}</span>
                                    </td>
                                    <td>
                                        <CategoryTreeSelect
                                            v-model="source.categoryId"
                                            :categories="categories"
                                            size="sm"
                                            :auto-load="false"
                                        />
                                    </td>
                                    <td>
                                        <div class="d-flex align-items-center gap-1">
                                            <input
                                                type="text"
                                                class="form-control form-control-sm"
                                                v-model="source.classification"
                                                maxlength="200"
                                                placeholder="Классификация..."
                                            >
                                            <span
                                                v-if="source.isAiClassified && source.classification === source.originalClassification"
                                                class="badge text-bg-info"
                                                title="Классификацию задала LLM"
                                            >ИИ</span>
                                        </div>
                                    </td>
                                    <td>
                                        <textarea
                                            class="form-control form-control-sm"
                                            v-model="source.description"
                                            rows="1"
                                            placeholder="Описание..."
                                        ></textarea>
                                    </td>
                                    <td class="created-date">
                                        <small class="text-muted">{{ formatDate(source.createdAt) }}</small>
                                    </td>
                                    <td class="text-center">
                                        <div class="btn-group btn-group-sm" role="group">
                                            <button
                                                class="btn btn-success"
                                                @click="saveSingleItem(source)"
                                                :disabled="!hasChangesForSource(source) || saving"
                                                title="Сохранить"
                                            >💾</button>
                                            <button
                                                class="btn btn-info text-white"
                                                @click="showPaymentsForSource(source)"
                                                title="Показать платежи"
                                            >👁</button>
                                        </div>
                                    </td>
                                </tr>
                            </tbody>
                        </table>
                    </div>

                    <!-- Пустой список -->
                    <div v-if="paymentSources.length === 0" class="text-center py-4 text-muted">
                        Нет источников платежей
                    </div>

                    <!-- Пагинация -->
                    <div v-if="totalPages > 1" class="d-flex justify-content-between align-items-center mt-3">
                        <small class="text-muted">
                            {{ totalCount }} записей, страница {{ currentPage }} из {{ totalPages }}
                        </small>
                        <nav>
                            <ul class="pagination pagination-sm mb-0">
                                <li class="page-item" :class="{ disabled: currentPage === 1 }">
                                    <button class="page-link" @click="changePage(1)">«</button>
                                </li>
                                <li class="page-item" :class="{ disabled: currentPage === 1 }">
                                    <button class="page-link" @click="changePage(currentPage - 1)">‹</button>
                                </li>
                                <li
                                    v-for="p in visiblePages"
                                    :key="p"
                                    class="page-item"
                                    :class="{ active: p === currentPage }"
                                >
                                    <button class="page-link" @click="changePage(p)">{{ p }}</button>
                                </li>
                                <li class="page-item" :class="{ disabled: currentPage === totalPages }">
                                    <button class="page-link" @click="changePage(currentPage + 1)">›</button>
                                </li>
                                <li class="page-item" :class="{ disabled: currentPage === totalPages }">
                                    <button class="page-link" @click="changePage(totalPages)">»</button>
                                </li>
                            </ul>
                        </nav>
                    </div>
                </template>

                <!-- Несохраненные изменения -->
                <div v-if="hasChanges" class="mt-3 p-3 bg-light rounded">
                    <div class="row align-items-center">
                        <div class="col">
                            <strong>Несохраненные изменения:</strong>
                            {{ changedItemsCount }} источник(ов)
                        </div>
                        <div class="col text-end">
                            <button
                                class="btn btn-success"
                                @click="saveAllChanges"
                                :disabled="saving"
                            >
                                {{ saving ? 'Сохранение...' : `Сохранить все (${changedItemsCount})` }}
                            </button>
                            <button
                                class="btn btn-outline-secondary ms-2"
                                @click="resetAllChanges"
                            >
                                Сбросить все
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Модальное окно для отображения платежей -->
        <div class="modal fade" id="paymentsModal" tabindex="-1" aria-labelledby="paymentsModalLabel" aria-hidden="true">
            <div class="modal-dialog modal-lg">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title" id="paymentsModalLabel">
                            Платежи по источнику: {{ selectedSource?.paymentSourceName }}
                        </h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body">
                        <AlertMessage :message="paymentsError" @close="paymentsError = null" />
                        <div v-if="loadingPayments" class="text-center py-3">
                            <div class="spinner-border spinner-border-sm" role="status">
                                <span class="visually-hidden">Загрузка платежей...</span>
                            </div>
                            <span class="ms-2">Загрузка платежей...</span>
                        </div>
                        <div v-else>
                            <div v-if="sourcePayments.length === 0 && !paymentsError" class="text-center py-3 text-muted">
                                Нет платежей для этого источника
                            </div>
                            <div v-else class="table-responsive">
                                <table class="table table-sm table-striped">
                                    <thead>
                                        <tr>
                                            <th>Дата</th>
                                            <th>Сумма</th>
                                            <th>Категория</th>
                                            <th>Описание</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        <tr v-for="payment in sourcePayments" :key="payment.paymentId">
                                            <td>{{ formatDate(payment.paymentDate) }}</td>
                                            <td :class="payment.paymentSum >= 0 ? 'text-success' : 'text-danger'">
                                                <strong>{{ formatCurrency(payment.paymentSum) }}</strong>
                                            </td>
                                            <td>
                                                <span class="badge bg-secondary">
                                                    {{ payment.effectiveCategory?.categoryName || 'Без категории' }}
                                                </span>
                                            </td>
                                            <td>{{ payment.paymentDescription || '-' }}</td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Закрыть</button>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import axios from 'axios'
import { getErrorMessage } from '../../utils/apiError'
import { formatCurrency, formatDate } from '../../utils/format'
import { APP_CONFIG } from '../../config'
import { Modal } from 'bootstrap'
import CategoryTreeSelect from '../../components/CategoryTreeSelect.vue'
import AlertMessage from '../../components/AlertMessage.vue'
import { useAlerts } from '../../composables/useAlerts'

const paymentSources = ref([])
const categories = ref([])
const loading = ref(false)
const saving = ref(false)
const { error, success, showError, showSuccess, clearAlerts } = useAlerts()

const searchQuery = ref('')
const filterNoCategory = ref(false)
const currentPage = ref(1)
const pageSize = ref(500)
const totalCount = ref(0)
const pageSizeOptions = [500, 1000, 5000, null]

const selectedSource = ref(null)
const sourcePayments = ref([])
const loadingPayments = ref(false)
const paymentsError = ref(null)
const paymentsModal = ref(null)

const totalPages = computed(() =>
    pageSize.value ? Math.ceil(totalCount.value / pageSize.value) : 1
)

const visiblePages = computed(() => {
    const total = totalPages.value, current = currentPage.value, delta = 2
    const left = Math.max(1, current - delta)
    const right = Math.min(total, current + delta)
    const pages = []
    for (let i = left; i <= right; i++) pages.push(i)
    return pages
})

const hasChanges = computed(() => paymentSources.value.some(s => hasChangesForSource(s)))

const changedItemsCount = computed(() => paymentSources.value.filter(s => hasChangesForSource(s)).length)

const hasChangesForSource = (source) =>
    source.categoryId !== source.originalCategoryId ||
    source.description !== source.originalDescription ||
    source.classification !== source.originalClassification

// После сохранения текущие значения становятся исходными; изменённая классификация — уже не от ИИ (так же решает backend).
const markSaved = (source) => {
    if (source.classification !== source.originalClassification) source.isAiClassified = false
    source.originalCategoryId = source.categoryId
    source.originalDescription = source.description
    source.originalClassification = source.classification
}

const toSourcePayload = (source) => ({
    paymentSourceName: source.paymentSourceName,
    categoryId: source.categoryId,
    description: source.description,
    classification: source.classification
})

const loadPaymentSources = async () => {
    loading.value = true
    clearAlerts()
    try {
        const params = {
            search: searchQuery.value || undefined,
            noCategoryOnly: filterNoCategory.value || undefined,
            page: currentPage.value,
            pageSize: pageSize.value || undefined
        }
        const response = await axios.get(APP_CONFIG.apiBaseUrl + '/api/PaymentSourcesApi/list', { params })
        totalCount.value = response.data.totalCount
        paymentSources.value = response.data.items.map(source => ({
            ...source,
            classification: source.classification ?? '',
            originalCategoryId: source.categoryId,
            originalDescription: source.description,
            originalClassification: source.classification ?? '',
            createdAt: source.createdAt || new Date().toISOString()
        }))
    } catch (error) {
        console.error('Ошибка загрузки источников платежей:', error)
        showError('Ошибка загрузки данных')
    } finally {
        loading.value = false
    }
}

const reload = () => {
    currentPage.value = 1
    loadPaymentSources()
}

const changePage = (page) => {
    if (page < 1 || page > totalPages.value || page === currentPage.value) return
    if (hasChanges.value && !confirm('Есть несохраненные изменения. Сменить страницу?')) return
    currentPage.value = page
    loadPaymentSources()
}

const setPageSize = (ps) => {
    pageSize.value = ps
    reload()
}

const loadCategories = async () => {
    try {
        const response = await axios.get(APP_CONFIG.apiBaseUrl + '/api/CategoriesApi/getTree')
        categories.value = response.data || []
    } catch (error) {
        console.error('Ошибка загрузки категорий:', error)
        categories.value = []
    }
}

const loadPaymentsForSource = async (paymentSourceId) => {
    loadingPayments.value = true
    paymentsError.value = null
    try {
        const response = await axios.get(`${APP_CONFIG.apiBaseUrl}/api/PaymentsApi/by-source/${paymentSourceId}`)
        sourcePayments.value = response.data
    } catch (error) {
        console.error('Ошибка загрузки платежей:', error)
        sourcePayments.value = []
        paymentsError.value = 'Ошибка загрузки платежей'
    } finally {
        loadingPayments.value = false
    }
}

const showPaymentsForSource = async (source) => {
    selectedSource.value = source
    await loadPaymentsForSource(source.paymentSourceId)
    if (!paymentsModal.value) {
        paymentsModal.value = new Modal(document.getElementById('paymentsModal'))
    }
    paymentsModal.value.show()
}

const saveSingleItem = async (source) => {
    saving.value = true
    clearAlerts()
    try {
        await axios.put(
            `${APP_CONFIG.apiBaseUrl}/api/PaymentSourcesApi/${source.paymentSourceId}`,
            toSourcePayload(source)
        )
        markSaved(source)
    } catch (error) {
        console.error('Ошибка сохранения источника:', error)
        showError(getErrorMessage(error, 'Ошибка сохранения'))
    } finally {
        saving.value = false
    }
}

const saveAllChanges = async () => {
    saving.value = true
    clearAlerts()
    try {
        const changedSources = paymentSources.value.filter(s => hasChangesForSource(s))
        await Promise.all(changedSources.map(source =>
            axios.put(`${APP_CONFIG.apiBaseUrl}/api/PaymentSourcesApi`, {
                paymentSourceId: source.paymentSourceId,
                ...toSourcePayload(source)
            })
        ))
        changedSources.forEach(markSaved)
        showSuccess(`Успешно обновлено источников: ${changedSources.length}`)
    } catch (error) {
        console.error('Ошибка массового сохранения:', error)
        showError(getErrorMessage(error, 'Ошибка сохранения некоторых источников'))
    } finally {
        saving.value = false
    }
}

const resetAllChanges = () => {
    paymentSources.value.forEach(source => {
        source.categoryId = source.originalCategoryId
        source.description = source.originalDescription
        source.classification = source.originalClassification
    })
}

watch(filterNoCategory, reload)

onMounted(() => {
    loadCategories()
    loadPaymentSources()
})
</script>

<style scoped>
.payment-sources-container {
    max-width: 100%;
}

.table td {
    vertical-align: middle;
}

.source-name {
    font-weight: 500;
}

.created-date {
    font-size: 0.875rem;
}

.table-warning {
    background-color: #fff3cd;
}

textarea.form-control {
    resize: vertical;
    min-height: 38px;
}
</style>
