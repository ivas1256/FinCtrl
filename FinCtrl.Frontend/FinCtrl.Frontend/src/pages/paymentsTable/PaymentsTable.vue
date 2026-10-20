<template>
    <div class="card border-0 shadow-sm mb-4">
        <div class="card-body">
            <form @submit.prevent="submitForm" class="d-flex flex-wrap gap-3 align-items-center">
                <div class="d-flex align-items-center gap-1">
                    <input type="date" class="form-control form-control-sm date-input" v-model="formData.startDate">
                    <span class="text-muted small">—</span>
                    <input type="date" class="form-control form-control-sm date-input" v-model="formData.endDate">
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

                <button type="submit" class="btn btn-primary btn-sm" :disabled="loading">
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
                    @click="setQuickPeriod(qp.key)"
                >{{ qp.label }}</button>
            </div>
        </div>
    </div>

    <!-- Таблица платежей — тепловая карта -->
    <div class="card border-0 shadow-sm mt-4">
        <div class="card-header d-flex justify-content-between align-items-center">
            <h5 class="mb-0">Расходы по категориям</h5>
            <div class="hm-legend">
                <span class="text-muted small">меньше</span>
                <div class="hm-legend-bar"></div>
                <span class="text-muted small">больше</span>
            </div>
        </div>
        <div class="card-body p-0">
            <div class="hm-wrap">
                <table id="payments-table" class="hm-table">
                    <thead>
                        <tr>
                            <th class="hm-col-cat">Категория</th>
                            <th class="hm-col-period" v-for="period in periodHeaders" :key="period.key">
                                {{ period.label }}
                            </th>
                        </tr>
                    </thead>
                    <tbody>
                        <template v-for="(category, index) in paymentsTree" :key="category.categoryId">
                            <PaymentCategoryRow
                                :category="category"
                                :periods="periodHeaders"
                                :level="0"
                                :max-value="maxValue"
                                :cat-index="index"
                                :selected-category-id="selectedCategoryId"
                                :selected-period-key="selectedPeriodKey"
                                @cell-click="handleCellClick"
                            />
                        </template>
                    </tbody>
                    <tfoot v-if="paymentsTree.length > 0">
                        <tr class="hm-total-row">
                            <td class="hm-cat-cell hm-total-label">Итого</td>
                            <td class="hm-total-amt" v-for="period in periodHeaders" :key="period.key">
                                {{ formatHeatCurrency(periodTotal(period.key)) }}
                            </td>
                        </tr>
                    </tfoot>
                </table>
            </div>
        </div>
    </div>

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
        <div class="modal-dialog">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">Редактировать платеж</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body">
                    <form v-if="editingPayment">
                        <div class="mb-3">
                            <label class="form-label">Сумма</label>
                            <input type="text" class="form-control" :value="formatCurrency(editingPayment.paymentSum)" disabled>
                        </div>
                        
                        <div class="mb-3">
                            <label class="form-label">Дата</label>
                            <input type="text" class="form-control" :value="formatDate(editingPayment.paymentDate)" disabled>
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
import { ref, reactive, onMounted, computed, watch } from 'vue' // Добавлен watch
import axios from 'axios'
import { getErrorMessage } from '../../utils/apiError'
import { APP_CONFIG } from '../../../config'
import PaymentCategoryRow from './PaymentCategoryRow.vue'
import PaymentList from './PaymentList.vue'
import CategoryTreeSelect from '../../components/CategoryTreeSelect.vue'
import TagMultiSelect from '../../components/TagMultiSelect.vue'
import PaymentSourcesList from './PaymentSourcesList.vue' // Добавляем импорт нового компонента
import { computeMaxValue, formatHeatCurrency } from '../../utils/heatmap'
import { formatPeriodHeader, GroupingType } from '../../utils/periodHeader'

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

