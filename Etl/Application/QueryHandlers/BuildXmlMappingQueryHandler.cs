using Etl.DataStructures.Forest;
using Etl.Domain.Entities;

namespace Etl.Application.QueryHandlers;

using System.Xml;

public class BuildXmlMappingQueryHandler
{
    
    private const string _xmlFileName = @"D:\FILES_STORAGE\report-afabad2e-ede0-4035-a94b-90409adbf863-BC-2024-08-19-340838-52-01[0].xml";
    
    public int BufLength { get; set; }
    
    public void Bench1()
    {
        using var fileStream = new FileStream(_xmlFileName, FileMode.Open, FileAccess.Read, FileShare.Read, BufLength, FileOptions.SequentialScan);
        using var reader = XmlReader.Create(fileStream, new XmlReaderSettings() { IgnoreComments = true, IgnoreWhitespace = true });
        
        // Восстанавливаем лес с XML-файла
        var forest =ForestSerializer.LoadForestFromFile<int, MappingRecord>("forest.xml");
        
        reader.MoveToContent();
        while (reader.Read())
        {
            
        }
    }
}