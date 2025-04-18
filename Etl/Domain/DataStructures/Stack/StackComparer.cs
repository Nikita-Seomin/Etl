namespace Etl.Domain.DataStructures.Stack;

public class StackComparer<T> : IEqualityComparer<Stack<T>>
{
    private readonly IEqualityComparer<T> _elemComparer;

    public StackComparer() : this(EqualityComparer<T>.Default) { }

    public StackComparer(IEqualityComparer<T> elemComparer)
    {
        _elemComparer = elemComparer;
    }

    public bool Equals(Stack<T>? x, Stack<T>? y)
    {
        if (ReferenceEquals(x, y))
            return true;
        if (x == null || y == null)
            return false;
        if (x.Count != y.Count)
            return false;

        // сравниваем последовательность, не разрушая Stack:
        return x.SequenceEqual(y, _elemComparer);
    }

    public int GetHashCode(Stack<T> obj)
    {
        if (obj == null)
            return 0;
            
        // Для стека, можно обойтись XOR элементов, для порядка — берем порядок
        // Не самый быстрый, но типовой вариант для коллекции:
        int hash = 17;
        foreach (var item in obj)
        {
            hash = hash * 31 + (_elemComparer?.GetHashCode(item) ?? 0);
        }
        return hash;
    }
}