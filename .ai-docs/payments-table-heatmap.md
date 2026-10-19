# Таблица расходов — Тепловая карта

Компонент для отображения расходов по категориям в разрезе временных периодов. Принятое дизайн-решение: **тепловая карта**, где интенсивность фона ячейки кодирует размер расхода.

---

## 1. Концепция

- **Строки** = категории с вложенностью (родительская → дочерние)
- **Столбцы** = временные периоды (день / неделя / месяц / квартал / год)
- **Фон ячейки** = оттенок красного, прозрачность пропорциональна сумме / максимум по выборке
- Размер трат читается мгновенно без сравнения цифр
- Строка итогов фиксирована в конце, выделена синим акцентом

---

## 2. Знак суммы

Значения из API передаются как есть, без преобразования:

| Тип | Знак | Пример отображения |
|---|---|---|
| Расход | отрицательный | `−3 500 ₽` |
| Поступление | положительный | `+1 200 ₽` |

**Абсолютные значения в таблице не используются.** `Math.abs` применяется только внутри тепловой формулы (для расчёта интенсивности фона), но не для отображения числа пользователю.

---

## 3. Тепловая формула

### Расчёт прозрачности фона

Интенсивность считается по модулю, потому что расходы хранятся как отрицательные числа:

```js
// maxV — максимальный |totalSum| по всем ячейкам выборки
// v    — значение конкретной ячейки (может быть отрицательным)

function heatAlpha(v, maxV) {
  if (!v || !maxV) return 0
  return 0.06 + (Math.abs(v) / maxV) * 0.70  // диапазон: 0.06 – 0.76
}

// Inline-стиль на <td>
const heatStyle = (value, maxV) => {
  if (!value || !maxV) return {}
  const alpha = (0.06 + (Math.abs(value) / maxV) * 0.70).toFixed(2)
  return { background: `rgba(200, 76, 78, ${alpha})` }
}
```

**Минимум 0.06:** сохраняется для любого ненулевого значения — полностью прозрачная ячейка читалась бы как «нет данных», а не «маленькая трата».

### Максимум выборки

Пересчитывается каждый раз при загрузке данных. Включает все строки — и родительские, и дочерние. Берётся модуль, чтобы корректно работать с отрицательными суммами расходов:

```js
const computeMaxValue = (tree) => {
  let max = 0
  const traverse = (nodes) => nodes.forEach(node => {
    Object.values(node.periods || {}).forEach(p => {
      const abs = Math.abs(p.totalSum || 0)
      if (abs > max) max = abs
    })
    if (node.children?.length) traverse(node.children)
  })
  traverse(tree)
  return max
}
```

---

## 4. Цвет текста в ячейке

Знак суммы определяет базовый цвет, интенсивность — уточнение для высоконасыщенного фона:

| Условие | Светлая тема | Тёмная тема |
|---|---|---|
| `v === 0` | `#aab4c2` (muted) | `#3e5068` |
| `v > 0` (поступление) | Bootstrap `text-success` | Bootstrap `text-success` |
| `v < 0`, `|v| / maxV ≤ 0.52` | `#c84c4e` (expense) | `#e07070` |
| `v < 0`, `|v| / maxV > 0.52` | `#6e1517` (тёмно-красный) | `#ffcdd2` (светло-розовый) |

Ячейки с насыщенным фоном (> 52% от максимума) требуют тёмного текста в светлой теме — иначе красный на красном. В тёмной теме логика обратная: нужен светлый текст.

Поступления не участвуют в тепловой шкале (фон остаётся прозрачным) — шкала построена только на расходах.

### CSS-классы для цвета текста

```js
// Vue: вычислить CSS-класс ячейки
const heatClass = (value, maxV) => {
  if (!value) return 'hm-zero'
  if (value > 0) return 'hm-income'
  // value < 0 — расход
  return Math.abs(value) / maxV > 0.52 ? 'hm-hi' : ''
}
```

```css
.hm-cell          { color: #c84c4e; }          /* расход, низкая интенсивность */
.hm-cell.hm-zero  { color: #aab4c2; cursor: default; }
.hm-cell.hm-income { color: #198754; }          /* Bootstrap success */
.hm-cell.hm-hi    { color: #6e1517; }           /* расход, высокая интенсивность */

/* Тёмная тема — высокая интенсивность расхода */
@media (prefers-color-scheme: dark) {
  :root:not([data-theme="light"]) .hm-cell.hm-hi { color: #ffcdd2; }
}
:root[data-theme="dark"] .hm-cell.hm-hi { color: #ffcdd2; }
```

