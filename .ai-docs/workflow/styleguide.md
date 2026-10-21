# FinCtrl — Style Guide

Документ описывает дизайн-паттерны, используемые в приложении. При создании новых страниц и компонентов нужно следовать этим правилам, чтобы UI выглядел единообразно.

---

## 1. Стек и базовая система

- **CSS-фреймворк**: Bootstrap 5.3 (utility-классы + компоненты)
- **Фреймворк**: Vue 3 (Composition API, `<script setup>`)
- **Иконки**: Unicode-эмодзи (💾, 👁 и т.д.) — только для кнопок-действий в таблицах
- **Графики**: Chart.js + vue-chartjs + chartjs-plugin-datalabels
- **HTTP**: axios
- **Глобальные стили**: только Bootstrap + `src/assets/main.css` (базовый размер текста 15px через `--bs-body-font-size`). Шаблонного `base.css` нет; тема только светлая — `prefers-color-scheme`/тёмную тему не добавлять (это отдельная задача на `data-bs-theme`). Стили страниц — в `<style scoped>`.

---

## 2. Цветовая палитра

### Смысловые цвета (Bootstrap-семантика)

| Роль | Класс Bootstrap | Hex | Применение |
|---|---|---|---|
| Основное действие | `btn-primary` | Bootstrap blue | Активная вкладка/режим |
| Успех / сохранение | `btn-success` | Bootstrap green | Кнопка «Сохранить», положительные суммы |
| Расходы / опасность | `text-danger` | Bootstrap red | Суммы трат |
| Нейтральное | `btn-outline-secondary` | — | Неактивные вкладки, вспомогательные кнопки |
| Приглушённый текст | `text-muted` | — | Метки, даты, вторичная информация |
| Предупреждение | `table-warning` | `#fff3cd` | Строки с несохранёнными изменениями |

### Палитра графиков (Chart.js)

Используется строго в этом порядке для серий данных и progress-bar категорий:

```
1. #4e79a7   (синий — основной)
2. #f28e2b   (оранжевый)
3. #e15759   (красный)
4. #76b7b2   (бирюзовый)
5. #59a14f   (зелёный)
6. #edc948   (жёлтый)
7. #b07aa1   (фиолетовый)
8. #ff9da7   (розовый)
9. #9c755f   (коричневый)
10. #bab0ac  (серый)
```

Основной цвет бара на графике: `rgba(78, 121, 167, 0.75)` заливка + `#4e79a7` граница.

---

## 3. Типографика

### Заголовки

```html
<!-- Заголовок страницы -->
<h4 class="mb-4">Название страницы</h4>

<!-- Заголовок блока/карточки -->
<h6 class="card-title text-muted mb-3">Название блока</h6>

<!-- Заголовок в шапке card с действиями -->
<h5 class="mb-0">Название</h5>
```

### Метки (лейблы)

```css
/* Метка статистической карточки (сегодня / вчера / ...) */
.period-label {
    font-size: 0.72rem;
    text-transform: uppercase;
    letter-spacing: 0.05em;
}
```

### Суммы денег

- Расходы: `class="text-danger fw-bold"` 
- Доходы: `class="text-success fw-bold"`
- Форматировать только через `src/utils/format.js`, без локальных `Intl.NumberFormat` в компонентах:
  - `formatCurrency(value)` — `1 234,50 ₽` (ru-RU, RUB, 2 знака; пустое значение — `0,00 ₽`);
  - `formatSignedCurrency(value)` — со знаком `+`/`-` (тепловая карта);
  - `formatShortAmount(value)` — сокращённый формат для осей графика и лейблов: `12к`, `1.5М`.

### Даты

Только через `src/utils/format.js` (второй параметр — заглушка для пустого значения, по умолчанию `-`):

```js
import { formatDate, formatShortDate, formatLongDate, formatDateTime, toInputDate } from '../../utils/format'

formatDate(str)        // Полная дата: 07.09.2026
formatShortDate(str)   // Короткая (без года): 07.09
formatLongDate(str)    // С днём недели: понедельник, 7 сентября 2026 г.
formatDateTime(str)    // Дата и время: 07.09.2026 14:05

// Диапазон недели: 02.09 – 08.09
`${formatShortDate(from)} – ${formatShortDate(to)}`

// Значение для <input type="date"> (YYYY-MM-DD) — по локальному календарю.
// Не использовать toISOString(): он считает в UTC и сдвигает дату на день.
toInputDate(new Date())
```

---

## 4. Компоненты

### 4.1 Карточка (card)

