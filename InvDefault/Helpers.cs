﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace InvDefault
{


    public static class Helpers
    {

        /// <summary>
        /// Returns a MD5 hash as a string
        /// </summary>
        /// <param name="TextToHash">String to be hashed.</param>
        /// <returns>Hash as string.</returns>
        public static String GetMD5Hash(String TextToHash)
        {
            //Check wether data was passed
            if ((TextToHash == null) || (TextToHash.Length == 0))
            {
                return String.Empty;
            }

            //Calculate MD5 hash. This requires that the string is splitted into a byte[].
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] textToHash = Encoding.Default.GetBytes(TextToHash);
            byte[] result = md5.ComputeHash(textToHash);

            //Convert result back to string.
            return System.BitConverter.ToString(result);
        }

    }

    /// <summary>
    /// The IPriorityQueue interface.  This is mainly here for purists, and in case I decide to add more implementations later.
    /// For speed purposes, it is actually recommended that you *don't* access the priority queue through this interface, since the JIT can
    /// (theoretically?) optimize method calls from concrete-types slightly better.
    /// </summary>
    public interface IPriorityQueue<TItem, in TPriority> : IEnumerable<TItem>
    where TPriority : IComparable<TPriority>
{
    /// <summary>
    /// Enqueue a node to the priority queue.  Lower values are placed in front. Ties are broken by first-in-first-out.
    /// See implementation for how duplicates are handled.
    /// </summary>
    void Enqueue(TItem node, TPriority priority);

    /// <summary>
    /// Removes the head of the queue (node with minimum priority; ties are broken by order of insertion), and returns it.
    /// </summary>
    TItem Dequeue();

    /// <summary>
    /// Removes every node from the queue.
    /// </summary>
    void Clear();

    /// <summary>
    /// Returns whether the given node is in the queue.
    /// </summary>
    bool Contains(TItem node);

    /// <summary>
    /// Removes a node from the queue.  The node does not need to be the head of the queue.  
    /// </summary>
    void Remove(TItem node);

    /// <summary>
    /// Call this method to change the priority of a node.  
    /// </summary>
    void UpdatePriority(TItem node, TPriority priority);

    /// <summary>
    /// Returns the head of the queue, without removing it (use Dequeue() for that).
    /// </summary>
    TItem First { get; }

    /// <summary>
    /// Returns the number of nodes in the queue.
    /// </summary>
    int Count { get; }
}

public class GenericPriorityQueueNode<TPriority>
{
    /// <summary>
    /// The Priority to insert this node at.  Must be set BEFORE adding a node to the queue (ideally just once, in the node's constructor).
    /// Should not be manually edited once the node has been enqueued - use queue.UpdatePriority() instead
    /// </summary>
    public TPriority Priority { get; protected internal set; }

    /// <summary>
    /// Represents the current position in the queue
    /// </summary>
    public int QueueIndex { get; internal set; }

    /// <summary>
    /// Represents the order the node was inserted in
    /// </summary>
    public long InsertionIndex { get; internal set; }
}



/// <summary>
/// A helper-interface only needed to make writing unit tests a bit easier (hence the 'internal' access modifier)
/// </summary>
internal interface IFixedSizePriorityQueue<TItem, in TPriority> : IPriorityQueue<TItem, TPriority>
    where TPriority : IComparable<TPriority>
{
    /// <summary>
    /// Resize the queue so it can accept more nodes.  All currently enqueued nodes are remain.
    /// Attempting to decrease the queue size to a size too small to hold the existing nodes results in undefined behavior
    /// </summary>
    void Resize(int maxNodes);

    /// <summary>
    /// Returns the maximum number of items that can be enqueued at once in this queue.  Once you hit this number (ie. once Count == MaxSize),
    /// attempting to enqueue another item will cause undefined behavior.
    /// </summary>
    int MaxSize { get; }
}

