// Bootstrap — единственная копия из npm; CSS подключается первым,
// чтобы собственные стили (main.css) переопределяли его, как раньше с CDN
import 'bootstrap/dist/css/bootstrap.min.css'
import './assets/main.css'
import 'vue-multiselect/dist/vue-multiselect.min.css'

import { createApp } from 'vue'
import App from './App.vue'
import router from '..'

const app = createApp(App)

app.use(router) // используем роутер
app.mount('#app')