Все блоки контента — карточки без рамки с тенью:

```html
<div class="card border-0 shadow-sm mb-4">
    <div class="card-body">
        <!-- содержимое -->
    </div>
</div>
```

Карточка с шапкой и действиями (для таблиц):

```html
<div class="card">
    <div class="card-header d-flex justify-content-between align-items-center">
        <h5 class="mb-0">Заголовок</h5>
        <div class="d-flex gap-2">
            <!-- кнопки -->
        </div>
    </div>
    <div class="card-body">
        <!-- содержимое -->
    </div>
</div>
```

**Hover-эффект** для интерактивных карточек:

```css
.card {
    transition: box-shadow 0.15s;
}
.card:hover {
    box-shadow: 0 4px 12px rgba(0,0,0,0.1) !important;
}
```

### 4.2 Статистическая карточка

Используется для отображения числовых KPI (суммы за период):

```html
<div class="card h-100 border-0 shadow-sm stat-card">
    <div class="card-body text-center py-3">
        <div class="period-label text-muted small mb-1">Сегодня</div>
        <div class="period-date text-muted mb-1">07.09.2026</div>
        <div class="period-amount fw-bold text-danger">1 500,00 ₽</div>
    </div>
</div>
```

Сетка для 4 карточек в ряд:

```html
<div class="row g-3 mb-4">
    <div class="col-6 col-md-3"> <!-- 2 в ряд на мобиле, 4 на десктопе -->
```

### 4.3 Кнопки

```html
<!-- Основное действие -->
<button class="btn btn-success btn-sm">Сохранить все</button>

<!-- Вторичное -->
<button class="btn btn-outline-secondary btn-sm">Обновить</button>

<!-- Ссылка-кнопка (раскрыть/скрыть) -->
<button class="btn btn-link btn-sm text-muted p-0">Ещё 5</button>

<!-- Группа переключателей (режимы) -->
<div class="btn-group btn-group-sm" role="group">
    <button class="btn btn-primary">Активный</button>
    <button class="btn btn-outline-secondary">Неактивный</button>
</div>

<!-- Действие в таблице -->
<div class="btn-group btn-group-sm">
    <button class="btn btn-success" title="Сохранить">💾</button>
    <button class="btn btn-info text-white" title="Посмотреть">👁</button>
</div>
```

### 4.4 Поля ввода

```html
<!-- Date range (пара полей) -->
<div class="d-flex align-items-center gap-1">
    <input type="date" class="form-control form-control-sm date-input" v-model="from" @change="load">
    <span class="text-muted small">—</span>
    <input type="date" class="form-control form-control-sm date-input" v-model="to" @change="load">
</div>

<!-- Textarea в таблице -->
<textarea class="form-control form-control-sm" rows="1" placeholder="Описание..."></textarea>
```

```css
.date-input { width: 130px; }
textarea.form-control { resize: vertical; min-height: 38px; }
```

### 4.5 Таблица

```html
<div class="table-responsive">
    <table class="table table-hover">
        <thead class="table-light">
            <tr>
                <th width="80">ID</th>
                <th>Название</th>
                <th width="140" class="text-center">Действия</th>
            </tr>
        </thead>
        <tbody>
            <tr :class="{ 'table-warning': hasChanges(item) }">
                <td><span class="text-muted">{{ item.id }}</span></td>
                <td>{{ item.name }}</td>
                <td class="text-center">
                    <div class="btn-group btn-group-sm">...</div>
                </td>
            </tr>
        </tbody>
    </table>
</div>
```

```css
.table td { vertical-align: middle; }
```

ID-колонки всегда `text-muted`. Строки с несохранёнными изменениями — `table-warning`.

### 4.6 График (Chart.js Bar)

```js
// Регистрация
import { Chart as ChartJS, CategoryScale, LinearScale, BarElement, Tooltip, Legend } from 'chart.js'
import ChartDataLabels from 'chartjs-plugin-datalabels'
import { Bar } from 'vue-chartjs'
ChartJS.register(CategoryScale, LinearScale, BarElement, Tooltip, Legend, ChartDataLabels)
```

Стандартные опции:

```js
const chartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    layout: { padding: { top: 24 } }, // место под datalabels
    plugins: {
        legend: { display: false },
        tooltip: { callbacks: { label: ctx => formatCurrency(ctx.parsed.y) } },
        datalabels: {
            anchor: 'end',
            align: 'top',
            formatter: (value) => value > 0 ? formatShortAmount(value) : '',
            font: { size: 10 },
            color: '#555',
            clip: false
        }
    },
    scales: {
        y: {
            beginAtZero: true,
            ticks: { callback: val => formatShortAmount(val), font: { size: 11 } },
            grid: { color: '#f0f0f0' }
        },
        x: {
            ticks: { font: { size: 10 }, maxRotation: 45 },
            grid: { display: false }
        }
    }
}
```