---

## 5. Цвета категорий (точки)

Точка перед именем категории берётся из стандартной палитры графиков (см. основной стайлгайд, раздел 2):

```js
const PALETTE = [
  '#4e79a7', '#f28e2b', '#e15759', '#76b7b2',
  '#59a14f', '#edc948', '#b07aa1', '#ff9da7',
  '#9c755f', '#bab0ac'
]
const catColor = (index) => PALETTE[index % PALETTE.length]
```

Дочерние категории используют тот же цвет, что и родительская, но с `opacity: 0.35` на точке.

---

## 6. HTML-структура

### Шапка таблицы

```html
<thead>
  <tr>
    <th class="hm-col-cat">Категория</th>
    <th class="hm-col-period" v-for="period in periods" :key="period.key">
      {{ formatPeriodHeader(period) }}
    </th>
  </tr>
</thead>
```

### Строка — родительская категория

```html
<tr>
  <td class="hm-cat-cell">
    <div class="hm-cat-root">
      <span class="hm-dot" :style="{ background: catColor(catIndex) }"></span>
      <span class="hm-cat-name-root">{{ category.categoryName }}</span>
    </div>
  </td>
  <td
    v-for="period in periods"
    :key="period.key"
    class="hm-cell"
    :class="heatClass(category.periods[period.key]?.totalSum, maxValue)"
    :style="heatStyle(category.periods[period.key]?.totalSum, maxValue)"
    @click="handleCellClick(category.periods[period.key]?.payments, ...)"
  >
    {{ formatCurrency(category.periods[period.key]?.totalSum) }}
  </td>
</tr>
```

### Строка — дочерняя категория

```html
<tr>
  <td class="hm-cat-cell">
    <div class="hm-cat-child">
      <span class="hm-dot hm-dot-dim" :style="{ background: catColor(catIndex) }"></span>
      <span class="hm-cat-name-child">{{ child.categoryName }}</span>
    </div>
  </td>
  <!-- те же hm-cell -->
</tr>
```

### Строка итогов

```html
<tr class="hm-total-row">
  <td class="hm-cat-cell hm-total-label">Итого</td>
  <td class="hm-total-amt" v-for="period in periods" :key="period.key">
    {{ formatCurrency(periodTotal(period.key)) }}
  </td>
</tr>
```

---

## 7. CSS компонента

```css
/* Обёртка с горизонтальной прокруткой */
.hm-wrap {
  overflow-x: auto;
}

/* Таблица */
.hm-table {
  width: 100%;
  border-collapse: collapse;
  font-variant-numeric: tabular-nums; /* обязательно: выравнивание цифр */
  font-size: 12.5px;
}
.hm-table td {
  border-top: 1px solid #dee2e6;
  padding: 7px 12px;
  vertical-align: middle;
}

/* Шапка */
.hm-table thead th {
  background-color: #f8f9fa;
  font-size: 11px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  color: #6c757d;
  padding: 9px 14px;
  border-bottom: 1px solid #dee2e6;
  white-space: nowrap;
}
.hm-col-cat    { text-align: left; min-width: 180px; }
.hm-col-period { text-align: right; min-width: 100px; }

/* Колонка категории */
.hm-cat-cell { padding: 7px 14px; }
.hm-cat-root  { display: flex; align-items: center; gap: 8px; }
.hm-cat-child { display: flex; align-items: center; gap: 8px; padding-left: 15px; }

/* Цветная точка */
.hm-dot {
  width: 7px;
  height: 7px;
  border-radius: 50%;
  flex-shrink: 0;
}
.hm-dot-dim { opacity: 0.35; }

/* Имена категорий */
.hm-cat-name-root  { font-size: 13px; font-weight: 600; white-space: nowrap; }
.hm-cat-name-child { font-size: 12.5px; color: #6c757d; white-space: nowrap; }

/* Ячейка суммы */
.hm-cell {
  text-align: right;
  cursor: pointer;
  transition: filter 0.1s;
  /* цвет текста задаётся классами hm-zero / hm-hi, см. секцию 3 */
}
.hm-cell:hover { filter: brightness(0.90); }
.hm-cell.hm-zero:hover { filter: none; }

/* Выделенная ячейка (после клика) */
.hm-cell.selected {
  outline: 2px solid #0d6efd;
  outline-offset: -2px;
}

/* Строка итогов */
.hm-total-row td {
  background-color: #f8f9fa;
  border-top: 2px solid #0d6efd !important;
}
.hm-total-label {
  font-weight: 700;
  font-size: 13px;
  color: #0d6efd;
}
.hm-total-amt {
  text-align: right;
  font-weight: 700;
  color: #0d6efd;
  padding: 9px 12px;
}
```

