

namespace Etl.DataStructures.Tree;

using System.Xml.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Domain.Entities;


    // Сохранение
    public static class TreeSerializer
    {
        public static void SaveToFile<TId, TVal>(Tree<TId, TVal> tree, string filePath)
        {
            var serializer = new XmlSerializer(typeof(Tree<TId, TVal>));
            using var fs = new FileStream(filePath, FileMode.Create);
            serializer.Serialize(fs, tree);
        }

        public static Tree<TId, TVal> LoadFromFile<TId, TVal>(string filePath)
        {
            var serializer = new XmlSerializer(typeof(Tree<TId, TVal>));
            using var fs = new FileStream(filePath, FileMode.Open);
            return (Tree<TId, TVal>)serializer.Deserialize(fs);
        }

        // --- Код, который сформирует словарь ---
        public static Dictionary<Stack<string>, int> BuildSourcePathToTargetIdMap(Tree<int, MappingRecord> tree)
        {
            var result = new Dictionary<Stack<string>, int>(new StackComparer<string>());
            if (tree.Root != null)
            {
                Traverse(tree.Root, new Stack<string>(), result);
            }
            return result;
        }

        // Рекурсивный обход
        private static void Traverse(TreeNode<int, MappingRecord> node, Stack<string> path, Dictionary<Stack<string>, int> dict)
        {
            if (node == null)
                return;

            // Добавляем текущий SourceColumn в путь (вниз — push)
            path.Push(node.Value.SourceColumn);

            // Если TargetFieldId не пустой...
            if (node.Value.TargetFieldId.HasValue)
            {
                // Копируем Stack чтобы не тащить ссылку (иначе он мутируется далее по обходу)
                var pathCopy = new Stack<string>(path.Reverse()); // прямой путь (от корня к листу)
                dict.Add(pathCopy, node.Value.TargetFieldId.Value);
            }

            // Рекурсивно спускаемся в потомков
            foreach (var child in node.Children)
                Traverse(child, path, dict);

            // Возвращаемся вверх по стеку
            path.Pop();
        }

        // --- Сохранение в файл (JSON) ---
        public static void SavePathMappingDictionary(Dictionary<Stack<string>, int> dict, string filePath)
        {
            var list = dict.Select(kv => new PathMappingDto
            {
                Path = kv.Key.Reverse().ToList(),
                TargetFieldId = kv.Value
            }).ToList();

            var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        // --- Загрузка из файла ---
        public static Dictionary<Stack<string>, int> LoadPathMappingDictionary(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var list = JsonSerializer.Deserialize<List<PathMappingDto>>(json);

            var dict = new Dictionary<Stack<string>, int>(new StackComparer<string>());
            foreach (var dto in list)
            {
                var stack = new Stack<string>(dto.Path.Reverse<string>());
                dict.Add(stack, dto.TargetFieldId);
            }
            return dict;
        }

        // --- Для сравнения Stack при работе с Dictionary ---
        private class StackComparer<T> : IEqualityComparer<Stack<T>>
        {
            public bool Equals(Stack<T> x, Stack<T> y)
            {
                return x.SequenceEqual(y);
            }

            public int GetHashCode(Stack<T> obj)
            {
                unchecked
                {
                    int hash = 17;
                    foreach (var item in obj)
                        hash = hash * 23 + (item == null ? 0 : item.GetHashCode());
                    return hash;
                }
            }
        }
    }

    // DTO для сериализации одной записи
    public class PathMappingDto
    {
        public List<string> Path { get; set; }
        public int TargetFieldId { get; set; }
    }
