<template>
    <AlertMessage :message="error" @close="clearAlerts" />

    <div class="card border-0 shadow-sm mb-4">
        <div class="card-body">
            <form @submit.prevent="submitForm" class="d-flex flex-wrap gap-3 align-items-center">
                <div class="d-flex align-items-center gap-1">
                    <input type="date" class="form-control form-control-sm date-input" v-model="formData.startDate" @input="quickPeriod = null">
                    <span class="text-muted small">—</span>
                    <input type="date" class="form-control form-control-sm date-input" v-model="formData.endDate" @input="quickPeriod = null">
                </div>

                <select class="form-select form-select-sm w-auto" v-model="formData.groupingType">
                    <option value="0">Выберите группировку</option>
                    <option v-for="option in groupingTypeOptions" :key="option.value" :value="option.value">
                        {{ option.text }}
                    </option>
                </select>

                <div class="tag-filter-wrapper">
                    <TagMultiSelect v-model="selectedTagIds" :tags="tags" @created="loadTags" @update:model-value="submitForm" />
                </div>

                <button type="submit" class="btn btn-primary btn-sm ms-auto" :disabled="loading">
                    {{ loading ? 'Загрузка...' : 'Отправить' }}
                </button>
            </form>

            <div class="btn-group btn-group-sm mt-3" role="group">
                <button
                    v-for="qp in quickPeriods"
                    :key="qp.key"
                    type="button"
                    class="btn"
                    :class="quickPeriod === qp.key ? 'btn-primary' : 'btn-outline-secondary'"
                    :aria-pressed="quickPeriod === qp.key"
                    @click="setQuickPeriod(qp.key)"
                >{{ qp.label }}</button>
            </div>
        </div>
    </div>

    <!-- Таблица платежей — тепловая карта -->
    <div class="card border-0 shadow-sm mb-4">
        <div class="card-header d-flex justify-content-between align-items-center">
            <h5 ref="tableHeading" class="mb-0" tabindex="-1">Расходы по категориям</h5>
            <div class="hm-legend">
                <span class="text-muted small">меньше</span>
                <div class="hm-legend-bar"></div>
                <span class="text-muted small">больше</span>
            </div>
        </div>
        <div class="card-body p-0">
            <div v-if="isLoadingTable" class="text-center py-5">
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">Загрузка...</span>
                </div>
            </div>

            <div v-else-if="!hasData" class="text-center py-4 text-muted">
                Нет данных
            </div>

            <div v-else class="hm-wrap">
                <table id="payments-table" class="hm-table">
                    <thead>
                        <tr>
                            <th class="hm-col-cat" scope="col">Категория</th>
                            <th class="hm-col-period" scope="col" v-for="period in periodHeaders" :key="period.key">
                                {{ period.label }}
                            </th>
                            <th v-if="deltaHeader" class="hm-col-delta" scope="col" :title="deltaHeader.title">
                                {{ deltaHeader.text }}
                            </th>
                        </tr>
                    </thead>
                    <tbody>
                        <template v-for="(category, index) in treeRows" :key="category.categoryId">
                            <PaymentCategoryRow
                                :category="category"
                                :periods="periodHeaders"
                                :level="0"
                                :max-value="maxValue"
                                :cat-index="index"
                                :selected-category-id="selectedCategoryId"
                                :selected-period-key="selectedPeriodKey"
                                :show-delta="!!deltaHeader"
                                :comparison-labels="comparisonLabels"
                                :selected-delta-id="comparisonScopeKey"
                                @cell-click="handleCellClick"
                                @delta-click="toggleComparison"
                            />
                        </template>
                    </tbody>
                    <tfoot v-if="totalNode">
                        <tr class="hm-total-row">
                            <td class="hm-cat-cell hm-total-label">Итого</td>
                            <td class="hm-total-amt" v-for="period in periodHeaders" :key="period.key">
                                {{ formatSignedCurrency(totalNode.periods?.[period.key]?.totalSum || 0) }}
                            </td>
                            <DeltaCell
                                v-if="deltaHeader"
                                :node="totalNode"
                                :semantics="Semantics.Expense"
                                total
                                name="Все траты"
                                :labels="comparisonLabels"
                                :selected="comparisonScopeKey === TOTAL_SCOPE"
                                @select="toggleComparison(TOTAL_SCOPE)"
                            />
                        </tr>
                    </tfoot>
                </table>
                <div class="text-muted small px-3 py-2 hm-footnote">
                    <template v-if="deltaCaption">
                        <p class="mb-1">{{ deltaCaption }}</p>
                        <p class="mb-1">
                            ▲ красным — тратите больше, ▼ зелёным — меньше; для доходных строк и «Накоплений» —
                            наоборот: ▲ зелёным — поступило больше; у «Переводов» — всегда серым.
                            «новая» — в прошлом периоде сумм по категории не было.
                        </p>
                    </template>
                    <p class="mb-0">«Без категории» и «Переводы» (переводы между своими счетами) не входят в «Итого».</p>
                </div>
            </div>
        </div>
    </div>

    <!-- Карточка сравнения по клику на Δ; под таблицей открыт либо он, либо блок платежей (СР §3.2) -->
    <ComparisonCard
        v-if="comparisonCard"
        ref="comparisonCardRef"
        :id="COMPARISON_CARD_ID"
        :comparison="comparison"
        :grouping-type="comparisonRequest.groupingType"
        :with-tags="comparisonRequest.withTags"
        :node="comparisonCard.node"
        :parent="comparisonCard.parent"
        :semantics="comparisonCard.semantics"
        :is-total="comparisonCard.isTotal"
        @close="closeComparison"
    />

    <!-- PaymentsTable.vue - обновленная секция с выбранными платежами -->
    <div v-if="selectedPayments.length > 0" class="mt-4">
        <div class="row">
            <!-- Список платежей (занимает 6 колонок) -->
            <div class="col-md-6">
                <div class="card h-100">
                    <div class="card-header d-flex justify-content-between align-items-center">
                        <h5 class="mb-0">
                            Платежи: {{ selectedCellData.categoryName }} - {{ selectedCellData.periodName }}
                            <span class="badge bg-primary ms-2">
                                {{ selectedPayments.length }} платеж(ей)
                            </span>
                        </h5>
                        <button class="btn btn-sm btn-outline-secondary" @click="clearSelection">
                            Скрыть
                        </button>
                    </div>
                    <div class="card-body">
                        <div class="mb-3">
                            <strong>Общая сумма: {{ formatCurrency(selectedCellData.totalSum) }}</strong>
                        </div>
                        
                        <!-- Компактный список платежей -->
                        <PaymentList 
                            :payments="selectedPayments"
                            :categories="availableCategories"
                            @edit-payment="openEditModal"
                        />
                    </div>
                </div>
            </div>
            
            <!-- Список источников платежей (занимает 6 колонок) -->
            <div class="col-md-6">
                <div class="card h-100">
                    <div class="card-header">
                        <h5 class="mb-0">Распределение по источникам</h5>
                    </div>
                    <div class="card-body">
                        <PaymentSourcesList 
                            :payments="selectedPayments"
                        />
                    </div>
                </div>
            </div>
        </div>
    </div>

    <!-- Модальное окно редактирования платежа -->
    <div class="modal fade" id="editPaymentModal" tabindex="-1" aria-hidden="true">
        <div class="modal-dialog modal-lg">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">Редактировать платеж</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body">
                    <AlertMessage :message="modalError" @close="modalError = null" />
                    <form v-if="editingPayment">
                        <div class="mb-3">
                            <label class="form-label">Сумма</label>
                            <input type="text" class="form-control" :value="formatCurrency(editingPayment.paymentSum)" disabled>
                        </div>
                        
                        <div class="mb-3">
                            <label class="form-label">Дата</label>
                            <input type="text" class="form-control" :value="toInputDate(editingPayment.paymentDate)" disabled>
                        </div>
                        
                        <div class="mb-3">
                            <label class="form-label">Источник</label>
                            <input type="text" class="form-control" :value="editingPayment.paymentSource?.paymentSourceName || 'Не указан'" disabled>
                        </div>
                        
                        <div class="mb-3">
                            <label class="form-label">Счет</label>
                            <select class="form-select" v-model="editingPayment.accountId">
                                <option value="">Не указан</option>
                                <option v-for="account in accounts" :key="account.accountId" :value="account.accountId">
                                    {{ account.displayName }}
                                </option>
                            </select>
                        </div>

                        <div class="mb-3">
                            <div class="form-check">
                                <input
                                    id="edit-payment-is-transfer"
                                    type="checkbox"
                                    class="form-check-input"
                                    :checked="editingPayment.isTransfer"
                                    @change="onTransferToggle($event.target.checked)"
                                >
                                <label for="edit-payment-is-transfer" class="form-check-label">Перевод между своими счетами</label>
                            </div>
                        </div>

                        <div v-if="editingPayment.isTransfer" class="mb-3">
                            <label for="edit-payment-transfer-pair" class="form-label">Вторая сторона</label>
                            <select
                                id="edit-payment-transfer-pair"
                                class="form-select"
                                :value="editingPayment.transferPairPaymentId"
                                :disabled="transferCandidatesLoading"
                                @change="onTransferPairChange($event.target.value)"
                            >
                                <option value="">Не указывать</option>
                                <option v-for="option in transferPairOptions" :key="option.paymentId" :value="option.paymentId">
                                    {{ option.label }}
                                </option>
                            </select>
                            <div v-if="transferCandidatesLoading" class="form-text">Ищем подходящие операции…</div>
                            <div v-else-if="transferCandidates.length === 0" class="form-text">
                                Подходящих операций не найдено: нужна противоположная по знаку операция на ту же сумму в пределах 5 дней
                            </div>
                        </div>

                        <div class="mb-3">
                            <label class="form-label">Категория</label>
                            <CategoryTreeSelect
                                v-model="editingPayment.categoryId"
                                :categories="categoryTree"
                                :auto-load="false"
                            />
                        </div>
                        
                        <div class="mb-3">
                            <label class="form-label">Теги</label>
                            <TagMultiSelect v-model="editingPayment.tagIds" :tags="tags" @created="loadTags" />
                        </div>

                        <div class="mb-3">
                            <label class="form-label">Описание</label>
                            <textarea
                                v-model="editingPayment.paymentDescription"
                                class="form-control"
                                rows="3"
                                placeholder="Введите описание платежа..."
                            ></textarea>
                        </div>
                    </form>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Отмена</button>
                    <button type="button" class="btn btn-primary" @click="savePayment">Сохранить</button>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, reactive, onMounted, computed, nextTick } from 'vue'