---

## 8. Карточка-обёртка

Таблица размещается внутри стандартной карточки из основного стайлгайда (раздел 4.1). Легенда — в правой части шапки:

```html
<div class="card">
  <div class="card-header d-flex justify-content-between align-items-center">
    <h5 class="mb-0">Расходы по категориям</h5>
    <div class="hm-legend">
      <span class="text-muted small">меньше</span>
      <div class="hm-legend-bar"></div>
      <span class="text-muted small">больше</span>
    </div>
  </div>
  <div class="card-body p-0">
    <div class="hm-wrap">
      <table class="hm-table">...</table>
    </div>
  </div>
</div>
```

```css
/* Легенда-градиент */
.hm-legend {
  display: flex;
  align-items: center;
  gap: 7px;
  font-size: 11px;
  color: #6c757d;
}
.hm-legend-bar {
  width: 72px;
  height: 7px;
  border-radius: 4px;
  background: linear-gradient(to right, rgba(200, 76, 78, 0.07), rgba(200, 76, 78, 0.78));
}
```

---

## 9. Адаптация PaymentCategoryRow.vue

Существующий компонент нужно изменить в двух местах:

**1. Добавить prop `maxValue`**

```js
props: {
  // ... существующие props
  maxValue: { type: Number, default: 0 }
}
```

**2. Заменить стиль ячеек суммы**

Было:
```html
<td class="text-center clickable-cell" @click="handleCellClick(...)">
  {{ formatCurrency(...) }}
</td>
```

Стало:
```html
<td
  class="hm-cell"
  :class="heatClass(totalSum, maxValue)"
  :style="heatStyle(totalSum, maxValue)"
  @click="handleCellClick(...)"
>
  {{ formatCurrency(totalSum) }}
</td>
```

**3. В PaymentsTable.vue** — вычислять `maxValue` после загрузки данных и передавать в каждый `PaymentCategoryRow`:

```js
const maxValue = ref(0)

// После submitForm():
maxValue.value = computeMaxValue(paymentsTree.value)
```

```html
<PaymentCategoryRow
  :category="category"
  :periods="periodHeaders"
  :level="0"
  :max-value="maxValue"
  @cell-click="handleCellClick"
/>
```

---

## 10. Поведение expand/collapse

В тепловой карте **рекомендуется показывать все строки сразу** (без скрытия дочерних), чтобы пользователь видел полную картину и мог сравнивать интенсивность по всей таблице.

Если иерархия очень глубокая и строк много — допустимо оставить collapse, но тогда раскрывать по умолчанию (`isExpanded: true`).

---

## 11. Чего не делать

- **Не** передавать `Math.abs(value)` в ячейку для отображения — пользователь должен видеть знак: `−3 500 ₽` для расхода, `+1 200 ₽` для поступления.
- **Не** использовать красный фон тепловой карты для поступлений — шкала строится только по расходам (отрицательным значениям); ячейки с положительными суммами оставляются с прозрачным фоном.
- **Не** менять базовый цвет тепловой карты с красного на другой — красный = расходы, это семантика, согласованная с остальным приложением (`text-danger`).
- **Не** устанавливать `maxV` статически — максимум должен пересчитываться при каждой смене диапазона или фильтра, иначе шкала теряет смысл.
- **Не** убирать `font-variant-numeric: tabular-nums` — без него цифры разной ширины «прыгают» в столбцах.
- **Не** использовать зелёный где-либо кроме поступлений — `text-success` зарезервирован для дохода и действия «Сохранить» (Bootstrap success).