/// <summary>
/// A copy of StablePriorityQueue which also has generic priority-type
/// </summary>
/// <typeparam name="TItem">The values in the queue.  Must extend the GenericPriorityQueue class</typeparam>
/// <typeparam name="TPriority">The priority-type.  Must extend IComparable&lt;TPriority&gt;</typeparam>
public sealed class GenericPriorityQueue<TItem, TPriority> : IFixedSizePriorityQueue<TItem, TPriority>
    where TItem : GenericPriorityQueueNode<TPriority>
    where TPriority : IComparable<TPriority>
{
    private int _numNodes;
    private TItem[] _nodes;
    private long _numNodesEverEnqueued;

    /// <summary>
    /// Instantiate a new Priority Queue
    /// </summary>
    /// <param name="maxNodes">The max nodes ever allowed to be enqueued (going over this will cause undefined behavior)</param>
    public GenericPriorityQueue(int maxNodes)
    {
#if DEBUG
        if (maxNodes <= 0)
        {
            throw new InvalidOperationException("New queue size cannot be smaller than 1");
        }
#endif

        _numNodes = 0;
        _nodes = new TItem[maxNodes + 1];
        _numNodesEverEnqueued = 0;
    }

    /// <summary>
    /// Returns the number of nodes in the queue.
    /// O(1)
    /// </summary>
    public int Count
    {
        get
        {
            return _numNodes;
        }
    }

    /// <summary>
    /// Returns the maximum number of items that can be enqueued at once in this queue.  Once you hit this number (ie. once Count == MaxSize),
    /// attempting to enqueue another item will cause undefined behavior.  O(1)
    /// </summary>
    public int MaxSize
    {
        get
        {
            return _nodes.Length - 1;
        }
    }

    /// <summary>
    /// Removes every node from the queue.
    /// O(n) (So, don't do this often!)
    /// </summary>
#if NET_VERSION_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public void Clear()
    {
        Array.Clear(_nodes, 1, _numNodes);
        _numNodes = 0;
    }

    /// <summary>
    /// Returns (in O(1)!) whether the given node is in the queue.  O(1)
    /// </summary>
#if NET_VERSION_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public bool Contains(TItem node)
    {
#if DEBUG
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }
        if (node.QueueIndex < 0 || node.QueueIndex >= _nodes.Length)
        {
            throw new InvalidOperationException("node.QueueIndex has been corrupted. Did you change it manually? Or add this node to another queue?");
        }
#endif

        return (_nodes[node.QueueIndex] == node);
    }

    /// <summary>
    /// Enqueue a node to the priority queue.  Lower values are placed in front. Ties are broken by first-in-first-out.
    /// If the queue is full, the result is undefined.
    /// If the node is already enqueued, the result is undefined.
    /// O(log n)
    /// </summary>
#if NET_VERSION_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public void Enqueue(TItem node, TPriority priority)
    {
#if DEBUG
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }
        if (_numNodes >= _nodes.Length - 1)
        {
            throw new InvalidOperationException("Queue is full - node cannot be added: " + node);
        }
        if (Contains(node))
        {
            throw new InvalidOperationException("Node is already enqueued: " + node);
        }
#endif

        node.Priority = priority;
        _numNodes++;
        _nodes[_numNodes] = node;
        node.QueueIndex = _numNodes;
        node.InsertionIndex = _numNodesEverEnqueued++;
        CascadeUp(_nodes[_numNodes]);
    }

#if NET_VERSION_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private void Swap(TItem node1, TItem node2)
    {
        //Swap the nodes
        _nodes[node1.QueueIndex] = node2;
        _nodes[node2.QueueIndex] = node1;

        //Swap their indicies
        int temp = node1.QueueIndex;
        node1.QueueIndex = node2.QueueIndex;
        node2.QueueIndex = temp;
    }

    //Performance appears to be slightly better when this is NOT inlined o_O
    private void CascadeUp(TItem node)
    {
        //aka Heapify-up
        int parent = node.QueueIndex / 2;
        while (parent >= 1)
        {
            TItem parentNode = _nodes[parent];
            if (HasHigherPriority(parentNode, node))
                break;

            //Node has lower priority value, so move it up the heap
            Swap(node, parentNode); //For some reason, this is faster with Swap() rather than (less..?) individual operations, like in CascadeDown()

            parent = node.QueueIndex / 2;
        }
    }