import axios from 'axios'
import { getErrorMessage } from '../../utils/apiError'
import { APP_CONFIG } from '../../config'
import PaymentCategoryRow from './PaymentCategoryRow.vue'
import PaymentList from './PaymentList.vue'
import CategoryTreeSelect from '../../components/CategoryTreeSelect.vue'
import TagMultiSelect from '../../components/TagMultiSelect.vue'
import { Modal } from 'bootstrap'
import PaymentSourcesList from './PaymentSourcesList.vue' // Добавляем импорт нового компонента
import { computeMaxValue, splitTotalNode, hasAnySum } from '../../utils/heatmap'
import { formatCurrency, formatSignedCurrency, formatShortDate, toInputDate } from '../../utils/format'
import { resolveTransferCategory } from '../../utils/transferCategory'
import {
    formatPeriodHeader,
    formatComparisonHeader,
    formatComparisonCaption,
    formatComparisonPhrase,
    formatComparisonCellTitle,
    GroupingType
} from '../../utils/periodHeader'
import { Semantics, findComparisonScope } from '../../utils/periodComparison'
import DeltaCell from './DeltaCell.vue'
import ComparisonCard from './ComparisonCard.vue'
import AlertMessage from '../../components/AlertMessage.vue'
import { useAlerts } from '../../composables/useAlerts'
import { usePaymentsImport } from '../../composables/usePaymentsImport'

