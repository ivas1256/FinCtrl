<template>
    <div class="tags-container">
        <div class="card">
            <div class="card-header d-flex justify-content-between align-items-center flex-wrap gap-2">
                <h5 class="mb-0">Теги</h5>
                <div class="d-flex gap-2">
                    <button class="btn btn-primary btn-sm" @click="startCreate">
                        + Новый тег
                    </button>
                    <button class="btn btn-outline-secondary btn-sm" @click="loadTags">
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
                            <div class="col-md-8">
                                <label class="form-label mb-1">Название тега</label>
                                <input type="text" class="form-control form-control-sm" v-model="newTag.tagName">
                            </div>
                            <div class="col-md-4 d-flex gap-2">
                                <button class="btn btn-success btn-sm" :disabled="saving" @click="createTag">Создать</button>
                                <button class="btn btn-outline-secondary btn-sm" @click="cancelCreate">Отмена</button>
                            </div>
                        </div>
                    </div>

                    <div class="table-responsive">
                        <table class="table table-hover">
                            <thead class="table-light">
                                <tr>
                                    <th width="80">ID</th>
                                    <th>Название</th>
                                    <th width="140">Дата создания</th>
                                    <th width="120" class="text-center">Действия</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr v-for="tag in tags" :key="tag.tagId">
                                    <td><span class="text-muted">{{ tag.tagId }}</span></td>
                                    <td>
                                        <input
                                            type="text"
                                            class="form-control form-control-sm"
                                            v-model="tag.tagName"
                                        >
                                    </td>
                                    <td class="created-date">
                                        <small class="text-muted">{{ formatDate(tag.createdAt) }}</small>
                                    </td>
                                    <td class="text-center">
                                        <div class="btn-group btn-group-sm" role="group">
                                            <button
                                                class="btn btn-success"
                                                @click="saveTag(tag)"
                                                :disabled="!hasChanges(tag) || saving"
                                                title="Сохранить"
                                            >💾</button>
                                            <button
                                                class="btn btn-danger"
                                                @click="deleteTag(tag)"
                                                title="Удалить"
                                            >🗑</button>
                                        </div>
                                    </td>
                                </tr>
                            </tbody>
                        </table>
                    </div>

                    <div v-if="tags.length === 0" class="text-center py-4 text-muted">
                        Нет тегов
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

const tags = ref([])
const loading = ref(false)
const saving = ref(false)
const creating = ref(false)
const newTag = ref({ tagName: '' })
const { error, showError, clearAlerts } = useAlerts()

const hasChanges = (tag) => tag.tagName !== tag.originalTagName

const loadTags = async () => {
    loading.value = true
    clearAlerts()
    try {
        const response = await axios.get(APP_CONFIG.apiBaseUrl + '/api/TagsApi')
        tags.value = response.data.map(tag => ({
            ...tag,
            originalTagName: tag.tagName
        }))
    } catch (error) {
        console.error('Ошибка загрузки тегов:', error)
        showError('Ошибка загрузки данных')
    } finally {
        loading.value = false
    }
}

const startCreate = () => {
    newTag.value = { tagName: '' }
    creating.value = true
}

const cancelCreate = () => {
    creating.value = false
}

const createTag = async () => {
    if (!newTag.value.tagName.trim()) {
        showError('Заполните название тега')
        return
    }
    saving.value = true
    try {
        await axios.post(APP_CONFIG.apiBaseUrl + '/api/TagsApi', {
            tagName: newTag.value.tagName.trim()
        })
        creating.value = false
        await loadTags()
    } catch (error) {
        console.error('Ошибка создания тега:', error)
        showError(getErrorMessage(error, 'Ошибка создания тега'))
    } finally {
        saving.value = false
    }
}

const saveTag = async (tag) => {
    saving.value = true
    try {
        await axios.put(`${APP_CONFIG.apiBaseUrl}/api/TagsApi/${tag.tagId}`, {
            tagName: tag.tagName
        })
        tag.originalTagName = tag.tagName
    } catch (error) {
        console.error('Ошибка сохранения тега:', error)
        showError(getErrorMessage(error, 'Ошибка сохранения'))
    } finally {
        saving.value = false
    }
}

const deleteTag = async (tag) => {
    if (!confirm(`Удалить тег "${tag.tagName}"?`)) return
    saving.value = true
    try {
        await axios.delete(`${APP_CONFIG.apiBaseUrl}/api/TagsApi/${tag.tagId}`)
        await loadTags()
    } catch (error) {
        console.error('Ошибка удаления тега:', error)
        showError(getErrorMessage(error, 'Не удалось удалить тег'))
    } finally {
        saving.value = false
    }
}

onMounted(loadTags)
</script>

<style scoped>
.tags-container {
    max-width: 100%;
}

.table td {
    vertical-align: middle;
}

.created-date {
    font-size: 0.875rem;
}
</style>
