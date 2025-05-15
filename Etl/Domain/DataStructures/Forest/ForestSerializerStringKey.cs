using System.Xml.Serialization;
using Etl.Domain.Entities; // для MappingRecord
using Etl.DataStructures.Tree;
using System.Text;

namespace Etl.DataStructures.Forest;

// Сохранение
public static class ForestSerializerStringKey
{
    public static void SaveForestToFile<TId, TVal>(Forest<TId, TVal> forest, string filePath)
    {
        var serializer = new XmlSerializer(typeof(Forest<TId, TVal>));
        using var fs = new FileStream(filePath, FileMode.Create);
        serializer.Serialize(fs, forest);
    }

    public static Forest<TId, TVal> LoadForestFromFile<TId, TVal>(string filePath)
    {
        var serializer = new XmlSerializer(typeof(Forest<TId, TVal>));
        using var fs = new FileStream(filePath, FileMode.Open);
        return (Forest<TId, TVal>)serializer.Deserialize(fs);
    }
    
    public static Dictionary<string, (int, int, bool)> BuildSourcePathToTargetIdMap(Forest<int, MappingRecord> forest)
    {
        var nodeById = new Dictionary<int, TreeNode<int, MappingRecord>>();
        foreach (var tree in forest.Trees)
            CollectAllNodes(tree.Root, nodeById);

        // Соберём быстро: Id корня -> дерево
        var rootNodeIdToTree = forest.Trees.ToDictionary(t => t.Root.Value.Id);

        // 1. Граф зависимостей
        var treeDependencies = new Dictionary<Tree<int, MappingRecord>, HashSet<Tree<int, MappingRecord>>>();
        foreach (var tree in forest.Trees)
        {
            var rootParentId = tree.Root.Value.ParentId;
            if (rootParentId.HasValue && nodeById.TryGetValue(rootParentId.Value, out var parentNode))
            {
                Tree<int, MappingRecord>? parentTree = null;
                foreach (var t in forest.Trees)
                {
                    if (IsNodeInTree(t.Root, parentNode))
                    {
                        parentTree = t;
                        break;
                    }
                }
                if (parentTree != null && parentTree != tree)
                {
                    if (!treeDependencies.ContainsKey(tree))
                        treeDependencies[tree] = new HashSet<Tree<int, MappingRecord>>();
                    treeDependencies[tree].Add(parentTree);
                }
            }
            if (!treeDependencies.ContainsKey(tree))
                treeDependencies[tree] = new HashSet<Tree<int, MappingRecord>>();
        }
        
        
        bool IsNodeInTree(TreeNode<int, MappingRecord> root, TreeNode<int, MappingRecord> target)
        {
            if (root == target) return true;
            foreach (var child in root.Children)
                if (IsNodeInTree(child, target)) return true;
            return false;
        }

        // 2. Топологическая сортировка деревьев
        List<Tree<int, MappingRecord>> sortedTrees = new List<Tree<int, MappingRecord>>();
        var visited = new HashSet<Tree<int, MappingRecord>>();
        var inProcess = new HashSet<Tree<int, MappingRecord>>();
        
        void Visit(Tree<int, MappingRecord> t)
        {
            if (visited.Contains(t))
                return;
            if (inProcess.Contains(t))
                throw new Exception("Циклическая зависимость деревьев!");
            inProcess.Add(t);
            foreach (var depTree in treeDependencies[t])
                Visit(depTree);

            inProcess.Remove(t);
            visited.Add(t);
            sortedTrees.Add(t);
        }
        foreach (var tree in forest.Trees)
            Visit(tree);
        
        var result = new Dictionary<string, (int, int, bool)>();
        
        var eachNodePath = new Dictionary<string, (int, int)>();
        
        // Для кеша уже построенных путей
        var pathToNodeId = new Dictionary<int, string>();

        // 3. Обход всех деревьев (на корнях смотрим ParentId...)
        foreach (var tree in sortedTrees)
        {
            // Может понадобиться путь от родительского дерева
            string? basePath = null;
            var rootParentId = tree.Root.Value.ParentId;

            if (rootParentId.HasValue)
            {
                // Если уже есть, возьмем из кеша
                if (!pathToNodeId.TryGetValue(rootParentId.Value, out basePath))
                {
                    var s = nodeById.TryGetValue(rootParentId.Value, out var parentNode1);
                    // Построим путь до родительского узла (если не найден — ошибка структуры)
                    if (!nodeById.TryGetValue(rootParentId.Value, out var parentNode))
                        throw new Exception($"Не найден узел с Id = {rootParentId} (ParentId корня дерева)");

                    // Путь от корня его дерева до этого узла
                    basePath = BuildPathToNode(nodeById, parentNode); // функция ниже
                    pathToNodeId[rootParentId.Value] = basePath;
                }
            }
            else
            {
                basePath = string.Empty;
            }

            // 4. Обходим дерево, но начальный path — не пустой, а тот что нашли
            TraverseWithBasePath(tree.Root, basePath, result, pathToNodeId, rootNodeIdToTree);
        }

        return result;
    }
    
