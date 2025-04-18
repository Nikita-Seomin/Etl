﻿using System;
   using System.IO;
   using System.Xml.Linq;
   using Etl.DataStructures.Forest;
   using Etl.DataStructures.Tree;
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
       }
   }