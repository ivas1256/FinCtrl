namespace FinCtrl.Domain
{
    /// <summary>
    /// Перевод между своими счетами: связь списания и поступления. Обе операции имеют тип <see cref="PaymentTypeEnum.Transfer"/>.
    /// </summary>
    public class Transfer
    {
        public int TransferId { get; set; }

        /// <summary>Списание (отрицательная сумма).</summary>
        public int OutPaymentId { get; set; }

        /// <summary>Поступление (положительная сумма); <c>null</c> — вторая сторона не указана.</summary>
        public int? InPaymentId { get; set; }

        public DateTime CreatedAt { get; set; }

        /// <summary>Id второй стороны перевода относительно платежа <paramref name="paymentId"/>.</summary>
        public int? GetPairPaymentId(int paymentId) => paymentId == OutPaymentId ? InPaymentId : OutPaymentId;
    }
}
