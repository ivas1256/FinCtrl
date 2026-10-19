<template>
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
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import Multiselect from 'vue-multiselect'
import axios from 'axios'
import { APP_CONFIG } from '../../config'

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
    try {
        const response = await axios.post(APP_CONFIG.apiBaseUrl + '/api/TagsApi', { tagName })
        const newTag = response.data
        localTags.value.push(newTag)
        emit('update:modelValue', [...props.modelValue, newTag.tagId])
        emit('created', newTag)
    } catch (error) {
        console.error('Ошибка создания тега:', error)
        alert(error?.response?.data || 'Ошибка создания тега')
    }
}
</script>

<style scoped>
:deep(.multiselect) {
    min-width: 220px;
}
</style>
