using FinCtrl.DAL.Models;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace FinCtrl.DAL
{
    public class FinCtrlDBContext : DbContext
    {
        public FinCtrlDBContext(DbContextOptions<FinCtrlDBContext> options) : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<PaymentSource> PaymentSources { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
    }
}
