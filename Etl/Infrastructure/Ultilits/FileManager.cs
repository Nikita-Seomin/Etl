using System;
using System.Text.Json;
using System.Xml.Linq;
using Etl.DataStructures.Forest;
using Etl.DataStructures.Tree;
using Etl.Domain.DataStructures.Stack;
using Etl.Domain.Entities;
using Etl.Infrastructure.Ultilits;

namespace Etl.Infrastructure.Utilities
{
   public class FileManager
   {
       public void SaveForestToXmlFiles<TId, TVal>(Forest<TId, TVal> forest, string directoryPath)
           where TVal : IMappingRecord
       {
           if (forest == null)
               throw new ArgumentNullException(nameof(forest));

           if (string.IsNullOrWhiteSpace(directoryPath))
               throw new ArgumentException("Путь к директории не может быть пустым.", nameof(directoryPath));

           var dirMenger = new DirectoryManager();

           // Создаем директорию, если она не существует
           string fullPath = dirMenger.CreateDirectory(directoryPath);

           foreach (var tree in forest.Trees)
           {
               // Генерируем имя файла на основе идентификатора корневого узла
               string fileName = $"{tree.Root.Id}.xml";
               string filePath = Path.Combine(fullPath, fileName);

               // Преобразуем дерево в XML и сохраняем в файл
               XElement xmlTree = ConvertTreeToXml(tree);
               xmlTree.Save(filePath);
           }
       }

       private XElement ConvertTreeToXml<TId, TVal>(Tree<TId, TVal> tree)
           where TVal : IMappingRecord
       {
           if (tree == null)
               throw new ArgumentNullException(nameof(tree));

           return ConvertNodeToXml(tree.Root);
       }

       private XElement ConvertNodeToXml<TId, TVal>(TreeNode<TId, TVal> node)
           where TVal : IMappingRecord
       {
           if (node == null)
               throw new ArgumentNullException(nameof(node));

           // Создаем элемент XML для текущего узла
           var element = new XElement(node.Value.SourceColumn,
               new XAttribute("Id", node.Id));

           // Добавляем атрибут TargetFieldId только если он не равен null
           if (node.Value.TargetFieldId.HasValue)
           {
               element.Add(new XAttribute("TargetFieldId", node.Value.TargetFieldId.Value));
           }


           // Рекурсивно добавляем дочерние узлы
           foreach (var child in node.Children)
           {
               element.Add(ConvertNodeToXml(child));
           }

           return element;
       }
       
       public static void SaveDictionary(Dictionary<Stack<string>, (int, int)>? res, string filePath)
       {
           if(res == null)
               return;

           var data = res.Select(kvp => new DictEntry {
               StackValues = kvp.Key.Reverse().ToList(), // Stack сохраняем как List (снизу вверх)
               Item1 = kvp.Value.Item1,
               Item2 = kvp.Value.Item2
           }).ToList();

           var json = JsonSerializer.Serialize(data, new JsonSerializerOptions{ WriteIndented = true });
           File.WriteAllText(filePath, json);
       }
       
       
       public static Dictionary<Stack<string>, (int, int)> LoadDictionary(string filePath)
       {
           var json = File.ReadAllText(filePath);
           var data = JsonSerializer.Deserialize<List<DictEntry>>(json)!;
           var dict = new Dictionary<Stack<string>, (int, int)>(new StackComparer<string>());

           foreach(var entry in data)
           {
               // Восстановим Stack (создаем стек с элементов, начиная с низа)
               var stack = new Stack<string>(entry.StackValues!.Reverse<string>());
               dict[stack] = (entry.Item1, entry.Item2);
           }
           return dict;
       }
       
       public static void SaveDictionaryStringKey(Dictionary<string, (int, int, bool)>? res, string filePath)
       {
           if (res == null)
               return;

           var data = res.Select(kvp => new DictEntryStringKey {
               Path = kvp.Key,
               ObjectId = kvp.Value.Item1,  // First int in tuple (Item1)
               TargetFieldId = kvp.Value.Item2,  // Second int in tuple (Item2)
               IsArray = kvp.Value.Item3  // bool in tuple (Item3)
           }).ToList();

           var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
           File.WriteAllText(filePath, json);
       }

       public static Dictionary<string, (int, int, bool)> LoadDictionaryStringKey(string filePath)
       {
           var json = File.ReadAllText(filePath);
           var data = JsonSerializer.Deserialize<List<DictEntryStringKey>>(json)!;
           var dict = new Dictionary<string, (int, int,bool)>();

           foreach (var entry in data)
           {
               // Просто добавляем запись с строковым ключом
               dict[entry.Path!] = (entry.ObjectId, entry.TargetFieldId, entry.IsArray);
           }
           return dict;
       }
       
       
       
       
   }
   
   
   public class DictEntryStringKey
   {
       public string? Path { get; set; }  // Изменили название с StackValues на Path
       public int ObjectId  { get; set; }
       public int TargetFieldId { get; set; }
       public bool IsArray { get; set; }
   }
   
   
   public class DictEntry
   {
       public List<string>? StackValues { get; set; }
       public int Item1 { get; set; }
       public int Item2 { get; set; }
   }
}