Высота контейнера: `style="max-height: 260px;"` на элементе `<Bar>`.

### 4.7 Progress bar (категории)

```html
<div class="progress" style="height: 4px;">
    <div class="progress-bar"
         :style="`width: ${cat.percentage}%; background-color: ${catColor(i)}`">
    </div>
</div>
```

```css
.progress { border-radius: 3px; background-color: #f0f0f0; }
```

Цвет берётся из палитры графиков по индексу.

### 4.8 Состояние загрузки

```html
<!-- Полная страница -->
<div v-if="loading" class="text-center py-5">
    <div class="spinner-border text-primary" role="status">
        <span class="visually-hidden">Загрузка...</span>
    </div>
</div>

<!-- Внутри блока (маленький) -->
<div v-if="loading" class="text-center py-4">
    <div class="spinner-border spinner-border-sm text-primary" role="status"></div>
</div>
```

### 4.9 Пустое состояние

```html
<div v-if="!loading && items.length === 0" class="text-center py-4 text-muted">
    Нет данных
</div>
```

### 4.10 Несохранённые изменения (batch-редактирование)

```html
<div v-if="hasChanges" class="mt-3 p-3 bg-light rounded">
    <div class="row align-items-center">
        <div class="col">
            <strong>Несохраненные изменения:</strong> {{ count }} элементов
        </div>
        <div class="col text-end">
            <button class="btn btn-success" @click="saveAll" :disabled="saving">
                {{ saving ? 'Сохранение...' : `Сохранить все (${count})` }}
            </button>
            <button class="btn btn-outline-secondary ms-2" @click="reset">Сбросить все</button>
        </div>
    </div>
</div>
```

### 4.11 Модальное окно

Использует Bootstrap `Modal` класс напрямую:

```js
import { Modal } from 'bootstrap'
Modal.getOrCreateInstance(document.getElementById('myModal')).show()
// ...
Modal.getOrCreateInstance(document.getElementById('myModal')).hide()
```

Bootstrap подключается одной копией из npm: CSS — в `src/main.js`, JS — только `import { Modal } from 'bootstrap'`. Глобальный `bootstrap.Modal` и CDN-подключения в `index.html` не использовать.

Стандартный размер: `modal-lg` (все модалки с формами и списками). Исключение — короткий диалог подтверждения (удаление) без формы: стандартный размер `modal-dialog`. Кнопка закрытия: `data-bs-dismiss="modal"`.

Ошибки действий внутри модалки (сохранение, валидация) показываются **в `modal-body`** через `AlertMessage` (см. 4.12) — страница под модалкой не видна. При открытии модалки ошибку сбрасывать.

### 4.12 Ошибки и сообщения об успехе

`alert()` не использовать (блокирует вкладку, не стилизуется). Сообщения — компонент `src/components/AlertMessage.vue` (Bootstrap `alert-dismissible`, закрывается крестиком, многострочный текст сохраняет переводы строк) и composable `src/composables/useAlerts.js`:

```js
import AlertMessage from '../../components/AlertMessage.vue'
import { useAlerts } from '../../composables/useAlerts'
import { getErrorMessage } from '../../utils/apiError'

// error/success взаимоисключающие: showError сбрасывает success и наоборот
const { error, success, showError, showSuccess, clearAlerts } = useAlerts()

const load = async () => {
    loading.value = true
    clearAlerts() // загрузка и начало сохранения сбрасывают прошлые сообщения
    try { ... }
    catch (e) { console.error(e); showError('Ошибка загрузки данных') }
    finally { loading.value = false }
}

// Ошибка API — текст из ProblemDetails, иначе запасной
catch (e) { showError(getErrorMessage(e, 'Ошибка сохранения')) }

// Результат массовой операции
showSuccess(`Успешно обновлено операций: ${changed.length}`)
```

```html
<!-- Над содержимым страницы/карточки (первым элементом card-body или после h4) -->
<AlertMessage :message="error" @close="clearAlerts" />
<AlertMessage :message="success" type="success" @close="clearAlerts" />

<!-- Внутри модалки — отдельный ref -->
<AlertMessage :message="modalError" @close="modalError = null" />
```

