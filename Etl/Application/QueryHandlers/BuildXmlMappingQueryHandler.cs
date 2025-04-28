using System.Reflection;
using System.Xml;
using Etl.Application.Queries;
using Etl.Infrastructure.Ultilits;
using Etl.Infrastructure.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Etl.DataStructures.Tree;
using Etl.Domain.DataStructures.Stack;

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

        Dictionary<Stack<string>, (int ObjectId, int TargetFieldId)> dicTag2DbData =
            FileManager.LoadDictionary("D:\\Работа\\dev\\Etl\\Etl\\FILE_STORAGE\\rgis\\dictionary.json");

        var classAttrs = dicTag2DbData.Values
            .Distinct()
            .GroupBy(x => x.ObjectId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.TargetFieldId).Distinct().ToList());

        var typeMap = DynamicClassBuilder.BuildClasses(classAttrs);

        var pathStack = new Stack<string>();
        var nodeStack = new Stack<(TreeNode<Guid, dynamic> node, int objectId)>();
        Tree<Guid, dynamic> tree = new(); // Итоговое дерево

        reader.MoveToContent();

        while (!reader.EOF)
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                pathStack.Push(reader.Name);

                // Копия пути для поиска в словаре
                var currentPath = new Stack<string>(pathStack);

                if (dicTag2DbData.TryGetValue(currentPath, out var matchingEntry))
                {
                    var (objectId, targetFieldId) = matchingEntry;

                    // Проверка условий создания нового объекта
                    if (targetFieldId == 0 && objectId > 0)
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
                if (pathStack.Count > 0)
                {
                    // Копия пути для поиска в словаре
                    var currentPath = new Stack<string>(pathStack);
                    
                    // При возврате назад выходим из текущего объекта, если objectId совпадает
                    int objId = GetObjectId(dicTag2DbData, currentPath);
                    int fieldId = GetTargetFieldId(dicTag2DbData, currentPath);

                    if (nodeStack.Count > 0 && nodeStack.Peek().objectId == objId && fieldId == 0)
                    {
                        nodeStack.Pop();
                    }
                    pathStack.Pop();
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
                    
                    // Копия пути для поиска в словаре
                    var currentPath = new Stack<string>(pathStack);
 
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

        bool IsSuitablePath(Stack<string> dicPath, Stack<string> xmlPath)
        {
            // сравнение путей (или попроще — равно ли содержимое)
            return dicPath.SequenceEqual(xmlPath);
        }

        bool EndsWithS(string name) =>
            name.EndsWith("s", StringComparison.OrdinalIgnoreCase);

        bool PreviousLevelEndsWithS(Stack<string> pathStack)
        {
            if (pathStack.Count < 2) return false;
            var arr = pathStack.ToArray();
            return arr[1].EndsWith("s", StringComparison.OrdinalIgnoreCase);
        }

        int GetObjectId(Dictionary<Stack<string>, (int ObjectId, int TargetFieldId)> dic, IEnumerable<string> path)
        {
            var keyStack = new Stack<string>(path.Reverse());
            if (dic.TryGetValue(keyStack, out var val))
                return val.ObjectId;
            return 0;
        }

        int GetTargetFieldId(Dictionary<Stack<string>, (int ObjectId, int TargetFieldId)> dic, IEnumerable<string> path)
        {
            var keyStack = new Stack<string>(path.Reverse());
            if (dic.TryGetValue(keyStack, out var val))
                return val.TargetFieldId;
            return 0;
        }
    }
}