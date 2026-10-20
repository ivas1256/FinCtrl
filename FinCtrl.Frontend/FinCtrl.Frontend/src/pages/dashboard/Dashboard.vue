<template>
    <div class="dashboard">
        <h4 class="mb-4">Дашборд</h4>

        <div v-if="loading" class="text-center py-5">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Загрузка...</span>
            </div>
        </div>

        <template v-else-if="summary">
            <!-- Блок 1: Суммы по периодам -->
            <div class="row g-3 mb-4">
                <div class="col-6 col-md-3" v-for="period in periods" :key="period.key">
                    <div class="card h-100 border-0 shadow-sm stat-card">
                        <div class="card-body text-center py-3">
                            <div class="period-label text-muted small mb-1">{{ period.label }}</div>
                            <div class="period-date text-muted mb-1">{{ periodDateLabel(period) }}</div>
                            <div class="period-amount fw-bold">{{ formatCurrency(summary[period.key]) }}</div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Блок 2: График -->
            <div class="card border-0 shadow-sm mb-4">
                <div class="card-body">
                    <div class="d-flex flex-wrap gap-2 justify-content-between align-items-start mb-3">
                        <h6 class="card-title text-muted mb-0">Динамика расходов</h6>
                        <div class="d-flex flex-wrap gap-2 align-items-center">
                            <div class="btn-group btn-group-sm" role="group">
                                <button
                                    v-for="tab in chartTabs"
                                    :key="tab.key"
                                    class="btn"
                                    :class="chartMode === tab.key ? 'btn-primary' : 'btn-outline-secondary'"
                                    @click="setChartMode(tab.key)"
                                >{{ tab.label }}</button>
                            </div>
                            <div class="d-flex align-items-center gap-1">
                                <input
                                    type="date"
                                    class="form-control form-control-sm date-input"
                                    v-model="chartFrom"
                                    @change="loadChart"
                                >
                                <span class="text-muted small">—</span>
                                <input
                                    type="date"
                                    class="form-control form-control-sm date-input"
                                    v-model="chartTo"
                                    @change="loadChart"
                                >
                            </div>
                        </div>
                    </div>

                    <div v-if="chartLoading" class="text-center py-4">
                        <div class="spinner-border spinner-border-sm text-primary" role="status"></div>
                    </div>
                    <Bar
                        v-else-if="chartData"
                        :data="chartData"
                        :options="chartOptions"
                        style="max-height: 260px;"
                    />
                </div>
            </div>

            <!-- Блоки 3 и 4: Категории и топ покупки -->
            <div class="row g-3">
                <!-- Все категории -->
                <div class="col-md-6">
                    <div class="card border-0 shadow-sm h-100">
                        <div class="card-body">
                            <h6 class="card-title text-muted mb-3">Категории расходов</h6>
                            <div v-if="!summary.allCategories.length" class="text-muted small">Нет данных</div>
                            <div
                                v-for="(cat, i) in visibleCategories"
                                :key="cat.fullCategoryName"
                                class="mb-3"
                            >
                                <div class="d-flex justify-content-between align-items-start mb-1">
                                    <span class="cat-name text-truncate me-2" :title="cat.fullCategoryName">
                                        {{ cat.fullCategoryName }}
                                    </span>
                                    <div class="d-flex align-items-center gap-2 flex-shrink-0">
                                        <span class="text-muted small">{{ cat.percentage }}%</span>
                                        <span class="fw-bold" :class="cat.totalSum > 0 ? 'text-danger' : 'text-muted'">
                                            {{ formatCurrency(cat.totalSum) }}
                                        </span>
                                    </div>
                                </div>
                                <div class="progress" style="height: 4px;">
                                    <div
                                        class="progress-bar"
                                        :style="`width: ${cat.percentage}%; background-color: ${catColor(i)}`"
                                    ></div>
                                </div>
                            </div>
                            <button
                                v-if="summary.allCategories.length > 3"
                                class="btn btn-link btn-sm text-muted p-0 mt-1"
                                @click="showAllCategories = !showAllCategories"
                            >
                                {{ showAllCategories
                                    ? 'Свернуть'
                                    : `Ещё ${summary.allCategories.length - 3}` }}
                            </button>
                        </div>
                    </div>
                </div>

                <!-- Топ 3 покупки (без нумерации) -->
                <div class="col-md-6">
                    <div class="card border-0 shadow-sm h-100">
                        <div class="card-body">
                            <h6 class="card-title text-muted mb-3">Топ-3 покупки</h6>
                            <div v-if="!summary.topPayments.length" class="text-muted small">Нет данных</div>
                            <div
                                v-for="(p, i) in summary.topPayments"
                                :key="p.paymentId"
                                class="d-flex align-items-start py-2"
                                :class="{ 'border-top': i > 0 }"
                            >
                                <div class="flex-grow-1 overflow-hidden">
                                    <div class="fw-medium text-truncate">
                                        {{ p.paymentDescription || p.paymentSourceName || '—' }}
                                    </div>
                                    <div class="small text-muted">
                                        {{ formatDate(p.paymentDate) }}
                                        <span v-if="p.categoryName" class="ms-2 badge bg-light text-secondary">
                                            {{ p.categoryName }}
                                        </span>
                                    </div>
                                </div>
                                <div class="text-danger fw-bold ms-2 text-nowrap">{{ formatCurrency(p.paymentSum) }}</div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </template>

        <div v-if="error" class="alert alert-danger mt-3">{{ error }}</div>
    </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import axios from 'axios'
