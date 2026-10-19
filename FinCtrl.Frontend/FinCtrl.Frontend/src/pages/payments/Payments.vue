<template>
    <div class="payments-container">
        <div class="card">
            <div class="card-header d-flex justify-content-between align-items-center">
                <h5 class="mb-0">Операции</h5>
                <div class="d-flex gap-2">
                    <input
                        type="file"
                        ref="fileInput"
                        accept=".xlsx,.csv"
                        style="display: none"
                        @change="onFileSelected"
                    >
                    <button
                        class="btn btn-outline-secondary btn-sm"
                        @click="fileInput.click()"
                        :disabled="uploading"
                    >
                        {{ uploading ? 'Загрузка...' : 'Загрузить файл' }}
                    </button>
                    <button class="btn btn-outline-secondary btn-sm" @click="reload">
                        Обновить
                    </button>
                    <button class="btn btn-outline-secondary btn-sm" @click="openBulkModal">
                        Назначить теги…
                    </button>
                </div>
            </div>
            <div class="card-body">

                <!-- Фильтры -->
                <div class="d-flex flex-wrap align-items-end gap-3 mb-3">
                    <!-- Диапазон дат -->
                    <div class="d-flex align-items-center gap-1">
                        <input
                            type="date"
                            class="form-control form-control-sm date-input"
                            v-model="dateFrom"
                            @change="reload"
                        >
                        <span class="text-muted small">—</span>
                        <input
                            type="date"
                            class="form-control form-control-sm date-input"
                            v-model="dateTo"
                            @change="reload"
                        >
                    </div>

                    <!-- Источник с поиском -->
                    <div class="source-select-wrapper position-relative">
                        <input
                            type="text"
                            class="form-control form-control-sm source-input"
                            v-model="sourceSearch"
                            placeholder="Все источники"
                            @focus="sourceDropdownOpen = true"
                            @blur="onSourceBlur"
                            @input="sourceDropdownOpen = true"
                            autocomplete="off"
                        >
                        <div v-if="sourceDropdownOpen" class="dropdown-menu show w-100 source-dropdown">
                            <button
                                class="dropdown-item small"
                                :class="{ active: selectedSourceId === null }"
                                @mousedown.prevent="selectSource(null)"
                            >
                                Все источники
                            </button>
                            <button
                                v-for="src in filteredSourceOptions"
                                :key="src.paymentSourceId"
                                class="dropdown-item small"
                                :class="{ active: selectedSourceId === src.paymentSourceId }"
                                @mousedown.prevent="selectSource(src)"
                            >
                                {{ src.paymentSourceName }}
                            </button>
                            <div v-if="filteredSourceOptions.length === 0 && sourceSearch" class="dropdown-item disabled text-muted small">
                                Не найдено
                            </div>
                        </div>
                    </div>

                    <!-- Без категории -->
                    <div class="form-check mb-0">
                        <input
                            class="form-check-input"
                            type="checkbox"
                            id="noCategoryCheck"
                            v-model="showNoCategoryOnly"
                            @change="reload"
                        >
                        <label class="form-check-label small" for="noCategoryCheck">
                            Без категории
                        </label>
                    </div>

                    <!-- Фильтр по тегам -->
                    <div class="tag-filter-wrapper">
                        <TagMultiSelect v-model="selectedTagIds" :tags="tags" @created="loadTags" @update:model-value="reload" />
                    </div>
                </div>

                <!-- Таблица -->
                <div class="table-responsive">
                    <table class="table table-hover">
                        <thead class="table-light">
                            <tr>
                                <th width="80">ID</th>
                                <th width="130" class="sortable-th" @click="toggleSort('date')">
                                    Дата <span class="sort-icon">{{ sortIcon('date') }}</span>
                                </th>
                                <th width="130">Сумма</th>
                                <th class="sortable-th" @click="toggleSort('source')">
                                    Источник <span class="sort-icon">{{ sortIcon('source') }}</span>
                                </th>
                                <th>Счет</th>
                                <th class="sortable-th" @click="toggleSort('category')">
                                    Категория <span class="sort-icon">{{ sortIcon('category') }}</span>
                                </th>
                                <th>Описание</th>
                                <th width="220">Теги</th>
                                <th width="60" class="text-center">Действия</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr
                                v-for="payment in payments"
                                :key="payment.paymentId"
                                :class="{ 'table-warning': hasChangesForPayment(payment) }"
                            >
                                <td><span class="text-muted">{{ payment.paymentId }}</span></td>
                                <td><small class="text-muted">{{ formatDate(payment.paymentDate) }}</small></td>
                                <td :class="payment.paymentSum >= 0 ? 'text-success fw-bold' : 'text-danger fw-bold'">
                                    {{ formatCurrency(payment.paymentSum) }}
                                </td>
                                <td><span class="text-muted">{{ payment.paymentSource?.paymentSourceName || '-' }}</span></td>
                                <td><span class="text-muted">{{ payment.account?.displayName || '-' }}</span></td>
                                <td>
                                    <div class="d-flex align-items-center gap-2">
                                        <CategoryTreeSelect
                                            v-model="payment.categoryId"
                                            :categories="categories"
                                            size="sm"
                                            :auto-load="false"
                                        />
                                        <span
                                            v-if="payment.categorySource === 'payment'"
                                            class="badge bg-primary flex-shrink-0"
                                            title="Категория задана в платеже"
                                        >💳</span>
                                        <span
                                            v-else-if="payment.categorySource === 'source'"
                                            class="badge bg-secondary flex-shrink-0"
                                            title="Категория из источника"
                                        >📁</span>
                                    </div>
                                </td>
                                <td>
                                    <textarea
                                        class="form-control form-control-sm"
                                        v-model="payment.paymentDescription"
                                        rows="1"
                                        placeholder="Описание..."
                                    ></textarea>
                                </td>
                                <td>
                                    <TagMultiSelect v-model="payment.tagIds" :tags="tags" @created="loadTags" />
                                </td>
                                <td class="text-center">
                                    <div class="btn-group btn-group-sm">
                                        <button
                                            class="btn btn-success"
                                            @click="saveSinglePayment(payment)"
                                            :disabled="!hasChangesForPayment(payment) || saving"
                                            title="Сохранить"
                                        >💾</button>
                                    </div>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>

                <!-- Пагинация -->
                <div class="d-flex align-items-center flex-wrap gap-3 mt-3">
                    <!-- Размер страницы -->
                    <div class="d-flex align-items-center gap-2">
                        <span class="text-muted small">Строк:</span>
                        <div class="btn-group btn-group-sm">
                            <button
                                v-for="opt in pageSizeOptions"
                                :key="opt.label"
                                :class="['btn', pageSize === opt.value ? 'btn-primary' : 'btn-outline-secondary']"
                                @click="setPageSize(opt.value)"
                            >{{ opt.label }}</button>
                        </div>
                    </div>

                    <!-- Навигация -->
                    <nav v-if="totalPages > 1" aria-label="Страницы">
                        <ul class="pagination pagination-sm mb-0">
                            <li class="page-item" :class="{ disabled: currentPage === 1 }">
                                <button class="page-link" @click="changePage(1)" :disabled="currentPage === 1">«</button>
                            </li>
                            <li class="page-item" :class="{ disabled: currentPage === 1 }">
                                <button class="page-link" @click="changePage(currentPage - 1)" :disabled="currentPage === 1">‹</button>
                            </li>
                            <li v-if="visiblePages[0] > 1" class="page-item disabled">
                                <span class="page-link">…</span>
                            </li>
                            <li
                                v-for="page in visiblePages"
                                :key="page"
                                class="page-item"
                                :class="{ active: currentPage === page }"
                            >
                                <button class="page-link" @click="changePage(page)">{{ page }}</button>
                            </li>
                            <li v-if="visiblePages[visiblePages.length - 1] < totalPages" class="page-item disabled">
                                <span class="page-link">…</span>
                            </li>
                            <li class="page-item" :class="{ disabled: currentPage === totalPages }">
                                <button class="page-link" @click="changePage(currentPage + 1)" :disabled="currentPage === totalPages">›</button>
                            </li>
                            <li class="page-item" :class="{ disabled: currentPage === totalPages }">
                                <button class="page-link" @click="changePage(totalPages)" :disabled="currentPage === totalPages">»</button>
                            </li>
                        </ul>
                    </nav>

                    <span class="text-muted small ms-auto">Итого: {{ totalCount }}</span>
                </div>

                <!-- Несохранённые изменения -->
                <div v-if="hasChanges" class="mt-3 p-3 bg-light rounded">
                    <div class="row align-items-center">
                        <div class="col">
                            <strong>Несохраненные изменения:</strong> {{ changedItemsCount }} операций
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
                            >Сбросить все</button>
                        </div>
                    </div>
                </div>

                <!-- Состояние загрузки -->
                <div v-if="loading" class="text-center py-4">
                    <div class="spinner-border text-primary" role="status">
                        <span class="visually-hidden">Загрузка...</span>
                    </div>
                </div>

                <!-- Пустой список -->
                <div v-if="!loading && totalCount === 0" class="text-center py-4 text-muted">
                    Нет операций
                </div>
            </div>
        </div>

        <!-- Модальное окно массового назначения тегов -->
        <div class="modal fade" id="bulkTagModal" tabindex="-1" aria-hidden="true">
            <div class="modal-dialog">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">Назначить теги операциям</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body">
                        <div class="mb-3">
                            <label class="form-label">Период</label>
                            <div class="d-flex align-items-center gap-1">
                                <input type="date" class="form-control form-control-sm" v-model="bulkDateFrom">
                                <span class="text-muted small">—</span>
                                <input type="date" class="form-control form-control-sm" v-model="bulkDateTo">
                            </div>
                        </div>

                        <div class="mb-3">
                            <label class="form-label">Категории</label>
                            <Multiselect
                                v-model="bulkSelectedCategories"
                                :options="flatCategoryOptions"
                                label="displayName"
                                track-by="categoryId"
                                :multiple="true"
                                :close-on-select="false"
                                placeholder="Выберите категории"
                                select-label=""
                                deselect-label=""
                                selected-label=""
                            />
                        </div>

                        <div class="mb-3">
                            <label class="form-label">Теги</label>
                            <TagMultiSelect v-model="bulkTagIds" :tags="tags" @created="loadTags" />
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Отмена</button>
                        <button
                            type="button"
                            class="btn btn-primary"
                            :disabled="bulkSubmitting"
                            @click="submitBulkAssign"
                        >{{ bulkSubmitting ? 'Применение...' : 'Применить' }}</button>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import axios from 'axios'
