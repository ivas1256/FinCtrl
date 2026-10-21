<template>
    <div
        v-if="message"
        class="alert alert-dismissible py-2 mb-3"
        :class="`alert-${type}`"
        role="alert"
    >
        <span class="alert-text">{{ message }}</span>
        <button type="button" class="btn-close py-2" aria-label="Закрыть" @click="emit('close')"></button>
    </div>
</template>

<script setup>
// Сообщение об ошибке/успехе по стайлгайду (раздел 4.12) вместо системных диалогов браузера
defineProps({
    message: {
        type: String,
        default: null
    },
    type: {
        type: String,
        default: 'danger',
        validator: (value) => ['danger', 'success', 'warning', 'info'].includes(value)
    }
})

const emit = defineEmits(['close'])
</script>

<style scoped>
/* getErrorMessage склеивает ошибки валидации через перевод строки */
.alert-text { white-space: pre-line; }
</style>