// Добавляем флаг для отслеживания загрузки категорий
const categoriesLoaded = ref(false)

const loadCategories = async () => {
    try {
        var categories = await axios.get(APP_CONFIG.apiBaseUrl + '/api/CategoriesApi/getTree')
        categoryTree.value = categories.data || []
        categoryOptions.value = flattenCategories(categories.data || [])
        categoriesLoaded.value = true // Устанавливаем флаг после загрузки
        
        // Автоматически загружаем платежи только если нет текущей загрузки
        if (!loading.value) {
            await submitForm()
        }
    }
    catch (err) {
        console.error('Ошибка загрузки категорий:', err)
        categoryTree.value = []
        categoryOptions.value = []
        categoriesLoaded.value = true
        showError('Ошибка загрузки категорий')
    }
}

// Функция для преобразования дерева в плоский список
const flattenCategories = (categories) => {
    const result = []
    function flatten(cats) {
        if (!Array.isArray(cats)) return
        cats.forEach(cat => {
            if (cat && cat.categoryId) {
                result.push({
                    categoryId: cat.categoryId,
                    categoryName: cat.categoryName
                })
            }
            if (cat && cat.children && Array.isArray(cat.children) && cat.children.length > 0) {
                flatten(cat.children)
            }
        })
    }
    flatten(categories)
    return result
}

