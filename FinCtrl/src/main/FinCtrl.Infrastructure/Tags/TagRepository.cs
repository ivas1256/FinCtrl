using FinCtrl.Application.Tags;
using FinCtrl.Common.Infrastructure;
using FinCtrl.Domain;

namespace FinCtrl.Infrastructure.Tags
{
    internal class TagRepository : RepositoryBase, ITagRepository
    {
        public TagRepository(IDbConnectionFactory dbConnectionFactory) : base(dbConnectionFactory)
        {
        }

        private const string _select = @"SELECT
                  t.TagId
                 ,t.TagName
                 ,t.CreatedAt
                 ,t.LastUpdatedAt
              FROM Tags t";

        public async Task<List<Tag>> GetAll()
        {
            return (await Query<Tag>(_select + " ORDER BY t.TagName")).ToList();
        }

        public async Task<Tag?> GetById(int tagId)
        {
            return await FirstOrDefault<Tag>(_select + " WHERE t.TagId = @tagId", new { tagId });
        }

        public async Task<Tag?> GetByName(string tagName)
        {
            return await FirstOrDefault<Tag>(_select + " WHERE t.TagName = @tagName", new { tagName });
        }

        public async Task<bool> Exists(string tagName)
        {
            return await GetByName(tagName) is not null;
        }

        public async Task<int> Create(Tag tag)
        {
            var sql = @"INSERT INTO dbo.Tags (TagName, CreatedAt, LastUpdatedAt)
                        OUTPUT INSERTED.TagId
                        VALUES (@TagName, @CreatedAt, @LastUpdatedAt);";

            return await ExecuteScalar<int>(sql, new
            {
                tag.TagName,
                tag.CreatedAt,
                tag.LastUpdatedAt
            });
        }

        public async Task Update(params Tag[] tags)
        {
            var sql = @"UPDATE Tags
                        SET TagName = @TagName
                           ,LastUpdatedAt = @LastUpdatedAt
                        WHERE TagId = @TagId;";

            foreach (var tag in tags)
            {
                await Execute(sql, new
                {
                    tag.TagId,
                    tag.TagName,
                    tag.LastUpdatedAt
                });
            }
        }

        public async Task Delete(params Tag[] tags)
        {
            // Нет каскадного удаления на уровне БД (FK_PaymentTags_Tags — NO ACTION),
            // поэтому сначала вручную чистим связи, затем сам тег.
            var deleteLinksSql = @"DELETE FROM dbo.PaymentTags WHERE TagId = @TagId;";
            var deleteTagSql = @"DELETE FROM dbo.Tags WHERE TagId = @TagId;";

            foreach (var tag in tags)
            {
                await Execute(deleteLinksSql, new { tag.TagId });
                await Execute(deleteTagSql, new { tag.TagId });
            }
        }
    }
}
