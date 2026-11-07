<template>
    <div class="accounts-container">
        <div class="card">
            <div class="card-header d-flex justify-content-between align-items-center flex-wrap gap-2">
                <h5 class="mb-0">Счета</h5>
                <div class="d-flex gap-2">
                    <button class="btn btn-primary btn-sm" @click="startCreate">
                        + Новый счет
                    </button>
                    <button class="btn btn-outline-secondary btn-sm" @click="loadAccounts">
                        Обновить
                    </button>
                </div>
            </div>

            <div class="card-body">
                <AlertMessage :message="error" @close="clearAlerts" />

                <div v-if="loading" class="text-center py-4">
                    <div class="spinner-border text-primary" role="status">
                        <span class="visually-hidden">Загрузка...</span>
                    </div>
                </div>

                <template v-else>
                    <!-- Форма создания -->
                    <div v-if="creating" class="p-3 mb-3 bg-light rounded">
                        <div class="row g-2 align-items-end">
                            <div class="col-md-3">
                                <label class="form-label mb-1">Название счета в excel файле</label>
                                <input type="text" class="form-control form-control-sm" v-model="newAccount.excelAccountName">
                            </div>
                            <div class="col-md-2">
                                <label for="new-account-number" class="form-label mb-1">Номер счета</label>
                                <input id="new-account-number" type="text" class="form-control form-control-sm" v-model="newAccount.accountNumber">
                            </div>
                            <div class="col-md-3">
                                <label class="form-label mb-1">Отображаемое название</label>
                                <input type="text" class="form-control form-control-sm" v-model="newAccount.displayName">
                            </div>
                            <div class="col-md-2">
                                <div class="form-check mb-1">
                                    <input id="new-account-is-savings" type="checkbox" class="form-check-input" v-model="newAccount.isSavings">
                                    <label for="new-account-is-savings" class="form-check-label">Накопительный</label>
                                </div>
                            </div>
                            <div class="col-md-2 d-flex gap-2">
                                <button class="btn btn-success btn-sm" :disabled="saving" @click="createAccount">Создать</button>
                                <button class="btn btn-outline-secondary btn-sm" @click="cancelCreate">Отмена</button>
                            </div>
                        </div>
                    </div>

                    <div class="table-responsive">
                        <table class="table table-hover">
                            <thead class="table-light">
                                <tr>
                                    <th width="80">ID</th>
                                    <th>Название счета в excel файле</th>
                                    <th title="По номеру счёт сопоставляется при загрузке файла">Номер счета</th>
                                    <th>Отображаемое название</th>
                                    <th width="140" class="text-center" title="Все операции по накопительному счёту считаются в «Накоплениях»">Накопительный</th>
                                    <th width="140">Дата создания</th>
                                    <th width="120" class="text-center">Действия</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr v-for="account in accounts" :key="account.accountId">
                                    <td><span class="text-muted">{{ account.accountId }}</span></td>
                                    <td>
                                        <input
                                            type="text"
                                            class="form-control form-control-sm"
                                            v-model="account.excelAccountName"
                                        >
                                    </td>
                                    <td>
                                        <input
                                            type="text"
                                            class="form-control form-control-sm"
                                            :aria-label="`Номер счета ${account.displayName}`"
                                            v-model="account.accountNumber"
                                        >
                                    </td>
                                    <td>
                                        <input
                                            type="text"
                                            class="form-control form-control-sm"
                                            v-model="account.displayName"
                                        >
                                    </td>
                                    <td class="text-center">
                                        <input
                                            type="checkbox"
                                            class="form-check-input"
                                            :aria-label="`Накопительный счет ${account.displayName}`"
                                            v-model="account.isSavings"
                                        >
                                    </td>
                                    <td class="created-date">
                                        <small class="text-muted">{{ formatDate(account.createdAt) }}</small>
                                    </td>
                                    <td class="text-center">
                                        <div class="btn-group btn-group-sm" role="group">
                                            <button
                                                class="btn btn-success"
                                                @click="saveAccount(account)"
                                                :disabled="!hasChanges(account) || saving"
                                                title="Сохранить"
                                            >💾</button>
                                            <button
                                                class="btn btn-danger"
                                                @click="deleteAccount(account)"
                                                title="Удалить"
                                            >🗑</button>
                                        </div>
                                    </td>
                                </tr>
                            </tbody>
                        </table>
                    </div>

                    <div v-if="accounts.length === 0" class="text-center py-4 text-muted">
                        Нет счетов
                    </div>
                </template>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import axios from 'axios'
