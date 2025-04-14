using Etl.Infrastructure.Exceptions;

namespace Etl.Infrastructure.Ultilits;

using System;
using System.IO;
using System.Xml.Linq;

public class DirectoryManager
{
    private const string FileStorage = "FILE_STORAGE";

    /// <summary>
    /// Создает директорию по указанному пути. Если путь содержит переменные окружения, они будут раскрыты.
    /// </summary>
    /// <param name="newDirectoryName">Имя новой директории</param>
    /// <param name="additionalPath">Доп путь</param>
    public void CreateDirectory(string newDirectoryName, string additionalPath = "")
    {
        
        // Раскрываем переменные окружения в пути
        string? basePath = Environment.ExpandEnvironmentVariables(FileStorage);
        
        // Проверяем, установлена ли переменная окружения
        if (string.IsNullOrEmpty(basePath))
        {
            throw new EnvironmentVariableNotFoundException(basePath);
        }
        
        // Формируем полный путь к новой директории
        string fullPath = Path.Combine(basePath, additionalPath, newDirectoryName);

        // Проверяем, существует ли уже директория
        if (!Directory.Exists(fullPath))
        {
            // Создаем директорию
            Directory.CreateDirectory(fullPath);
            // Console.WriteLine($"Директория создана: {expandedPath}");
        }
    }

    /// <summary>
    /// Удаляет директорию по указанному пути. Если путь содержит переменные окружения, они будут раскрыты.
    /// </summary>
    /// <param name="newDirectoryName">Имя новой директории</param>
    /// <param name="additionalPath">Доп путь</param>
    public static void DeleteDirectory(string newDirectoryName, string additionalPath = "")
    {
        // Раскрываем переменные окружения в пути
        string? basePath = Environment.ExpandEnvironmentVariables(FileStorage);
        
        // Проверяем, установлена ли переменная окружения
        if (string.IsNullOrEmpty(basePath))
        {
            throw new EnvironmentVariableNotFoundException(basePath);
        }
        
        // Формируем полный путь к новой директории
        string fullPath = Path.Combine(basePath, additionalPath, newDirectoryName);
        
        if (Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, true);
            // Console.WriteLine($"Директория {path} и её содержимое успешно удалены.");
        }
    }

    /// <summary>
    /// Создает или пересоздает директорию по указанному пути. Если путь содержит переменные окружения, они будут раскрыты.
    /// </summary>
    /// <param name="newDirectoryName">Имя новой директории</param>
    /// <param name="additionalPath">Доп путь</param>
    public static void CreateOrRecreateDirectory(string newDirectoryName, string additionalPath = "")
    {
        // Раскрываем переменные окружения в пути
        string? basePath = Environment.ExpandEnvironmentVariables(FileStorage);
        
        // Проверяем, установлена ли переменная окружения
        if (string.IsNullOrEmpty(basePath))
        {
            throw new EnvironmentVariableNotFoundException(basePath);
        }
        
        // Формируем полный путь к новой директории
        string fullPath = Path.Combine(basePath, additionalPath, newDirectoryName);
        
        if (Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, true);
            // Console.WriteLine($"Существующая директория {path} удалена.");
        }

        Directory.CreateDirectory(fullPath);
        // Console.WriteLine($"Директория {expandedPath} создана.");
    }
}