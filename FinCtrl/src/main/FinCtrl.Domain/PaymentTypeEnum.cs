namespace FinCtrl.Domain
{
    /// <summary>Тип операции. Значения хранятся в <c>Payments.PaymentType</c>.</summary>
    public enum PaymentTypeEnum
    {
        Income = 0,
        Spending = 1,

        /// <summary>Перевод между своими счетами; стороны связаны в таблице <c>Transfers</c>.</summary>
        Transfer = 2
    }
}