import {
    Chart as ChartJS,
    CategoryScale,
    LinearScale,
    BarElement,
    Tooltip,
    Legend
} from 'chart.js'
import ChartDataLabels from 'chartjs-plugin-datalabels'
import { Bar } from 'vue-chartjs'
import { APP_CONFIG } from '../../../config'
import { formatCurrency, formatShortAmount, formatDate, formatShortDate, toInputDate } from '../../utils/format'

ChartJS.register(CategoryScale, LinearScale, BarElement, Tooltip, Legend, ChartDataLabels)

const summary = ref(null)
const loading = ref(false)
const error = ref(null)

const chartMode = ref('Day')
const chartFrom = ref('')
const chartTo = ref('')
const chartLoading = ref(false)
const chartPoints = ref([])
const showAllCategories = ref(false)

const periods = [
    { key: 'todaySpending',     label: 'Сегодня',  dateKey: 'todayDate',  fromKey: null,       toKey: null },
    { key: 'yesterdaySpending', label: 'Вчера',    dateKey: 'yesterdayDate', fromKey: null,    toKey: null },
    { key: 'weekSpending',      label: 'Неделя',   dateKey: null,         fromKey: 'weekFrom',  toKey: 'weekTo' },
    { key: 'monthSpending',     label: 'Месяц',    dateKey: null,         fromKey: 'monthFrom', toKey: 'monthTo' },
]

const chartTabs = [
    { key: 'Day',   label: 'По дням' },
    { key: 'Week',  label: 'По неделям' },
    { key: 'Month', label: 'По месяцам' },
]

const catColorPalette = [
    '#4e79a7', '#f28e2b', '#e15759', '#76b7b2',
    '#59a14f', '#edc948', '#b07aa1', '#ff9da7',
    '#9c755f', '#bab0ac'
]
const catColor = (i) => catColorPalette[i % catColorPalette.length]

const visibleCategories = computed(() =>
    showAllCategories.value
        ? (summary.value?.allCategories ?? [])
        : (summary.value?.allCategories ?? []).slice(0, 3)
)

// ── Stat card date label ───────────────────────────────────────────────────

const periodDateLabel = (period) => {
    if (!summary.value) return ''
    if (period.dateKey) {
        return formatDate(summary.value[period.dateKey], '')
    }
    if (period.fromKey && period.toKey) {
        const from = summary.value[period.fromKey]
        const to = summary.value[period.toKey]
        return `${formatShortDate(from, '')} – ${formatShortDate(to, '')}`
    }
    return ''
}

