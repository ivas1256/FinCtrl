using FinCtrl.DAL;
using FinCtrl.DAL.Models;
using FinCtrl.Website.BL.Data;
using System;
using System.Collections.Generic;

namespace FinCtrl.Website.BL.Data
{
    public class GeneralStatisticListItem
    {
        public int PaymentId { get; set; }
        public PaymentTypes PaymentType { get; set; }
        public decimal PaymentSum { get; set; }
        public DateTime PaymentDate { get; set; }
        public PaymentSource PaymentSource { get; set; }
        public Category Category { get; set; }
        public string PaymentDescription { get; set; }
    }

    public class MainStatisticGroupItem
    {
        public DatePeriod Period { get; set; }
        public Category Category { get; set; }

        public decimal TotalSum { get; set; }

        public List<GeneralStatisticListItem> Payments { get; set; }

        public override string ToString()
        {
            return $"{Category?.CategoryName ?? "Без категории"} {Period?.ToString() ?? "нет периода"}";
        }
    }
}
