# Таблица расходов — Тепловая карта

Компонент для отображения расходов по категориям в разрезе временных периодов. Принятое дизайн-решение: **тепловая карта**, где интенсивность фона ячейки кодирует размер расхода.

> Актуальные значения цветов, отступов и состояний — в `payments-table-design-requirements.md`; цвета вынесены в CSS-переменные `--fc-*` (`src/assets/main.css`), scoped-стили главной ссылаются на них. Код в этом документе — иллюстрация, где он расходится с реальным кодом, прав код.

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
  if (!v || v > 0 || !maxV) return 0 // поступления в шкалу не входят
  return 0.04 + (Math.abs(v) / maxV) * 0.32  // диапазон: 0.04 – 0.36
}

// Inline-стиль на <td>
const heatStyle = (value, maxV) => {
  const alpha = heatAlpha(value, maxV)
  if (!alpha) return {}
  return { background: `rgba(200, 76, 78, ${alpha.toFixed(2)})` }
}
```

**Минимум 0.04:** сохраняется для любого ненулевого значения — полностью прозрачная ячейка читалась бы как «нет данных», а не «маленькая трата».

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

| Условие | Цвет |
|---|---|
| `v === 0` | `#aab4c2` (muted) |
| `v > 0` (поступление) | Bootstrap `text-success` |
| `v < 0`, `|v| / maxV ≤ 0.52` | `#c84c4e` (expense) |
| `v < 0`, `|v| / maxV > 0.52` | `#6e1517` (тёмно-красный) |

Ячейки с насыщенным фоном (> 52% от максимума) требуют тёмного текста — иначе красный на красном.

Приложение работает только в светлой теме (с задачи 0022: шаблонный `base.css` с `prefers-color-scheme` удалён). Правила тёмной темы для ячеек убраны — светлый текст `#ffcdd2` на светло-розовом фоне светлой темы нечитаем. Если тёмная тема появится (`data-bs-theme="dark"`), цвета ячеек нужно будет подобрать заново.

Поступления не участвуют в тепловой шкале (фон остаётся прозрачным) — шкала построена только на расходах.

Строки «Переводы», «Без категории» и «Накопления» (вместе со всеми потомками) в тепловую карту не входят: их суммы не участвуют в максимуме шкалы, а ячейки не подсвечиваются фоном (`isHeatExcluded` в `utils/heatmap.js`).

### CSS-классы для цвета текста

```js
// Vue: вычислить CSS-класс ячейки
const heatClass = (value, maxV) => {
  if (!value) return 'hm-zero'
  if (value > 0) return 'hm-income'
  // value < 0 — расход: нейтральный текст, интенсивность передаёт только фон
  return ''
}
```

```css
.hm-cell           { color: var(--fc-text); }          /* расход: #212529 */
.hm-cell.hm-zero   { color: var(--fc-text-secondary); cursor: default; }  /* «—», #6c757d */
.hm-cell.hm-income { color: var(--fc-success-text); }  /* #146c43 */
```

Пустая ячейка (ноль/нет данных) выводит «—» с `aria-label="нет трат"`: без ховера, фокуса, клика и `title`. Непустая — `role="button"`, `tabindex="0"`, `aria-label="{категория}, {период}: {сумма}"`, `aria-pressed`, Enter/Space = клик. Ховер — рамка 1px `#6c757d` (фон не меняется), фокус — 2px `#0a58ca`, выбранная — 2px `#0a58ca` и вес 600 (задача 0029).

---

## 5. Цвета категорий (точки)

Точка перед именем категории — `categoryColor(category)` (`src/utils/heatmap.js`): цвет, заданный категории на странице «Категории», иначе серый `#808080`. Палитра по индексу больше не используется для точек.

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
  width: auto;
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
  background-color: var(--fc-card-header-bg);
  font-size: 11px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  color: var(--fc-text-secondary-on-gray); /* #5c636a: контраст на сером фоне */
  padding: 9px 14px;
  border-bottom: 1px solid #dee2e6;
  white-space: nowrap;
}
.hm-col-cat    { text-align: left; min-width: 180px; }
.hm-col-period { text-align: right; width: 132px; }

/* Колонка категории */
.hm-cat-cell { padding: 7px 14px; }
.hm-cat-root  { display: flex; align-items: center; gap: 8px; }
.hm-cat-child { display: flex; align-items: center; gap: 8px; } /* отступ — 15px на уровень, inline-стилем */

/* Цветная точка */
.hm-dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  flex-shrink: 0;
}
.hm-dot-dim { opacity: 0.35; }

/* Имена категорий */
.hm-cat-name-root  { font-size: 13px; font-weight: 600; white-space: nowrap; }
.hm-cat-name-child { font-size: 12.5px; color: var(--fc-text-secondary); white-space: nowrap; }
.hm-cat-name-muted { font-size: 13px; font-weight: 400; color: var(--fc-text-secondary); white-space: nowrap; } /* «Без категории» */

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
  outline: 2px solid var(--fc-accent);
  outline-offset: -2px;
}

/* Строка итогов */
.hm-total-row td {
  background-color: var(--fc-card-header-bg);
  border-top: 2px solid var(--fc-accent) !important;
}
.hm-total-label {
  font-weight: 700;
  font-size: 13px;
  color: var(--fc-accent-text); /* #0a58ca: контраст на сером фоне */
}
.hm-total-amt {
  text-align: right;
  font-weight: 700;
  color: var(--fc-accent-text);
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
  color: var(--fc-text-secondary);
}
.hm-legend-bar {
  width: 72px;
  height: 7px;
  border-radius: 4px;
  /* совпадает с диапазоном heatAlpha: 0.04 – 0.36 */
  background: linear-gradient(to right, rgba(200, 76, 78, 0.04), rgba(200, 76, 78, 0.36));
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
- **Не** использовать зелёный где-либо кроме поступлений — `text-success` зарезервирован для дохода и действия «Сохранить» (Bootstrap success). **Исключение (решение В5, 2026-10-23):** зелёный `#146c43` (`--fc-success-text`) с `▼` допустим для «снижения трат» в колонке сравнения Δ и в карточке сравнения; в ячейках сумм тепловой карты зелёный по-прежнему только для поступлений.

## 12. Колонка «Δ к прошлому периоду» (задача 0028)

Последняя колонка таблицы — изменение к прошлому периоду группировки; полные требования — `payments-table-month-comparison-requirements.md` §2.
- Есть, когда в ответе API есть `comparison` и таблица не пустая. Шапка — `th.hm-col-delta` («Δ к сентябрю», `title` с полной формулировкой), ширина 176px, левый разделитель `--fc-border`.
- Ячейка — `DeltaCell.vue`: `{стрелка} {Δ₽} ({Δ%})`, Δ₽ — 600, стрелка `0.75em` с `aria-hidden`, скрытый текст «рост»/«снижение»/… в `visually-hidden`; «—» — нет сумм в обоих периодах. Фон без тепла.
- Цвета: рост трат `--fc-danger-text` ▲, снижение `--fc-success-text` ▼ (исключение из §11), без изменений/нет данных — `--fc-text-secondary` (в «Итого» — `--fc-text-secondary-on-gray`); у доходов и «Накоплений» рост/падение инвертированы.
- В «Итого» — Δ узла «ВСЕГО» (`hm-delta-total`, padding `9px 12px`).
- Под таблицей — подпись «Δ — {Т} по {D} против всего {П}» (+ «с учётом выбранных тегов») и легенда цветов.
