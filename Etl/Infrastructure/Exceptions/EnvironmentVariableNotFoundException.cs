namespace Etl.Infrastructure.Exceptions;

using System;

public class EnvironmentVariableNotFoundException : AbstractInfrastructureException
{
    /// <summary>
    /// Имя переменной окружения
    /// </summary>
    public string VariableName { get; }

    /// <summary>
    /// Конструктор EnvironmentVariableNotFoundException
    /// </summary>
    /// <param name="variableName">Имя переменной окружения</param>
    public EnvironmentVariableNotFoundException(string variableName)
        : base($"Переменная окружения '{variableName}' не найдена.", variableName)
    {
        VariableName = variableName;
        Code = "environment_variable_not_found";
    }
}