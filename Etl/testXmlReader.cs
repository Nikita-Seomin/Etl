// using System;
// using System.Xml;
// using System.Xml.Schema;
// using Npgsql;
//
// class TestXmlReader
// {
//     static void Main()
//     {
//         XmlReaderSettings settings = new XmlReaderSettings();
//         XmlSchemaSet schemas = new XmlSchemaSet();
//         
//         // Загружаем основную схему
//         schemas.Add(null, "..\\..\\..\\..\\Schemes\\extract_cadastral_plan_territory_v02.xsd");
//         
//         // Получаем путь к директории с дополнительными схемами
//         string additionalTypesPath = "..\\..\\..\\..\\Schemes\\additionalTypes\\";
//         
//         // Проходимся по всем файлам .xsd в директории и подключаем их
//         foreach (string file in Directory.GetFiles(additionalTypesPath, "*.xsd"))
//         {
//             schemas.Add(null, file);
//         }
//         
//         settings.Schemas = schemas;
//         settings.ValidationType = ValidationType.Schema;
//
//         settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
//         settings.ValidationEventHandler += ValidationEventHandler;
//
//         using var reader = XmlReader.Create("..\\..\\..\\..\\report-234465bb-992c-489c-a3be-54366ca54931-number-52-01[0].xml", settings);
//         XmlDocument document = new XmlDocument();
//         document.Load(reader);
//         document.Validate(ValidationEventHandler);
//         
//         // Обработка XML и сохранение данных в БД
//         SaveCadNumbersToDatabase(document);
//     }
//
//     static void ValidationEventHandler(object sender, ValidationEventArgs e)
//     {
//         if (e.Severity == XmlSeverityType.Error)
//         {
//             Console.WriteLine("Error: {0}", e.Message);
//         }
//         else if (e.Severity == XmlSeverityType.Warning)
//         {
//             Console.WriteLine("Warning: {0}", e.Message);
//         }
//     }
//     
//     static void SaveCadNumbersToDatabase(XmlDocument document)
//     {
//         string connectionString = "Host=localhost;Username=postgres;Password=nik09012002;Database=etl"; // Укажите свои данные для подключения
//
//         using (var connection = new NpgsqlConnection(connectionString))
//         {
//             connection.Open();
//
//             var namespaceManager = new XmlNamespaceManager(new NameTable());
//             // namespaceManager.AddNamespace("", ""); // Замените на нужный вам namespace, если это необходимо
//
//             var cadBlocks = document.SelectNodes("//land_records/land_record", namespaceManager);
//             foreach (XmlNode cadBlock in cadBlocks)
//             {
//                 var cadNumberNode = cadBlock.SelectSingleNode("object/common_data/cad_number", namespaceManager);
//                 if (cadNumberNode != null)
//                 {
//                     var cadNumber = cadNumberNode.InnerText;
//
//                     // Сохранение в БД
//                     using (var cmd = new NpgsqlCommand("INSERT INTO registry.land_plot (cad_number) VALUES (@cad_number)", connection))
//                     {
//                         cmd.Parameters.AddWithValue("cad_number", cadNumber);
//                         cmd.ExecuteNonQuery();
//                     }
//                 }
//             }
//         }
//     }
// }