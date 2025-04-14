namespace Etl.DataStructures.Tree;

public class TreeNode<TId, TVal>
{
    public TId Id { get; }
    public TVal Value { get; }
    public List<TreeNode<TId, TVal>> Children { get; } = new List<TreeNode<TId, TVal>>();

    public TreeNode(TId id, TVal value)
    {
        Id = id;
        Value = value;
    }
}