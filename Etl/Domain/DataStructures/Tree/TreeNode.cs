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
    
    
    // Добавляет узел-ребёнок к текущему узлу
    // public void AddChild(TreeNode<TId, TVal> child)
    // {
    //     if (child == null)
    //         throw new ArgumentNullException(nameof(child));
    //
    //     // Не допускаем дублирование
    //     if (!Children.Contains(child))
    //     {
    //         Children.Add(child);
    //     }
    // }
}