// Функция для получения даты начала (первое число 3 месяца назад)
const getDefaultStartDate = () => {
    const today = new Date()
    return toInputDate(new Date(today.getFullYear(), today.getMonth() - 2, 1))
}

// Функция для получения сегодняшней даты
const getDefaultEndDate = () => {
    return toInputDate(new Date())
}

const formData = reactive({
    startDate: getDefaultStartDate(),
    endDate: getDefaultEndDate(),
    groupingType: 1
})

const quickPeriod = ref(null)
const quickPeriods = [
    { key: '3d',      label: '3 дня' },
    { key: 'week',    label: 'Эта неделя' },
    { key: 'month',   label: 'Месяц' },
    { key: 'quarter', label: 'Квартал' },
    { key: 'year',    label: 'Год' },
]

const setQuickPeriod = (key) => {
    const today = new Date()
    let from

    if (key === '3d') {
        const d = new Date(today); d.setDate(d.getDate() - 2)
        from = toInputDate(d)
    } else if (key === 'week') {
        const daysSinceMonday = (today.getDay() + 6) % 7
        const d = new Date(today); d.setDate(d.getDate() - daysSinceMonday)
        from = toInputDate(d)
    } else if (key === 'month') {
        from = toInputDate(new Date(today.getFullYear(), today.getMonth(), 1))
    } else if (key === 'quarter') {
        const quarterStartMonth = Math.floor(today.getMonth() / 3) * 3
        from = toInputDate(new Date(today.getFullYear(), quarterStartMonth, 1))
    } else if (key === 'year') {
        from = toInputDate(new Date(today.getFullYear(), 0, 1))
    }

    formData.startDate = from
    formData.endDate = toInputDate(today)
    quickPeriod.value = key
    submitForm()
}

const groupingTypeOptions = ref([
    {text: "Месяц", value: 1},
    {text: "Неделя", value: 2},
    {text: "Квартал", value: 3},
    {text: "Год", value: 4},
    {text: "День", value: 5},
])
const categoryOptions = ref([])
const categoryTree = ref([])
const accounts = ref([])
const tags = ref([])
const selectedTagIds = ref([])
const loading = ref(false)
const paymentsTree = ref([])
const periodHeaders = ref([])
const treeRows = computed(() => splitTotalNode(paymentsTree.value).rows)
const totalNode = computed(() => splitTotalNode(paymentsTree.value).totalNode)
const hasData = computed(() => hasAnySum(treeRows.value))
const maxValue = ref(0)
// Сравнение с прошлым периодом (СР §2): блок comparison ответа, группировка и теги на момент запроса
const comparison = ref(null)
const comparisonRequest = ref({ groupingType: GroupingType.NotSelected, withTags: false })
const deltaHeader = computed(() =>
    comparison.value && hasData.value
        ? formatComparisonHeader(comparison.value, comparisonRequest.value.groupingType)
        : null)
