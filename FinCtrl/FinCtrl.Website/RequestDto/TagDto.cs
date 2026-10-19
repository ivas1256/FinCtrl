using FinCtrl.Domain;
using System;

namespace FinCtrl.Website.RequestDto
{
    public class TagDto
    {
        public int TagId { get; set; }
        public string TagName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUpdatedAt { get; set; }

        public static TagDto FromModel(Tag tag) => new TagDto
        {
            TagId = tag.TagId,
            TagName = tag.TagName,
            CreatedAt = tag.CreatedAt,
            LastUpdatedAt = tag.LastUpdatedAt
        };
    }
}
