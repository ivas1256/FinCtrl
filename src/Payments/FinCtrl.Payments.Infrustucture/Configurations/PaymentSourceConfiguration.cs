using FinCtrl.Payments.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinCtrl.Payments.Infrastructure.Configurations
{
    internal class PaymentSourceConfiguration : IEntityTypeConfiguration<PaymentSource>
    {
        public void Configure(EntityTypeBuilder<PaymentSource> builder)
        {
            builder.HasKey(p => p.PaymentSourceId);

            builder.HasOne<Category>()
                .WithMany()
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