const deltaCaption = computed(() => {
    if (!deltaHeader.value) return null
    const caption = formatComparisonCaption(comparison.value, comparisonRequest.value.groupingType)
    return comparisonRequest.value.withTags ? `${caption}, с учётом выбранных тегов` : caption
})
// Карточка сравнения (СР §3–§4): охват — categoryId строки или TOTAL_SCOPE для «Итого»
const COMPARISON_CARD_ID = 'comparison-card'
const TOTAL_SCOPE = 'total'
const comparisonScopeKey = ref(null)
const comparisonCardRef = ref(null)
const tableHeading = ref(null)
const comparisonLabels = computed(() => deltaHeader.value
    ? {
        phrase: formatComparisonPhrase(comparison.value, comparisonRequest.value.groupingType),
        title: formatComparisonCellTitle(comparison.value, comparisonRequest.value.groupingType),
        cardId: COMPARISON_CARD_ID
    }
    : null)
const comparisonCard = computed(() => {
    const key = comparisonScopeKey.value
    if (key === null || !deltaHeader.value) return null
    if (key === TOTAL_SCOPE) {
        return totalNode.value ? { node: totalNode.value, parent: null, semantics: Semantics.Expense, isTotal: true } : null
    }
    const scope = findComparisonScope(treeRows.value, key)
    return scope ? { ...scope, isTotal: false } : null
})
const selectedPayments = ref([])
const selectedCellData = reactive({
    categoryName: '',
    periodName: '',
    totalSum: 0
})
const selectedCategoryId = ref(null)
const selectedPeriodKey = ref(null)
const editingPayment = ref(null)
const { error, showError, clearAlerts } = useAlerts()
// Ошибка сохранения платежа показывается внутри модалки
const modalError = ref(null)

// Перевод между своими счетами в модалке правки (PaymentTypeEnum.Transfer = 2 на backend)
const TRANSFER_PAYMENT_TYPE = 2
const transferCandidates = ref([])
const transferCandidatesLoading = ref(false)
// Автоподстановка «Накоплений»: null или { previousCategoryId } — см. utils/transferCategory.js
const transferCategoryAuto = ref(null)

const formatTransferCandidate = (payment) => [
    formatShortDate(payment.paymentDate),
    formatSignedCurrency(payment.paymentSum),
    payment.account?.displayName || 'без счёта',
    payment.paymentSource?.paymentSourceName || 'источник не указан'
].join(' · ')

// Кандидаты + текущая вторая сторона, если она не попала в кандидаты (например, дальше 5 дней)
const transferPairOptions = computed(() => {
    const options = transferCandidates.value.map(c => ({ paymentId: c.paymentId, label: formatTransferCandidate(c) }))
    const pairId = editingPayment.value?.originalTransferPairPaymentId
    if (pairId && !options.some(o => o.paymentId === pairId)) {
        const accountName = editingPayment.value.transferPairAccount?.displayName || 'счёт не указан'
        options.unshift({ paymentId: pairId, label: `Связанная операция · ${accountName}` })
    }
    return options
})

const loadTransferCandidates = async (paymentId) => {
    transferCandidatesLoading.value = true
    try {
        const response = await axios.get(`${APP_CONFIG.apiBaseUrl}/api/PaymentsApi/${paymentId}/transfer-candidates`)
        if (editingPayment.value?.paymentId === paymentId) transferCandidates.value = response.data || []
    } catch (err) {
        console.error('Ошибка загрузки кандидатов перевода:', err)
        modalError.value = getErrorMessage(err, 'Не удалось подобрать вторую сторону перевода')
    } finally {
        transferCandidatesLoading.value = false
    }
}

