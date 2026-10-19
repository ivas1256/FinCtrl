import { createRouter, createWebHistory } from 'vue-router'
import PaymentsTable from '@/pages/paymentsTable/PaymentsTable.vue'
import PaymentSources from '@/pages/paymentSources/PaymentSources.vue'
import Payments from '@/pages/payments/Payments.vue'
import Dashboard from '@/pages/dashboard/Dashboard.vue'
import Categories from '@/pages/categories/Categories.vue'
import Accounts from '@/pages/accounts/Accounts.vue'
import Tags from '@/pages/tags/Tags.vue'

// Определяем маршруты
const routes = [
  {
    path: '/',
    name: 'Главная',
    component: PaymentsTable
  },
  {
    path: '/dashboard',
    name: 'Дашборд',
    component: Dashboard
  },
  {
    path: '/payments',
    name: 'Операции',
    component: Payments
  },
  {
    path: '/payment-sources',
    name: 'Источники платежей',
    component: PaymentSources
  },
  {
    path: '/categories',
    name: 'Категории',
    component: Categories
  },
  {
    path: '/accounts',
    name: 'Счета',
    component: Accounts
  },
  {
    path: '/tags',
    name: 'Теги',
    component: Tags
  }
]

// Создаем экземпляр роутера
const router = createRouter({
  history: createWebHistory(), // Режим истории (красивые URL без #)
  routes // сокращение от `routes: routes`
})

export default router