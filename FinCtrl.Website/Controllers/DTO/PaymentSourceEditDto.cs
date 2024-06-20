namespace FinCtrl.Website.Controllers.DTO
{
    public class PaymentSourceEditDto
    {
        public int PaymentSourceId { get; set; }
        public string PaymentSourceName { get; set; }
        public int? CategoryId { get; set; }
        public string? Description { get; set; }
    }
}
