using Etl.Application.Queries;
using Etl.DataStructures.Forest;
using Etl.Domain.Entities;
using Etl.Infrastructure.Repositories;
using Etl.Infrastructure.Utilities;
using Wolverine;


namespace Etl.Application.QueryHandlers
{
    public class BuildForestQueryHandler //: IQueryHandler<BuildForestQuery, Forest<int, MappingRecord>>
    {
        private readonly IMappingRecordRepository _repository;

        public BuildForestQueryHandler(IMappingRecordRepository repository)
        {
            _repository = repository;
        }

        public Forest<int, MappingRecord> Handle(BuildForestQuery query)
        {
            var records = _repository.GetMappingRecords(query.ConnectionParams);
            var forest = new Forest<int, MappingRecord>();

            foreach (var record in records)
            {
                if (record.ElementTypeId == "array")
                {
                    var tree = new DataStructures.Tree.Tree<int, MappingRecord>(record.Id, record);
                    forest.AddTree(tree);
                }
                else
                {
                    foreach (var tree in forest.Trees)
                    {
                        if (record.ParentId == null) continue;

                        var parentNode = tree.FindNodeById((int)record.ParentId);
                        if (parentNode != null)
                        {
                            tree.AddChild(parentNode, record.Id, record);
                            break;
                        }
                    }
                }
            }
            
            // Сохраняем лес в XML-файлы
            var fileManager = new FileManager();
            // передаем лес и имя папки куда сохраним структуру = имя БД
            fileManager.SaveForestToXmlFiles(forest, query.ConnectionParams.Database );

            return forest;
        }
    }
}