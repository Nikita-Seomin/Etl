﻿using System.Xml.Serialization;
using Etl.Domain.Entities; // для MappingRecord
using Etl.DataStructures.Tree;
using Etl.Domain.DataStructures.Stack;

namespace Etl.DataStructures.Forest;

// Сохранение
public static class ForestSerializer
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
    
    public static Dictionary<Stack<string>, (int, int)> BuildSourcePathToTargetIdMap(Forest<int, MappingRecord> forest)
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
        
        var result = new Dictionary<Stack<string>, (int, int)>(new StackComparer<string>());
        
        var eachNodePath = new Dictionary<Stack<string>, (int, int)>(new StackComparer<string>());
        
        // Для кеша уже построенных путей
        var pathToNodeId = new Dictionary<int, Stack<string>>();

        // 3. Обход всех деревьев (на корнях смотрим ParentId...)
        foreach (var tree in sortedTrees)
        {
            
            // Может понадобиться путь от родительского дерева
            Stack<string>? basePath = null;
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
                basePath = new Stack<string>();
            }

            // 4. Обходим дерево, но начальный path — не пустой, а тот что нашли
            TraverseWithBasePath(tree.Root, new Stack<string>(basePath.Reverse()), result, pathToNodeId);
        }

        return result;
    }
    
    // --- Собирает все узлы дерева по id ---
    private static void CollectAllNodes(TreeNode<int, MappingRecord> node, Dictionary<int, TreeNode<int, MappingRecord>> dict)
    {
        if (node == null) return;
        dict[node.Value.Id] = node; // или то поле, которое хранит id-кандидата
        foreach (var ch in node.Children)
            CollectAllNodes(ch, dict);
    }
    

    // --- Строит путь (Stack) из SourceColumn-ов от корня дерева до этого узла ---
    private static Stack<string> BuildPathToNode(
        Dictionary<int, TreeNode<int, MappingRecord>> nodeById,
        TreeNode<int, MappingRecord> node)
    {
        var path = new Stack<string>();
        var current = node;

        // Идём к родителю через. Parent, если есть (если нет Parent property — сложнее!)
        while (current != null)
        {
            path.Push(current.Value.SourceColumn);
            // Здесь вопрос, как из node дойти до родителя? В структуре TreeNode нет BackReference!
            // Решение: на этапе CollectAllNodes собрать также Dictionary<NodeId, ParentNodeId>
            // Пусть сейчас возьмём через node.Value.ParentId если реальный parent-ид доступен
            // (если node ссылается сам на себя — остановим цикл)
            if (current.Value.ParentId == null || current.Value.ParentId == current.Value.Id)
                break;

            if (!nodeById.TryGetValue(current.Value.ParentId.Value, out var parent))
                break; // нельзя идти дальше (конечно, лучше throw ex)

            current = parent;
        }
        return path;
    }

    
    // --- Рекурсивный обход, где путь стартует не пустым, а с какой-то базой ---
    private static void TraverseWithBasePath(
        TreeNode<int, MappingRecord> node,
        Stack<string> path,
        Dictionary<Stack<string>, (int, int)> dict,
        Dictionary<int, Stack<string>> pathToNodeId)
    {
        if (node == null) return;
        path.Push(node.Value.SourceColumn);

        // Запоминаем: путь до node.Value.Id = (копия текущего стека), чтобы другие деревья могли продолжать
        var stackCopy = new Stack<string>(path.Reverse());
        pathToNodeId[node.Value.Id] = stackCopy;

        if (node.Value.ElementTypeId != "array")
        {
            dict.Add(stackCopy, (node.Value.ObjectId.Value, node.Value.TargetFieldId.Value));
        }
        // для массива логика такова: т.к. в xml <cadastral_bloks><cadastral_blok>...</cadastral_blok></cadastral_bloks> массивом может быть
        // и </cadastral_bloks> , и <cadastral_blok>, то возьмем в словарь текущую ноду - если у нее > 1 ребенок element, или ребенка element при = 1
        else if (node.Value.ElementTypeId == "array")
        {
            var countClindren = 0;
            // Считаем колличество детей с типой element
            foreach (var child in node.Children)
            {
                if (child.Value.ElementTypeId == "array")
                {
                    countClindren = 2;
                    break;
                }
                if (child.Value.ElementTypeId == "element")
                { 
                    countClindren = countClindren + 1;
                    if (countClindren > 1)
                    {
                        break;
                    }
                }
            }
            
            //копируем путь
            var stackCopyChild = new Stack<string>(path.Reverse());
            // если детей с типом element много - добавляем в словарь текущую ноду
            if (countClindren > 1)
            {
                stackCopyChild.Push(node.Value.SourceColumn);
                dict.Add(stackCopyChild, (node.Value.ObjectId ?? 0, node.Value.TargetFieldId ?? 0));
            }
            else // если ребенок с типом element один - добавляем его в словарь
            {
                foreach (var child in node.Children)
                {
                    // добавить детей
                    if (child.Value.ElementTypeId == "element")
                    {
                        stackCopyChild.Push(child.Value.SourceColumn);
                        dict.Add(stackCopyChild, (node.Value.ObjectId ?? 0, child.Value.TargetFieldId ?? 0));
                        break;
                    }
                }
            }
        }

        // Рекурсивно потомки
        foreach (var child in node.Children)
            TraverseWithBasePath(child, path, dict, pathToNodeId);

        path.Pop();
    }
}


