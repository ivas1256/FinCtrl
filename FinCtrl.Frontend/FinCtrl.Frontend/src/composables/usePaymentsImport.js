import { ref, watch } from 'vue'

// Модалка импорта живёт в App.vue, а страницы о ней не знают: общий счётчик завершённых импортов
// позволяет странице перезагрузить свои данные, когда в базе появились новые платежи.
const importsCompleted = ref(0)

export const usePaymentsImport = () => {
    const notifyImported = () => {
        importsCompleted.value += 1
    }

    const onImported = (callback) => watch(importsCompleted, () => callback())

    return { importsCompleted, notifyImported, onImported }
}
