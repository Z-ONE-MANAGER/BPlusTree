using System;
using System.Collections.Generic;
using System.Text;

namespace BPlusTree
{
    public class BTreeNode<T> where T : class, IComparable
    {
        private readonly Guid identifier;
        private readonly int order;
        private readonly IPromotionListener<T> tree;

        /// <summary>
        /// 第一间隙
        /// </summary>
        private BTreeNodeElementInterstice<T> firstInterstice;

        public BTreeNode(int order, IPromotionListener<T> tree)
        {
            identifier = Guid.NewGuid();
            this.order = order;
            this.tree = tree;
        }

        /// <summary>
        /// 父节点
        /// </summary>
        public BTreeNode<T> Parent { private get; set; }

        private Guid Identifier
        {
            get { return identifier; }
        }

        /// <summary>
        /// 本节点下第一个间隙
        /// </summary>
        private BTreeNodeElementInterstice<T> FirstInterstice
        {
            get { return firstInterstice; }
        }

        /// <summary>
        /// 本节点下第一个元素
        /// </summary>
        private BTreeNodeElement<T> FirstElement
        {
            get
            {
                if (firstInterstice == null) return null;
                return firstInterstice.Larger;
            }
        }

        /// <summary>
        /// 本节点下的元素数量
        /// </summary>
        private int Count
        {
            get
            {
                if (firstInterstice == null) return 0;
                int count = 0;
                BTreeNodeElement<T> element = firstInterstice.Larger;
                while (element != null)
                {
                    ++count;
                    element = element.Next;
                }
                return count;
            }
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            BTreeNodeElement<T> start = FirstElement;

            while (start != null)
            {
                builder.Append(start + " - ");
                start = start.Next;
            }
            builder.Append(string.Format("self = {0}, parent = {1}", identifier,
                                         Parent == null ? "NULL" : Parent.Identifier.ToString()));
            builder.AppendLine();
            BTreeNodeElementInterstice<T> startInterstice = FirstInterstice;
            while (startInterstice != null)
            {
                if (!(startInterstice.NodePointer is NullBTreeNode<T>))
                    builder.AppendLine(startInterstice.NodePointer.ToString());
                startInterstice = startInterstice.Next;
            }

            return builder.ToString();
        }

        public void Insert(T value)
        {
            //创建一个元素
            var element = new BTreeNodeElement<T>(value);
            //前隙
            element.PreviousInterval = new BTreeNodeElementInterstice<T>(null, element);
            //后隙
            element.NextInterval = new BTreeNodeElementInterstice<T>(element, null);

            InternalInsert(element);
        }

        private void Insert(BTreeNodeElement<T> element)
        {
            var copiedElement = new BTreeNodeElement<T>(element.Value);
            copiedElement.PreviousInterval = new BTreeNodeElementInterstice<T>(element.PreviousInterval.NodePointer,
                                                                               null, copiedElement);
            copiedElement.NextInterval = new BTreeNodeElementInterstice<T>(element.NextInterval.NodePointer,
                                                                           copiedElement,
                                                                           null);

            copiedElement.PreviousInterval.NodePointer.Parent = copiedElement.NextInterval.NodePointer.Parent = this;
            InternalInsert(copiedElement);
        }

        /// <summary>
        /// 元素插入
        /// </summary>
        /// <param name="element"></param>
        private void InternalInsert(BTreeNodeElement<T> element)
        {
            //执行元素插入
            InsertInCurrentNode(element);

            //若是节点数大于Order，则进行分裂操作
            if (Count <= order) return;

            //执行分裂的元素下标
            int indexOfValueToPromote = (int)Math.Round((double)Count / 2) - 1;

            //获取执行分裂的元素
            BTreeNodeElement<T> elementToPromote = ElementAt(indexOfValueToPromote);

            //从指定元素进行分裂，并返回新节点
            BTreeNode<T> node = DissectedNodeFrom(elementToPromote.Next);

            //若本节点没有子节点【第一间隙没有下级节点】
            if (!(FirstInterstice.NodePointer is NullBTreeNode<T>))
            {
                //将指向右侧元素的指针置空
                elementToPromote.Previous.BreakForwardLink();
            }
            //将右隙置空
            elementToPromote.TerminateForwardLink();

            //新建一个元素
            var promotedReplica = new BTreeNodeElement<T>(elementToPromote.Value);

            Promote(promotedReplica, this, node);
        }

