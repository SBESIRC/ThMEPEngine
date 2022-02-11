using System.Linq;

/// DisposableList<T> class
/// Copyright (c) 2012  Tony Tanzillo 
/// https://www.theswamp.org/index.php?topic=42399.45
namespace System.Collections.Generic
{

    /// DisposableList is a specialization of System.Collections.Generic.List<T> that 
    /// stores and manages elements that implement the IDisposable interface. 
    ///
    /// The main purpose of DisposableList is to gurantee that all items in the list 
    /// are disposed when the list is disposed, deterministically, even if a call to 
    /// any item's Dispose() method throws an exception. 
    /// 
    /// Note that DisposableList<T> does not supress exceptions raised by calls to the
    /// Dispose() method of any item, it merely ensures that any remaining 'undisposed' 
    /// items are disposed before the exception propagates up the call stack.
    /// 
    /// Use DisposableList<T> exactly as you would use its base type, and when you want
    /// all contained elements disposed, call it's Dispose() method.
    /// 
    /// Using DisposableList<T> with a DBObjectCollection:
    /// 
    /// DisposableList can be handy when working with DBObjectCollections containing
    /// DBObjects that are not database-resident. It helps to ensure that if your code 
    /// fails before all items retreived from the collection are processed, those items
    /// will be determinstically disposed, and will not have their finalizer's called
    /// on the GC's thread, which could lead to a fatal error that terminates AutoCAD.
    /// 
    /// In addition, because DisposableList is a strongly-typed collection, you can
    /// avoid repetitively casting items to the type you need to work with, and can
    /// more easily use the contents with LINQ.
    /// 
    /// For example, you can pull all the items out of a DBObjectCollection from a
    /// call to the DisposableList's constructor overload that takes an untyped
    /// System.Collections.IEnumerable, and subsequently work exclusively with the
    /// the DisposableList rather than the DBObjectCollection:
    /// 
    ///    using( DBObjectCollection items = myCurve.GetSplitCurves(...) )
    ///    using( DisposableList<Curve> curves = new DisposableList<Curve>( items ) )
    ///    {
    ///       // Work with each Curve in the curves list. 
    ///       // Once control leaves this using() block, all
    ///       // items in the curves list will be disposed.
    ///    }
    ///
    public class DisposableList<T> : List<T>, IDisposable
       where T : IDisposable
    {
        bool disposed = false;

        /// <summary>
        ///
        /// If true, contained items are disposed of in reverse order
        /// (last to first). Otherwise, items are disposed in the order
        /// they appear in the list.
        ///
        /// </summary>

        bool ReverseOrderedDispose
        {
            get;
            set;
        }

        public DisposableList()
        {
        }

        public DisposableList(int capacity)
           : base(capacity)
        {
        }

        public DisposableList(IEnumerable<T> collection)
           : base(collection)
        {
        }

        public DisposableList(System.Collections.IEnumerable collection)
           : base(collection.Cast<T>())
        {
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            bool flag = this.disposed;
            this.disposed = true;
            if (disposing && !flag)
            {
                if (this.ReverseOrderedDispose)
                    base.Reverse();
                using (IEnumerator<T> e = base.GetEnumerator())
                    DisposeItems(e);
            }
        }

        private void DisposeItems(IEnumerator<T> e)
        {
            while (e.MoveNext())
            {
                try
                {
                    T item = e.Current;
                    if (item != null)
                        item.Dispose();
                }
                catch
                {
                    DisposeItems(e);
                    throw;
                }
            }
        }
    }
}