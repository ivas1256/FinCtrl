using Dapper.FluentMap.Dommel.Mapping;
using FinCtrl.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinCtrl.Infrastructure.Mapping
{
    internal class PaymentMap : DommelEntityMap<Payment>
    {
        public PaymentMap()
        {
            ToTable("Payments", "dbo");
            Map(x => x.PaymentId).IsKey().IsIdentity();
        }
    }
}