const applyTransferCategory = () => {
    const payment = editingPayment.value
    const pair = transferCandidates.value.find(c => c.paymentId === payment.transferPairPaymentId)
    const result = resolveTransferCategory({
        isTransfer: payment.isTransfer,
        pairIsSavings: !!(pair
            ? pair.account?.isSavings
            : payment.transferPairPaymentId && payment.transferPairPaymentId === payment.originalTransferPairPaymentId
                && payment.transferPairAccount?.isSavings),
        categoryId: payment.categoryId,
        auto: transferCategoryAuto.value
    })
    payment.categoryId = result.categoryId
    transferCategoryAuto.value = result.auto
}

const onTransferToggle = async (checked) => {
    editingPayment.value.isTransfer = checked
    applyTransferCategory()
    if (checked && transferCandidates.value.length === 0) {
        await loadTransferCandidates(editingPayment.value.paymentId)
    }
}

const onTransferPairChange = (value) => {
    editingPayment.value.transferPairPaymentId = value === '' ? '' : Number(value)
    applyTransferCategory()
}

// Спиннер вместо таблицы: пока грузятся категории (до первого запроса) и во время запроса платежей
const isLoadingTable = computed(() => loading.value || !categoriesLoaded.value)

// Вычисляемое свойство для группировки платежей по дате
const groupedPayments = computed(() => {
    const grouped = {}
    selectedPayments.value.forEach(payment => {
        const date = payment.paymentDate.split('T')[0]
        if (!grouped[date]) {
            grouped[date] = []
        }
        grouped[date].push(payment)
    })
    return grouped
})

// Получаем все доступные категории для выпадающего списка
const availableCategories = computed(() => {
    return flattenCategories(categoryTree.value)
})

const fetchData = async (groupingType) => {
    const params = new URLSearchParams()
    params.append('from', formData.startDate)
    params.append('to', formData.endDate)
    if (groupingType !== GroupingType.NotSelected) {
        params.append('periodType', groupingType)
    }
    selectedTagIds.value.forEach(id => params.append('tagIds', id))

    return await axios.get(APP_CONFIG.apiBaseUrl + '/api/PaymentsApi', { params })
}

const handleCellClick = (payments, categoryName, periodName, totalSum, categoryId, periodKey) => {
    comparisonScopeKey.value = null
    selectedPayments.value = payments
    selectedCellData.categoryName = categoryName
    selectedCellData.periodName = periodName
    selectedCellData.totalSum = totalSum
    selectedCategoryId.value = categoryId ?? null
    selectedPeriodKey.value = periodKey ?? null

    setTimeout(() => {
        const element = document.querySelector('.card-header')
        if (element) {
            element.scrollIntoView({ behavior: 'smooth', block: 'start' })
        }
    }, 100)
}

const clearSelection = () => {
    selectedPayments.value = []
    selectedCellData.categoryName = ''
    selectedCellData.periodName = ''
    selectedCellData.totalSum = 0
    selectedCategoryId.value = null
    selectedPeriodKey.value = null
}


// Клик по Δ: закрывает блок платежей и открывает карточку; повторный клик по той же Δ — закрывает (СР §3.2)
const toggleComparison = async (key) => {
    if (comparisonScopeKey.value === key) {
        comparisonScopeKey.value = null
        return
    }
    clearSelection()
    comparisonScopeKey.value = key
    await nextTick()
    comparisonCardRef.value?.reveal()
}

// «Скрыть»/Esc: фокус возвращается на Δ-ячейку, если её строка видна, иначе — на заголовок таблицы (СР §3.3)
const closeComparison = async () => {
    const key = comparisonScopeKey.value
    comparisonScopeKey.value = null
    await nextTick()
    const cell = document.querySelector(`#payments-table [data-delta-key="${key}"]`)
    if (cell) cell.focus()
    else tableHeading.value?.focus()
}

