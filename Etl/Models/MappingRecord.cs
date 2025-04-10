namespace Etl.Models
{
    public class MappingRecord
    {
        public int Id { get; set; }
        public string SourceColumn { get; set; }
        public string ElementTypeId { get; set; }
        public int? ParentId { get; set; }
        public int? TargetFieldId { get; set; }
        public int? LoaderId { get; set; }
        public int? Value { get; set; }
        
    }
}