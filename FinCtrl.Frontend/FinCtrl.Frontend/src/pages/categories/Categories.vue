<template>
    <div>
        <h4 class="mb-4">Категории</h4>

        <AlertMessage :message="error" @close="clearAlerts" />

        <div class="card">
            <div class="card-header d-flex justify-content-between align-items-center">
                <h5 class="mb-0">Дерево категорий</h5>
                <div class="d-flex gap-2">
                    <button class="btn btn-outline-secondary btn-sm" @click="loadCategories">Обновить</button>
                    <button class="btn btn-primary btn-sm" @click="openCreateModal(null)">+ Категория</button>
                </div>
            </div>
            <div class="card-body">
                <div v-if="loading" class="text-center py-5">
                    <div class="spinner-border text-primary" role="status">
                        <span class="visually-hidden">Загрузка...</span>
                    </div>
                </div>

                <div v-else-if="categories.length === 0" class="text-center py-4 text-muted">
                    Нет данных
                </div>

                <div v-else>
                    <CategoryNode
                        v-for="cat in categories"
                        :key="cat.categoryId"
                        :node="cat"
                        :depth="0"
                        @edit="openEditModal"
                        @delete="confirmDelete"
                        @add-child="openCreateModal"
                    />
                </div>
            </div>
        </div>

        <!-- Модальное окно создания/редактирования -->
        <div class="modal fade" id="categoryModal" tabindex="-1" aria-hidden="true">
            <div class="modal-dialog modal-lg">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">{{ modalTitle }}</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <AlertMessage :message="modalError" @close="modalError = null" />
                        <div class="mb-3">
                            <label class="form-label">Название</label>
                            <input
                                type="text"
                                class="form-control"
                                v-model="form.categoryName"
                                placeholder="Название категории"
                            >
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Родительская категория</label>
                            <select class="form-select" v-model="form.parentCategoryId">
                                <option :value="null">— Корневая категория —</option>
                                <template v-for="cat in flatCategories" :key="cat.categoryId">
                                    <option
                                        :value="cat.categoryId"
                                        :disabled="editingId !== null && cat.categoryId === editingId"
                                    >{{ cat.displayName }}</option>
                                </template>
                            </select>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Порядок отображения</label>
                            <input
                                type="number"
                                class="form-control"
                                v-model.number="form.orderIndex"
                                min="0"
                            >
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Цвет</label>
                            <div class="d-flex align-items-center gap-2">
                                <input
                                    type="color"
                                    class="form-control form-control-color"
                                    v-model="form.color"
                                    title="Цвет категории"
                                >
                                <button
                                    type="button"
                                    class="btn btn-outline-secondary btn-sm"
                                    @click="form.color = DEFAULT_COLOR"
                                >Сбросить (серый)</button>
                            </div>
                        </div>
                        <div class="form-check">
                            <input
                                class="form-check-input"
                                type="checkbox"
                                id="isOnlyIncome"
                                v-model="form.isOnlyIncome"
                            >
                            <label class="form-check-label" for="isOnlyIncome">
                                Только доход
                            </label>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Отмена</button>
                        <button
                            type="button"
                            class="btn btn-success"
                            @click="saveCategory"
                            :disabled="saving || !form.categoryName.trim()"
                        >
                            {{ saving ? 'Сохранение...' : 'Сохранить' }}
                        </button>
                    </div>
                </div>
            </div>
        </div>

        <!-- Модальное окно подтверждения удаления -->
        <div class="modal fade" id="deleteModal" tabindex="-1" aria-hidden="true">
            <div class="modal-dialog">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">Удалить категорию</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <AlertMessage :message="modalError" @close="modalError = null" />
                        <p>Удалить категорию <strong>«{{ deletingCategory?.categoryName }}»</strong>?</p>
                        <p v-if="deletingCategory?.children?.length > 0" class="text-warning">
                            ⚠️ Категория содержит {{ deletingCategory.children.length }} подкатегорий(ю).
                        </p>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Отмена</button>
                        <button
                            type="button"
                            class="btn btn-danger"
                            @click="deleteCategory"
                            :disabled="saving"
                        >
                            {{ saving ? 'Удаление...' : 'Удалить' }}
                        </button>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import axios from 'axios'
