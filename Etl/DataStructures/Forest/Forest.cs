using Etl.DataStructures.Tree;

namespace Etl.DataStructures.Forest;

public class Forest<TId, TVal>
{
    public List<Tree<TId, TVal>> Trees { get; set; }

    public Forest()
    {
        Trees = new List<Tree<TId, TVal>>();
    }

    public void AddTree(Tree<TId, TVal> tree)
    {
        Trees.Add(tree);
    }
}
