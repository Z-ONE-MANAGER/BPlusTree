using System;

namespace BPlusTree
{
    public class BTreeNodeElementInterstice<T> where T : class, IComparable
    {
        private BTreeNode<T> container;
        private BTreeNode<T> nodePointer;

        public BTreeNodeElementInterstice(BTreeNode<T> pointer, BTreeNodeElement<T> smaller, BTreeNodeElement<T> larger)
        {
            Smaller = smaller;
            Larger = larger;
            NodePointer = pointer;
        }

        public BTreeNodeElementInterstice(BTreeNodeElement<T> smaller, BTreeNodeElement<T> larger)
            : this(new NullBTreeNode<T>(), smaller, larger)
        {
        }

        /// <summary>
        /// 指向节点的指针
        /// </summary>
        public BTreeNode<T> NodePointer
        {
            get { return nodePointer; }
            set
            {
                nodePointer = value;
                if (nodePointer == null) return;
                nodePointer.Parent = container;
            }
        }

        /// <summary>
        /// 间隙左边的邻近元素
        /// </summary>
        public BTreeNodeElement<T> Smaller { get; set; }

        /// <summary>
        /// 间隙右边的邻近元素
        /// </summary>
        public BTreeNodeElement<T> Larger { get; set; }

        /// <summary>
        /// 下一个间隙
        /// </summary>
        public BTreeNodeElementInterstice<T> Next
        {
            get
            {
                if (Larger == null) return null;
                return Larger.NextInterval;
            }
        }

        /// <summary>
        /// 间隙的拥有者
        /// </summary>
        public BTreeNode<T> Container
        {
            set
            {
                container = value;
                if (nodePointer == null) return;
                nodePointer.Parent = container;
            }
        }
    }

    public class NullBTreeNode<T> : BTreeNode<T> where T : class, IComparable
    {
        public NullBTreeNode() : base(0, null)
        {
        }

        public override string ToString()
        {
            return string.Empty;
        }
    }
}