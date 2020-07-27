using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TianHua.AutoCAD.Utility.ExtensionTools
{

    public class CompareElemnet<T> : IEqualityComparer<T>
    {
        public Func<T, T, bool> Func { get; set; }
        public bool Equals(T x, T y)
        {
            //xList.Intersect(yList).Count() > 0
            return Func(x, y);
        }
        public int GetHashCode(T obj)
        {
            return 1;
        }

        public CompareElemnet(Func<T, T, bool> func)
        {
            this.Func = func;
        }

    }

    public class CompareSort<T> : IComparer<double>
    {
        public double Tolerance { get; set; }
        public CompareSort(double tol)
        {
            this.Tolerance = tol;
        }
        public int Compare(double x, double y)
        {
            //大超过容差
            if (x - y > this.Tolerance)
            {
                return 1;
            }
            //小超过容差
            else if (y - x > this.Tolerance)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }
    }

    public class CompareChildElement<T, T1, T2> : IEqualityComparer<T>
    where T1 : T
    where T2 : T
    {
        public Func<T1, T2, bool> Func { get; set; }

        public bool Equals(T x, T y)
        {
            if (x is T1 && y is T2)
            {
                var t1Val = (T1)x;
                var t2Val = (T2)y;

                return Func(t1Val, t2Val);
            }
            else if (y is T1 && x is T2)
            {
                var t1Val = (T1)y;
                var t2Val = (T2)x;

                return Func(t1Val, t2Val);
            }
            else
            {
                return false;
            }

        }

        public int GetHashCode(T obj)
        {
            return 1;
        }

        public CompareChildElement(Func<T1, T2, bool> func)
        {
            this.Func = func;
        }

    }

}
