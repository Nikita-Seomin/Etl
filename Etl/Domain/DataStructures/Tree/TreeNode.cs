namespace Etl.DataStructures.Tree;

public class TreeNode<TId, TVal>
{
    public TId Id { get; set; }
    public TVal Value { get; set; }
    public List<TreeNode<TId, TVal>> Children { get; set; } = new();

    public TreeNode() {} // Пустой конструктор обязателен

    public TreeNode(TId id, TVal value)
    {
        Id = id;
        Value = value;
    }
}