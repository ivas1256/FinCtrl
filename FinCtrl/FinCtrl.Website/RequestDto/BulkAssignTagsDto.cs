using System.ComponentModel.DataAnnotations;

namespace FinCtrl.Website.RequestDto
{
    public class BulkAssignTagsDto
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        [MinLength(1, ErrorMessage = "Выберите хотя бы одну категорию")]
        public List<int> CategoryIds { get; set; } = new();

        [MinLength(1, ErrorMessage = "Выберите хотя бы один тег")]
        public List<int> TagIds { get; set; } = new();
    }
}
