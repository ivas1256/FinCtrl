<template>
  <div class="container-fluid my-2">
    <div class="row">
        <div class="col-1">
            <ul class="list-group">
                <li class="list-group-item">
                    <router-link to="/">Главная</router-link>
                </li>
                <li class="list-group-item">
                    <router-link to="/dashboard">Дашборд</router-link>
                </li>
                <li class="list-group-item">
                    <router-link to="/payments">Операции</router-link>
                </li>
                <li class="list-group-item">
                    <router-link to="/payment-sources">Источники платежей</router-link>
                </li>
                <li class="list-group-item">
                    <router-link to="/categories">Категории</router-link>
                </li>
                <li class="list-group-item">
                    <router-link to="/accounts">Счета</router-link>
                </li>
                <li class="list-group-item">
                    <router-link to="/tags">Теги</router-link>
                </li>
            </ul>
            <button type="button" class="btn btn-outline-secondary btn-sm w-100 mt-2" @click="importModal.open()">
                Загрузить файл
            </button>
        </div>
        <div class="col content-col">
            <main role="main" class="py-3">
                <router-view />
            </main>
        </div>
    </div>
    <PaymentsImportModal ref="importModal" />
  </div>
</template>

<script setup>
import { ref } from 'vue'
import PaymentsImportModal from './components/PaymentsImportModal.vue'

const importModal = ref(null)
</script>