import { getErrorMessage } from '../../utils/apiError'
import { Modal } from 'bootstrap'
import { APP_CONFIG } from '../../config'
import CategoryNode from './CategoryNode.vue'
import AlertMessage from '../../components/AlertMessage.vue'
import { useAlerts } from '../../composables/useAlerts'
import { DEFAULT_CATEGORY_COLOR } from '../../utils/heatmap'

const DEFAULT_COLOR = DEFAULT_CATEGORY_COLOR

const categories = ref([])
const loading = ref(false)
const saving = ref(false)
const { error, showError, clearAlerts } = useAlerts()
// Ошибка сохранения/удаления показывается внутри открытой модалки
const modalError = ref(null)

const editingId = ref(null)
const deletingCategory = ref(null)

const form = ref({
    categoryName: '',
    parentCategoryId: null,
    isOnlyIncome: false,
    orderIndex: 0,
    color: DEFAULT_COLOR
})

let categoryModal = null
let deleteModal = null

const initModals = () => {
    categoryModal = new Modal(document.getElementById('categoryModal'))
    deleteModal = new Modal(document.getElementById('deleteModal'))
}

const modalTitle = computed(() =>
    editingId.value !== null ? 'Редактировать категорию' : 'Новая категория'
)

const flatCategories = computed(() => {
    const result = []
    function flatten(list, level = 0) {
        for (const cat of list) {
            const prefix = level === 0 ? '' : '-'.repeat(level) + ' '
            result.push({ categoryId: cat.categoryId, displayName: prefix + cat.categoryName })
            if (cat.children?.length) flatten(cat.children, level + 1)
        }
    }
    flatten(categories.value)
    return result
})

const loadCategories = async () => {
    loading.value = true
    clearAlerts()
    try {
        const res = await axios.get(`${APP_CONFIG.apiBaseUrl}/api/CategoriesApi/getTree`)
        categories.value = res.data || []
    } catch (e) {
        console.error(e)
        showError('Ошибка загрузки категорий')
    } finally {
        loading.value = false
    }
}

const openCreateModal = (parent) => {
    editingId.value = null
    form.value = {
        categoryName: '',
        parentCategoryId: parent?.categoryId ?? null,
        isOnlyIncome: false,
        orderIndex: 0,
        color: DEFAULT_COLOR
    }
    modalError.value = null
    categoryModal.show()
}

const openEditModal = (node) => {
    editingId.value = node.categoryId
    form.value = {
        categoryName: node.categoryName,
        parentCategoryId: node.parentCategoryId ?? null,
        isOnlyIncome: node.isOnlyIncome,
        orderIndex: node.orderIndex,
        color: node.color || DEFAULT_COLOR
    }
    modalError.value = null
    categoryModal.show()
}

const confirmDelete = (node) => {
    deletingCategory.value = node
    modalError.value = null
    deleteModal.show()
}

const saveCategory = async () => {
    if (!form.value.categoryName.trim()) return
    saving.value = true
    try {
        const payload = {
            categoryName: form.value.categoryName.trim(),
            parentCategoryId: form.value.parentCategoryId,
            isOnlyIncome: form.value.isOnlyIncome,
            orderIndex: form.value.orderIndex,
            color: form.value.color || DEFAULT_COLOR
        }
        if (editingId.value !== null) {
            await axios.put(`${APP_CONFIG.apiBaseUrl}/api/CategoriesApi/${editingId.value}`, payload)
        } else {
            await axios.post(`${APP_CONFIG.apiBaseUrl}/api/CategoriesApi`, payload)
        }
        categoryModal.hide()
        await loadCategories()
    } catch (e) {
        console.error(e)
        modalError.value = getErrorMessage(e, 'Ошибка сохранения')
    } finally {
        saving.value = false
    }
}

const deleteCategory = async () => {
    if (!deletingCategory.value) return
    saving.value = true
    try {
        await axios.delete(`${APP_CONFIG.apiBaseUrl}/api/CategoriesApi/${deletingCategory.value.categoryId}`)
        deleteModal.hide()
        await loadCategories()
    } catch (e) {
        console.error(e)
        modalError.value = getErrorMessage(e, 'Ошибка удаления')
    } finally {
        saving.value = false
    }
}

onMounted(() => {
    initModals()
    loadCategories()
})
</script>

<style scoped>
.category-node + .category-node {
    border-top: 1px solid #f0f0f0;
}
</style>