#if NET_VERSION_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private void CascadeDown(TItem node)
    {
        //aka Heapify-down
        TItem newParent;
        int finalQueueIndex = node.QueueIndex;
        while (true)
        {
            newParent = node;
            int childLeftIndex = 2 * finalQueueIndex;

            //Check if the left-child is higher-priority than the current node
            if (childLeftIndex > _numNodes)
            {
                //This could be placed outside the loop, but then we'd have to check newParent != node twice
                node.QueueIndex = finalQueueIndex;
                _nodes[finalQueueIndex] = node;
                break;
            }

            TItem childLeft = _nodes[childLeftIndex];
            if (HasHigherPriority(childLeft, newParent))
            {
                newParent = childLeft;
            }

            //Check if the right-child is higher-priority than either the current node or the left child
            int childRightIndex = childLeftIndex + 1;
            if (childRightIndex <= _numNodes)
            {
                TItem childRight = _nodes[childRightIndex];
                if (HasHigherPriority(childRight, newParent))
                {
                    newParent = childRight;
                }
            }

            //If either of the children has higher (smaller) priority, swap and continue cascading
            if (newParent != node)
            {
                //Move new parent to its new index.  node will be moved once, at the end
                //Doing it this way is one less assignment operation than calling Swap()
                _nodes[finalQueueIndex] = newParent;

                int temp = newParent.QueueIndex;
                newParent.QueueIndex = finalQueueIndex;
                finalQueueIndex = temp;
            }
            else
            {
                //See note above
                node.QueueIndex = finalQueueIndex;
                _nodes[finalQueueIndex] = node;
                break;
            }
        }
    }

    /// <summary>
    /// A helper-interface only needed to make writing unit tests a bit easier (hence the 'internal' access modifier)
    /// </summary>
    internal interface IFixedSizePriorityQueue<TItem, in TPriority> : IPriorityQueue<TItem, TPriority>
        where TPriority : IComparable<TPriority>
    {
        /// <summary>
        /// Resize the queue so it can accept more nodes.  All currently enqueued nodes are remain.
        /// Attempting to decrease the queue size to a size too small to hold the existing nodes results in undefined behavior
        /// </summary>
        void Resize(int maxNodes);

        /// <summary>
        /// Returns the maximum number of items that can be enqueued at once in this queue.  Once you hit this number (ie. once Count == MaxSize),
        /// attempting to enqueue another item will cause undefined behavior.
        /// </summary>
        int MaxSize { get; }
    }

    /// <summary>
    /// Returns true if 'higher' has higher priority than 'lower', false otherwise.
    /// Note that calling HasHigherPriority(node, node) (ie. both arguments the same node) will return false
    /// </summary>
#if NET_VERSION_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    private bool HasHigherPriority(TItem higher, TItem lower)
    {
        var cmp = higher.Priority.CompareTo(lower.Priority);
        return (cmp < 0 || (cmp == 0 && higher.InsertionIndex < lower.InsertionIndex));
    }

    /// <summary>
    /// Removes the head of the queue (node with minimum priority; ties are broken by order of insertion), and returns it.
    /// If queue is empty, result is undefined
    /// O(log n)
    /// </summary>
    public TItem Dequeue()
    {
#if DEBUG
        if (_numNodes <= 0)
        {
            throw new InvalidOperationException("Cannot call Dequeue() on an empty queue");
        }

        if (!IsValidQueue())
        {
            throw new InvalidOperationException("Queue has been corrupted (Did you update a node priority manually instead of calling UpdatePriority()?" +
                                                "Or add the same node to two different queues?)");
        }
#endif

        TItem returnMe = _nodes[1];
        Remove(returnMe);
        return returnMe;
    }

    /// <summary>
    /// Resize the queue so it can accept more nodes.  All currently enqueued nodes are remain.
    /// Attempting to decrease the queue size to a size too small to hold the existing nodes results in undefined behavior
    /// O(n)
    /// </summary>
    public void Resize(int maxNodes)
    {
#if DEBUG
        if (maxNodes <= 0)
        {
            throw new InvalidOperationException("Queue size cannot be smaller than 1");
        }

        if (maxNodes < _numNodes)
        {
            throw new InvalidOperationException("Called Resize(" + maxNodes + "), but current queue contains " + _numNodes + " nodes");
        }
#endif

        TItem[] newArray = new TItem[maxNodes + 1];
        int highestIndexToCopy = Math.Min(maxNodes, _numNodes);
        for (int i = 1; i <= highestIndexToCopy; i++)
        {
            newArray[i] = _nodes[i];
        }
        _nodes = newArray;
    }

    /// <summary>
    /// Returns the head of the queue, without removing it (use Dequeue() for that).
    /// If the queue is empty, behavior is undefined.
    /// O(1)
    /// </summary>
    public TItem First
    {
        get
        {
#if DEBUG
            if (_numNodes <= 0)
            {
                throw new InvalidOperationException("Cannot call .First on an empty queue");
            }
#endif

            return _nodes[1];
        }
    }

    /// <summary>
    /// This method must be called on a node every time its priority changes while it is in the queue.  
    /// <b>Forgetting to call this method will result in a corrupted queue!</b>
    /// Calling this method on a node not in the queue results in undefined behavior
    /// O(log n)
    /// </summary>
