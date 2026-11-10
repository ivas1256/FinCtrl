# Контракт API: чек платежа (задача 0041)

Документ для интеграции frontend с backend. Источник истины — код:
`FinCtrl/FinCtrl.Website/Controllers/PaymentsApi/PaymentsApiController.cs` (`GetReceipt`) и
`FinCtrl/FinCtrl.Website/Controllers/PaymentsApi/ReceiptDto.cs`.

## Получить чек платежа

```
GET {apiBaseUrl}/api/PaymentsApi/{paymentId}/receipt
```

Только чтение: создавать, править и удалять позиции через API пока нельзя.

### Параметры

| Где   | Имя         | Тип | Обязателен | Описание   |
|-------|-------------|-----|------------|------------|
| route | `paymentId` | int | да         | id платежа |

Тела запроса и query-параметров нет.

### Ответ 200 — `ReceiptDto`

```json
{
  "paymentId": 8270,
  "totalSum": 1365.89,
  "items": [
    {
      "receiptItemId": 3,
      "itemName": "Томаты 1кг",
      "price": 199.99,
      "quantity": 0.624,
      "unitId": 2,
      "unitName": "кг",
      "totalPrice": 124.79
    }
  ]
}
```

| Поле        | Тип             | Описание |
|-------------|-----------------|----------|
| `paymentId` | number (int)    | id платежа из запроса |
| `totalSum`  | number          | сумма `totalPrice` всех позиций; `0`, если позиций нет |
| `items`     | ReceiptItemDto[] | позиции чека, отсортированы по `receiptItemId` по возрастанию; никогда не `null` |

`ReceiptItemDto`:

| Поле            | Тип          | Описание |
|-----------------|--------------|----------|
| `receiptItemId` | number (int) | id позиции |
| `itemName`      | string       | название как в чеке (до 500 символов) |
| `price`         | number       | цена за единицу, 2 знака после запятой |
| `quantity`      | number       | количество, до 3 знаков после запятой (весовой товар: `0.624`) |
| `unitId`        | number (int) | id единицы измерения (справочник `Units`) |
| `unitName`      | string       | единица как в чеке: `шт.`, `кг`, `г`, `л`, `мл` и т.п. |
| `totalPrice`    | number       | итог по позиции из чека, 2 знака после запятой |

Все поля всегда присутствуют, `null` не бывает. Суммы положительные: знак суммы платежа здесь не отражается.

### Чек отсутствует

Если у платежа нет позиций, ответ **200**, а не ошибка:

```json
{ "paymentId": 8269, "totalSum": 0, "items": [] }
```

Признак «у платежа есть чек» — `items.length > 0`.

### Ошибки

| Код | Когда | Тело |
|-----|-------|------|
| 404 | платёж не существует или мягко удалён (`IsDeleted`) | ProblemDetails: `{ "title": "Не найдено", "status": 404, "detail": "Операция {id} не найдена" }` |
| 404 | `paymentId` не целое число (ограничение маршрута `:int`) | пустое тело |

Для показа ошибки берите `detail` из ProblemDetails, как в остальных запросах, через `useAlerts`.

## Замечания для frontend

- Вызов — как в остальных страницах, напрямую через axios:
  ```js
  const { data } = await axios.get(`${APP_CONFIG.apiBaseUrl}/api/PaymentsApi/${paymentId}/receipt`)
  ```
- `price`, `totalPrice`, `totalSum` форматировать только через `formatCurrency` из `src/utils/format.js`.
- `quantity` — не деньги: показывать числом с единицей (`0.624 кг`, `2 шт.`), без валюты.
- `totalSum` может не совпадать с суммой платежа: платёж может оплачивать несколько чеков, а чек может быть оплачен частично. Расхождение — не ошибка.
- В ответах `GET api/PaymentsApi/list` и других списках платежей признака наличия чека нет. Узнать о чеке можно только этим запросом.

## Проверено (dev2, 2026-11-10)

- `8270` — 200, 12 позиций, `totalSum` 1365.89;
- `8269` — 200, `items: []`, `totalSum: 0`;
- `-1` и удалённый `3312` — 404.
