namespace Etl.Domain.Entities;

/// <summary>
/// Класс реализующий данные таблицы Mappings etl_editor.
/// </summary>
public interface IMappingRecord
{
    public int Id { get; set; }
    
    /// <summary>
    /// Наименование элемента в etl_editor
    /// </summary>
    public string SourceColumn { get; set; }
    
    /// <summary>
    /// Наименование элемента в etl_editor
    /// </summary>
    public int? TargetFieldId { get; set; }
}