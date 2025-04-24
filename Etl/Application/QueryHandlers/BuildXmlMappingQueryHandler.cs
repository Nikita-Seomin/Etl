using System.Reflection;
using System.Xml;
using Etl.Application.Queries;
using Etl.Infrastructure.Ultilits;
using Etl.Infrastructure.Utilities;

public class BuildXmlMappingQueryHandler
{
    private const string _xmlFileName = @"D:\FILE_STORAGE\report-c56e1a2f-ad5b-4e84-bbe4-bb2a3f0f7721-BC-2025-04-03-145343-52-01[0].xml";
    
    public int BufLength { get; set; }

    public void Handle(BuildXmlMappingQuery query)
    {
        using var fileStream = new FileStream(_xmlFileName, FileMode.Open, FileAccess.Read, FileShare.Read, BufLength,
            FileOptions.SequentialScan);
        using var reader = XmlReader.Create(fileStream,
            new XmlReaderSettings() { IgnoreComments = true, IgnoreWhitespace = true });

        // Загрузка словаря маппинга
        Dictionary<Stack<string>, (int ObjectId, int TargetFieldId)> dicTag2DbData =
            FileManager.LoadDictionary("D:\\Работа\\dev\\Etl\\Etl\\FILE_STORAGE\\rgis\\dictionary.json");

        // Построение динамических классов
        var classAttrs = dicTag2DbData.Values
            .Distinct()
            .GroupBy(x => x.ObjectId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.TargetFieldId).Distinct().ToList());

        var typeMap = DynamicClassBuilder.BuildClasses(classAttrs);

        // Структуры для построения дерева
        Stack<string> currentPath = new Stack<string>();
        Stack<dynamic> objectStack = new Stack<dynamic>();
        Stack<int> objectIdStack = new Stack<int>();
        Stack<string> objectCreationTags = new Stack<string>(); // Для отслеживания тегов, по которым создавались объекты
        dynamic rootObject = null;
        dynamic currentObject = null;
        int? currentObjectId = null;
        string currentElementName = null;

        reader.MoveToContent();
        while (reader.Read())
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    currentElementName = reader.Name;
                    currentPath.Push(currentElementName);

                    // Проверяем соответствие текущего пути с ключами словаря
                    // var matchingEntry = dicTag2DbData.TryGetValue(currentPath, out var matchingEntry);
                    
                    if (dicTag2DbData.TryGetValue(currentPath, out var matchingEntry))
                    {
                        var (objectId, targetFieldId) = matchingEntry;

                        // Условие для создания нового объекта
                        if (targetFieldId == 0 && objectId > 0 && 
                            (currentObjectId == null || objectId != currentObjectId))
                        {
                            CreateNewObject(typeMap, objectId, ref currentObject, ref currentObjectId, 
                                objectStack, objectIdStack, objectCreationTags, currentElementName, ref rootObject);
                        }
                    }
                    break;

                case XmlNodeType.EndElement:
                    if (currentPath.Count > 0)
                    {
                        string closingTag = currentPath.Pop();
                        
                        // Проверяем, закрывается ли тег, по которому был создан текущий объект
                        if (objectCreationTags.Count > 0 && closingTag == objectCreationTags.Peek())
                        {
                            objectCreationTags.Pop();
                            
                            // Возвращаемся к родительскому объекту
                            if (objectStack.Count > 0)
                            {
                                objectStack.Pop();
                                objectIdStack.Pop();
                                
                                if (objectStack.Count > 0)
                                {
                                    currentObject = objectStack.Peek();
                                    currentObjectId = objectIdStack.Peek();
                                }
                                else
                                {
                                    currentObject = null;
                                    currentObjectId = null;
                                }
                            }
                        }
                    }
                    break;

                case XmlNodeType.Text:
                    if (currentObject != null && !string.IsNullOrEmpty(currentElementName))
                    {
                        // Ищем поле для заполнения
                        ;
                        
                        if (dicTag2DbData.TryGetValue(currentPath, out var fieldEntry) && fieldEntry.TargetFieldId > 0)
                        {
                            string fieldName = $"attr_{fieldEntry.TargetFieldId}_";
                            SetDynamicField(currentObject, fieldName, reader.Value);
                        }
                    }
                    break;
            }
        }
        Console.Write("dawd");
        // Возвращаем результат - корневой объект дерева
        // Можно добавить логику для работы с rootObject
    }

    private void CreateNewObject(
        Dictionary<int, Type> typeMap, 
        int objectId,
        ref dynamic currentObject,
        ref int? currentObjectId,
        Stack<dynamic> objectStack,
        Stack<int> objectIdStack,
        Stack<string> objectCreationTags,
        string creationTag,
        ref dynamic rootObject)
    {
        if (typeMap.TryGetValue(objectId, out Type objectType))
        {
            var newObject = Activator.CreateInstance(objectType);
            
            // Добавляем новый объект как child к текущему
            if (currentObject != null)
            {
                AddChildToParent(currentObject, newObject);
            }
            else
            {
                rootObject = newObject;
            }

            // Обновляем текущие ссылки
            currentObject = newObject;
            currentObjectId = objectId;
            objectStack.Push(currentObject);
            objectIdStack.Push(objectId);
            objectCreationTags.Push(creationTag);
        }
    }

    private void AddChildToParent(dynamic parent, dynamic child)
    {
        try
        {
            var parentType = (Type)parent.GetType();
            
            // Ищем подходящее свойство-коллекцию
            foreach (var prop in parentType.GetProperties())
            {
                if (prop.PropertyType.IsGenericType && 
                    prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var itemType = prop.PropertyType.GetGenericArguments()[0];
                    if (itemType == child.GetType())
                    {
                        var collection = prop.GetValue(parent) ?? 
                                       Activator.CreateInstance(prop.PropertyType);
                        collection.Add(child);
                        prop.SetValue(parent, collection);
                        return;
                    }
                }
            }
            
            // Ищем обычное свойство подходящего типа
            foreach (var prop in parentType.GetProperties())
            {
                if (prop.PropertyType == child.GetType())
                {
                    prop.SetValue(parent, child);
                    return;
                }
            }
            
            // Если не нашли подходящее свойство, создаем новое List<ChildType>
            var listType = typeof(List<>).MakeGenericType(child.GetType());
            var newList = Activator.CreateInstance(listType);
            newList.Add(child);
            
            // Ищем подходящее имя для новой коллекции
            string childName = child.GetType().Name.ToLower() + "s";
            var newProp = parentType.GetProperty(childName) ?? 
                         parentType.GetProperties().FirstOrDefault(p => p.PropertyType == listType);
            
            if (newProp != null)
            {
                newProp.SetValue(parent, newList);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при добавлении child к parent: {ex.Message}");
        }
    }

    private void SetDynamicField(dynamic obj, string fieldName, string value)
    {
        try
        {
            var field = ((Type)obj.GetType()).GetField(fieldName, 
                BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при установке поля {fieldName}: {ex.Message}");
        }
    }
}