const findCategoryById = (tree, categoryId) => {
    for (const cat of tree) {
        if (cat.categoryId === categoryId) return cat
        if (cat.children?.length) {
            const found = findCategoryById(cat.children, categoryId)
            if (found) return found
        }
    }
    return null
}

const loadAccounts = async () => {
    try {
        const response = await axios.get(APP_CONFIG.apiBaseUrl + '/api/AccountsApi')
        accounts.value = response.data || []
    } catch (err) {
        console.error('Ошибка загрузки счетов:', err)
        accounts.value = []
    }
}

const loadTags = async () => {
    try {
        const response = await axios.get(APP_CONFIG.apiBaseUrl + '/api/TagsApi')
        tags.value = response.data || []
    } catch (err) {
        console.error('Ошибка загрузки тегов:', err)
        tags.value = []
    }
}

const openEditModal = (payment) => {
    editingPayment.value = {
        ...payment,
        categoryId: payment.effectiveCategory?.categoryId || '',
        accountId: payment.account?.accountId || '',
        tagIds: (payment.tags || []).map(t => t.tagId),
        isTransfer: payment.paymentType === TRANSFER_PAYMENT_TYPE,
        originalIsTransfer: payment.paymentType === TRANSFER_PAYMENT_TYPE,
        transferPairPaymentId: payment.transferPairPaymentId || '',
        originalTransferPairPaymentId: payment.transferPairPaymentId || ''
    }
    modalError.value = null
    transferCandidates.value = []
    transferCategoryAuto.value = null

    Modal.getOrCreateInstance(document.getElementById('editPaymentModal')).show()

    if (editingPayment.value.isTransfer) {
        loadTransferCandidates(payment.paymentId)
    }
}

// Поля перевода — только изменённые (null = не трогать): IsTransfer false снимает отметку и связь,
// TransferPairPaymentId 0 — отвязать вторую сторону
const buildTransferChanges = (payment) => {
    const changes = {}
    if (payment.isTransfer !== payment.originalIsTransfer) changes.IsTransfer = payment.isTransfer
    if (payment.isTransfer && payment.transferPairPaymentId !== payment.originalTransferPairPaymentId) {
        changes.TransferPairPaymentId = payment.transferPairPaymentId === '' ? 0 : payment.transferPairPaymentId
    }
    return changes
}

const savePayment = async () => {
    if (!editingPayment.value) return
    
    try {
        const reselectCategoryId = selectedCategoryId.value
        const reselectPeriodKey = selectedPeriodKey.value

        // PUT отвечает 204 без тела; ошибка (4xx/5xx) уходит в catch
        await axios.put(
            `${APP_CONFIG.apiBaseUrl}/api/PaymentsApi/${editingPayment.value.paymentId}`,
            {
                CategoryId: editingPayment.value.categoryId || null,
                AccountId: editingPayment.value.accountId === '' ? 0 : editingPayment.value.accountId,
                Description: editingPayment.value.paymentDescription,
                TagIds: editingPayment.value.tagIds,
                ...buildTransferChanges(editingPayment.value)
            }
        )

        Modal.getOrCreateInstance(document.getElementById('editPaymentModal')).hide()

        // Перезагружаем данные
        await submitForm()

        // Повторно выбираем ту же ячейку по id категории и ключу периода
        if (reselectCategoryId && reselectPeriodKey) {
            const category = findCategoryById(paymentsTree.value, reselectCategoryId)
            const periodData = category?.periods?.[reselectPeriodKey]
            const period = periodHeaders.value.find(p => p.key === reselectPeriodKey)
            if (category && periodData && period) {
                handleCellClick(
                    periodData.payments || [],
                    category.categoryName,
                    period.name,
                    periodData.totalSum || 0,
                    category.categoryId,
                    reselectPeriodKey
                )
            }
        }
    } catch (error) {
        console.error('Ошибка при обновлении платежа:', error)
        modalError.value = getErrorMessage(error, 'Произошла ошибка при обновлении платежа')
    }
}

