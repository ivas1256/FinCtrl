using Dapper.FluentMap.Dommel.Mapping;
using FinCtrl.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinCtrl.Infrastructure.Mapping
{
    internal class PaymentSourceMap : DommelEntityMap<PaymentSource>
    {
        public PaymentSourceMap()
        {
            ToTable("PaymentSources", "dbo");
            Map(x => x.PaymentSourceId).ToColumn("PaymentSourceId").IsKey().IsIdentity();
            Map(x => x.PaymentSourceCategory.CategoryId).ToColumn("CategoryId");
        }
    }
}