Валидация форм (пустые поля и т.п.) — тем же `showError`/ошибкой модалки. В переиспользуемом поле внутри таблицы (например, `TagMultiSelect`) — мелкий текст под полем: `<div class="small text-danger mt-1" role="alert">`. `confirm()` для подтверждения удаления/ухода со страницы с несохранёнными изменениями пока допустим.

---

## 5. Структура страницы

### Максимальная ширина

- Страницы с карточками/дашборды: `max-width: 960px`
- Страницы с таблицами: `max-width: 100%`

### Типовой layout страницы с дашбордом

```
h4.mb-4                     — заголовок страницы
  ↓
row.g-3.mb-4                — строка stat-карточек
  col-6.col-md-3 × N
  ↓
card.border-0.shadow-sm.mb-4 — главный блок с переключателем
  ↓
row.g-3                     — два блока в ряд
  col-md-6  |  col-md-6
```

### Отступы

- Между секциями: `mb-4`
- Внутри card-body: `p-3` (Bootstrap default) или `py-3` / `py-4` для stat-карточек
- Gap в flex-контейнерах: `gap-2` (стандарт), `gap-1` (тесно), `gap-3` (широко)

---

## 6. Vue-паттерны

### Структура компонента

Только `<script setup>` — Options API (`export default { data, methods }`) не использовать. Рекурсивный компонент ссылается на себя по имени файла; имя задаётся явно через `defineOptions({ name: 'PaymentCategoryRow' })`. Соблюдение (нет Options API, `alert()`, `prefers-color-scheme`/`base.css`; `modal-lg` у модалок правки) проверяет `src/styleguide.spec.js`.

```vue
<template>
    <!-- разметка -->
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import axios from 'axios'
import { APP_CONFIG } from '../../config'
import { formatCurrency, formatDate } from '../../utils/format'
import { useAlerts } from '../../composables/useAlerts'

// Состояние
const data = ref([])
const loading = ref(false)
const { error, showError, clearAlerts } = useAlerts() // + <AlertMessage :message="error" @close="clearAlerts" /> в шаблоне

// Вычисляемые
const filtered = computed(() => ...)

// Методы
const loadData = async () => {
    loading.value = true
    clearAlerts()
    try { ... }
    catch (e) { showError('Ошибка загрузки данных'); console.error(e) }
    finally { loading.value = false }
}

onMounted(loadData)
</script>

<style scoped>
/* только специфичные стили */
</style>
```

### Отслеживание несохранённых изменений

```js
// Сохраняем оригинал при загрузке
items.value = response.data.map(item => ({
    ...item,
    originalFieldA: item.fieldA,
    originalFieldB: item.fieldB
}))

// Проверяем изменения
const hasChanges = (item) =>
    item.fieldA !== item.originalFieldA || item.fieldB !== item.originalFieldB

// Сбрасываем изменения
const reset = () => items.value.forEach(item => {
    item.fieldA = item.originalFieldA
    item.fieldB = item.originalFieldB
})
```

### Обращение к API

```js
// Базовый URL (src/config.js): VITE_API_BASE_URL, по умолчанию "http://localhost:5000"
import { APP_CONFIG } from '../../config'

// GET с параметрами
await axios.get(`${APP_CONFIG.apiBaseUrl}/api/EntityApi/list`, { params: { from, to } })

// PUT
await axios.put(`${APP_CONFIG.apiBaseUrl}/api/EntityApi/${id}`, { field: value })
```

---

## 7. Backend-паттерны

Слои, контроллеры, сервисы, репозитории и код-стайл C# — в [`backend-guide.md`](backend-guide.md). Ниже — доменные соглашения, которые затрагивают и UI.

### Фильтрация расходов

Стандартная цепочка фильтров для расчёта трат:

```csharp
// 1. Только отрицательные суммы
// 2. IsIncludeInTotalResults() — исключает IsOnlyIncome, категорию 33 (Накопления), "Без категории"
var spendings = payments
    .Where(p => p.PaymentSum < 0
        && (p.CalculatedCategory?.IsIncludeInTotalResults() ?? false))
    .ToList();
```

### DateRange в репозитории

`To` — **исключающий** (SQL: `PaymentDate < @To`). Всегда передавать `to = targetDate.AddDays(1)`.

```csharp
// Включить сегодня в выборку:
new DateRange(from, DateTime.Today.AddDays(1))
```

### Форматирование числовых значений

Суммы денег — `decimal`. На клиент передаются как есть (с копейками). Округления и форматирование — только на фронтенде.