        /// <summary>
        /// 将元素晋升
        /// </summary>
        /// <param name="elementToPromote"></param>
        /// <param name="smallerNode"></param>
        /// <param name="largerNode"></param>
        private void Promote(BTreeNodeElement<T> elementToPromote, BTreeNode<T> smallerNode, BTreeNode<T> largerNode)
        {
            if (Parent == null)
            {
                Parent = new BTreeNode<T>(order, tree);
                tree.RootIs(Parent);
            }

            //将本元素归于父节点
            elementToPromote.NextInterval.Container = elementToPromote.PreviousInterval.Container = Parent;
            elementToPromote.PreviousInterval.NodePointer = smallerNode;
            elementToPromote.NextInterval.NodePointer = largerNode;
            Parent.Insert(elementToPromote);
        }

        /// <summary>
        /// 从指定元素进行分裂
        /// 将包括本元素以内的所有元素移入新建的一个节点
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private BTreeNode<T> DissectedNodeFrom(BTreeNodeElement<T> element)
        {
            BTreeNodeElement<T> start = element;
            var node = new BTreeNode<T>(order, tree);
            while (start != null)
            {
                node.Insert(start);
                start = start.Next;
            }
            return node;
        }

        /// <summary>
        /// 根据下标找到对应的元素
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private BTreeNodeElement<T> ElementAt(int index)
        {
            BTreeNodeElement<T> start = FirstElement;
            int i = 0;
            while (i < index)
            {
                //start = start.Next;
                start = start.NextInterval.Larger;
                ++i;
            }
            return start;
        }

        /// <summary>
        /// 将元素值插入当前节点
        /// </summary>
        /// <param name="element"></param>
        private void InsertInCurrentNode(BTreeNodeElement<T> element)
        {
            if (firstInterstice == null)
            {
                //若第一间隙为空，证明该节点为新建节点，该元素为第一个元素，所以初始化节点的第一间隙
                element.PreviousInterval.Smaller = null;
                element.NextInterval.Larger = null;
                firstInterstice = element.PreviousInterval;
                element.PreviousInterval.Container = element.NextInterval.Container = this;
                return;
            }

            //找到符合条件的间隙
            BTreeNodeElementInterstice<T> insertionInterstice = IntersticeToSearch(element.Value);

            //替换指针
            if (insertionInterstice.Smaller != null)
            {
                insertionInterstice.Smaller.NextInterval = element.PreviousInterval;
            }
            if (insertionInterstice.Larger != null)
            {
                insertionInterstice.Larger.PreviousInterval = element.NextInterval;
            }
            element.PreviousInterval.Smaller = insertionInterstice.Smaller;
            element.NextInterval.Larger = insertionInterstice.Larger;

            if (insertionInterstice == firstInterstice) firstInterstice = element.PreviousInterval;
        }

        /// <summary>
        /// 找寻适合插入该元素的节点
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public BTreeNode<T> Find(T value)
        {
            //当前节点是否含有该元素
            BTreeNodeElement<T> node = ElementContaining(value);
            if (node != null) return this;

            //寻找符合该元素存放的间隙
            BTreeNodeElementInterstice<T> interstice = IntersticeToSearch(value);

            //如果该间隙为空，证明本节点未满，直接返回进行添加即可
            if (interstice == null || interstice.NodePointer is NullBTreeNode<T>) return this;

            //否则进入下一层递归遍历
            return interstice.NodePointer.Find(value);
        }

        public BTreeNodeElement<T> Search(T value)
        {
            BTreeNodeElement<T> element = ElementContaining(value);
            if (element != null) return element;
            BTreeNodeElementInterstice<T> interstice = IntersticeToSearch(value);
            if (interstice == null || interstice.NodePointer is NullBTreeNode<T>) return new NullBTreeNodeElement<T>();
            return interstice.NodePointer.Search(value);
        }

        /// <summary>
        /// 在本节点内寻找元素
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private BTreeNodeElement<T> ElementContaining(T value)
        {
            if (firstInterstice == null) return null;
            BTreeNodeElement<T> element = firstInterstice.Larger;
            while (element != null)
            {
                if (element.Value.Equals(value)) return element;
                element = element.Next;
            }
            return null;
        }

        /// <summary>
        /// 寻找符合条件的间隙
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private BTreeNodeElementInterstice<T> IntersticeToSearch(T value)
        {
            //第一个间隙
            BTreeNodeElementInterstice<T> counter = firstInterstice;

            //遍历所有非空间隙，直到找到符合条件的间隙
            while (counter != null)
            {
                if (counter.Smaller == null && counter.Larger != null && counter.Larger.Value.CompareTo(value) > 0 ||
                     counter.Smaller != null && counter.Larger == null && counter.Smaller.Value.CompareTo(value) < 0 ||
                     counter.Smaller != null && counter.Larger != null && counter.Smaller.Value.CompareTo(value) < 0 &&
                     counter.Larger.Value.CompareTo(value) > 0)
                    return counter;
                counter = counter.Next;
            }
            return null;
        }


    }
}