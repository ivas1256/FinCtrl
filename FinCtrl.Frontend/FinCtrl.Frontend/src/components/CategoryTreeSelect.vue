<template>
    <div class="category-tree-select">
        <select 
            :class="['form-select', sizeClass]"
            :value="modelValue === null ? '' : modelValue"
            @change="handleChange"
            :disabled="loading"
        >
            <option value=""></option>
            <template v-for="category in flattenedCategories" :key="category.categoryId">
                <option 
                    :value="category.categoryId"
                    :disabled="category.disabled"
                >
                    {{ category.displayName }}
                </option>
            </template>
        </select>
        <div v-if="loading" class="spinner-border spinner-border-sm position-absolute" style="right: 10px; top: 50%; transform: translateY(-50%);">
            <span class="visually-hidden">Загрузка...</span>
        </div>
    </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import axios from 'axios'
import { APP_CONFIG } from '../config'

const props = defineProps({
    modelValue: {
        type: [Number, String, null],
        default: null
    },
    size: {
        type: String,
        default: 'sm', // 'sm', 'md', 'lg'
        validator: (value) => ['sm', 'md', 'lg'].includes(value)
    },
    indentChar: {
        type: String,
        default: '  ' // Два пробела для отступа
    },
    indentSymbol: {
        type: String,
        default: '└─ ' // Символ для обозначения вложенности
    },
    autoLoad: {
        type: Boolean,
        default: true
    },
    categories: {
        type: Array,
        default: null // Если передан, используется вместо загрузки
    },
    emptyOptionText: {
        type: String,
        default: 'Без категории'
    }
})

const emit = defineEmits(['update:modelValue', 'change'])

const categoriesTree = ref([])
const loading = ref(false)

const sizeClass = computed(() => {
    if (props.size === 'sm') return 'form-select-sm'
    if (props.size === 'lg') return 'form-select-lg'
    return ''
})

// Загрузка дерева категорий из API
const loadCategories = async () => {
    if (!props.autoLoad || props.categories !== null) {
        return
    }
    
    loading.value = true
    try {
        const response = await axios.get(APP_CONFIG.apiBaseUrl + '/api/CategoriesApi/getTree')
        categoriesTree.value = response.data || []
    } catch (error) {
        console.error('Ошибка загрузки категорий:', error)
        categoriesTree.value = []
    } finally {
        loading.value = false
    }
}

// Используем переданные категории или загруженные
const currentCategories = computed(() => {
    return props.categories !== null ? props.categories : categoriesTree.value
})

// Функция для "расплющивания" дерева категорий в плоский список
const flattenedCategories = computed(() => {
    const result = []
    
    function flatten(categories, level = 0) {
        if (!Array.isArray(categories)) return
        
        categories.forEach(category => {
            if (!category || !category.categoryId) return
            
            // Создаем отступ для вложенности
            let displayName = ''
            
            if (level === 0) {
                // Корневой уровень - без отступов
                displayName = category.categoryName
            } else {
                // Для вложенных уровней используем тире для визуализации
                // Для уровня 1: "─ Категория"
                // Для уровня 2: "── Категория"
                // Для уровня 3: "─── Категория"
                const dashes = '-'.repeat(level)
                displayName = dashes + ' ' + category.categoryName
            }
            
            result.push({
                categoryId: category.categoryId,
                categoryName: category.categoryName,
                displayName: displayName,
                level: level
            })
            
            // Рекурсивно обрабатываем дочерние категории
            if (category.children && Array.isArray(category.children) && category.children.length > 0) {
                flatten(category.children, level + 1)
            }
        })
    }
    
    flatten(currentCategories.value)
    return result
})

const handleChange = (event) => {
    let value = event.target.value
    if (value === 'null' || value === '' || value === null) {
        value = null
    } else {
        value = Number(value)
    }
    
    emit('update:modelValue', value)
    emit('change', value)
}

// Загружаем категории при монтировании, если autoLoad включен
onMounted(() => {
    if (props.autoLoad && props.categories === null) {
        loadCategories()
    }
})

// Если категории переданы извне, обновляем их
watch(() => props.categories, (newCategories) => {
    if (newCategories !== null) {
        categoriesTree.value = newCategories
    }
}, { immediate: true })
</script>

<style scoped>
.category-tree-select {
    position: relative;
    width: 100%;
}

.form-select {
    width: 100%;
}

/* Улучшаем читаемость для вложенных элементов */
.form-select option {
    padding: 0.5rem;
    white-space: pre;
}
</style>

