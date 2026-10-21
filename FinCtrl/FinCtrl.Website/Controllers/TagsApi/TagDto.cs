using System.ComponentModel.DataAnnotations;
using FinCtrl.Domain;

namespace FinCtrl.Website.Controllers.TagsApi
{
    public class TagDto
    {
        public int TagId { get; set; }

        [Required(ErrorMessage = "Название тега обязательно")]
        [StringLength(255, ErrorMessage = "Название тега — не длиннее 255 символов")]
        public string TagName { get; set; } = string.Empty;

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
