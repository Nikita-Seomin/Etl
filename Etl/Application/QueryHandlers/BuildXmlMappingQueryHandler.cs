using System.Reflection;
using System.Xml;
using Etl.Application.Queries;
using Etl.Infrastructure.Ultilits;
using Etl.Infrastructure.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Etl.DataStructures.Tree;

public class BuildXmlMappingQueryHandler
{
    private const string _xmlFileName = @"D:\FILE_STORAGE\report-c56e1a2f-ad5b-4e84-bbe4-bb2a3f0f7721-BC-2025-04-03-145343-52-01[0].xml";
    
    public int BufLength { get; set; }

    public Tree<Guid, dynamic> Handle(BuildXmlMappingQuery query)
    {
        using var fileStream = new FileStream(_xmlFileName, FileMode.Open, FileAccess.Read, FileShare.Read, BufLength,
            FileOptions.SequentialScan);
        using var reader = XmlReader.Create(fileStream,
            new XmlReaderSettings() { IgnoreComments = true, IgnoreWhitespace = true });

        Dictionary<string, (int ObjectId, int TargetFieldId, bool isArrayElement)> dicTag2DbData =
            FileManager.LoadDictionaryStringKey("D:\\Работа\\dev\\Etl\\Etl\\FILE_STORAGE\\rgis\\dictionary.json");

        var classAttrs = dicTag2DbData.Values
            .Distinct()
            .GroupBy(x => x.ObjectId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.TargetFieldId).Distinct().ToList());

        var typeMap = DynamicClassBuilder.BuildClasses(classAttrs);

        var pathParts = new List<string>();
        var nodeStack = new Stack<(TreeNode<Guid, dynamic> node, int objectId)>();
        Tree<Guid, dynamic> tree = new(); // Итоговое дерево

        reader.MoveToContent();

        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                pathParts.Add(reader.Name);
                var currentPath = string.Join("\\", pathParts);

                if (dicTag2DbData.TryGetValue(currentPath, out var matchingEntry))
                {
                    var (objectId, targetFieldId, isArrayElement) = matchingEntry;

                    // Проверка условий создания нового объекта
                    if (isArrayElement)
                    {
                        // Создаем новый экземпляр класса
                        var type = typeMap[objectId];
                        dynamic obj = Activator.CreateInstance(type);

                        // Генерим уникальный Guid
                        Guid objId = Guid.NewGuid();
                        var newNode = new TreeNode<Guid, dynamic>(objId, obj);

                        if (nodeStack.Count == 0)
                        {
                            tree.Root = newNode; // root
                        }
                        else
                        {
                            nodeStack.Peek().node.Children.Add(newNode); // как child
                        }

                        nodeStack.Push((newNode, objectId));
                    }
                }

                reader.Read();
            }
            else if (reader.NodeType == XmlNodeType.EndElement)
            {
                if (pathParts.Count > 0)
                {
                    var currentPath = string.Join("\\", pathParts);
                    
                    // При возврате назад выходим из текущего объекта, если objectId совпадает
                    int objId = GetObjectId(dicTag2DbData, currentPath);
                    int fieldId = GetTargetFieldId(dicTag2DbData, currentPath);
                    bool isArrayElement = GetIsArrayElement(dicTag2DbData, currentPath);

                    if (nodeStack.Count > 0 && isArrayElement)
                    {
                        nodeStack.Pop();
                    }
                    pathParts.RemoveAt(pathParts.Count - 1);
                }

                reader.Read();
            }
            else if (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.CDATA)
            {
                // Положить значение в текущий объект (если есть текущий nodeStack)
                if (nodeStack.Count > 0)
                {
                    var curNode = nodeStack.Peek();
                    var curObj = curNode.node.Value;
                    var curObjectId = curNode.objectId;
                    
                    var currentPath = string.Join("\\", pathParts);
 
                    var fieldId = GetTargetFieldId(dicTag2DbData, currentPath);
                    var prop = curObj.GetType().GetField($"attr_{fieldId}_");

                    if (prop != null)
                        prop.SetValue(curObj, reader.Value);
                }

                reader.Read();
            }
            else
            {
                reader.Read();
            }
        }

        return tree;

        bool EndsWithS(string name) =>
            name.EndsWith("s", StringComparison.OrdinalIgnoreCase);

        int GetObjectId(Dictionary<string, (int ObjectId, int TargetFieldId, bool isArrayElement)> dic, string path)
        {
            if (dic.TryGetValue(path, out var val))
                return val.ObjectId;
            return 0;
        }

        int GetTargetFieldId(Dictionary<string, (int ObjectId, int TargetFieldId, bool isArrayElement)> dic, string path)
        {
            if (dic.TryGetValue(path, out var val))
                return val.TargetFieldId;
            return 0;
        }
        
        bool GetIsArrayElement(Dictionary<string, (int ObjectId, int TargetFieldId, bool isArrayElement)> dic, string path)
        {
            if (dic.TryGetValue(path, out var val))
                return val.isArrayElement;
            return false;
        }
    }
}