import { APP_CONFIG } from '../../../config'
import CategoryTreeSelect from '../../components/CategoryTreeSelect.vue'
import TagMultiSelect from '../../components/TagMultiSelect.vue'
import Multiselect from 'vue-multiselect'

const payments = ref([])
const categories = ref([])
const sources = ref([])
const tags = ref([])
const totalCount = ref(0)
const loading = ref(false)
const saving = ref(false)
const uploading = ref(false)
const fileInput = ref(null)

// Фильтры
const dateFrom = ref('')
const dateTo = ref('')
const selectedSourceId = ref(null)
const sourceSearch = ref('')
const sourceDropdownOpen = ref(false)
const showNoCategoryOnly = ref(false)
const selectedTagIds = ref([])

// Массовое назначение тегов
const bulkDateFrom = ref('')
const bulkDateTo = ref('')
const bulkSelectedCategories = ref([])
const bulkTagIds = ref([])
const bulkSubmitting = ref(false)

// Сортировка
const sortColumn = ref('date')
const sortDirection = ref('desc')

// Пагинация
const currentPage = ref(1)
const pageSize = ref(500)
const pageSizeOptions = [
    { label: '500', value: 500 },
    { label: '1 000', value: 1000 },
    { label: '5 000', value: 5000 },
    { label: 'Все', value: null },
]

