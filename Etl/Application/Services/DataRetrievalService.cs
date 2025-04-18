﻿// /Services/DataRetrievalService.cs

using Etl.Data;
using Etl.DataStructures.Forest;
using Etl.DataStructures.Tree;
using Etl.Domain.Entities;

namespace Etl.Application.Services
{
    public class DataRetrievalService
    {
        private readonly DbConnectionParams _dbConnectionParams;

        public DataRetrievalService(DbConnectionParams dbConnectionParams)
        {
            _dbConnectionParams = dbConnectionParams;
        }

        public Forest<int, MappingRecord> BuildForest()
        {
            using (var dbContext = new DbContext(_dbConnectionParams))
            {
                var records = dbContext.GetMappingRecords();
                var forest = new Forest<int, MappingRecord>();
                Dictionary<int, int?> rootParents = new Dictionary<int, int?>();

                foreach (var record in records)
                {
                    // Создаем новое дерево для данного массива и добавляем его в лес
                    if (record.ElementTypeId == "array")
                    {
                        var tree = new Tree<int, MappingRecord>(record.Id, record); // Используем Id как корень дерева
                        forest.AddTree(tree);
                        rootParents.Add(record.Id, record.ParentId);
                    }
                    // Обработка для "element", "constant", "attribute"
                    else
                           
                    {
                        // Ищем подходящее дерево для текущего элемента
                        foreach (var tree in forest.Trees)
                        {
                            if (record.ParentId == null)
                                continue;
                            // Ищем родительскую ноду по ParentId
                            var parentNode = tree.FindNodeById((int)record.ParentId);
                            if (parentNode != null) // если в текущем дереве нет нужной ноды = null
                            {
                                // Добавляем текущую запись как дочерний узел к найденному родителю
                                tree.AddChild(parentNode, record.Id, record);
                                break; // Можно выйти из цикла, так как мы добавили запись
                            }
                        }
                    }
                }

                return forest;
            }
        }
        
        
        
    }
}