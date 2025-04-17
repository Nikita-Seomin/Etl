using Etl.DataStructures.Tree;

namespace Etl.DataStructures.Forest;

public class Forest<TId, TVal>
{
    public List<Tree<TId, TVal>> Trees { get; set; } = new();

    public Forest() {} // Пустой конструктор обязателен

    public void AddTree(Tree<TId, TVal> tree)
    {
        Trees.Add(tree);
    }
}
