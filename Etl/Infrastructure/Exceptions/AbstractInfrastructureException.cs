namespace Etl.Infrastructure.Exceptions;

using System;

public abstract class AbstractInfrastructureException : Exception
{
    /// <summary>
    /// Код статуса
    /// </summary>
    public int Status { get; protected set; } = 400;

    /// <summary>
    /// Код ошибки
    /// </summary>
    public string Code { get; protected set; } = "infrastructure_exception";

    /// <summary>
    /// Сообщение об ошибке
    /// </summary>
    public override string Message { get; }

    /// <summary>
    /// Параметры ошибки
    /// </summary>
    public object[] Params { get; }

    /// <summary>
    /// Тип ошибки
    /// </summary>
    public string Type { get; protected set; } = "system";

    /// <summary>
    /// Конструктор AbstractInfrastructureException
    /// </summary>
    /// <param name="message">Сообщение об ошибке</param>
    /// <param name="parameters">Параметры ошибки</param>
    protected AbstractInfrastructureException(string message = "", params object[] parameters)
        : base(message)
    {
        Message = message;
        Params = parameters ?? Array.Empty<object>();
    }
}