const totalPages = computed(() => {
    if (pageSize.value === null) return 1
    return Math.max(1, Math.ceil(totalCount.value / pageSize.value))
})

const visiblePages = computed(() => {
    const total = totalPages.value
    const current = currentPage.value
    const delta = 2
    const left = Math.max(1, current - delta)
    const right = Math.min(total, current + delta)
    const pages = []
    for (let i = left; i <= right; i++) pages.push(i)
    return pages
})

// Источник: поиск
const filteredSourceOptions = computed(() => {
    if (!sourceSearch.value) return sources.value
    const q = sourceSearch.value.toLowerCase()
    return sources.value.filter(s => s.paymentSourceName.toLowerCase().includes(q))
})

const selectSource = (src) => {
    selectedSourceId.value = src?.paymentSourceId ?? null
    sourceSearch.value = src?.paymentSourceName ?? ''
    sourceDropdownOpen.value = false
    reload()
}

const onSourceBlur = () => {
    setTimeout(() => { sourceDropdownOpen.value = false }, 200)
}

// Сортировка
const toggleSort = (col) => {
    if (sortColumn.value === col) {
        sortDirection.value = sortDirection.value === 'asc' ? 'desc' : 'asc'
    } else {
        sortColumn.value = col
        sortDirection.value = 'asc'
    }
    currentPage.value = 1
    loadPayments()
}

const sortIcon = (col) => {
    if (sortColumn.value !== col) return '↕'
    return sortDirection.value === 'asc' ? '↑' : '↓'
}

// Пагинация
const changePage = (page) => {
    if (hasChanges.value) {
        if (!confirm('Есть несохранённые изменения. Перейти на другую страницу?')) return
    }
    currentPage.value = page
    loadPayments()
}

