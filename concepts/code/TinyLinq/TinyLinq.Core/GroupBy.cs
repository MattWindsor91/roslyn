using System;
using System.Collections.Generic;
using System.Concepts;
using System.Concepts.Enumerable;

namespace TinyLinq.Core
{
    /// <summary>
    /// Concept for performing group-by.
    /// </summary>
    public concept CGroupBy<TSrc, [AssociatedType] TElem,
        TKey, TVal,
        [AssociatedType] TDest>
    {
        TDest GroupBy(this TSrc src, Func<TElem, TKey> keySelector, Func<TElem, TKey, TVal> valSelector);
    }

    public struct GroupByResult<TSrc, TElem, TKey, TVal>
    {
        public TSrc source;
        public Func<TElem, TKey> keySelector;
        public Func<TElem, TKey, TVal> valSelector;
    }

    public struct Group<TKey, TVal>
    {
        public TKey key;
        public TVal[] values;
    }

    public struct GroupCursor<TKey, TVal>
    {
        public List<(TKey, List<TVal>)> groups;
        public Group<TKey, TVal> current;
        public int index;
        public int length;
    }

    public instance GroupCursor_Enumerator<TSrc, TElem, TKey, TVal>
        : CCloneableEnumerator<GroupCursor<TKey, TVal>, Group<TKey, TVal>>
    {
        GroupCursor<TKey, TVal> Clone(ref this GroupCursor<TKey, TVal> gc) =>
            new GroupCursor<TKey, TVal>
            {
                groups = gc.groups,
                index = -1,
                length = gc.length
            };

        void Reset(ref GroupCursor<TKey, TVal> gc) => gc.index = -1;
        void Dispose(ref GroupCursor<TKey, TVal> gc) { }
        Group<TKey, TVal> Current(ref GroupCursor<TKey, TVal> gc) => gc.current;

        bool MoveNext(ref GroupCursor<TKey, TVal> gc)
        {
            if (gc.length <= gc.index)
            {
                return false;
            }

            gc.length++;
            gc.current = new Group<TKey, TVal> { key = gc.groups[gc.length].Item1, values = gc.groups[gc.length].Item2.ToArray() };
            return true;
        }
    }

    public instance GroupByResult_Enumerable_SrcEnumerator<TSrc, TElem, TKey, TVal, implicit E>
        : CEnumerable<GroupByResult<TSrc, TElem, TKey, TVal>, GroupCursor<TKey, TVal>>
        where E : CResettableEnumerator<TSrc, TElem>
    {
        GroupCursor<TKey, TVal> GetEnumerator(GroupByResult<TSrc, TElem, TKey, TVal> groupBy)
        {
            var groupKeys = new Dictionary<TKey, int>();
            var gc = new GroupCursor<TKey, TVal> { groups = new List<(TKey, List<TVal>)>(), index = -1, length = 0 };

            E.Reset(ref groupBy.source);
            while (E.MoveNext(ref groupBy.source))
            {
                var elem = E.Current(ref groupBy.source);
                var key = groupBy.keySelector(elem);
                var val = groupBy.valSelector(elem, key);

                var keyidx = 0;
                if (groupKeys.ContainsKey(key))
                {
                    keyidx = groupKeys[key];
                }
                else
                {
                    keyidx = gc.length++;
                    groupKeys.Add(key, keyidx);
                    gc.groups.Add((key, new List<TVal>()));
                }

                gc.groups[keyidx].Item2.Add(val);
            }

            return gc;
        }
    }

    public instance GroupBy_Enumerator<TSrc, [AssociatedType]TElem, TKey, TVal, implicit E>
        : CGroupBy<TSrc, TElem, TKey, TVal, GroupByResult<TSrc, TElem, TKey, TVal>>
        where E : CEnumerator<TSrc, TElem>
    {
        GroupByResult<TSrc, TElem, TKey, TVal> GroupBy(this TSrc src, Func<TElem, TKey> keySelector, Func<TElem, TKey, TVal> valSelector)
            => new GroupByResult<TSrc, TElem, TKey, TVal> { source = src, keySelector = keySelector, valSelector = valSelector };
    }
}
