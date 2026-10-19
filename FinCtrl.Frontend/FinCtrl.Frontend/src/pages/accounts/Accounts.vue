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
                <div v-if="loading" class="text-center py-4">
                    <div class="spinner-border text-primary" role="status">
                        <span class="visually-hidden">Загрузка...</span>
                    </div>
                </div>

                <template v-else>
                    <!-- Форма создания -->
                    <div v-if="creating" class="p-3 mb-3 bg-light rounded">
                        <div class="row g-2 align-items-end">
                            <div class="col-md-4">
                                <label class="form-label mb-1">Название счета в excel файле</label>
                                <input type="text" class="form-control form-control-sm" v-model="newAccount.excelAccountName">
                            </div>
                            <div class="col-md-4">
                                <label class="form-label mb-1">Отображаемое название</label>
                                <input type="text" class="form-control form-control-sm" v-model="newAccount.displayName">
                            </div>
                            <div class="col-md-4 d-flex gap-2">
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
                                    <th>Отображаемое название</th>
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
                                            v-model="account.displayName"
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
import { APP_CONFIG } from '../../../config'

const accounts = ref([])
const loading = ref(false)
const saving = ref(false)
const creating = ref(false)
const newAccount = ref({ excelAccountName: '', displayName: '' })

const formatDate = (dateString) => {
    if (!dateString) return '-'
    return new Date(dateString).toLocaleDateString('ru-RU', {
        day: '2-digit', month: '2-digit', year: 'numeric'
    })
}

const hasChanges = (account) =>
    account.excelAccountName !== account.originalExcelAccountName ||
    account.displayName !== account.originalDisplayName

const loadAccounts = async () => {
    loading.value = true
    try {
        const response = await axios.get(APP_CONFIG.apiBaseUrl + '/api/AccountsApi')
        accounts.value = response.data.map(account => ({
            ...account,
            originalExcelAccountName: account.excelAccountName,
            originalDisplayName: account.displayName
        }))
    } catch (error) {
        console.error('Ошибка загрузки счетов:', error)
        alert('Ошибка загрузки данных')
    } finally {
        loading.value = false
    }
}

const startCreate = () => {
    newAccount.value = { excelAccountName: '', displayName: '' }
    creating.value = true
}

const cancelCreate = () => {
    creating.value = false
}

const createAccount = async () => {
    if (!newAccount.value.excelAccountName.trim() || !newAccount.value.displayName.trim()) {
        alert('Заполните оба поля')
        return
    }
    saving.value = true
    try {
        await axios.post(APP_CONFIG.apiBaseUrl + '/api/AccountsApi', {
            excelAccountName: newAccount.value.excelAccountName.trim(),
            displayName: newAccount.value.displayName.trim()
        })
        creating.value = false
        await loadAccounts()
    } catch (error) {
        console.error('Ошибка создания счета:', error)
        alert(error?.response?.data || 'Ошибка создания счета')
    } finally {
        saving.value = false
    }
}

const saveAccount = async (account) => {
    saving.value = true
    try {
        await axios.put(`${APP_CONFIG.apiBaseUrl}/api/AccountsApi/${account.accountId}`, {
            excelAccountName: account.excelAccountName,
            displayName: account.displayName
        })
        account.originalExcelAccountName = account.excelAccountName
        account.originalDisplayName = account.displayName
    } catch (error) {
        console.error('Ошибка сохранения счета:', error)
        alert(error?.response?.data || 'Ошибка сохранения')
    } finally {
        saving.value = false
    }
}

const deleteAccount = async (account) => {
    if (!confirm(`Удалить счет "${account.displayName}"?`)) return
    saving.value = true
    try {
        await axios.delete(`${APP_CONFIG.apiBaseUrl}/api/AccountsApi/${account.accountId}`)
        await loadAccounts()
    } catch (error) {
        console.error('Ошибка удаления счета:', error)
        alert('Не удалось удалить счет — возможно, он используется в платежах')
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