const setPageSize = (size) => {
    pageSize.value = size
    currentPage.value = 1
    loadPayments()
}

// Плоский список категорий с отступами по уровню — для мультиселекта в модалке массового назначения тегов
const flatCategoryOptions = computed(() => {
    const result = []
    function flatten(cats, level = 0) {
        if (!Array.isArray(cats)) return
        cats.forEach(cat => {
            if (!cat || !cat.categoryId) return
            const prefix = level === 0 ? '' : '-'.repeat(level) + ' '
            result.push({ categoryId: cat.categoryId, displayName: prefix + cat.categoryName })
            if (cat.children?.length) flatten(cat.children, level + 1)
        })
    }
    flatten(categories.value)
    return result
})

const sameTagIds = (a, b) =>
    JSON.stringify([...a].sort((x, y) => x - y)) === JSON.stringify([...b].sort((x, y) => x - y))

// Отслеживание изменений
const hasChangesForPayment = (payment) =>
    payment.categoryId !== payment.originalCategoryId ||
    payment.paymentDescription !== payment.originalDescription ||
    !sameTagIds(payment.tagIds, payment.originalTagIds)

const hasChanges = computed(() => payments.value.some(hasChangesForPayment))
const changedItemsCount = computed(() => payments.value.filter(hasChangesForPayment).length)

// Форматирование
const formatDate = (dateString) => {
    if (!dateString) return '-'
    const date = new Date(dateString)
    const datePart = date.toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric' })
    const timePart = date.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })
    return `${datePart} ${timePart}`
}

const formatCurrency = (value) =>
    new Intl.NumberFormat('ru-RU', { style: 'currency', currency: 'RUB', minimumFractionDigits: 2 }).format(value)

// Загрузка данных
const loadPayments = async () => {
    loading.value = true
    try {
        const params = new URLSearchParams()
        if (dateFrom.value) params.append('from', dateFrom.value)
        if (dateTo.value) params.append('to', dateTo.value)
        if (selectedSourceId.value != null) params.append('sourceId', selectedSourceId.value)
        if (showNoCategoryOnly.value) params.append('noCategoryOnly', 'true')
        params.append('sortBy', sortColumn.value)
        params.append('sortDir', sortDirection.value)
        params.append('page', currentPage.value)
        if (pageSize.value != null) params.append('pageSize', pageSize.value)
        selectedTagIds.value.forEach(id => params.append('tagIds', id))

        const response = await axios.get(APP_CONFIG.apiBaseUrl + '/api/PaymentsApi/list', { params })
        payments.value = response.data.items.map(p => {
            const effectiveCategoryId = p.effectiveCategory?.categoryId ?? null
            const tagIds = (p.tags || []).map(t => t.tagId)
            return {
                ...p,
                categoryId: effectiveCategoryId,
                originalCategoryId: effectiveCategoryId,
                originalDescription: p.paymentDescription,
                tagIds,
                originalTagIds: [...tagIds],
            }
        })
        totalCount.value = response.data.totalCount
    } catch (error) {
        console.error('Ошибка загрузки операций:', error)
        alert('Ошибка загрузки данных')
    } finally {
        loading.value = false
    }
}

const reload = () => {
    currentPage.value = 1
    loadPayments()
}

const loadCategories = async () => {
    try {
        const response = await axios.get(APP_CONFIG.apiBaseUrl + '/api/CategoriesApi/getTree')
        categories.value = response.data || []
    } catch (error) {
        categories.value = []
    }
}

const loadSources = async () => {
    try {
        const response = await axios.get(APP_CONFIG.apiBaseUrl + '/api/PaymentSourcesApi')
        sources.value = response.data || []
    } catch (error) {
        sources.value = []
    }
}

const loadTags = async () => {
    try {
        const response = await axios.get(APP_CONFIG.apiBaseUrl + '/api/TagsApi')
        tags.value = response.data || []
    } catch (error) {
        tags.value = []
    }
}

const onFileSelected = async (event) => {
    const file = event.target.files[0]
    if (!file) return
    uploading.value = true
    try {
        const formData = new FormData()
        formData.append('file', file)
        await axios.post(APP_CONFIG.apiBaseUrl + '/api/PaymentsApi/upload', formData, {
            headers: { 'Content-Type': 'multipart/form-data' }
        })
        await loadPayments()
    } catch (error) {
        alert(error.response?.data || 'Ошибка загрузки файла')
    } finally {
        uploading.value = false
        event.target.value = ''
    }
}

