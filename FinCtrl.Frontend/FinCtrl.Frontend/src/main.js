import './assets/main.css'
import 'vue-multiselect/dist/vue-multiselect.min.css'

import { createApp } from 'vue'
import App from './App.vue'
import router from '..'

const app = createApp(App)

app.use(router) // используем роутер
app.mount('#app')