#if NET_VERSION_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public void UpdatePriority(TItem node, TPriority priority)
    {
#if DEBUG
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }
        if (!Contains(node))
        {
            throw new InvalidOperationException("Cannot call UpdatePriority() on a node which is not enqueued: " + node);
        }
#endif

        node.Priority = priority;
        OnNodeUpdated(node);
    }

    private void OnNodeUpdated(TItem node)
    {
        //Bubble the updated node up or down as appropriate
        int parentIndex = node.QueueIndex / 2;
        TItem parentNode = _nodes[parentIndex];

        if (parentIndex > 0 && HasHigherPriority(node, parentNode))
        {
            CascadeUp(node);
        }
        else
        {
            //Note that CascadeDown will be called if parentNode == node (that is, node is the root)
            CascadeDown(node);
        }
    }

    /// <summary>
    /// Removes a node from the queue.  The node does not need to be the head of the queue.  
    /// If the node is not in the queue, the result is undefined.  If unsure, check Contains() first
    /// O(log n)
    /// </summary>
    public void Remove(TItem node)
    {
#if DEBUG
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }
        if (!Contains(node))
        {
            throw new InvalidOperationException("Cannot call Remove() on a node which is not enqueued: " + node);
        }
#endif

        //If the node is already the last node, we can remove it immediately
        if (node.QueueIndex == _numNodes)
        {
            _nodes[_numNodes] = null;
            _numNodes--;
            return;
        }

        //Swap the node with the last node
        TItem formerLastNode = _nodes[_numNodes];
        Swap(node, formerLastNode);
        _nodes[_numNodes] = null;
        _numNodes--;

        //Now bubble formerLastNode (which is no longer the last node) up or down as appropriate
        OnNodeUpdated(formerLastNode);
    }

    public IEnumerator<TItem> GetEnumerator()
    {
        for (int i = 1; i <= _numNodes; i++)
            yield return _nodes[i];
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// <b>Should not be called in production code.</b>
    /// Checks to make sure the queue is still in a valid state.  Used for testing/debugging the queue.
    /// </summary>
    public bool IsValidQueue()
    {
        for (int i = 1; i < _nodes.Length; i++)
        {
            if (_nodes[i] != null)
            {
                int childLeftIndex = 2 * i;
                if (childLeftIndex < _nodes.Length && _nodes[childLeftIndex] != null && HasHigherPriority(_nodes[childLeftIndex], _nodes[i]))
                    return false;

                int childRightIndex = childLeftIndex + 1;
                if (childRightIndex < _nodes.Length && _nodes[childRightIndex] != null && HasHigherPriority(_nodes[childRightIndex], _nodes[i]))
                    return false;
            }
        }
        return true;
    }
}