    // --- Собирает все узлы дерева по id ---
    private static void CollectAllNodes(TreeNode<int, MappingRecord> node, Dictionary<int, TreeNode<int, MappingRecord>> dict)
    {
        if (node == null) return;
        dict[node.Value.Id] = node;
        foreach (var ch in node.Children)
            CollectAllNodes(ch, dict);
    }
    

    // --- Строит путь (string) из SourceColumn-ов от корня дерева до этого узла ---
    private static string BuildPathToNode(
        Dictionary<int, TreeNode<int, MappingRecord>> nodeById,
        TreeNode<int, MappingRecord> node)
    {
        var pathParts = new List<string>();
        var current = node;

        while (current != null)
        {
            pathParts.Add(current.Value.SourceColumn);
            if (current.Value.ParentId == null || current.Value.ParentId == current.Value.Id)
                break;

            if (!nodeById.TryGetValue(current.Value.ParentId.Value, out var parent))
                break;

            current = parent;
        }
        
        pathParts.Reverse();
        return string.Join("\\", pathParts);
    }

    
    // --- Рекурсивный обход, где путь стартует не пустым, а с какой-то базой ---
    private static void TraverseWithBasePath(
        TreeNode<int, MappingRecord> node,
        string basePath,
        Dictionary<string, (int, int, bool)> dict,
        Dictionary<int, string> pathToNodeId,
        Dictionary<int, Tree<int, MappingRecord>> rootNodeIdToTree)
    {
        if (node == null) return;
        
        var currentPath = string.IsNullOrEmpty(basePath) 
            ? node.Value.SourceColumn 
            : $"{basePath}\\{node.Value.SourceColumn}";

        // Запоминаем путь до node.Value.Id
        pathToNodeId[node.Value.Id] = currentPath;
        
        bool isArrayElement = node.Value.ElementTypeId == "array" ? true : false;

        if (node.Value.ElementTypeId == "element" && node.Value.ObjectId.HasValue &&  node.Value.TargetFieldId.HasValue)
        {
            dict.Add(currentPath, (node.Value.ObjectId.Value, node.Value.TargetFieldId.Value, isArrayElement));
        }
        // для массива логика такова: т.к. в xml <cadastral_bloks><cadastral_blok>...</cadastral_blok></cadastral_bloks> массивом может быть
        // и </cadastral_bloks> , и <cadastral_blok>, то возьмем в словарь текущую ноду - если у нее > 1 ребенок element, или ребенка element при = 1
        else if (node.Value.ElementTypeId == "array")
        {
            var nodeTargetFieldId = node.Value.TargetFieldId.HasValue ? node.Value.TargetFieldId.Value : 0;
            var nodeObjectId = node.Value.ObjectId.HasValue ? node.Value.ObjectId.Value : 0;
            // Считаем количество детей с типом "element"
            int elementChildrenCount = 0;
            // Проверяем что ни один массив не ссылается на данную ноду , т.е. чуть ниже по вложенности не начинается сразу же новый подреестр
            foreach (var root in rootNodeIdToTree.Values)
            {
                if (root.Root.Value.ParentId.HasValue && 
                    root.Root.Value.ParentId.Value == node.Value.Id)
                {
                    elementChildrenCount = 2;
                    break;
                }
            }
            // Если на уровне ниже точно нет подреестра, то проверяем количество ссылающихся element
            if (elementChildrenCount == 0)
            {
                foreach (var child in node.Children)
                {
                    if (child.Value.ElementTypeId == "element")
                    {
                        elementChildrenCount++;
                        if (elementChildrenCount > 1)
                        {
                            break; // Нет смысла считать дальше, если уже больше одного
                        }
                    }
                }
            }
            // массив содержит массив или массив содержит много элементов или пустой массив - в словарь тек ноду
            if (elementChildrenCount > 1 || elementChildrenCount == 0)
            {
                // Если детей с типом "element" много - добавляем текущую ноду
                dict.Add(currentPath, (node.Value.ObjectId ?? nodeObjectId, node.Value.TargetFieldId ?? nodeTargetFieldId, isArrayElement));
            }
            else
            {
                // Если ребенок с типом "element" один - добавляем его
                foreach (var child in node.Children)
                {
                    if (child.Value.ElementTypeId == "element")
                    {
                        var childPath = $"{currentPath}\\{child.Value.SourceColumn}";
                        dict.Add(childPath, (node.Value.ObjectId ?? nodeObjectId, child.Value.TargetFieldId ?? nodeTargetFieldId, isArrayElement));
                        break; // Добавляем только первого подходящего ребенка
                    }
                }
            }
        }
        else if (string.IsNullOrEmpty(basePath))
        {
            dict.Add(currentPath, (node.Value.ObjectId ?? 2, node.Value.TargetFieldId ?? 0, isArrayElement));
        }

        // Рекурсивно потомки
        foreach (var child in node.Children)
            TraverseWithBasePath(child, currentPath, dict, pathToNodeId, rootNodeIdToTree);
    }
}