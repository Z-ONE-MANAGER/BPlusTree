using System;
using System.Collections.Generic;

namespace BPlusTree
{
    public class BTree<T> : IPromotionListener<T> where T : class, IComparable
    {
        private BTreeNode<T> root;

        public BTree(int order)
        {
            root = new BTreeNode<T>(order, this);
        }

        #region IPromotionListener<T> Members

        public void RootIs(BTreeNode<T> node)
        {
            root = node;
        }

        #endregion

        /// <summary>
        /// 添加元素
        /// </summary>
        /// <param name="value"></param>
        public void Insert(T value)
        {
            //找到适合插入元素的节点
            var node = root.Find(value);

            //指向插入操作
            node.Insert(value);
        }

        public override string ToString()
        {
            return root.ToString();
        }

        public BTreeNodeElement<T> Search(T searchKey)
        {
            return root.Search(searchKey);
        }
    }
}