import { getErrorMessage } from '../../utils/apiError'
import { useAlerts } from '../../composables/useAlerts'
import AlertMessage from '../../components/AlertMessage.vue'
import { formatDate } from '../../utils/format'
import { APP_CONFIG } from '../../config'

const accounts = ref([])
const loading = ref(false)
const saving = ref(false)
const creating = ref(false)
const newAccount = ref({ excelAccountName: '', accountNumber: '', displayName: '', isSavings: false })
const { error, showError, clearAlerts } = useAlerts()

// Номер необязателен: пустая строка уходит на backend как null
const toAccountNumber = (value) => value.trim() || null

const hasChanges = (account) =>
    account.excelAccountName !== account.originalExcelAccountName ||
    account.accountNumber !== account.originalAccountNumber ||
    account.displayName !== account.originalDisplayName ||
    account.isSavings !== account.originalIsSavings

const loadAccounts = async () => {
    loading.value = true
    clearAlerts()
    try {
        const response = await axios.get(APP_CONFIG.apiBaseUrl + '/api/AccountsApi')
        accounts.value = response.data.map(account => ({
            ...account,
            accountNumber: account.accountNumber ?? '',
            originalAccountNumber: account.accountNumber ?? '',
            originalExcelAccountName: account.excelAccountName,
            originalDisplayName: account.displayName,
            originalIsSavings: account.isSavings
        }))
    } catch (error) {
        console.error('Ошибка загрузки счетов:', error)
        showError('Ошибка загрузки данных')
    } finally {
        loading.value = false
    }
}

const startCreate = () => {
    newAccount.value = { excelAccountName: '', accountNumber: '', displayName: '', isSavings: false }
    creating.value = true
}

const cancelCreate = () => {
    creating.value = false
}

const createAccount = async () => {
    if (!newAccount.value.excelAccountName.trim() || !newAccount.value.displayName.trim()) {
        showError('Заполните название в excel файле и отображаемое название')
        return
    }
    saving.value = true
    clearAlerts()
    try {
        await axios.post(APP_CONFIG.apiBaseUrl + '/api/AccountsApi', {
            excelAccountName: newAccount.value.excelAccountName.trim(),
            accountNumber: toAccountNumber(newAccount.value.accountNumber),
            displayName: newAccount.value.displayName.trim(),
            isSavings: newAccount.value.isSavings
        })
        creating.value = false
        await loadAccounts()
    } catch (error) {
        console.error('Ошибка создания счета:', error)
        showError(getErrorMessage(error, 'Ошибка создания счета'))
    } finally {
        saving.value = false
    }
}

const saveAccount = async (account) => {
    saving.value = true
    clearAlerts()
    try {
        await axios.put(`${APP_CONFIG.apiBaseUrl}/api/AccountsApi/${account.accountId}`, {
            excelAccountName: account.excelAccountName,
            accountNumber: toAccountNumber(account.accountNumber),
            displayName: account.displayName,
            isSavings: account.isSavings
        })
        account.accountNumber = account.accountNumber.trim()
        account.originalAccountNumber = account.accountNumber
        account.originalExcelAccountName = account.excelAccountName
        account.originalDisplayName = account.displayName
        account.originalIsSavings = account.isSavings
    } catch (error) {
        console.error('Ошибка сохранения счета:', error)
        showError(getErrorMessage(error, 'Ошибка сохранения'))
    } finally {
        saving.value = false
    }
}

const deleteAccount = async (account) => {
    if (!confirm(`Удалить счет "${account.displayName}"?`)) return
    saving.value = true
    clearAlerts()
    try {
        await axios.delete(`${APP_CONFIG.apiBaseUrl}/api/AccountsApi/${account.accountId}`)
        await loadAccounts()
    } catch (error) {
        console.error('Ошибка удаления счета:', error)
        showError(getErrorMessage(error, 'Не удалось удалить счет'))
    } finally {
        saving.value = false
    }
}

onMounted(loadAccounts)
</script>

<style scoped>
.accounts-container {
    max-width: 100%;
}

.table td {
    vertical-align: middle;
}

.created-date {
    font-size: 0.875rem;
}
</style>
