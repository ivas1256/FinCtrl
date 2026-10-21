namespace FinCtrl.Website.RequestDto
{
    public class BulkAssignTagsDto
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public List<int> CategoryIds { get; set; } = new();
        public List<int> TagIds { get; set; } = new();
    }
}