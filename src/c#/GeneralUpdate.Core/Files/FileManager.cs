﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace GeneralUpdate.Core.Files
{
    public class FileManager<T> : IEnumerable<T> where T : IComparable
    {
        private readonly int maxKeysPerNode;
        private readonly int minKeysPerNode;
        internal FileNode<T> Root;

        /// <summary>
        /// Keep a reference of Bottom Left/Right Node
        /// for fast ascending/descending enumeration using Next pointer.
        /// See IEnumerable and IEnumerableDesc implementation at bottom.
        /// </summary>
        internal FileNode<T> BottomLeftNode;
        internal FileNode<T> BottomRightNode;

        public int Count { get; private set; }

        public FileManager(int maxKeysPerNode = 3)
        {
            if (maxKeysPerNode < 3) throw new Exception("Max keys per node should be atleast 3.");
            this.maxKeysPerNode = maxKeysPerNode;
            this.minKeysPerNode = maxKeysPerNode / 2;
        }

        /// <summary>
        /// Time complexity: O(1).
        /// </summary>
        public T Max
        {
            get
            {
                if (Root == null) return default(T);
                var maxNode = BottomRightNode;
                return maxNode.Keys[maxNode.KeyCount - 1];
            }
        }

        /// <summary>
        /// Time complexity: O(1).
        /// </summary>
        public T Min
        {
            get
            {
                if (Root == null) return default(T);
                var minNode = BottomLeftNode;
                return minNode.Keys[0];
            }
        }

        /// <summary>
        /// Time complexity: O(log(n)).
        /// </summary>
        public bool HasItem(T value)
        {
            return Find(Root, value) != null;
        }

        /// <summary>
        /// Find the given value node under given node
        /// </summary>
        private FileNode<T> Find(FileNode<T> node, T value)
        {
            //if leaf then its time to insert
            if (node.IsLeaf)
            {
                for (var i = 0; i < node.KeyCount; i++)
                {
                    if (value.CompareTo(node.Keys[i]) == 0)
                    {
                        return node;
                    }
                }
            }
            else
            {
                //if not leaf then drill down to leaf
                for (var i = 0; i < node.KeyCount; i++)
                {
                    //current value is less than new value
                    //drill down to left child of current value
                    if (value.CompareTo(node.Keys[i]) < 0)
                    {
                        return Find(node.Children[i], value);
                    }
                    //current value is grearer than new value
                    //and current value is last element 

                    if (node.KeyCount == i + 1)
                    {
                        return Find(node.Children[i + 1], value);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Time complexity: O(log(n)).
        /// </summary>
        public void Insert(T newValue)
        {
            if (Root == null)
            {
                Root = new FileNode<T>(maxKeysPerNode, null) { Keys = { [0] = newValue } };
                Root.KeyCount++;
                Count++;
                BottomLeftNode = Root;
                BottomRightNode = Root;
                return;
            }
            var leafToInsert = FindInsertionLeaf(Root, newValue);
            InsertAndSplit(ref leafToInsert, newValue, null, null);
            Count++;
        }

        /// <summary>
        /// Find the leaf node to start initial insertion.
        /// </summary>
        private FileNode<T> FindInsertionLeaf(FileNode<T> node, T newValue)
        {
            //if leaf then its time to insert
            if (node.IsLeaf) return node;
            //if not leaf then drill down to leaf
            for (var i = 0; i < node.KeyCount; i++)
            {
                //current value is less than new value
                //drill down to left child of current value
                if (newValue.CompareTo(node.Keys[i]) < 0)
                {
                    return FindInsertionLeaf(node.Children[i], newValue);
                }
                //current value is grearer than new value
                //and current value is last element 
                if (node.KeyCount == i + 1)
                {
                    return FindInsertionLeaf(node.Children[i + 1], newValue);
                }
            }
            return node;
        }

        /// <summary>
        /// Insert and split recursively up until no split is required
        /// </summary>
        private void InsertAndSplit(ref FileNode<T> node, T newValue,
            FileNode<T> newValueLeft, FileNode<T> newValueRight)
        {
            //add new item to current node
            //this increases the height of B+ tree by one by adding a new root at top
            if (node == null)
            {
                node = new FileNode<T>(maxKeysPerNode, null);
                Root = node;
            }

            //newValue have room to fit in this node
            //so just insert in right spot in asc order of keys
            if (node.KeyCount != maxKeysPerNode)
            {
                InsertToNotFullNode(ref node, newValue, newValueLeft, newValueRight);
                return;
            }

            //if node is full then split node
            //and then insert new median to parent.

            //divide the current node values + new Node as left and right sub nodes
            var left = new FileNode<T>(maxKeysPerNode, null);
            var right = new FileNode<T>(maxKeysPerNode, null);

            //connect leaves via linked list for faster enumeration
            if (node.IsLeaf) ConnectLeaves(node, left, right);

            //median of current Node
            var currentMedianIndex = node.GetMedianIndex();

            //init currentNode under consideration to left
            var currentNode = left;
            var currentNodeIndex = 0;

            //new Median also takes new Value in to Account
            var newMedian = default(T);
            var newMedianSet = false;
            var newValueInserted = false;

            //keep track of each insertion
            var insertionCount = 0;

            //insert newValue and existing values in sorted order
            //to left and right nodes
            //set new median during sorting
            for (var i = 0; i < node.KeyCount; i++)
            {
                //if insertion count reached new median
                //set the new median by picking the next smallest value
                if (!newMedianSet && insertionCount == currentMedianIndex)
                {
                    newMedianSet = true;
                    //median can be the new value or node.keys[i] (next node key)
                    //whichever is smaller
                    if (!newValueInserted && newValue.CompareTo(node.Keys[i]) < 0)
                    {
                        //median is new value
                        newMedian = newValue;
                        newValueInserted = true;
                        if (newValueLeft != null)
                        {
                            SetChild(currentNode, currentNode.KeyCount, newValueLeft);
                        }
                        //now fill right node
                        currentNode = right;
                        currentNodeIndex = 0;
                        if (newValueRight != null)
                        {
                            SetChild(currentNode, 0, newValueRight);
                        }
                        i--;
                        insertionCount++;
                        continue;
                    }
                    //median is next node
                    newMedian = node.Keys[i];
                    //now fill right node
                    currentNode = right;
                    currentNodeIndex = 0;
                    continue;
                }

                //pick the smaller among newValue and node.Keys[i]
                //and insert in to currentNode (left and right nodes)
                //if new Value was already inserted then just copy from node.Keys in sequence
                //since node.Keys is already in sorted order it should be fine
                if (newValueInserted || node.Keys[i].CompareTo(newValue) < 0)
                {
                    currentNode.Keys[currentNodeIndex] = node.Keys[i];
                    currentNode.KeyCount++;

                    //if child is set don't set again
                    //the child was already set by last newValueRight or last node
                    if (currentNode.Children[currentNodeIndex] == null)
                    {
                        SetChild(currentNode, currentNodeIndex, node.Children[i]);
                    }
                    SetChild(currentNode, currentNodeIndex + 1, node.Children[i + 1]);
                }
                else
                {
                    currentNode.Keys[currentNodeIndex] = newValue;
                    currentNode.KeyCount++;

                    SetChild(currentNode, currentNodeIndex, newValueLeft);
                    SetChild(currentNode, currentNodeIndex + 1, newValueRight);

                    i--;
                    newValueInserted = true;
                }
                currentNodeIndex++;
                insertionCount++;
            }
            //could be that thew newKey is the greatest 
            //so insert at end
            if (!newValueInserted)
            {
                currentNode.Keys[currentNodeIndex] = newValue;
                currentNode.KeyCount++;

                SetChild(currentNode, currentNodeIndex, newValueLeft);
                SetChild(currentNode, currentNodeIndex + 1, newValueRight);
            }
            if (node.IsLeaf)
            {
                InsertAt(right.Keys, 0, newMedian);
                right.KeyCount++;
            }
            //insert overflow element (newMedian) to parent
            var parent = node.Parent;
            InsertAndSplit(ref parent, newMedian, left, right);
        }

        /// <summary>
        /// Insert to a node that is not full.
        /// </summary>
        private void InsertToNotFullNode(ref FileNode<T> node, T newValue,
            FileNode<T> newValueLeft, FileNode<T> newValueRight)
        {
            var inserted = false;
            //if left is not null
            //then right should'nt be null
            if (newValueLeft != null)
            {
                newValueLeft.Parent = node;
                newValueRight.Parent = node;
            }
            //insert in sorted order
            for (var i = 0; i < node.KeyCount; i++)
            {
                if (newValue.CompareTo(node.Keys[i]) >= 0) continue;
                InsertAt(node.Keys, i, newValue);
                node.KeyCount++;
                //Insert children if any
                SetChild(node, i, newValueLeft);
                InsertChild(node, i + 1, newValueRight);
                inserted = true;
                break;
            }

            //newValue is the greatest
            //element should be inserted at the end then
            if (inserted) return;
            node.Keys[node.KeyCount] = newValue;
            node.KeyCount++;
            SetChild(node, node.KeyCount - 1, newValueLeft);
            SetChild(node, node.KeyCount, newValueRight);
        }

        private void ConnectLeaves(FileNode<T> node, FileNode<T> left, FileNode<T> right)
        {
            left.Next = right;
            right.Prev = left;

            if (node.Next != null)
            {
                right.Next = node.Next;
                node.Next.Prev = right;
            }
            else
            {
                //bottom right most node
                BottomRightNode = right;
            }

            if (node.Prev != null)
            {
                left.Prev = node.Prev;
                node.Prev.Next = left;
            }
            else
            {
                //bottom left most node
                BottomLeftNode = left;
            }
        }

        /// <summary>
        /// Time complexity: O(log(n)).
        /// </summary>
        public void Delete(T value)
        {
            var node = FindDeletionNode(Root, value);
            if (node == null) throw new Exception("Item do not exist in this tree.");
            for (var i = 0; i < node.KeyCount; i++)
            {
                if (value.CompareTo(node.Keys[i]) != 0)
                {
                    continue;
                }
                RemoveAt(node.Keys, i);
                node.KeyCount--;
                if (node.Parent != null && node != node.Parent.Children[0] && node.KeyCount > 0)
                {
                    var separatorIndex = GetPrevSeparatorIndex(node);
                    node.Parent.Keys[separatorIndex] = node.Keys[0];
                }
                Balance(node, value);
                Count--;
                return;
            }
        }

        /// <summary>
        /// return the node containing min value which will be a leaf at the left most
        /// </summary>
        private FileNode<T> FindMinNode(FileNode<T> node)
        {
            while (true)
            {
                //if leaf return node
                if (node.IsLeaf) return node;
                node = node.Children[0];
            }
        }

        /// <summary>
        /// return the node containing max value which will be a leaf at the right most
        /// </summary>
        private FileNode<T> FindMaxNode(FileNode<T> node)
        {
            while (true)
            {
                //if leaf return node
                if (node.IsLeaf) return node;
                node = node.Children[node.KeyCount];
            }
        }

        /// <summary>
        /// Balance a node which is short of Keys by rotations or merge
        /// </summary>
        private void Balance(FileNode<T> node, T deleteKey)
        {
            if (node == Root) return;
            if (node.KeyCount >= minKeysPerNode)
            {
                UpdateIndex(node, deleteKey, true);
                return;
            }
            var rightSibling = GetRightSibling(node);
            if (rightSibling != null
                && rightSibling.KeyCount > minKeysPerNode)
            {
                LeftRotate(node, rightSibling);
                FindMinNode(node);
                UpdateIndex(node, deleteKey, true);
                return;
            }
            var leftSibling = GetLeftSibling(node);
            if (leftSibling != null
                && leftSibling.KeyCount > minKeysPerNode)
            {
                RightRotate(leftSibling, node);
                UpdateIndex(node, deleteKey, true);
                return;
            }
            if (rightSibling != null)
            {
                Sandwich(node, rightSibling, deleteKey);
            }
            else
            {
                Sandwich(leftSibling, node, deleteKey);
            }
        }

        /// <summary>
        /// optionally recursively update outdated index with new min of right node 
        /// after deletion of a value
        /// </summary>
        private void UpdateIndex(FileNode<T> node, T deleteKey, bool spiralUp)
        {
            while (true)
            {
                if (node == null) return;
                if (node.IsLeaf || node.Children[0].IsLeaf)
                {
                    node = node.Parent;
                    continue;
                }
                for (var i = 0; i < node.KeyCount; i++)
                {
                    if (node.Keys[i].CompareTo(deleteKey) == 0)
                    {
                        node.Keys[i] = FindMinNode(node.Children[i + 1]).Keys[0];
                    }
                }
                if (spiralUp)
                {
                    node = node.Parent;
                    continue;
                }
                break;
            }
        }

        /// <summary>
        /// merge two adjacent siblings to one node
        /// </summary>
        private void Sandwich(FileNode<T> leftSibling, FileNode<T> rightSibling, T deleteKey)
        {
            var separatorIndex = GetNextSeparatorIndex(leftSibling);
            var parent = leftSibling.Parent;

            var newNode = new FileNode<T>(maxKeysPerNode, leftSibling.Parent);

            //if leaves are merged then update the Next and Prev pointers
            if (leftSibling.IsLeaf)
            {
                MergeLeaves(newNode, leftSibling, rightSibling);
            }

            var newIndex = 0;
            for (var i = 0; i < leftSibling.KeyCount; i++)
            {
                newNode.Keys[newIndex] = leftSibling.Keys[i];

                if (leftSibling.Children[i] != null)
                {
                    SetChild(newNode, newIndex, leftSibling.Children[i]);
                }

                if (leftSibling.Children[i + 1] != null)
                {
                    SetChild(newNode, newIndex + 1, leftSibling.Children[i + 1]);
                }

                newIndex++;
            }

            //special case when left sibling is empty 
            if (leftSibling.KeyCount == 0 && leftSibling.Children[0] != null)
            {
                SetChild(newNode, newIndex, leftSibling.Children[0]);
            }

            if (!rightSibling.IsLeaf)
            {
                newNode.Keys[newIndex] = parent.Keys[separatorIndex];
                newIndex++;
            }

            for (var i = 0; i < rightSibling.KeyCount; i++)
            {
                newNode.Keys[newIndex] = rightSibling.Keys[i];

                if (rightSibling.Children[i] != null)
                {
                    SetChild(newNode, newIndex, rightSibling.Children[i]);

                    //when a left leaf is added as right leaf
                    //we need to push parent key as first node of new leaf
                    if (i == 0 && rightSibling.Children[i].IsLeaf
                        && rightSibling.Children[i].Keys[0].CompareTo(newNode.Keys[newIndex - 1]) != 0)
                    {
                        InsertAt(rightSibling.Children[i].Keys, 0, newNode.Keys[newIndex - 1]);
                        rightSibling.Children[i].KeyCount++;
                    }
                }

                if (rightSibling.Children[i + 1] != null)
                {
                    SetChild(newNode, newIndex + 1, rightSibling.Children[i + 1]);
                }

                newIndex++;
            }

            //special case when left sibling is empty 
            if (rightSibling.KeyCount == 0 && rightSibling.Children[0] != null)
            {
                SetChild(newNode, newIndex, rightSibling.Children[0]);

                if (newNode.Children[newIndex].IsLeaf)
                {
                    newNode.Keys[newIndex - 1] = newNode.Children[newIndex].Keys[0];
                }
            }

            newNode.KeyCount = newIndex;

            SetChild(parent, separatorIndex, newNode);

            RemoveAt(parent.Keys, separatorIndex);
            parent.KeyCount--;

            RemoveChild(parent, separatorIndex + 1);

            if (newNode.IsLeaf && newNode.Parent.Children[0] != newNode)
            {
                separatorIndex = GetPrevSeparatorIndex(newNode);
                newNode.Parent.Keys[separatorIndex] = newNode.Keys[0];
            }

            UpdateIndex(newNode, deleteKey, false);

            if (parent.KeyCount == 0
                && parent == Root)
            {
                Root = newNode;
                Root.Parent = null;

                if (Root.KeyCount == 0)
                {
                    Root = null;
                }
                return;
            }
            if (parent.KeyCount < minKeysPerNode)
            {
                Balance(parent, deleteKey);
            }
            UpdateIndex(newNode, deleteKey, true);
        }

        /// <summary>
        /// do a right rotation 
        /// </summary>
        private void RightRotate(FileNode<T> leftSibling, FileNode<T> rightSibling)
        {
            var parentIndex = GetNextSeparatorIndex(leftSibling);
            //move parent value to right
            InsertAt(rightSibling.Keys, 0, rightSibling.Parent.Keys[parentIndex]);
            rightSibling.KeyCount++;
            InsertChild(rightSibling, 0, leftSibling.Children[leftSibling.KeyCount]);
            if (rightSibling.Children[1] != null
                && rightSibling.Children[1].IsLeaf)
            {
                rightSibling.Keys[0] = rightSibling.Children[1].Keys[0];
            }
            //move rightmost element in left sibling to parent
            rightSibling.Parent.Keys[parentIndex] = leftSibling.Keys[leftSibling.KeyCount - 1];

            //remove rightmost element of left sibling
            RemoveAt(leftSibling.Keys, leftSibling.KeyCount - 1);
            leftSibling.KeyCount--;

            RemoveChild(leftSibling, leftSibling.KeyCount + 1);

            if (rightSibling.IsLeaf)
            {
                rightSibling.Keys[0] = rightSibling.Parent.Keys[parentIndex];
            }
        }

        /// <summary>
        /// do a left rotation
        /// </summary>
        private void LeftRotate(FileNode<T> leftSibling, FileNode<T> rightSibling)
        {
            var parentIndex = GetNextSeparatorIndex(leftSibling);

            //move root to left
            leftSibling.Keys[leftSibling.KeyCount] = leftSibling.Parent.Keys[parentIndex];
            leftSibling.KeyCount++;

            SetChild(leftSibling, leftSibling.KeyCount, rightSibling.Children[0]);

            if (leftSibling.Children[leftSibling.KeyCount] != null
                && leftSibling.Children[leftSibling.KeyCount].IsLeaf)
            {
                leftSibling.Keys[leftSibling.KeyCount - 1] = leftSibling.Children[leftSibling.KeyCount].Keys[0];
            }

            //move right to parent
            leftSibling.Parent.Keys[parentIndex] = rightSibling.Keys[0];
            //remove right
            RemoveAt(rightSibling.Keys, 0);
            rightSibling.KeyCount--;

            RemoveChild(rightSibling, 0);

            if (rightSibling.IsLeaf)
            {
                rightSibling.Parent.Keys[parentIndex] = rightSibling.Keys[0];
            }

            if (leftSibling.IsLeaf && leftSibling.Parent.Children[0] != leftSibling)
            {
                parentIndex = GetPrevSeparatorIndex(leftSibling);
                leftSibling.Parent.Keys[parentIndex] = leftSibling.Keys[0];
            }
        }

        private void MergeLeaves(FileNode<T> newNode, FileNode<T> leftSibling, FileNode<T> rightSibling)
        {
            if (leftSibling.Prev != null)
            {
                newNode.Prev = leftSibling.Prev;
                leftSibling.Prev.Next = newNode;
            }
            else
            {
                BottomLeftNode = newNode;
            }

            if (rightSibling.Next != null)
            {
                newNode.Next = rightSibling.Next;
                rightSibling.Next.Prev = newNode;
            }
            else
            {
                BottomRightNode = newNode;
            }
        }

        /// <summary>
        /// Locate the node in which the item to delete exist
        /// </summary>
        private FileNode<T> FindDeletionNode(FileNode<T> node, T value)
        {
            //if leaf then its time to insert
            if (node.IsLeaf)
            {
                for (var i = 0; i < node.KeyCount; i++)
                {
                    if (value.CompareTo(node.Keys[i]) == 0)
                    {
                        return node;
                    }
                }
            }
            else
            {
                //if not leaf then drill down to leaf
                for (var i = 0; i < node.KeyCount; i++)
                {
                    //current value is less than new value
                    //drill down to left child of current value
                    if (value.CompareTo(node.Keys[i]) < 0)
                    {
                        return FindDeletionNode(node.Children[i], value);
                    }
                    //current value is grearer than new value
                    //and current value is last element 
                    if (node.KeyCount == i + 1)
                    {
                        return FindDeletionNode(node.Children[i + 1], value);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Get prev separator key of this child Node in parent
        /// </summary>
        private int GetPrevSeparatorIndex(FileNode<T> node)
        {
            var parent = node.Parent;
            if (node.Index == 0) return 0;
            if (node.Index == parent.KeyCount) return node.Index - 1;
            return node.Index - 1;
        }

        /// <summary>
        /// Get next separator key of this child Node in parent
        /// </summary>
        private int GetNextSeparatorIndex(FileNode<T> node)
        {
            var parent = node.Parent;
            if (node.Index == 0) return 0;
            if (node.Index == parent.KeyCount) return node.Index - 1;
            return node.Index;
        }

        /// <summary>
        /// get the right sibling node
        /// </summary>
        private FileNode<T> GetRightSibling(FileNode<T> node)
        {
            var parent = node.Parent;
            return node.Index == parent.KeyCount ? null : parent.Children[node.Index + 1];
        }

        /// <summary>
        /// get left sibling node
        /// </summary>
        private FileNode<T> GetLeftSibling(FileNode<T> node)
        {
            return node.Index == 0 ? null : node.Parent.Children[node.Index - 1];
        }

        private void SetChild(FileNode<T> parent, int childIndex, FileNode<T> child)
        {
            parent.Children[childIndex] = child;
            if (child == null) return;
            child.Parent = parent;
            child.Index = childIndex;
        }

        private void InsertChild(FileNode<T> parent, int childIndex, FileNode<T> child)
        {
            InsertAt(parent.Children, childIndex, child);
            if (child != null) child.Parent = parent;
            //update indices
            for (var i = childIndex; i <= parent.KeyCount; i++)
            {
                if (parent.Children[i] != null)
                {
                    parent.Children[i].Index = i;
                }
            }
        }

        private void RemoveChild(FileNode<T> parent, int childIndex)
        {
            RemoveAt(parent.Children, childIndex);
            //update indices
            for (var i = childIndex; i <= parent.KeyCount; i++)
            {
                if (parent.Children[i] != null)
                {
                    parent.Children[i].Index = i;
                }
            }
        }

        /// <summary>
        /// Shift array right at index to make room for new insertion
        /// And then insert at index
        /// Assumes array have atleast one empty index at end
        /// </summary>
        private void InsertAt<TS>(TS[] array, int index, TS newValue)
        {
            //shift elements right by one indice from index
            Array.Copy(array, index, array, index + 1, array.Length - index - 1);
            //now set the value
            array[index] = newValue;
        }

        /// <summary>
        /// Shift array left at index    
        /// </summary>
        private void RemoveAt<TS>(TS[] array, int index)
        {
            //shift elements right by one indice from index
            Array.Copy(array, index + 1, array, index, array.Length - index - 1);
        }

        /// <summary>
        /// Descending enumerable.
        /// </summary>
        //public IEnumerable<T> AsEnumerableDesc()=> GetEnumeratorDesc().AsEnumerable();

        //Implementation for the GetEnumerator method.
        IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();

        public IEnumerator<T> GetEnumerator()=> new FileEnumerator<T>(this);

        public IEnumerator<T> GetEnumeratorDesc()=> new FileEnumerator<T>(this, false);

        public IEnumerator<T> RecursiveScan(string rootPath) 
        {

            return null;
        }
    }
}
