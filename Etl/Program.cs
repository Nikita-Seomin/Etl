using System;
using System.Xml;
using System.Xml.Schema;

class Program
{
    static void Main()
    {
        XmlReaderSettings settings = new XmlReaderSettings();
        XmlSchemaSet schemas = new XmlSchemaSet();
        
        // Загружаем основную схему
        schemas.Add(null, "..\\..\\..\\..\\Schemes\\extract_cadastral_plan_territory_v02.xsd");
        
        // Получаем путь к директории с дополнительными схемами
        string additionalTypesPath = "..\\..\\..\\..\\Schemes\\additionalTypes\\";
        
        // Проходимся по всем файлам .xsd в директории и подключаем их
        foreach (string file in Directory.GetFiles(additionalTypesPath, "*.xsd"))
        {
            schemas.Add(null, file);
        }
        
        settings.Schemas = schemas;
        settings.ValidationType = ValidationType.Schema;

        settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
        settings.ValidationEventHandler += ValidationEventHandler;

        using var reader = XmlReader.Create("..\\..\\..\\..\\report-234465bb-992c-489c-a3be-54366ca54931-number-52-01[0].xml", settings);
        XmlDocument document = new XmlDocument();
        document.Load(reader);
        document.Validate(ValidationEventHandler);
    }

    static void ValidationEventHandler(object sender, ValidationEventArgs e)
    {
        if (e.Severity == XmlSeverityType.Error)
        {
            Console.WriteLine("Error: {0}", e.Message);
        }
        else if (e.Severity == XmlSeverityType.Warning)
        {
            Console.WriteLine("Warning: {0}", e.Message);
        }
    }
}