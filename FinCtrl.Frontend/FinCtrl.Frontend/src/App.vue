<script setup>
import HelloWorld from './components/HelloWorld.vue'
import TheWelcome from './components/TheWelcome.vue'
import PaymentsTable from './pages/paymentsTable/PaymentsTable.vue';
</script>

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
        </div>
        <div class="col content-col">
            <main role="main" class="py-3">
                <router-view />
            </main>
        </div>
    </div>
  </div>
</template>

