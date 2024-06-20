using FinCtrl.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace FinCtrl.DAL.Implementation
{
    public class PaymentRepository : CRUDRepository<Payment>
    {
        public DbSet<Payment> Payments { get => dbContext.Payments; }

        public PaymentRepository(FinCtrlDBContext dbContext) : base(dbContext)
        {
        }

        public override Payment Get(int id)
        {
            return dbContext.Payments
                .Include(e => e.PaymentSource)
                .Include(e => e.PaymentCategory)
                .FirstOrDefault(x => x.PaymentId == id);
        }

        public override List<Payment> GetAll(int pageIndex = 0, int pageSize = 100)
        {
            return dbContext.Payments
                .Include(e => e.PaymentSource.Category)
                .Include(e => e.PaymentCategory)
                .OrderByDescending(x => x.PaymentDate)
                .Skip(pageSize * pageIndex)
                .Take(pageSize)
                .ToList()
                .Select(x =>
                {
                    var p = new Payment(
                    x.PaymentId,
                    x.PaymentType, // TODO: it's for test
                    x.PaymentSum,
                    x.PaymentDate,
                    x.PaymentSource)
                    {
                        PaymentCategory = x.PaymentCategory,
                        PaymentDescription = x.PaymentDescription
                    };

                    if (x.PaymentCategory == null)
                        p.PaymentCategory = x.PaymentSource.Category;

                    return p;
                }
                )
                .ToList();
        }

        public bool Exists(Payment payment)
        {
            var date = payment.PaymentDate;
            var sum = payment.PaymentSum;

            return dbContext.Set<Payment>().Any(x => x.PaymentDate == date && x.PaymentSum == sum);
        }

        //public override List<PaymentDTO> GetAllDTO(int pageIndex = 0, int pageSize = 100, object filter = null)
        //{
        //    if (filter == null)
        //        return dbContext.Payments
        //            .Include(e => e.PaymentSource)
        //            .Include(e => e.PaymentCategory)
        //            .Select(x => new PaymentDTO()
        //            {
        //                PaymentId = x.PaymentId,
        //                PaymentType = x.PaymentType,
        //                PaymentSum = x.PaymentSum,
        //                PaymentDate = x.PaymentDate,
        //                PaymentSource = new PaymentSourceDto(x.PaymentSource.PaymentSourceId, x.PaymentSource.PaymentSourceName),
        //                PaymentCategory = x.PaymentCategory == null ? null : new CategoryDTO(x.PaymentCategory.CategoryId, x.PaymentCategory.CategoryName),
        //                PaymentDescription = x.PaymentDescription

        //            })
        //            .OrderByDescending(x => x.PaymentDate)
        //            .Skip(pageSize * pageIndex)
        //            .Take(pageSize)
        //            .ToList();

        //    if (bool.Parse(filter.ToString()) == true)
        //        return GetAllByFilter(pageIndex, pageSize);

        //    return new List<PaymentDTO>();
        //}

        //public override Payment FromDTO(PaymentDTO dto)
        //{
        //    var entity = Get(dto.PaymentId);

        //    if (dto.PaymentType.HasValue)
        //        entity.PaymentType = dto.PaymentType.Value;
        //    if (dto.PaymentSum.HasValue)
        //        entity.PaymentSum = dto.PaymentSum.Value;
        //    if (dto.PaymentDate.HasValue)
        //        entity.PaymentDate = dto.PaymentDate.Value;
        //    if (!string.IsNullOrEmpty(dto.PaymentDescription))
        //        entity.PaymentDescription = dto.PaymentDescription;
        //    if (dto.PaymentSource != null)
        //        entity.PaymentSource = dbContext.PaymentSources.Find(dto.PaymentSource.PaymentSourceId);
        //    if (dto.PaymentCategory != null)
        //        entity.PaymentCategory = dbContext.Categories.Find(dto.PaymentCategory.CategoryId);

        //    return entity;
        //}

        //public override PaymentDTO ToDTO(Payment entity)
        //{
        //    var dto = new PaymentDTO()
        //    {
        //        PaymentId = entity.PaymentId,
        //        PaymentDate = entity.PaymentDate,
        //        PaymentSum = entity.PaymentSum,
        //        PaymentType = entity.PaymentType,
        //        PaymentDescription = entity.PaymentDescription
        //    };

        //    if (entity.PaymentCategory != null)
        //        dto.PaymentCategory = new CategoryDTO(entity.PaymentCategory.CategoryId, entity.PaymentCategory.CategoryName);
        //    if (entity.PaymentSource != null)
        //        dto.PaymentSource = new PaymentSourceDto(entity.PaymentSource.PaymentSourceId, entity.PaymentSource.PaymentSourceName);

        //    return dto;
        //}

        public int TotalCount()
        {
            return dbContext.Payments.Count();
        }

        //public List<PaymentDTO> GetAllByFilter(int pageIndex, int pageSize)
        //{
        //    return dbContext.Payments
        //        .Include(x => x.PaymentSource.Category)
        //        .Include(x => x.PaymentCategory)
        //        .Where(x => x.PaymentSource.Category == null && x.PaymentCategory == null)
        //        .Skip(pageIndex * pageSize)
        //        .Take(pageSize)
        //        .ToList()
        //        .Select(x => ToDTO(x)).ToList();
        //}

        public void Edit(int paymentId, Category newCategory, string newDescription)
        {
            var paym = Get(paymentId);
            paym.PaymentCategory = newCategory;
            paym.PaymentDescription = newDescription;
        }
    }
}