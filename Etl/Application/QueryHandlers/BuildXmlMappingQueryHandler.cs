using Etl.Application.Queries;
using Etl.Infrastructure.Ultilits;
using Etl.Infrastructure.Utilities;

namespace Etl.Application.QueryHandlers;

using System.Xml;

public class BuildXmlMappingQueryHandler
{
    
    private const string _xmlFileName = @"D:\FILE_STORAGE\report-c56e1a2f-ad5b-4e84-bbe4-bb2a3f0f7721-BC-2025-04-03-145343-52-01[0].xml";
    
    public int BufLength { get; set; }
    
    public void Handle(BuildXmlMappingQuery query)
    {
        using var fileStream = new FileStream(_xmlFileName, FileMode.Open, FileAccess.Read, FileShare.Read, BufLength, FileOptions.SequentialScan);
        using var reader = XmlReader.Create(fileStream, new XmlReaderSettings() { IgnoreComments = true, IgnoreWhitespace = true });
        
        // Восстанавливаем лес с XML-файла
        var dicTag2DbData = FileManager.LoadDictionary("D:\\Работа\\dev\\Etl\\Etl\\FILE_STORAGE\\rgis\\dictionary.json");
        
        // 1. Группируете поля:
        var classAttrs = dicTag2DbData.Values
            .Distinct()
            .GroupBy(x => x.Item1)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Item2).Distinct().ToList());

        // 2. Строите динамические классы:
        var typeMap = DynamicClassBuilder.BuildClasses(classAttrs);     
        
        reader.MoveToContent();
        while (reader.Read())
        {
            
        }
    }
}