// ── Chart ──────────────────────────────────────────────────────────────────

const chartData = computed(() => {
    if (!chartPoints.value.length) return null
    return {
        labels: chartPoints.value.map(p => p.label),
        datasets: [{
            label: 'Расходы',
            data: chartPoints.value.map(p => p.value),
            backgroundColor: 'rgba(78,121,167,0.75)',
            borderColor: '#4e79a7',
            borderWidth: 1,
            borderRadius: 4,
        }]
    }
})

const chartOptions = computed(() => ({
    responsive: true,
    maintainAspectRatio: false,
    layout: { padding: { top: 24 } },
    plugins: {
        legend: { display: false },
        tooltip: {
            callbacks: { label: ctx => formatCurrency(ctx.parsed.y) }
        },
        datalabels: {
            anchor: 'end',
            align: 'top',
            formatter: (value) => value > 0 ? formatShortAmount(value) : '',
            font: { size: 10 },
            color: '#555',
            clip: false
        }
    },
    scales: {
        y: {
            beginAtZero: true,
            ticks: { callback: val => formatShortAmount(val), font: { size: 11 } },
            grid: { color: '#f0f0f0' }
        },
        x: {
            ticks: { font: { size: 10 }, maxRotation: 45 },
            grid: { display: false }
        }
    }
}))

// ── Helpers ────────────────────────────────────────────────────────────────

const getDefaultRange = (mode) => {
    const today = new Date()
    const todayStr = toInputDate(today)
    if (mode === 'Day') {
        const from = new Date(today); from.setDate(from.getDate() - 29)
        return { from: toInputDate(from), to: todayStr }
    }
    if (mode === 'Week') {
        const daysSinceMonday = (today.getDay() + 6) % 7
        const ws = new Date(today); ws.setDate(ws.getDate() - daysSinceMonday - 21)
        return { from: toInputDate(ws), to: todayStr }
    }
    // Month
    return { from: `${today.getFullYear()}-01-01`, to: todayStr }
}

// ── Data loading ───────────────────────────────────────────────────────────

const loadSummary = async () => {
    loading.value = true
    error.value = null
    try {
        const res = await axios.get(APP_CONFIG.apiBaseUrl + '/api/DashboardApi/summary')
        summary.value = res.data
    } catch (e) {
        error.value = 'Ошибка загрузки данных'
        console.error(e)
    } finally {
        loading.value = false
    }
}

const loadChart = async () => {
    chartLoading.value = true
    try {
        const res = await axios.get(APP_CONFIG.apiBaseUrl + '/api/DashboardApi/chart', {
            params: { mode: chartMode.value, from: chartFrom.value, to: chartTo.value }
        })
        chartPoints.value = res.data.points
    } catch (e) {
        console.error('Ошибка загрузки графика', e)
    } finally {
        chartLoading.value = false
    }
}

const setChartMode = (mode) => {
    chartMode.value = mode
    const range = getDefaultRange(mode)
    chartFrom.value = range.from
    chartTo.value = range.to
    loadChart()
}

onMounted(() => {
    const range = getDefaultRange(chartMode.value)
    chartFrom.value = range.from
    chartTo.value = range.to
    loadSummary()
    loadChart()
})
</script>

<style scoped>
.dashboard {
    max-width: 960px;
}

.stat-card {
    transition: box-shadow 0.15s;
}
.stat-card:hover {
    box-shadow: 0 4px 12px rgba(0,0,0,0.1) !important;
}

.period-label {
    font-size: 0.72rem;
    text-transform: uppercase;
    letter-spacing: 0.05em;
}

.period-date {
    font-size: 0.75rem;
    min-height: 1.1em;
}

.period-amount {
    color: #e15759;
    font-size: 1rem;
}

.date-input {
    width: 130px;
}

.cat-name {
    font-size: 0.875rem;
    max-width: 200px;
    display: inline-block;
}

.progress {
    border-radius: 3px;
    background-color: #f0f0f0;
}

.card {
    transition: box-shadow 0.15s;
}
</style>