/// <summary>
/// A simplified priority queue implementation.  Is stable, auto-resizes, and thread-safe, at the cost of being slightly slower than
/// FastPriorityQueue
/// </summary>
/// <typeparam name="TItem">The type to enqueue</typeparam>
/// <typeparam name="TPriority">The priority-type to use for nodes.  Must extend IComparable&lt;TPriority&gt;</typeparam>
public class SimplePriorityQueue<TItem, TPriority> : IPriorityQueue<TItem, TPriority>
    where TPriority : IComparable<TPriority>
{
    protected class SimpleNode : GenericPriorityQueueNode<TPriority>
    {
        public TItem Data { get; private set; }

        public SimpleNode(TItem data)
        {
            Data = data;
        }
    }

    private const int INITIAL_QUEUE_SIZE = 10;
    protected readonly GenericPriorityQueue<SimpleNode, TPriority> _queue;

    public SimplePriorityQueue()
    {
        _queue = new GenericPriorityQueue<SimpleNode, TPriority>(INITIAL_QUEUE_SIZE);
    }

    /// <summary>
    /// Given an item of type T, returns the exist SimpleNode in the queue
    /// </summary>
    private SimpleNode GetExistingNode(TItem item)
    {
        var comparer = EqualityComparer<TItem>.Default;
        foreach (var node in _queue)
        {
            if (comparer.Equals(node.Data, item))
            {
                return node;
            }
        }
        throw new InvalidOperationException("Item cannot be found in queue: " + item);
    }

    /// <summary>
    /// Returns the number of nodes in the queue.
    /// O(1)
    /// </summary>
    public int Count
    {
        get
        {
            lock (_queue)
            {
                return _queue.Count;
            }
        }
    }


    /// <summary>
    /// Returns the head of the queue, without removing it (use Dequeue() for that).
    /// Throws an exception when the queue is empty.
    /// O(1)
    /// </summary>
    public TItem First
    {
        get
        {
            lock (_queue)
            {
                if (_queue.Count <= 0)
                {
                    throw new InvalidOperationException("Cannot call .First on an empty queue");
                }

                SimpleNode first = _queue.First;
                return (first != null ? first.Data : default(TItem));
            }
        }
    }

    /// <summary>
    /// Removes every node from the queue.
    /// O(n)
    /// </summary>
    public void Clear()
    {
        lock (_queue)
        {
            _queue.Clear();
        }
    }

    /// <summary>
    /// Returns whether the given item is in the queue.
    /// O(n)
    /// </summary>
    public bool Contains(TItem item)
    {
        lock (_queue)
        {
            var comparer = EqualityComparer<TItem>.Default;
            foreach (var node in _queue)
            {
                if (comparer.Equals(node.Data, item))
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Removes the head of the queue (node with minimum priority; ties are broken by order of insertion), and returns it.
    /// If queue is empty, throws an exception
    /// O(log n)
    /// </summary>
    public virtual TItem Dequeue()
    {
        lock (_queue)
        {
            if (_queue.Count <= 0)
            {
                throw new InvalidOperationException("Cannot call Dequeue() on an empty queue");
            }

            SimpleNode node = _queue.Dequeue();
            return node.Data;
        }
    }

    /// <summary>
    /// Enqueue a node to the priority queue.  Lower values are placed in front. Ties are broken by first-in-first-out.
    /// This queue automatically resizes itself, so there's no concern of the queue becoming 'full'.
    /// Duplicates are allowed.
    /// O(log n)
    /// </summary>
    public virtual void Enqueue(TItem item, TPriority priority)
    {
        lock (_queue)
        {
            SimpleNode node = new SimpleNode(item);
            if (_queue.Count == _queue.MaxSize)
            {
                _queue.Resize(_queue.MaxSize * 2 + 1);
            }
            _queue.Enqueue(node, priority);
        }
    }

    /// <summary>
    /// Removes an item from the queue.  The item does not need to be the head of the queue.  
    /// If the item is not in the queue, an exception is thrown.  If unsure, check Contains() first.
    /// If multiple copies of the item are enqueued, only the first one is removed. 
    /// O(n)
    /// </summary>
    public virtual void Remove(TItem item)
    {
        lock (_queue)
        {
            try
            {
                _queue.Remove(GetExistingNode(item));
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException("Cannot call Remove() on a node which is not enqueued: " + item, ex);
            }
        }
    }

    /// <summary>
    /// Call this method to change the priority of an item.
    /// Calling this method on a item not in the queue will throw an exception.
    /// If the item is enqueued multiple times, only the first one will be updated.
    /// (If your requirements are complex enough that you need to enqueue the same item multiple times <i>and</i> be able
    /// to update all of them, please wrap your items in a wrapper class so they can be distinguished).
    /// O(n)
    /// </summary>
    public void UpdatePriority(TItem item, TPriority priority)
    {
        lock (_queue)
        {
            try
            {
                SimpleNode updateMe = GetExistingNode(item);
                _queue.UpdatePriority(updateMe, priority);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException("Cannot call UpdatePriority() on a node which is not enqueued: " + item, ex);
            }
        }
    }

    public IEnumerator<TItem> GetEnumerator()
    {
        List<TItem> queueData = new List<TItem>();
        lock (_queue)
        {
            //Copy to a separate list because we don't want to 'yield return' inside a lock
            foreach (var node in _queue)
            {
                queueData.Add(node.Data);
            }
        }

        return queueData.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public bool IsValidQueue()
    {
        lock (_queue)
        {
            return _queue.IsValidQueue();
        }
    }
}



}
