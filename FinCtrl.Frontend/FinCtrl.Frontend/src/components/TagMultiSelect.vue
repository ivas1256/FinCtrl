<template>
    <div>
        <Multiselect
            v-model="selectedTags"
            :options="localTags"
            label="tagName"
            track-by="tagId"
            :multiple="true"
            :taggable="true"
            :close-on-select="false"
            tag-placeholder="Добавить тег"
            placeholder="Теги"
            select-label=""
            deselect-label=""
            selected-label=""
            @tag="onCreateTag"
        />
        <div v-if="error" class="small text-danger mt-1" role="alert">{{ error }}</div>
    </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import Multiselect from 'vue-multiselect'
import axios from 'axios'
import { getErrorMessage } from '../utils/apiError'
import { APP_CONFIG } from '../config'

const props = defineProps({
    modelValue: {
        type: Array,
        default: () => []
    },
    tags: {
        type: Array,
        default: () => []
    }
})

const emit = defineEmits(['update:modelValue', 'created'])

// Ошибка создания тега — под полем, а не системным диалогом
const error = ref(null)

const localTags = ref([...props.tags])
watch(() => props.tags, (newTags) => {
    localTags.value = [...newTags]
})

const selectedTags = computed({
    get() {
        return props.modelValue
            .map(id => localTags.value.find(t => t.tagId === id))
            .filter(Boolean)
    },
    set(newSelection) {
        emit('update:modelValue', newSelection.map(t => t.tagId))
    }
})

const onCreateTag = async (tagName) => {
    error.value = null
    try {
        const response = await axios.post(APP_CONFIG.apiBaseUrl + '/api/TagsApi', { tagName })
        const newTag = response.data
        localTags.value.push(newTag)
        emit('update:modelValue', [...props.modelValue, newTag.tagId])
        emit('created', newTag)
    } catch (err) {
        console.error('Ошибка создания тега:', err)
        error.value = getErrorMessage(err, 'Ошибка создания тега')
    }
}
</script>

<style scoped>
:deep(.multiselect) {
    min-width: 220px;
}

/* Под размер sm Bootstrap-контролов: высота 31px, рамка #dee2e6, радиус 4px, шрифт 14px */
:deep(.multiselect),
:deep(.multiselect__input),
:deep(.multiselect__single) {
    font-size: 14px;
}
:deep(.multiselect__tags) {
    min-height: 31px;
    padding: 3px 32px 0 6px;
    border: 1px solid var(--fc-border);
    border-radius: 4px;
}
:deep(.multiselect__select) {
    height: 29px;
    top: 0;
}
:deep(.multiselect__placeholder) {
    margin-bottom: 0;
    padding-top: 2px;
}
:deep(.multiselect__tag) {
    margin-bottom: 0;
    padding-top: 2px;
    padding-bottom: 2px;
}
:deep(.multiselect__input) {
    margin-bottom: 0;
    padding: 0;
}
</style>
