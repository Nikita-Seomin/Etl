﻿namespace Etl.Domain.Entities
{
    public class MappingRecord : IMappingRecord
    {
        public int Id { get; set; }
        public string SourceColumn { get; set; }
        public string ElementTypeId { get; set; }
        public int? ParentId { get; set; }
        public int? TargetFieldId { get; set; }
        public int? LoaderId { get; set; }
        public int? Value { get; set; }
        public int? ObjectId { get; set; }
        
    }
}