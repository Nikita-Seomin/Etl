using Etl.Application.Queries;
using Etl.DataStructures.Forest;
using Etl.DataStructures.Tree;
using Etl.Domain.Entities;
using Etl.Infrastructure.Repositories;
using Etl.Infrastructure.Utilities;
using Wolverine;


namespace Etl.Application.QueryHandlers
{
    public class BuildForestCommandHandler //: IQueryHandler<BuildForestQuery, Forest<int, MappingRecord>>
    {
        private readonly IMappingRecordRepository _repository;

        public BuildForestCommandHandler(IMappingRecordRepository repository)
        {
            _repository = repository;
        }

        public Forest<int, MappingRecord> Handle(BuildForestCommand command)
        {
            var records = _repository.GetMappingRecords(command.ConnectionParams).ToList();
            var forest = new Forest<int, MappingRecord>();

            // Индексируем все записи по Id для быстрого доступа
            var recordById = records.ToDictionary(r => r.Id);

            // Кэш для уже созданных узлов
            var nodeById = new Dictionary<int, TreeNode<int, MappingRecord>>();

            // Рекурсивная функция добавления узла и его родителей
            TreeNode<int, MappingRecord> EnsureNode(int id)
            {
                // Проверяем что создали узел
                if (nodeById.TryGetValue(id, out var node))
                    return node; // Уже создан
                
                // Получаем запись
                var record = recordById[id];
                
                // Если корень дерева
                if (record.ParentId == null || record.ElementTypeId == "array")
                {
                    var tree = new Tree<int, MappingRecord>(record.Id , record);
                    nodeById[id] = node;
                    forest.AddTree(tree);
                    return node;
                }
                
                // Не корень и еще нет => пытаемся присоединить к дереву.
                // Проверяем присоединили ли к дереву, если нет - значит нет родителя в дереве, надо присоединить родителя
                if (!tryAddNodeToTree(record))
                {
                    EnsureNode(record.ParentId.Value);
                    tryAddNodeToTree(record);
                }
                
                nodeById[id] = node;
                
                return node;
            }

            bool tryAddNodeToTree(MappingRecord record)
            {
                foreach (var tree in forest.Trees)
                {
                    var parentNode = tree.FindNodeById((int)record.ParentId);
                    if (parentNode != null)
                    {
                        tree.AddChild(parentNode, record.Id, record);
                        return true;
                    }
                }
                return false;
            }

            // Создаём все узлы (в том числе и их родителей)
            foreach (var record in records)
            {
                EnsureNode(record.Id);
            }

            // ForestSerializer.SaveToFile(forest, "forest.xml");
            var res = ForestSerializer.BuildSourcePathToTargetIdMap(forest);
            FileManager.SaveDictionary(res, "D:\\Работа\\dev\\Etl\\Etl\\FILE_STORAGE\\rgis\\dictionary.json");

            return forest;
        }
    }
}