const saveSinglePayment = async (payment) => {
    saving.value = true
    try {
        let categoryIdToSave = payment.categoryId
        if (payment.categoryId === payment.paymentSource?.categoryId) {
            categoryIdToSave = null
        }
        await axios.put(`${APP_CONFIG.apiBaseUrl}/api/PaymentsApi/${payment.paymentId}`, {
            paymentId: payment.paymentId,
            categoryId: categoryIdToSave,
            description: payment.paymentDescription,
            excludeFromStatsStr: payment.excludeFromStats ? 'on' : null,
            tagIds: payment.tagIds,
        })
        payment.originalCategoryId = payment.categoryId
        payment.originalDescription = payment.paymentDescription
        payment.originalTagIds = [...payment.tagIds]
    } catch (error) {
        console.error('Ошибка сохранения:', error)
        alert('Ошибка сохранения')
    } finally {
        saving.value = false
    }
}

const saveAllChanges = async () => {
    saving.value = true
    try {
        const changed = payments.value.filter(hasChangesForPayment)
        await Promise.all(changed.map(payment => {
            let categoryIdToSave = payment.categoryId
            if (payment.categoryId === payment.paymentSource?.categoryId) {
                categoryIdToSave = null
            }
            return axios.put(`${APP_CONFIG.apiBaseUrl}/api/PaymentsApi/${payment.paymentId}`, {
                paymentId: payment.paymentId,
                categoryId: categoryIdToSave,
                description: payment.paymentDescription,
                excludeFromStatsStr: payment.excludeFromStats ? 'on' : null,
                tagIds: payment.tagIds,
            })
        }))
        changed.forEach(payment => {
            payment.originalCategoryId = payment.categoryId
            payment.originalDescription = payment.paymentDescription
            payment.originalTagIds = [...payment.tagIds]
        })
        alert(`Успешно обновлено ${changed.length} операций!`)
    } catch (error) {
        console.error('Ошибка сохранения:', error)
        alert('Ошибка сохранения некоторых операций')
    } finally {
        saving.value = false
    }
}

const resetAllChanges = () => {
    payments.value.forEach(payment => {
        payment.categoryId = payment.originalCategoryId
        payment.paymentDescription = payment.originalDescription
        payment.tagIds = [...payment.originalTagIds]
    })
}

const openBulkModal = () => {
    bulkDateFrom.value = dateFrom.value || ''
    bulkDateTo.value = dateTo.value || ''
    bulkSelectedCategories.value = []
    bulkTagIds.value = []

    const modal = new bootstrap.Modal(document.getElementById('bulkTagModal'))
    modal.show()
}

const submitBulkAssign = async () => {
    if (!bulkDateFrom.value || !bulkDateTo.value) {
        alert('Укажите период')
        return
    }
    if (bulkSelectedCategories.value.length === 0) {
        alert('Выберите хотя бы одну категорию')
        return
    }
    if (bulkTagIds.value.length === 0) {
        alert('Выберите хотя бы один тег')
        return
    }

    bulkSubmitting.value = true
    try {
        const response = await axios.post(`${APP_CONFIG.apiBaseUrl}/api/PaymentsApi/bulk-tags`, {
            from: bulkDateFrom.value,
            to: bulkDateTo.value,
            categoryIds: bulkSelectedCategories.value.map(c => c.categoryId),
            tagIds: bulkTagIds.value,
        })

        const modal = bootstrap.Modal.getInstance(document.getElementById('bulkTagModal'))
        modal.hide()

        alert(`Тегами помечено операций: ${response.data.affectedCount}`)
        await loadPayments()
    } catch (error) {
        console.error('Ошибка массового назначения тегов:', error)
        alert('Ошибка массового назначения тегов')
    } finally {
        bulkSubmitting.value = false
    }
}

onMounted(() => {
    loadCategories()
    loadSources()
    loadTags()
    loadPayments()
})
</script>

<style scoped>
.payments-container {
    max-width: 100%;
}

.table td {
    vertical-align: middle;
}

textarea.form-control {
    resize: vertical;
    min-height: 38px;
}

.date-input {
    width: 130px;
}

.source-input {
    width: 280px;
}

.source-select-wrapper {
    min-width: 280px;
}

.tag-filter-wrapper {
    min-width: 220px;
}

.source-dropdown {
    z-index: 1050;
    max-height: 220px;
    overflow-y: auto;
}

.sortable-th {
    cursor: pointer;
    user-select: none;
    white-space: nowrap;
}

.sortable-th:hover {
    background-color: #e9ecef;
}

.sort-icon {
    font-size: 0.75rem;
    color: #6c757d;
}

.card {
    transition: box-shadow 0.15s;
}
</style>
