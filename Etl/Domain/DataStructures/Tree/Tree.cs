namespace Etl.DataStructures.Tree
{
    public class Tree<TId, TVal>
    {
        public TreeNode<TId, TVal> Root { get; set; }

        public Tree() {} // Пустой конструктор обязателен

        public Tree(TId rootId, TVal rootValue)
        {
            Root = new TreeNode<TId, TVal>(rootId, rootValue);
        }

        public void AddChild(TreeNode<TId, TVal> parent, TId childId, TVal childValue)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            var childNode = new TreeNode<TId, TVal>(childId, childValue);
            parent.Children.Add(childNode);
        }

        public TreeNode<TId, TVal> FindNodeById(TId id)
        {
            return FindNodeById(Root, id);
        }

        public TreeNode<TId, TVal> FindNodeById(TreeNode<TId, TVal> node, TId id)
        {
            if (node == null)
                return null;

            // Сравнение по Id
            if (node.Id.Equals(id))
                return node;

            foreach (var child in node.Children)
            {
                var result = FindNodeById(child, id);
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}