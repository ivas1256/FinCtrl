// Направление перевода между своими счетами для списка операций: «Счёт A → Счёт B».
// PaymentTypeEnum.Transfer = 2 на backend.
export const TRANSFER_PAYMENT_TYPE = 2

const NO_ACCOUNT = 'не указан'

// Возвращает строку «A → B» для перевода или null для обычного платежа.
// Списание (сумма < 0): A — свой счёт, B — счёт второй стороны; поступление — наоборот.
export const getTransferDirection = (payment) => {
    if (!payment || payment.paymentType !== TRANSFER_PAYMENT_TYPE) return null

    const own = payment.account?.displayName || NO_ACCOUNT
    const pair = payment.transferPairAccount?.displayName || NO_ACCOUNT
    return payment.paymentSum < 0 ? `${own} → ${pair}` : `${pair} → ${own}`
}