const submitForm = async () => {
    // Проверяем, что категории загружены
    if (!categoriesLoaded.value) {
        return
    }
    
    loading.value = true
    clearAlerts()

    // Тип группировки фиксируем на момент запроса: select могут поменять, не нажав «Показать»
    const groupingType = Number(formData.groupingType)
    const withTags = selectedTagIds.value.length > 0

    try {
        const response = await fetchData(groupingType)

        paymentsTree.value = response.data.paymentsTree || []
        comparison.value = response.data.comparison || null
        comparisonRequest.value = { groupingType, withTags }
        maxValue.value = computeMaxValue(treeRows.value)

        // Формируем заголовки периодов с правильной сортировкой
        const periods = response.data.periods || {}
        
        periodHeaders.value = Object.entries(periods)
            .map(([key, value]) => ({
                key,
                from: new Date(value.from),
                to: new Date(value.to),
                name: value.from.split('T')[0] + ' - ' + value.to.split('T')[0],
                label: formatPeriodHeader(value, groupingType)
            }))
            .sort((a, b) => a.from - b.from)
            .map(item => ({
                key: item.key,
                name: item.name,
                label: item.label
            }))
        
        // Очищаем выбранные платежи и закрываем карточку сравнения при обновлении данных (СР §3.5)
        clearSelection()
        comparisonScopeKey.value = null
    } catch (err) {
        console.error('Ошибка загрузки платежей:', err)
        paymentsTree.value = []
        comparison.value = null
        comparisonScopeKey.value = null
        periodHeaders.value = []
        maxValue.value = 0
        showError('Ошибка загрузки платежей')
    } finally {
        loading.value = false
    }
}

// Файл загружен модалкой из меню (App.vue): уже показанную таблицу пересчитываем с новыми платежами
usePaymentsImport().onImported(() => {
    if (periodHeaders.value.length > 0) submitForm()
})

onMounted(() => {
    loadCategories()
    loadAccounts()
    loadTags()
})
</script>

<style scoped>
.tag-filter-wrapper {
    min-width: 220px;
}

.table td {
    vertical-align: middle;
}

.card {
    box-shadow: var(--bs-box-shadow-sm);
}

.card-header {
    background-color: var(--fc-card-header-bg);
    border-bottom: 1px solid var(--fc-border);
}

/* Тепловая карта — см. .ai-docs/project/payments-table-heatmap.md */
.hm-wrap {
    overflow-x: auto;
}

.hm-table {
    width: auto;
    border-collapse: collapse;
    font-variant-numeric: tabular-nums;
    font-size: 12.5px;
}
.hm-table td {
    border-top: 1px solid var(--fc-border);
    padding: 7px 12px;
    vertical-align: middle;
}

.hm-table thead th {
    background-color: var(--fc-card-header-bg);
    font-size: 11px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.06em;
    color: var(--fc-text-secondary-on-gray);
    padding: 9px 14px;
    border-bottom: 1px solid var(--fc-border);
    white-space: nowrap;
}
.hm-col-cat    { text-align: left; min-width: 180px; }
.hm-col-period { text-align: right; width: 132px; }
.hm-col-delta  { text-align: right; width: 176px; border-left: 1px solid var(--fc-border); }

.hm-cat-cell { padding: 7px 14px; }

.hm-total-row td {
    background-color: var(--fc-card-header-bg);
    border-top: 2px solid var(--fc-accent) !important;
}
.hm-total-label {
    font-weight: 700;
    font-size: 13px;
    color: var(--fc-accent-text);
}
.hm-total-amt {
    text-align: right;
    font-weight: 700;
    color: var(--fc-accent-text);
    white-space: nowrap;
    padding: 9px 12px;
}

.hm-legend {
    display: flex;
    align-items: center;
    gap: 7px;
    font-size: 11px;
    color: var(--fc-text-secondary);
}
.hm-legend-bar {
    width: 72px;
    height: 7px;
    border-radius: 4px;
    background: linear-gradient(to right, rgba(200, 76, 78, 0.04), rgba(200, 76, 78, 0.36));
}
</style>