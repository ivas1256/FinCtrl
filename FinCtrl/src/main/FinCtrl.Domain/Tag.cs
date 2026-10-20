using System;

namespace FinCtrl.Domain
{
    public class Tag
    {
        public Tag()
        {

        }

        public Tag(int tagId, string tagName, DateTime createdAt, DateTime? lastUpdatedAt)
        {
            TagId = tagId;
            TagName = tagName;
            CreatedAt = createdAt;
            LastUpdatedAt = lastUpdatedAt;
        }

        public int TagId { get; set; }
        public string TagName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUpdatedAt { get; set; }

        public void Updated(DateTime now)
        {
            LastUpdatedAt = now;
        }

        public static Tag CreateFromExisting(int tagId, string tagName) => new Tag
        {
            TagId = tagId,
            TagName = tagName,
        };

        public static Tag CreateNew(string tagName, DateTime createdAt) => new Tag
        {
            TagName = tagName,
            CreatedAt = createdAt,
        };
    }
}