// Локальная дата в формате YYYY-MM-DD (без сдвига через UTC, в отличие от toISOString)
const toInputDate = (date) => {
    const year = date.getFullYear()
    const month = String(date.getMonth() + 1).padStart(2, '0')
    const day = String(date.getDate()).padStart(2, '0')
    return `${year}-${month}-${day}`
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
const maxValue = ref(0)
const selectedPayments = ref([])
const selectedCellData = reactive({
    categoryName: '',
    periodName: '',
    totalSum: 0
})
const selectedCategoryId = ref(null)
const selectedPeriodKey = ref(null)
const editingPayment = ref(null)

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

const formatDate = (dateString) => {
    if (!dateString) return ''
    const date = new Date(dateString)
    return date.toISOString().split('T')[0] // Возвращаем в формате YYYY-MM-DD
}

const formatCurrency = (value) => {
    return new Intl.NumberFormat('ru-RU', {
        style: 'currency',
        currency: 'RUB',
        minimumFractionDigits: 2
    }).format(value)
}

const fetchData = async (groupingType) => {
    const params = new URLSearchParams()
    params.append('from', formData.startDate)
    params.append('to', formData.endDate)
    if (groupingType !== GroupingType.NotSelected) {
        params.append('periodType', groupingType)
    }
    selectedTagIds.value.forEach(id => params.append('tagIds', id))

    console.log('Fetching data with params:', params.toString()) // Для отладки
    return await axios.get(APP_CONFIG.apiBaseUrl + '/api/PaymentsApi', { params })
}

const handleCellClick = (payments, categoryName, periodName, totalSum, categoryId, periodKey) => {
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

// Сумма по всем корневым категориям за период (для строки "Итого")
const periodTotal = (periodKey) => {
    return paymentsTree.value.reduce((sum, cat) => sum + (cat.periods?.[periodKey]?.totalSum || 0), 0)
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
        categoryId: payment.category?.categoryId || '',
        accountId: payment.account?.accountId || '',
        tagIds: (payment.tags || []).map(t => t.tagId)
    }

    const modal = new bootstrap.Modal(document.getElementById('editPaymentModal'))
    modal.show()
}

const savePayment = async () => {
    if (!editingPayment.value) return
    
    try {
        const reselectCategoryId = selectedCategoryId.value
        const reselectPeriodKey = selectedPeriodKey.value

        const response = await axios.put(
            `${APP_CONFIG.apiBaseUrl}/api/PaymentsApi/${editingPayment.value.paymentId}`,
            {
                PaymentId: editingPayment.value.paymentId,
                CategoryId: editingPayment.value.categoryId || null,
                AccountId: editingPayment.value.accountId === '' ? 0 : editingPayment.value.accountId,
                Description: editingPayment.value.paymentDescription,
                TagIds: editingPayment.value.tagIds
            }
        )

        if (response.status === 200) {
            const modal = bootstrap.Modal.getInstance(document.getElementById('editPaymentModal'))
            modal.hide()

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
        }
    } catch (error) {
        console.error('Ошибка при обновлении платежа:', error)
        alert(getErrorMessage(error, 'Произошла ошибка при обновлении платежа'))
    }
}

const submitForm = async () => {
    // Проверяем, что категории загружены
    if (!categoriesLoaded.value) {
        console.log('Категории еще не загружены, откладываем загрузку платежей')
        return
    }
    
    loading.value = true
    console.log('Loading payments data...') // Для отладки
    
    // Тип группировки фиксируем на момент запроса: select могут поменять, не нажав «Показать»
    const groupingType = Number(formData.groupingType)

    try {
        const response = await fetchData(groupingType)
        console.log('Payments data received:', response.data) // Для отладки
        
        paymentsTree.value = response.data.paymentsTree || []
        maxValue.value = computeMaxValue(paymentsTree.value)

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
        
        console.log('Period headers:', periodHeaders.value) // Для отладки
        
        // Очищаем выбранные платежи при обновлении данных
        clearSelection()
    } catch (err) {
        console.error('Ошибка загрузки платежей:', err)
        paymentsTree.value = []
        periodHeaders.value = []
        maxValue.value = 0
        alert('Произошла ошибка при загрузке платежей. Проверьте консоль для подробностей.')
    } finally {
        loading.value = false
        console.log('Loading finished') // Для отладки
    }
}

onMounted(() => {
    loadCategories()
    loadAccounts()
    loadTags()
})

// Добавляем watcher для отладки
watch(formData, (newValue) => {
    console.log('Form data changed:', newValue)
}, { deep: true })
</script>

<style scoped>
.tag-filter-wrapper {
    min-width: 220px;
}

.table td {
    vertical-align: middle;
}

.card {
    box-shadow: 0 0.125rem 0.25rem rgba(0, 0, 0, 0.075);
}

.card-header {
    background-color: #f8f9fa;
    border-bottom: 1px solid #dee2e6;
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
    border-top: 1px solid #dee2e6;
    padding: 7px 12px;
    vertical-align: middle;
}

.hm-table thead th {
    background-color: #f8f9fa;
    font-size: 11px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.06em;
    color: #6c757d;
    padding: 9px 14px;
    border-bottom: 1px solid #dee2e6;
    white-space: nowrap;
}
.hm-col-cat    { text-align: left; min-width: 180px; }
.hm-col-period { text-align: right; width: 132px; }

.hm-cat-cell { padding: 7px 14px; }

.hm-total-row td {
    background-color: #f8f9fa;
    border-top: 2px solid #0d6efd !important;
}
.hm-total-label {
    font-weight: 700;
    font-size: 13px;
    color: #0d6efd;
}
.hm-total-amt {
    text-align: right;
    font-weight: 700;
    color: #0d6efd;
    white-space: nowrap;
    padding: 9px 12px;
}

.hm-legend {
    display: flex;
    align-items: center;
    gap: 7px;
    font-size: 11px;
    color: #6c757d;
}
.hm-legend-bar {
    width: 72px;
    height: 7px;
    border-radius: 4px;
    background: linear-gradient(to right, rgba(200, 76, 78, 0.07), rgba(200, 76, 78, 0.78));
}
</style>