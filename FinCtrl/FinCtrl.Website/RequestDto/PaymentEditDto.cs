using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FinCtrl.Website.RequestDto
{
    public class PaymentEditDto
    {
        [FromRoute]
        [JsonPropertyName(nameof(PaymentId))]
        public int PaymentId { get; set; }
        public int? CategoryId { get; set; }
        public int? AccountId { get; set; }
        public string Description { get; set; }
        public string ExcludeFromStatsStr { get; set; }

        /// <summary>
        /// null — теги не трогать, пустой или непустой список — заменить набор целиком.
        /// </summary>
        public List<int>? TagIds { get; set; }
    }
}
