//this file is for debugging only by Feng




#pragma warning disable

namespace ThMEPWSS.DebugNs
{
    using TypeDescriptor = System.ComponentModel.TypeDescriptor;
    using System;
    using System.Linq;
    using System.Text;
    using System.Reflection;
    using System.Collections.Generic;
    using System.Windows.Forms;
    using ThMEPWSS.JsonExtensionsNs;
    using Dbg = ThMEPWSS.DebugNs.ThDebugTool;
    using DU = ThMEPWSS.Assistant.DrawUtils;
    using Autodesk.AutoCAD.EditorInput;
    using AcHelper;
    using Autodesk.AutoCAD.Geometry;
    using Linq2Acad;
    using ThMEPWSS.Pipe.Model;
    using ThMEPWSS.Pipe.Engine;
    using Autodesk.AutoCAD.DatabaseServices;
    using System.Diagnostics;
    using Autodesk.AutoCAD.ApplicationServices;
    using Dreambuild.AutoCAD;
    using DotNetARX;
    using Autodesk.AutoCAD.Internal;
    using static ThMEPWSS.DebugNs.ThPublicMethods;
    using ThMEPWSS.CADExtensionsNs;
    using ThMEPWSS.Uitl;
    using ThMEPWSS.Uitl.DebugNs;
    using ThMEPWSS.Uitl.ExtensionsNs;
    using ThMEPWSS.Assistant;
    using ThMEPWSS.Pipe.Service;
    using NFox.Cad;
    using ThCADCore.NTS;
    using Autodesk.AutoCAD.Colors;
    using System.Runtime.Remoting;
    using PolylineTools = Pipe.Service.PolylineTools;
    using CircleTools = Pipe.Service.CircleTools;
    using System.IO;
    using Autodesk.AutoCAD.Runtime;
    using static StaticMethods;
    using ThMEPWSS.Pipe;
    using Newtonsoft.Json;
    using System.Text.RegularExpressions;
    using ThCADExtension;
    using System.Collections;
    using ThCADCore.NTS.IO;
    using Newtonsoft.Json.Linq;
    using ThMEPEngineCore.Engine;
    using NetTopologySuite.Geometries;
    using NetTopologySuite.Operation.Linemerge;
    using Microsoft.CSharp;
    using System.CodeDom.Compiler;
    using System.Linq.Expressions;
    using ThMEPEngineCore.Algorithm;


    public static class HighlightHelper
    {
        public static Point2d GetCurrentViewSize()
        {
            double h = (double)Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("VIEWSIZE");
            Point2d screen = (Point2d)Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("SCREENSIZE");
            double w = h * (screen.X / screen.Y);
            return new Point2d(w, h);
        }
        public static Extents2d GetCurrentViewBound(double shrinkScale = 1.0)
        {
            Point2d vSize = GetCurrentViewSize();
            Point3d center = ((Point3d)Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("VIEWCTR")).
                    TransformBy(Active.Editor.CurrentUserCoordinateSystem);
            double w = vSize.X * shrinkScale;
            double h = vSize.Y * shrinkScale;
            Point2d minPoint = new Point2d(center.X - w / 2.0, center.Y - h / 2.0);
            Point2d maxPoint = new Point2d(center.X + w / 2.0, center.Y + h / 2.0);
            return new Extents2d(minPoint, maxPoint);
        }
        public static void HighLight(IEnumerable<Entity> ents)
        {
            //var extents = ThAuxiliaryUtils.GetCurrentViewBound();
            var extents = GetCurrentViewBound();
            foreach (var e in ents)
            {
                if (!e.IsErased && !e.IsDisposed && e.Bounds is Extents3d ext)
                {
                    if (IsInActiveView(ext.MinPoint,
                        extents.MinPoint.X, extents.MaxPoint.X,
                        extents.MinPoint.Y, extents.MaxPoint.Y) ||
                    IsInActiveView(ext.MaxPoint,
                    extents.MinPoint.X, extents.MaxPoint.X,
                    extents.MinPoint.Y, extents.MaxPoint.Y))
                    {
                        e.Highlight();
                    }
                }
            }
        }
        public static void UnHighLight(IEnumerable<Entity> ents)
        {
            foreach (var e in ents)
            {
                if (!e.IsErased && !e.IsDisposed)
                {
                    e.Unhighlight();
                }
            }
        }
        private static bool IsInActiveView(Point3d pt, double minX, double maxX, double minY, double maxY)
        {
            return pt.X >= minX && pt.X <= maxX && pt.Y >= minY && pt.Y <= maxY;
        }
    }
    public static class CloneHelper
    {
        public static readonly MethodInfo ObjectMemberwiseCloneMethodInfo = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);
        public static readonly MethodInfo CopyListMethodInfo = typeof(CloneHelper).GetMethod(nameof(CopyList));
        public static readonly MethodInfo CopyHashSetMethodInfo = typeof(CloneHelper).GetMethod(nameof(CopyHashSet));
        public static readonly MethodInfo CopyQueueMethodInfo = typeof(CloneHelper).GetMethod(nameof(CopyQueue));
        public static readonly MethodInfo CopyStackMethodInfo = typeof(CloneHelper).GetMethod(nameof(CopyStack));
        public static readonly MethodInfo CopyObservableCollectionMethodInfo = typeof(CloneHelper).GetMethod(nameof(CopyObservableCollection));
        public static readonly MethodInfo CopyArrayMethodInfo = typeof(CloneHelper).GetMethod(nameof(CopyArray));
        public static readonly MethodInfo CopyDictionaryMethodInfo = typeof(CloneHelper).GetMethod(nameof(CopyDictionary));
        public static readonly MethodInfo CopySortedDictionaryMethodInfo = typeof(CloneHelper).GetMethod(nameof(CopySortedDictionary));
        public static readonly MethodInfo CopySortedListMethodInfo = typeof(CloneHelper).GetMethod(nameof(CopySortedList));
        public static object GetDefaultValue(Type type)
        {
            return type.IsClass ? null : Activator.CreateInstance(type);
        }
        public static List<T> CopyList<T>(List<T> src) => new List<T>(src);
        public static HashSet<T> CopyHashSet<T>(HashSet<T> src) => new HashSet<T>(src);
        public static Queue<T> CopyQueue<T>(Queue<T> src) => new Queue<T>(src);
        public static Stack<T> CopyStack<T>(Stack<T> src) => new Stack<T>(src);
        public static System.Collections.ObjectModel.ObservableCollection<T> CopyObservableCollection<T>(System.Collections.ObjectModel.ObservableCollection<T> src) => new System.Collections.ObjectModel.ObservableCollection<T>(src);
        public static T[] CopyArray<T>(T[] src)
        {
            var dst = new T[src.Length];
            Array.Copy(src, dst, src.Length);
            return dst;
        }
        public static Dictionary<K, V> CopyDictionary<K, V>(Dictionary<K, V> src) => new Dictionary<K, V>(src);
        public static SortedDictionary<K, V> CopySortedDictionary<K, V>(SortedDictionary<K, V> src) => new SortedDictionary<K, V>(src);
        public static SortedList<K, V> CopySortedList<K, V>(SortedList<K, V> src) => new SortedList<K, V>(src);
        public static Func<T, T> MemberwiseCloneF<T>()
        {
            var pe = Expression.Parameter(typeof(T), "v");
            if (typeof(T).IsValueType) return Expression.Lambda<Func<T, T>>(Expression.Block(pe), pe).Compile();
            return Expression.Lambda<Func<T, T>>(Expression.Block(Expression.Convert(Expression.Call(pe, CloneHelper.ObjectMemberwiseCloneMethodInfo), typeof(T))), pe).Compile();
        }

        public static Action<T, T> CopyFieldsMemberwiseF<T>()
        {
            var src = Expression.Parameter(typeof(T), "src");
            var dst = Expression.Parameter(typeof(T), "dst");
            return Expression.Lambda<Action<T, T>>(Expression.Block(typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Select(fi => Expression.Assign(Expression.Field(dst, fi), Expression.Field(src, fi)))), src, dst).Compile();
        }

        public static Action<T, T> CopyCollectionFieldsF<T>()
        {
            var src = Expression.Parameter(typeof(T), "src");
            var dst = Expression.Parameter(typeof(T), "dst");
            var exprs = new List<Expression>();
            foreach (var fi in typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var mi = TryGetCollectionCloneMethodInfo(fi.FieldType);
                if (mi != null)
                {
                    exprs.Add(Expression.Assign(Expression.Field(dst, fi), Expression.Call(null, mi, Expression.Field(src, fi))));
                }
            }
            return Expression.Lambda<Action<T, T>>(Expression.Block(exprs), src, dst).Compile();
        }
        public static MethodInfo TryGetCollectionCloneMethodInfo(Type type)
        {
            if (type.IsArray)
            {
                var itemType = type.GetElementType();
                return CopyArrayMethodInfo.MakeGenericMethod(itemType);
            }
            if (type.IsGenericType)
            {
                var gtypes = type.GetGenericArguments();
                if (gtypes.Length == 1)
                {
                    var itemType = gtypes[0];
                    var gtypeDef = type.GetGenericTypeDefinition();
                    if (gtypeDef == typeof(List<>))
                    {
                        return CopyListMethodInfo.MakeGenericMethod(itemType);
                    }
                    if (gtypeDef == typeof(System.Collections.ObjectModel.ObservableCollection<>))
                    {
                        return CopyObservableCollectionMethodInfo.MakeGenericMethod(itemType);
                    }
                    if (gtypeDef == typeof(HashSet<>))
                    {
                        return CopyHashSetMethodInfo.MakeGenericMethod(itemType);
                    }
                    if (gtypeDef == typeof(Queue<>))
                    {
                        return CopyQueueMethodInfo.MakeGenericMethod(itemType);
                    }
                    if (gtypeDef == typeof(Stack<>))
                    {
                        return CopyStackMethodInfo.MakeGenericMethod(itemType);
                    }
                }
                else if (gtypes.Length == 2)
                {
                    var gtypeDef = type.GetGenericTypeDefinition();
                    if (gtypeDef == typeof(Dictionary<string, string>).GetGenericTypeDefinition())
                    {
                        var ktype = gtypes[0];
                        var vtype = gtypes[1];
                        return CopyDictionaryMethodInfo.MakeGenericMethod(ktype, vtype);
                    }
                    if (gtypeDef == typeof(SortedDictionary<string, string>).GetGenericTypeDefinition())
                    {
                        var ktype = gtypes[0];
                        var vtype = gtypes[1];
                        return CopySortedDictionaryMethodInfo.MakeGenericMethod(ktype, vtype);
                    }
                    if (gtypeDef == typeof(SortedList<string, string>).GetGenericTypeDefinition())
                    {
                        var ktype = gtypes[0];
                        var vtype = gtypes[1];
                        return CopySortedListMethodInfo.MakeGenericMethod(ktype, vtype);
                    }
                }
            }
            return null;
        }
    }
    public class ReturnBlockBuilder
    {
        public LabelTarget returnTarget;
        public LabelExpression returnLabel;
        public List<Expression> expressions { get; } = new List<Expression>();
        public ReturnBlockBuilder(Type type, object dftValue)
        {
            returnTarget = Expression.Label(type);
            returnLabel = Expression.Label(returnTarget, Expression.Constant(dftValue, type));
        }
        public ReturnBlockBuilder(Type type) : this(type, CloneHelper.GetDefaultValue(type)) { }
        public BlockExpression BuildBlockExpression()
        {
            try
            {
                expressions.Add(returnLabel);
                return Expression.Block(expressions);
            }
            finally
            {
                expressions.Clear();
            }
        }
        public void AddReturnExpression(Expression expr)
        {
            expressions.Add(Expression.Return(returnTarget, expr));
        }
    }
    public static class ExpTree
    {
        public static Action<object, V> AssignPropertyOrFieldF<V>(Type type, PropertyInfo pi)
        {
            var pe = Expression.Parameter(typeof(object), "v1");
            var peV = Expression.Parameter(typeof(V), "v2");
            return Expression.Lambda<Action<object, V>>(Expression.Block(Expression.Assign(Expression.Property(Expression.Convert(pe, type), pi), peV)), pe, peV).Compile();
        }
        public static Action<object, V> AssignPropertyOrFieldF<V>(Type type, FieldInfo fi)
        {
            var pe = Expression.Parameter(typeof(object), "v1");
            var peV = Expression.Parameter(typeof(V), "v2");
            return Expression.Lambda<Action<object, V>>(Expression.Block(Expression.Assign(Expression.Field(Expression.Convert(pe, type), fi), peV)), pe, peV).Compile();
        }
        public static Action<object, V> AssignPropertyOrFieldF<V>(Type type, string name)
        {
            var pe = Expression.Parameter(typeof(object), "v1");
            var peV = Expression.Parameter(typeof(V), "v2");
            return Expression.Lambda<Action<object, V>>(Expression.Block(Expression.Assign(Expression.PropertyOrField(Expression.Convert(pe, type), name), peV)), pe, peV).Compile();
        }
        public static Func<object, V> PropertyOrFieldF<V>(Type type, PropertyInfo pi)
        {
            var pe = Expression.Parameter(typeof(object), "value");
            return Expression.Lambda<Func<object, V>>(Expression.Block(Expression.Property(Expression.Convert(pe, type), pi)), pe).Compile();
        }
        public static Func<object, V> PropertyOrFieldF<V>(Type type, FieldInfo fi)
        {
            var pe = Expression.Parameter(typeof(object), "value");
            return Expression.Lambda<Func<object, V>>(Expression.Block(Expression.Field(Expression.Convert(pe, type), fi)), pe).Compile();
        }
        public static Func<object, V> PropertyOrFieldF<V>(Type type, string name)
        {
            var pe = Expression.Parameter(typeof(object), "value");
            return Expression.Lambda<Func<object, V>>(Expression.Block(Expression.PropertyOrField(Expression.Convert(pe, type), name)), pe).Compile();
        }
        public static Func<T, V> PropertyOrFieldF<T, V>(PropertyInfo pi)
        {
            var pe = Expression.Parameter(typeof(T), "value");
            return Expression.Lambda<Func<T, V>>(Expression.Block(Expression.Property(pe, pi)), pe).Compile();
        }
        public static Func<T, V> PropertyOrFieldF<T, V>(FieldInfo fi)
        {
            var pe = Expression.Parameter(typeof(T), "value");
            return Expression.Lambda<Func<T, V>>(Expression.Block(Expression.Field(pe, fi)), pe).Compile();
        }
        public static Func<T, V> PropertyOrFieldF<T, V>(string name)
        {
            var pe = Expression.Parameter(typeof(T), "value");
            return Expression.Lambda<Func<T, V>>(Expression.Block(Expression.PropertyOrField(pe, name)), pe).Compile();
        }
        public static Action A(MethodInfo mi)
        {
            return Expression.Lambda<Action>(Expression.Block(Expression.Call(null, mi))).Compile();
        }
        public static Action<T0> A<T0>(MethodInfo mi)
        {
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            return Expression.Lambda<Action<T0>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
        }
        public static Action<T0, T1> A<T0, T1>(MethodInfo mi)
        {
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            var pe1 = Expression.Parameter(typeof(T1), "v1");
            return Expression.Lambda<Action<T0, T1>>(Expression.Block(Expression.Call(null, mi, pe0, pe1)), pe0, pe1).Compile();
        }
        public static Action<T0, T1, T2> A<T0, T1, T2>(MethodInfo mi)
        {
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            var pe1 = Expression.Parameter(typeof(T1), "v1");
            var pe2 = Expression.Parameter(typeof(T2), "v2");
            return Expression.Lambda<Action<T0, T1, T2>>(Expression.Block(Expression.Call(null, mi, pe0, pe1, pe2)), pe0, pe1, pe2).Compile();
        }
        public static Action<T0, T1, T2, T3> A<T0, T1, T2, T3>(MethodInfo mi)
        {
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            var pe1 = Expression.Parameter(typeof(T1), "v1");
            var pe2 = Expression.Parameter(typeof(T2), "v2");
            var pe3 = Expression.Parameter(typeof(T3), "v3");
            return Expression.Lambda<Action<T0, T1, T2, T3>>(Expression.Block(Expression.Call(null, mi, pe0, pe1, pe2, pe3)), pe0, pe1, pe2, pe3).Compile();
        }
        public static Action<T0, T1, T2, T3, T4> A<T0, T1, T2, T3, T4>(MethodInfo mi)
        {
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            var pe1 = Expression.Parameter(typeof(T1), "v1");
            var pe2 = Expression.Parameter(typeof(T2), "v2");
            var pe3 = Expression.Parameter(typeof(T3), "v3");
            var pe4 = Expression.Parameter(typeof(T4), "v4");
            return Expression.Lambda<Action<T0, T1, T2, T3, T4>>(Expression.Block(Expression.Call(null, mi, pe0, pe1, pe2, pe3, pe4)), pe0, pe1, pe2, pe3, pe4).Compile();
        }
        public static Action<T0, T1, T2, T3, T4, T5> A<T0, T1, T2, T3, T4, T5>(MethodInfo mi)
        {
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            var pe1 = Expression.Parameter(typeof(T1), "v1");
            var pe2 = Expression.Parameter(typeof(T2), "v2");
            var pe3 = Expression.Parameter(typeof(T3), "v3");
            var pe4 = Expression.Parameter(typeof(T4), "v4");
            var pe5 = Expression.Parameter(typeof(T5), "v5");
            return Expression.Lambda<Action<T0, T1, T2, T3, T4, T5>>(Expression.Block(Expression.Call(null, mi, pe0, pe1, pe2, pe3, pe4, pe5)), pe0, pe1, pe2, pe3, pe4, pe5).Compile();
        }
        public static Action<T0, T1, T2, T3, T4, T5, T6> A<T0, T1, T2, T3, T4, T5, T6>(MethodInfo mi)
        {
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            var pe1 = Expression.Parameter(typeof(T1), "v1");
            var pe2 = Expression.Parameter(typeof(T2), "v2");
            var pe3 = Expression.Parameter(typeof(T3), "v3");
            var pe4 = Expression.Parameter(typeof(T4), "v4");
            var pe5 = Expression.Parameter(typeof(T5), "v5");
            var pe6 = Expression.Parameter(typeof(T6), "v6");
            return Expression.Lambda<Action<T0, T1, T2, T3, T4, T5, T6>>(Expression.Block(Expression.Call(null, mi, pe0, pe1, pe2, pe3, pe4, pe5, pe6)), pe0, pe1, pe2, pe3, pe4, pe5, pe6).Compile();
        }
        public static Action<T0, T1, T2, T3, T4, T5, T6, T7> A<T0, T1, T2, T3, T4, T5, T6, T7>(MethodInfo mi)
        {
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            var pe1 = Expression.Parameter(typeof(T1), "v1");
            var pe2 = Expression.Parameter(typeof(T2), "v2");
            var pe3 = Expression.Parameter(typeof(T3), "v3");
            var pe4 = Expression.Parameter(typeof(T4), "v4");
            var pe5 = Expression.Parameter(typeof(T5), "v5");
            var pe6 = Expression.Parameter(typeof(T6), "v6");
            var pe7 = Expression.Parameter(typeof(T7), "v7");
            return Expression.Lambda<Action<T0, T1, T2, T3, T4, T5, T6, T7>>(Expression.Block(Expression.Call(null, mi, pe0, pe1, pe2, pe3, pe4, pe5, pe6, pe7)), pe0, pe1, pe2, pe3, pe4, pe5, pe6, pe7).Compile();
        }
        public static Func<T0> F<T0>(MethodInfo mi)
        {
            return Expression.Lambda<Func<T0>>(Expression.Block(Expression.Call(null, mi))).Compile();
        }
        public static Func<T0, T1> F<T0, T1>(MethodInfo mi)
        {
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            return Expression.Lambda<Func<T0, T1>>(Expression.Block(Expression.Call(null, mi, pe0)), pe0).Compile();
        }
        public static Func<T0, T1, T2> F<T0, T1, T2>(MethodInfo mi)
        {
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            var pe1 = Expression.Parameter(typeof(T1), "v1");
            return Expression.Lambda<Func<T0, T1, T2>>(Expression.Block(Expression.Call(null, mi, pe0, pe1)), pe0, pe1).Compile();
        }
        public static Func<T0, T1, T2, T3> F<T0, T1, T2, T3>(MethodInfo mi)
        {
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            var pe1 = Expression.Parameter(typeof(T1), "v1");
            var pe2 = Expression.Parameter(typeof(T2), "v2");
            return Expression.Lambda<Func<T0, T1, T2, T3>>(Expression.Block(Expression.Call(null, mi, pe0, pe1, pe2)), pe0, pe1, pe2).Compile();
        }
        public static Func<T0, T1, T2, T3, T4> F<T0, T1, T2, T3, T4>(MethodInfo mi)
        {
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            var pe1 = Expression.Parameter(typeof(T1), "v1");
            var pe2 = Expression.Parameter(typeof(T2), "v2");
            var pe3 = Expression.Parameter(typeof(T3), "v3");
            return Expression.Lambda<Func<T0, T1, T2, T3, T4>>(Expression.Block(Expression.Call(null, mi, pe0, pe1, pe2, pe3)), pe0, pe1, pe2, pe3).Compile();
        }
        public static Func<T0, T1, T2, T3, T4, T5> F<T0, T1, T2, T3, T4, T5>(MethodInfo mi)
        {
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            var pe1 = Expression.Parameter(typeof(T1), "v1");
            var pe2 = Expression.Parameter(typeof(T2), "v2");
            var pe3 = Expression.Parameter(typeof(T3), "v3");
            var pe4 = Expression.Parameter(typeof(T4), "v4");
            return Expression.Lambda<Func<T0, T1, T2, T3, T4, T5>>(Expression.Block(Expression.Call(null, mi, pe0, pe1, pe2, pe3, pe4)), pe0, pe1, pe2, pe3, pe4).Compile();
        }
        public static Func<T0, T1, T2, T3, T4, T5, T6> F<T0, T1, T2, T3, T4, T5, T6>(MethodInfo mi)
        {
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            var pe1 = Expression.Parameter(typeof(T1), "v1");
            var pe2 = Expression.Parameter(typeof(T2), "v2");
            var pe3 = Expression.Parameter(typeof(T3), "v3");
            var pe4 = Expression.Parameter(typeof(T4), "v4");
            var pe5 = Expression.Parameter(typeof(T5), "v5");
            return Expression.Lambda<Func<T0, T1, T2, T3, T4, T5, T6>>(Expression.Block(Expression.Call(null, mi, pe0, pe1, pe2, pe3, pe4, pe5)), pe0, pe1, pe2, pe3, pe4, pe5).Compile();
        }
        public static Func<T0, T1, T2, T3, T4, T5, T6, T7> F<T0, T1, T2, T3, T4, T5, T6, T7>(MethodInfo mi)
        {
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            var pe1 = Expression.Parameter(typeof(T1), "v1");
            var pe2 = Expression.Parameter(typeof(T2), "v2");
            var pe3 = Expression.Parameter(typeof(T3), "v3");
            var pe4 = Expression.Parameter(typeof(T4), "v4");
            var pe5 = Expression.Parameter(typeof(T5), "v5");
            var pe6 = Expression.Parameter(typeof(T6), "v6");
            return Expression.Lambda<Func<T0, T1, T2, T3, T4, T5, T6, T7>>(Expression.Block(Expression.Call(null, mi, pe0, pe1, pe2, pe3, pe4, pe5, pe6)), pe0, pe1, pe2, pe3, pe4, pe5, pe6).Compile();
        }
        public static Action<T> InstA<T>(MethodInfo mi)
        {
            var pe = Expression.Parameter(typeof(T), "v");
            return Expression.Lambda<Action<T>>(Expression.Block(Expression.Call(pe, mi)), pe).Compile();
        }
        public static Action<T, T0> InstA<T, T0>(MethodInfo mi)
        {
            var pe = Expression.Parameter(typeof(T), "v");
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            return Expression.Lambda<Action<T, T0>>(Expression.Block(Expression.Call(pe, mi, pe0)), pe, pe0).Compile();
        }
        public static Action<T, T0, T1> InstA<T, T0, T1>(MethodInfo mi)
        {
            var pe = Expression.Parameter(typeof(T), "v");
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            var pe1 = Expression.Parameter(typeof(T1), "v1");
            return Expression.Lambda<Action<T, T0, T1>>(Expression.Block(Expression.Call(pe, mi, pe0, pe1)), pe, pe0, pe1).Compile();
        }
        public static Action<T, T0, T1, T2> InstA<T, T0, T1, T2>(MethodInfo mi)
        {
            var pe = Expression.Parameter(typeof(T), "v");
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            var pe1 = Expression.Parameter(typeof(T1), "v1");
            var pe2 = Expression.Parameter(typeof(T2), "v2");
            return Expression.Lambda<Action<T, T0, T1, T2>>(Expression.Block(Expression.Call(pe, mi, pe0, pe1, pe2)), pe, pe0, pe1, pe2).Compile();
        }
        public static Action<T, T0, T1, T2, T3> InstA<T, T0, T1, T2, T3>(MethodInfo mi)
        {
            var pe = Expression.Parameter(typeof(T), "v");
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            var pe1 = Expression.Parameter(typeof(T1), "v1");
            var pe2 = Expression.Parameter(typeof(T2), "v2");
            var pe3 = Expression.Parameter(typeof(T3), "v3");
            return Expression.Lambda<Action<T, T0, T1, T2, T3>>(Expression.Block(Expression.Call(pe, mi, pe0, pe1, pe2, pe3)), pe, pe0, pe1, pe2, pe3).Compile();
        }
        public static Action<T, T0, T1, T2, T3, T4> InstA<T, T0, T1, T2, T3, T4>(MethodInfo mi)
        {
            var pe = Expression.Parameter(typeof(T), "v");
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            var pe1 = Expression.Parameter(typeof(T1), "v1");
            var pe2 = Expression.Parameter(typeof(T2), "v2");
            var pe3 = Expression.Parameter(typeof(T3), "v3");
            var pe4 = Expression.Parameter(typeof(T4), "v4");
            return Expression.Lambda<Action<T, T0, T1, T2, T3, T4>>(Expression.Block(Expression.Call(pe, mi, pe0, pe1, pe2, pe3, pe4)), pe, pe0, pe1, pe2, pe3, pe4).Compile();
        }
        public static Action<T, T0, T1, T2, T3, T4, T5> InstA<T, T0, T1, T2, T3, T4, T5>(MethodInfo mi)
        {
            var pe = Expression.Parameter(typeof(T), "v");
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            var pe1 = Expression.Parameter(typeof(T1), "v1");
            var pe2 = Expression.Parameter(typeof(T2), "v2");
            var pe3 = Expression.Parameter(typeof(T3), "v3");
            var pe4 = Expression.Parameter(typeof(T4), "v4");
            var pe5 = Expression.Parameter(typeof(T5), "v5");
            return Expression.Lambda<Action<T, T0, T1, T2, T3, T4, T5>>(Expression.Block(Expression.Call(pe, mi, pe0, pe1, pe2, pe3, pe4, pe5)), pe, pe0, pe1, pe2, pe3, pe4, pe5).Compile();
        }
        public static Action<T, T0, T1, T2, T3, T4, T5, T6> InstA<T, T0, T1, T2, T3, T4, T5, T6>(MethodInfo mi)
        {
            var pe = Expression.Parameter(typeof(T), "v");
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            var pe1 = Expression.Parameter(typeof(T1), "v1");
            var pe2 = Expression.Parameter(typeof(T2), "v2");
            var pe3 = Expression.Parameter(typeof(T3), "v3");
            var pe4 = Expression.Parameter(typeof(T4), "v4");
            var pe5 = Expression.Parameter(typeof(T5), "v5");
            var pe6 = Expression.Parameter(typeof(T6), "v6");
            return Expression.Lambda<Action<T, T0, T1, T2, T3, T4, T5, T6>>(Expression.Block(Expression.Call(pe, mi, pe0, pe1, pe2, pe3, pe4, pe5, pe6)), pe, pe0, pe1, pe2, pe3, pe4, pe5, pe6).Compile();
        }
        public static Action<T, T0, T1, T2, T3, T4, T5, T6, T7> InstA<T, T0, T1, T2, T3, T4, T5, T6, T7>(MethodInfo mi)
        {
            var pe = Expression.Parameter(typeof(T), "v");
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            var pe1 = Expression.Parameter(typeof(T1), "v1");
            var pe2 = Expression.Parameter(typeof(T2), "v2");
            var pe3 = Expression.Parameter(typeof(T3), "v3");
            var pe4 = Expression.Parameter(typeof(T4), "v4");
            var pe5 = Expression.Parameter(typeof(T5), "v5");
            var pe6 = Expression.Parameter(typeof(T6), "v6");
            var pe7 = Expression.Parameter(typeof(T7), "v7");
            return Expression.Lambda<Action<T, T0, T1, T2, T3, T4, T5, T6, T7>>(Expression.Block(Expression.Call(pe, mi, pe0, pe1, pe2, pe3, pe4, pe5, pe6, pe7)), pe, pe0, pe1, pe2, pe3, pe4, pe5, pe6, pe7).Compile();
        }
        public static Func<T> InstF<T>(MethodInfo mi)
        {
            var pe = Expression.Parameter(typeof(T), "v");
            return Expression.Lambda<Func<T>>(Expression.Block(Expression.Call(pe, mi)), pe).Compile();
        }
        public static Func<T, T0> InstF<T, T0>(MethodInfo mi)
        {
            var pe = Expression.Parameter(typeof(T), "v");
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            return Expression.Lambda<Func<T, T0>>(Expression.Block(Expression.Call(pe, mi, pe0)), pe, pe0).Compile();
        }
        public static Func<T, T0, T1> InstF<T, T0, T1>(MethodInfo mi)
        {
            var pe = Expression.Parameter(typeof(T), "v");
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            var pe1 = Expression.Parameter(typeof(T1), "v1");
            return Expression.Lambda<Func<T, T0, T1>>(Expression.Block(Expression.Call(pe, mi, pe0, pe1)), pe, pe0, pe1).Compile();
        }
        public static Func<T, T0, T1, T2> InstF<T, T0, T1, T2>(MethodInfo mi)
        {
            var pe = Expression.Parameter(typeof(T), "v");
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            var pe1 = Expression.Parameter(typeof(T1), "v1");
            var pe2 = Expression.Parameter(typeof(T2), "v2");
            return Expression.Lambda<Func<T, T0, T1, T2>>(Expression.Block(Expression.Call(pe, mi, pe0, pe1, pe2)), pe, pe0, pe1, pe2).Compile();
        }
        public static Func<T, T0, T1, T2, T3> InstF<T, T0, T1, T2, T3>(MethodInfo mi)
        {
            var pe = Expression.Parameter(typeof(T), "v");
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            var pe1 = Expression.Parameter(typeof(T1), "v1");
            var pe2 = Expression.Parameter(typeof(T2), "v2");
            var pe3 = Expression.Parameter(typeof(T3), "v3");
            return Expression.Lambda<Func<T, T0, T1, T2, T3>>(Expression.Block(Expression.Call(pe, mi, pe0, pe1, pe2, pe3)), pe, pe0, pe1, pe2, pe3).Compile();
        }
        public static Func<T, T0, T1, T2, T3, T4> InstF<T, T0, T1, T2, T3, T4>(MethodInfo mi)
        {
            var pe = Expression.Parameter(typeof(T), "v");
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            var pe1 = Expression.Parameter(typeof(T1), "v1");
            var pe2 = Expression.Parameter(typeof(T2), "v2");
            var pe3 = Expression.Parameter(typeof(T3), "v3");
            var pe4 = Expression.Parameter(typeof(T4), "v4");
            return Expression.Lambda<Func<T, T0, T1, T2, T3, T4>>(Expression.Block(Expression.Call(pe, mi, pe0, pe1, pe2, pe3, pe4)), pe, pe0, pe1, pe2, pe3, pe4).Compile();
        }
        public static Func<T, T0, T1, T2, T3, T4, T5> InstF<T, T0, T1, T2, T3, T4, T5>(MethodInfo mi)
        {
            var pe = Expression.Parameter(typeof(T), "v");
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            var pe1 = Expression.Parameter(typeof(T1), "v1");
            var pe2 = Expression.Parameter(typeof(T2), "v2");
            var pe3 = Expression.Parameter(typeof(T3), "v3");
            var pe4 = Expression.Parameter(typeof(T4), "v4");
            var pe5 = Expression.Parameter(typeof(T5), "v5");
            return Expression.Lambda<Func<T, T0, T1, T2, T3, T4, T5>>(Expression.Block(Expression.Call(pe, mi, pe0, pe1, pe2, pe3, pe4, pe5)), pe, pe0, pe1, pe2, pe3, pe4, pe5).Compile();
        }
        public static Func<T, T0, T1, T2, T3, T4, T5, T6> InstF<T, T0, T1, T2, T3, T4, T5, T6>(MethodInfo mi)
        {
            var pe = Expression.Parameter(typeof(T), "v");
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            var pe1 = Expression.Parameter(typeof(T1), "v1");
            var pe2 = Expression.Parameter(typeof(T2), "v2");
            var pe3 = Expression.Parameter(typeof(T3), "v3");
            var pe4 = Expression.Parameter(typeof(T4), "v4");
            var pe5 = Expression.Parameter(typeof(T5), "v5");
            var pe6 = Expression.Parameter(typeof(T6), "v6");
            return Expression.Lambda<Func<T, T0, T1, T2, T3, T4, T5, T6>>(Expression.Block(Expression.Call(pe, mi, pe0, pe1, pe2, pe3, pe4, pe5, pe6)), pe, pe0, pe1, pe2, pe3, pe4, pe5, pe6).Compile();
        }
        public static Func<T, T0, T1, T2, T3, T4, T5, T6, T7> InstF<T, T0, T1, T2, T3, T4, T5, T6, T7>(MethodInfo mi)
        {
            var pe = Expression.Parameter(typeof(T), "v");
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            var pe1 = Expression.Parameter(typeof(T1), "v1");
            var pe2 = Expression.Parameter(typeof(T2), "v2");
            var pe3 = Expression.Parameter(typeof(T3), "v3");
            var pe4 = Expression.Parameter(typeof(T4), "v4");
            var pe5 = Expression.Parameter(typeof(T5), "v5");
            var pe6 = Expression.Parameter(typeof(T6), "v6");
            var pe7 = Expression.Parameter(typeof(T7), "v7");
            return Expression.Lambda<Func<T, T0, T1, T2, T3, T4, T5, T6, T7>>(Expression.Block(Expression.Call(pe, mi, pe0, pe1, pe2, pe3, pe4, pe5, pe6, pe7)), pe, pe0, pe1, pe2, pe3, pe4, pe5, pe6, pe7).Compile();
        }
        public static Func<T> NewF<T>()
        {
            return Expression.Lambda<Func<T>>(Expression.Block(Expression.New(typeof(T)))).Compile();
        }

        public static Func<T0> NewF<T, T0>(MethodInfo mi)
        {
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            return Expression.Lambda<Func<T0>>(Expression.Block(Expression.New(typeof(T).GetConstructor(new Type[] { typeof(T0) }), pe0)), pe0).Compile();
        }
        public static Func<T0, T1> NewF<T, T0, T1>(MethodInfo mi)
        {
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            var pe1 = Expression.Parameter(typeof(T1), "v1");
            return Expression.Lambda<Func<T0, T1>>(Expression.Block(Expression.New(typeof(T).GetConstructor(new Type[] { typeof(T0), typeof(T1) }), pe0, pe1)), pe0, pe1).Compile();
        }
        public static Func<T0, T1, T2> NewF<T, T0, T1, T2>(MethodInfo mi)
        {
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            var pe1 = Expression.Parameter(typeof(T1), "v1");
            var pe2 = Expression.Parameter(typeof(T2), "v2");
            return Expression.Lambda<Func<T0, T1, T2>>(Expression.Block(Expression.New(typeof(T).GetConstructor(new Type[] { typeof(T0), typeof(T1), typeof(T2) }), pe0, pe1, pe2)), pe0, pe1, pe2).Compile();
        }
        public static Func<T0, T1, T2, T3> NewF<T, T0, T1, T2, T3>(MethodInfo mi)
        {
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            var pe1 = Expression.Parameter(typeof(T1), "v1");
            var pe2 = Expression.Parameter(typeof(T2), "v2");
            var pe3 = Expression.Parameter(typeof(T3), "v3");
            return Expression.Lambda<Func<T0, T1, T2, T3>>(Expression.Block(Expression.New(typeof(T).GetConstructor(new Type[] { typeof(T0), typeof(T1), typeof(T2), typeof(T3) }), pe0, pe1, pe2, pe3)), pe0, pe1, pe2, pe3).Compile();
        }
        public static Func<T0, T1, T2, T3, T4> NewF<T, T0, T1, T2, T3, T4>(MethodInfo mi)
        {
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            var pe1 = Expression.Parameter(typeof(T1), "v1");
            var pe2 = Expression.Parameter(typeof(T2), "v2");
            var pe3 = Expression.Parameter(typeof(T3), "v3");
            var pe4 = Expression.Parameter(typeof(T4), "v4");
            return Expression.Lambda<Func<T0, T1, T2, T3, T4>>(Expression.Block(Expression.New(typeof(T).GetConstructor(new Type[] { typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4) }), pe0, pe1, pe2, pe3, pe4)), pe0, pe1, pe2, pe3, pe4).Compile();
        }
        public static Func<T0, T1, T2, T3, T4, T5> NewF<T, T0, T1, T2, T3, T4, T5>(MethodInfo mi)
        {
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            var pe1 = Expression.Parameter(typeof(T1), "v1");
            var pe2 = Expression.Parameter(typeof(T2), "v2");
            var pe3 = Expression.Parameter(typeof(T3), "v3");
            var pe4 = Expression.Parameter(typeof(T4), "v4");
            var pe5 = Expression.Parameter(typeof(T5), "v5");
            return Expression.Lambda<Func<T0, T1, T2, T3, T4, T5>>(Expression.Block(Expression.New(typeof(T).GetConstructor(new Type[] { typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) }), pe0, pe1, pe2, pe3, pe4, pe5)), pe0, pe1, pe2, pe3, pe4, pe5).Compile();
        }
        public static Func<T0, T1, T2, T3, T4, T5, T6> NewF<T, T0, T1, T2, T3, T4, T5, T6>(MethodInfo mi)
        {
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            var pe1 = Expression.Parameter(typeof(T1), "v1");
            var pe2 = Expression.Parameter(typeof(T2), "v2");
            var pe3 = Expression.Parameter(typeof(T3), "v3");
            var pe4 = Expression.Parameter(typeof(T4), "v4");
            var pe5 = Expression.Parameter(typeof(T5), "v5");
            var pe6 = Expression.Parameter(typeof(T6), "v6");
            return Expression.Lambda<Func<T0, T1, T2, T3, T4, T5, T6>>(Expression.Block(Expression.New(typeof(T).GetConstructor(new Type[] { typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6) }), pe0, pe1, pe2, pe3, pe4, pe5, pe6)), pe0, pe1, pe2, pe3, pe4, pe5, pe6).Compile();
        }
        public static Func<T0, T1, T2, T3, T4, T5, T6, T7> NewF<T, T0, T1, T2, T3, T4, T5, T6, T7>(MethodInfo mi)
        {
            var pe0 = Expression.Parameter(typeof(T0), "v0");
            var pe1 = Expression.Parameter(typeof(T1), "v1");
            var pe2 = Expression.Parameter(typeof(T2), "v2");
            var pe3 = Expression.Parameter(typeof(T3), "v3");
            var pe4 = Expression.Parameter(typeof(T4), "v4");
            var pe5 = Expression.Parameter(typeof(T5), "v5");
            var pe6 = Expression.Parameter(typeof(T6), "v6");
            var pe7 = Expression.Parameter(typeof(T7), "v7");
            return Expression.Lambda<Func<T0, T1, T2, T3, T4, T5, T6, T7>>(Expression.Block(Expression.New(typeof(T).GetConstructor(new Type[] { typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7) }), pe0, pe1, pe2, pe3, pe4, pe5, pe6, pe7)), pe0, pe1, pe2, pe3, pe4, pe5, pe6, pe7).Compile();
        }

    }
    [Feng]
    public class Sankaku2
    {

        [Feng("qv17do")]
        public static void qv17do()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var basePt = Dbg.SelectPoint();
                var dg = new DrainageSystemDiagram();
                dg.Draw(basePt);
            }
        }



        [Feng("排出方式连线")]
        public static void qv1735()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                {
                    //480 1080 
                    {
                        var bsPt = Dbg.SelectPoint();
                        var points = new Point2d[] { new Point2d(0, 0), new Point2d(-121, -121), new Point2d(-2000, -121) };
                        var segs = points.CreateGLineSegments(bsPt);
                        DU.DrawLineSegmentsLazy(segs);
                    }
                    {
                        var bsPt = Dbg.SelectPoint();
                        var points = new Point2d[] { new Point2d(0, 0), new Point2d(-121, -121), new Point2d(-5300, -121) };
                        var segs = points.CreateGLineSegments(bsPt);
                        DU.DrawLineSegmentsLazy(segs);
                    }
                    {
                        var bsPt = Dbg.SelectPoint();
                        var points = new Point2d[] { new Point2d(0, 0), new Point2d(0, -1379), new Point2d(-121, -1500), new Point2d(-5900, -1500) };
                        var segs = points.CreateGLineSegments(bsPt);
                        DU.DrawLineSegmentsLazy(segs);
                    }
                }
            }
        }
        [Feng("立管检查口")]
        public static void qv17o4()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var bsPt = Dbg.SelectPoint();
                Dbg.ShowXLabel(bsPt);
                //left
                DU.DrawBlockReference(blkName: "立管检查口", basePt: bsPt,
               cb: br =>
               {
                   br.ScaleFactors = new Scale3d(-1, 1, 1);
                   br.Layer = "W-DRAI-EQPM";
               });
                //right
                DU.DrawBlockReference(blkName: "立管检查口", basePt: bsPt,
               cb: br =>
               {
                   br.Layer = "W-DRAI-EQPM";
               });
            }

        }
        [Feng("污废合流井编号")]
        public static void qv16xv()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var bsPt = Dbg.SelectPoint();
                Dbg.ShowXLabel(bsPt);
                DU.DrawBlockReference(blkName: "污废合流井编号", basePt: bsPt,
               scale: 0.5,
               props: new Dictionary<string, string>() { { "-", "666" } },
               cb: br =>
               {
                   br.Layer = "W-DRAI-EQPM";
               });
            }
        }
        [Feng("阳台支管块")]
        public static void qv16vz()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var bsPt = Dbg.SelectPoint();
                Dbg.ShowXLabel(bsPt);
                DU.DrawBlockReference("阳台支管块", bsPt, br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.Rotation = GeoAlgorithm.AngleFromDegree(270);
                });
            }
        }
        [Feng("套管系统")]
        public static void qv16k0()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var bsPt = Dbg.SelectPoint();
                Dbg.ShowXLabel(bsPt);
                DU.DrawBlockReference("套管系统", bsPt, br =>
                {
                    br.Layer = "W-BUSH";
                });
            }
        }
        [Feng("地漏系统1")]
        public static void qv1o5m()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var bsPt = Dbg.SelectPoint();
                Dbg.ShowXLabel(bsPt);
                //left
                DU.DrawBlockReference("地漏系统1", bsPt, br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.ScaleFactors = new Scale3d(2, 2, 2);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue("可见性", "普通地漏P弯");
                    }
                });
                //right
                DU.DrawBlockReference("地漏系统1", bsPt,
                    br =>
                    {
                        br.Layer = "W-DRAI-EQPM";
                        br.ScaleFactors = new Scale3d(-2, 2, 2);
                        if (br.IsDynamicBlock)
                        {
                            br.ObjectId.SetDynBlockValue("可见性", "普通地漏P弯");
                        }
                    });
            }
        }
        [Feng("S型存水弯")]
        public static void qv1mr0()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var bsPt = Dbg.SelectPoint();
                Dbg.ShowXLabel(bsPt);
                //left
                DU.DrawBlockReference("S型存水弯", bsPt, br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.ScaleFactors = new Scale3d(2, 2, 2);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue("可见性", "板上S弯");
                    }
                });
                //right
                DU.DrawBlockReference("S型存水弯", bsPt,
                    br =>
                    {
                        br.Layer = "W-DRAI-EQPM";
                        br.ScaleFactors = new Scale3d(-2, 2, 2);
                        if (br.IsDynamicBlock)
                        {
                            br.ObjectId.SetDynBlockValue("可见性", "板上S弯");
                        }
                    });
            }
        }
        [Feng("洗涤盆排水")]
        public static void qv15o8()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var bsPt = Dbg.SelectPoint();
                Dbg.ShowXLabel(bsPt);
                //left
                DU.DrawBlockReference("洗涤盆排水", bsPt, br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.ScaleFactors = new Scale3d(2, 2, 2);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue("可见性", "双池S弯");
                    }
                });
                //right
                DU.DrawBlockReference("双格洗涤盆排水", bsPt,
                    br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.ScaleFactors = new Scale3d(-2, 2, 2);
                    if (br.IsDynamicBlock)
                    {
                        br.ObjectId.SetDynBlockValue("可见性", "双池S弯");
                    }
                });
            }
        }
        [Feng("清扫口系统")]
        public static void qv15je()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var bsPt = Dbg.SelectPoint();
                //left
                DU.DrawBlockReference("清扫口系统", bsPt, scale: 2, cb: br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.Rotation = GeoAlgorithm.AngleFromDegree(90);
                });
                //right
                DU.DrawBlockReference("清扫口系统", bsPt, scale: 2, cb: br =>
                {
                    br.Layer = "W-DRAI-EQPM";
                    br.Rotation = GeoAlgorithm.AngleFromDegree(90 + 180);
                });
            }
        }
        [Feng("翻转测试")]
        public static void qv147n()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var bsPt = Dbg.SelectPoint();
                {
                    var points = new Point2d[] { new Point2d(0, 0), new Point2d(-121, -121), new Point2d(-1379, -121), new Point2d(-1500, -241) };
                    {
                        var segs = points.CreateGLineSegments(bsPt);
                        DU.DrawLineSegmentsLazy(segs);
                    }
                    {
                        var m = Matrix2dFac.YAxisMirroring;
                        var segs = points.CreateGLineSegments(bsPt, m);
                        DU.DrawLineSegmentsLazy(segs);
                    }
                }

            }
        }
        [Feng("雨水管径100")]
        public static void quev3n()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                //DU.DrawBlockReference(blkName: "雨水管径100", basePt:Dbg.SelectPoint(), layer: "W-NOTE", props: new Dictionary<string, string>() { { "可见性", /*Dr.GetFloorDrainDN()*/"DN25" }, { "角度1", "90" } });
                //DU.DrawBlockReference(blkName: "雨水管径100", basePt: Dbg.SelectPoint(), layer: "W-NOTE", cb: br =>
                //{
                //    br.ObjectId.SetDynBlockValue("可见性", Dr.GetFloorDrainDN());
                //});

                //DU.DrawBlockReference(blkName: "雨水管径100",scale:.5, basePt: Dbg.SelectPoint(), layer: "W-NOTE", cb: br =>
                DU.DrawBlockReference(blkName: "雨水管径100", scale: 1, basePt: Dbg.SelectPoint().OffsetX(600), layer: "W-NOTE", cb: br =>
                {
                    br.ObjectId.SetDynBlockValue("可见性", Dr.GetFloorDrainDN());
                    //br.ObjectId.SetDynBlockValue("角度1", Math.PI / 2);
                    br.ObjectId.SetDynBlockValue("角度1", Math.PI);
                });
            }

        }
        [Feng("画标高")]
        public static void NewMethod6()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var pt = Dbg.SelectPoint();
                ThRainSystemService.ImportElementsFromStdDwg();
                DU.DrawBlockReference(blkName: "标高", basePt: pt, layer: "W-NOTE", props: new Dictionary<string, string>() { { "标高", "666" } });
            }
        }
        [Feng("画系统图草稿1")]
        public static void qu600o()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var OFFSET_X = 2500.0;
                var SPAN_X = 5500.0;
                var HEIGHT = 1800.0;
                var COUNT = 20;
                var basePt = Dbg.SelectPoint();
                var lineLen = OFFSET_X + COUNT * SPAN_X + OFFSET_X;
                var storeys = Enumerable.Range(1, 32).Select(i => i + "F").Concat(new string[] { "RF", "RF+1", "RF+2" }).ToList();
                for (int i = 0; i < storeys.Count; i++)
                {
                    var storey = storeys[i];
                    var bsPt1 = basePt.OffsetY(HEIGHT * i);
                    DrainageSystemDiagram.DrawStoreyLine(storey, bsPt1, lineLen);
                    for (int j = 0; j < COUNT; j++)
                    {
                        var bsPt2 = bsPt1.OffsetX(OFFSET_X + (j + 1) * SPAN_X);

                        if (j == 0)
                        {
                            Dr.DrawCheckPoint(bsPt2);
                        }
                        else if (j == 1)
                        {
                            DU.DrawBlockReference("清扫口系统", bsPt2.OffsetY(HEIGHT - 300), scale: 2, cb: br =>
                            {
                                br.Layer = "W-DRAI-EQPM";
                                br.Rotation = GeoAlgorithm.AngleFromDegree(90);
                            });
                        }
                        else if (j == 2)
                        {
                            DU.DrawBlockReference("洗涤盆排水", bsPt2.OffsetXY(-1000, HEIGHT - 1720 - 80 + 200), br =>
                            {
                                br.Layer = "W-DRAI-EQPM";
                                if (br.IsDynamicBlock)
                                {
                                    br.ObjectId.SetDynBlockValue("可见性", "双池S弯");
                                }
                            });
                            {
                                var line = DU.DrawLineLazy(bsPt2.OffsetXY(-1000, 200), bsPt2.OffsetXY(0, 200));
                                line.Layer = "W-DRAI-DOME-PIPE";
                                line.ColorIndex = 256;
                            }
                        }
                        else if (j == 3)
                        {
                            Dr.DrawFloorDrain(bsPt2.OffsetX(180 - 1000));
                            {
                                var line = DU.DrawLineLazy(bsPt2.OffsetXY(-1000, -550), bsPt2.OffsetXY(0, -550));
                                line.Layer = "W-DRAI-DOME-PIPE";
                                line.ColorIndex = 256;
                            }
                        }
                        else if (j == 4)
                        {
                            DU.DrawBlockReference("阳台支管块", bsPt2.OffsetXY(-328257, 35827), br =>
                            {
                                br.Layer = "W-DRAI-EQPM";
                                br.Rotation = GeoAlgorithm.AngleFromDegree(270);
                            });
                        }
                        else if (j == 5)
                        {
                            var line = DU.DrawLineLazy(bsPt2.OffsetXY(-200, HEIGHT - 550), bsPt2.OffsetXY(200, HEIGHT - 550));
                            line.Layer = "W-DRAI-NOTE";
                            line.ColorIndex = 256;
                        }
                    }
                }
                for (int j = 0; j < COUNT; j++)
                {
                    var x = basePt.X + OFFSET_X + (j + 1) * SPAN_X;
                    var y1 = basePt.Y;
                    var y2 = y1 + HEIGHT * (storeys.Count - 1);
                    {
                        var line = DU.DrawLineLazy(x, y1, x, y2);
                        line.Layer = "W-RAIN-PIPE";
                        line.ColorIndex = 256;
                    }
                }

                {
                    var bsPt = basePt.OffsetY(-1000);
                    DU.DrawBlockReference(blkName: "重力流雨水井编号", basePt: bsPt,
                   scale: 0.5,
                   props: new Dictionary<string, string>() { { "-", "666" } },
                   cb: br =>
                   {
                       br.Layer = "W-RAIN-EQPM";
                   });
                }
                {
                    var bsPt = basePt.OffsetXY(500, -1000);
                    DU.DrawBlockReference(blkName: "污废合流井编号", basePt: bsPt,
                   scale: 0.5,
                   props: new Dictionary<string, string>() { { "-", "666" } },
                   cb: br =>
                   {
                       br.Layer = "W-DRAI-EQPM";
                   });
                }
            }
        }
        [Feng("画系统图草稿2")]
        public static void qu5x6k()
        {
            NewMethod5();
        }
        [Feng("画各种形状1")]
        public static void qu5xo6()
        {
            var i = 0;
            Action fs = null;
            fs += () => Dbg.FocusMainWindow();
            void f(Point2d[] points)
            {
                FengDbgTesting.AddLazyAction((++i).ToString(), adb =>
                {
                    var basePt = Dbg.SelectPoint().ToPoint2d();
                    DU.DrawLinesLazy(points.Select(p => p + basePt.ToVector2d()).ToArray()).ForEach(e => { e.ColorIndex = 256; e.Layer = "W-RAIN-PIPE"; });
                });
                fs += () =>
                {

                    using (Dbg.DocumentLock)
                    using (var adb = AcadDatabase.Active())
                    using (var tr = new DrawingTransaction(adb))
                    {
                        Dbg.BuildAndSetCurrentLayer(adb.Database);

                        var basePt = Dbg.SelectPoint().ToPoint2d();
                        DU.DrawLinesLazy(points.Select(p => p + basePt.ToVector2d()).ToArray()).ForEach(e => { e.ColorIndex = 256; e.Layer = "W-RAIN-PIPE"; });
                    }
                    //Autodesk.AutoCAD.ApplicationServices.Application.UpdateScreen();
                };
            }
            {
                FengDbgTesting.AddButton("全画", () => fs?.Invoke());
            }

            {
                var points = new Point2d[] { new Point2d(0, 0), new Point2d(-100, 100), new Point2d(-1200, 100) };
                f(points);
            }
            {
                var points = new Point2d[] { new Point2d(0, 0), new Point2d(-120, -120), new Point2d(-1380, -120), new Point2d(-1500, -240) };
                f(points);
            }
            {
                var points = new Point2d[] { new Point2d(0, 0), new Point2d(-200, -200), new Point2d(-1800, -200) };
                f(points);
            }
            {
                var points = new Point2d[] { new Point2d(0, 0), new Point2d(150, 150) };
                f(points);
            }
        }
        [Feng("画各种图块1")]
        public static void qu60i7()
        {
            FengDbgTesting.AddLazyAction("通气帽系统", adb =>
            {

            });
        }
        [Feng("画各种形状2")]
        public static void qu5x6f()
        {
            FengDbgTesting.AddLazyAction("1", adb =>
            {
                var basePt = Dbg.SelectPoint().ToPoint2d();
                var points = new Point2d[] { new Point2d(0, 0), new Point2d(-200, 200), new Point2d(-200, 500), new Point2d(-79, 621), new Point2d(1029, 621), new Point2d(1150, 741), new Point2d(1150, 1021) };
                DU.DrawLinesLazy(points.Select(p => p + basePt.ToVector2d()).ToArray()).ForEach(e => { e.ColorIndex = 256; e.Layer = "W-DRAI-DOME-PIPE"; });
            });
            FengDbgTesting.AddLazyAction("2", adb =>
            {
                var basePt = Dbg.SelectPoint().ToPoint2d();
                var points = new Point2d[] { new Point2d(0, 0), new Point2d(-121, -121), new Point2d(-1379, -121), new Point2d(-1500, -241) };
                DU.DrawLinesLazy(points.Select(p => p + basePt.ToVector2d()).ToArray()).ForEach(e => { e.ColorIndex = 256; e.Layer = "W-DRAI-DOME-PIPE"; });
            });
            FengDbgTesting.AddLazyAction("3", adb =>
            {
                var basePt = Dbg.SelectPoint().ToPoint2d();
                var points = new Point2d[] { new Point2d(0, 0), new Point2d(-121, -121), new Point2d(-3879, -121), new Point2d(-4000, -241) };
                DU.DrawLinesLazy(points.Select(p => p + basePt.ToVector2d()).ToArray()).ForEach(e => { e.ColorIndex = 256; e.Layer = "W-DRAI-DOME-PIPE"; });
            });
            FengDbgTesting.AddLazyAction("4", adb =>
            {
                var basePt = Dbg.SelectPoint().ToPoint2d();
                var points = new Point2d[] { new Point2d(0, 0), new Point2d(1779, 0), new Point2d(1900, 121), new Point2d(1900, 700) };
                DU.DrawLinesLazy(points.Select(p => p + basePt.ToVector2d()).ToArray()).ForEach(e => { e.ColorIndex = 256; e.Layer = "W-DRAI-DOME-PIPE"; });
            });
            FengDbgTesting.AddLazyAction("5", adb =>
            {
                var points = new Point2d[] { new Point2d(0, 0), new Point2d(-121, -121), new Point2d(-2500, -121) };
                var basePt = Dbg.SelectPoint().ToPoint2d();
                DU.DrawLinesLazy(points.Select(p => p + basePt.ToVector2d()).ToArray()).ForEach(e => { e.ColorIndex = 256; e.Layer = "W-DRAI-DOME-PIPE"; });
            });
            FengDbgTesting.AddLazyAction("6", adb => { });
            FengDbgTesting.AddLazyAction("7", adb => { });
            FengDbgTesting.AddLazyAction("8", adb => { });
        }
        [Feng("根据点收集点数据")]
        public static void qv1dze()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var points = new List<Point3d>();
                while (Dbg.TrySelectPoint(out Point3d pt))
                {
                    points.Add(pt);
                }
                var v = points[0].ToVector3d();
                for (int i = 0; i < points.Count; i++)
                {
                    points[i] = points[i] - v;
                }
                Dbg.PrintLine($"var points=new Point2d[]{{{points.Select(p => $"new Point2d({Convert.ToInt64(p.X)},{Convert.ToInt64(p.Y)})").JoinWith(",")}}};");
            }
        }
        [Feng("根据线收集点数据")]
        public static void qu5x68()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var basePt = Dbg.SelectPoint().ToPoint2d();
                var lines = Dbg.SelectEntities(adb).OfType<Line>().ToList();
                if (lines.Count > 0)
                {
                    //var points = new List<Point2d>();
                    //points.Add(lines[0].StartPoint.ToPoint2d());
                    //points.Add(lines[0].EndPoint.ToPoint2d());
                    //for (int i = 1; i < lines.Count; i++)
                    //{
                    //    points.Add(lines[i].EndPoint.ToPoint2d());
                    //}
                    //Dbg.PrintLine(basePt.ToPoint2d().ToCadJson());
                    //Dbg.PrintLine(points.ToCadJson());
                    var points = new List<Point2d>() { Point2d.Origin };
                    var curPt = basePt;
                    foreach (var line in lines)
                    {
                        var p1 = line.StartPoint.ToPoint2d();
                        var p2 = line.EndPoint.ToPoint2d();
                        if (p1.GetDistanceTo(curPt) < 1)
                        {
                            points.Add((p2 - basePt).ToPoint2d());
                            curPt = p2;
                        }
                        else if (p2.GetDistanceTo(curPt) < 1)
                        {
                            points.Add((p1 - basePt).ToPoint2d());
                            curPt = p1;
                        }
                        else
                        {
                            Dbg.PrintLine("err");
                            return;
                        }
                    }
                    //Dbg.PrintLine(points.Select(p=>p.ToLongPoint2d()).ToCadJson());
                    Dbg.PrintLine($"var points=new Point2d[]{{{points.Select(p => $"new Point2d({Convert.ToInt64(p.X)},{Convert.ToInt64(p.Y)})").JoinWith(",")}}};");
                }

            }
        }
        private static void NewMethod5()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var OFFSET_X = 2500.0;
                var SPAN_X = 5500.0;
                var HEIGHT = 1800.0;
                var COUNT = 20;
                var basePt = Dbg.SelectPoint();
                var lineLen = OFFSET_X + COUNT * SPAN_X + OFFSET_X;
                var storeys = Enumerable.Range(1, 32).Select(i => i + "F").Concat(new string[] { "RF", "RF+1", "RF+2" }).ToList();
                for (int i = 0; i < storeys.Count; i++)
                {
                    var storey = storeys[i];
                    var bsPt1 = basePt.OffsetY(HEIGHT * i);
                    DrainageSystemDiagram.DrawStoreyLine(storey, bsPt1, lineLen);
                    for (int j = 0; j < COUNT; j++)
                    {
                        var bsPt2 = bsPt1.OffsetX(OFFSET_X + (j + 1) * SPAN_X);

                        if (j == 0)
                        {
                            Dr.DrawCheckPoint(bsPt2);
                        }
                        else if (j == 1)
                        {
                            DU.DrawBlockReference("清扫口系统", bsPt2.OffsetY(HEIGHT - 300), scale: 2, cb: br =>
                            {
                                br.Layer = "W-DRAI-EQPM";
                                br.Rotation = GeoAlgorithm.AngleFromDegree(90);
                            });
                        }
                        else if (j == 2)
                        {
                            DU.DrawBlockReference("洗涤盆排水", bsPt2.OffsetXY(-1000, HEIGHT - 1720 - 80 + 200), br =>
                            {
                                br.Layer = "W-DRAI-EQPM";
                                if (br.IsDynamicBlock)
                                {
                                    br.ObjectId.SetDynBlockValue("可见性", "双池S弯");
                                }
                            });
                            {
                                var line = DU.DrawLineLazy(bsPt2.OffsetXY(-1000, 200), bsPt2.OffsetXY(0, 200));
                                line.Layer = "W-DRAI-DOME-PIPE";
                                line.ColorIndex = 256;
                            }
                        }
                        else if (j == 3)
                        {
                            Dr.DrawFloorDrain(bsPt2.OffsetX(180 - 1000));
                            {
                                var line = DU.DrawLineLazy(bsPt2.OffsetXY(-1000, -550), bsPt2.OffsetXY(0, -550));
                                line.Layer = "W-DRAI-DOME-PIPE";
                                line.ColorIndex = 256;
                            }
                        }
                        else if (j == 4)
                        {
                            DU.DrawBlockReference("阳台支管块", bsPt2.OffsetXY(-328257, 35827), br =>
                            {
                                br.Layer = "W-DRAI-EQPM";
                                br.Rotation = GeoAlgorithm.AngleFromDegree(270);
                            });
                        }
                        else if (j == 5)
                        {
                            var line = DU.DrawLineLazy(bsPt2.OffsetXY(-200, HEIGHT - 550), bsPt2.OffsetXY(200, HEIGHT - 550));
                            line.Layer = "W-DRAI-NOTE";
                            line.ColorIndex = 256;
                        }
                    }
                }
                for (int j = 0; j < COUNT; j++)
                {
                    var x = basePt.X + OFFSET_X + (j + 1) * SPAN_X;
                    var y1 = basePt.Y;
                    var y2 = y1 + HEIGHT * (storeys.Count - 1);
                    {
                        var line = DU.DrawLineLazy(x, y1, x, y2);
                        line.Layer = "W-DRAI-DOME-PIPE";
                        line.ColorIndex = 256;
                    }
                    {
                        var line = DU.DrawLineLazy(x + 300, y1, x + 300, y2);
                        line.Layer = "W-DRAI-VENT-PIPE";
                        line.ColorIndex = 256;
                    }
                }

                {
                    var bsPt = basePt.OffsetY(-1000);
                    DU.DrawBlockReference(blkName: "重力流雨水井编号", basePt: bsPt,
                   scale: 0.5,
                   props: new Dictionary<string, string>() { { "-", "666" } },
                   cb: br =>
                   {
                       br.Layer = "W-RAIN-EQPM";
                   });
                }
                {
                    var bsPt = basePt.OffsetXY(500, -1000);
                    DU.DrawBlockReference(blkName: "污废合流井编号", basePt: bsPt,
                   scale: 0.5,
                   props: new Dictionary<string, string>() { { "-", "666" } },
                   cb: br =>
                   {
                       br.Layer = "W-DRAI-EQPM";
                   });
                }
            }
        }

        private static void NewMethod4()
        {
            var OFFSET_X = 2500;
            var SPAN_X = 5500;
            var HEIGHT = 1800;
            var COUNT = 20;
            var basePt = Dbg.SelectPoint();
            var lineLen = OFFSET_X + COUNT * SPAN_X + OFFSET_X;
            var storeys = Enumerable.Range(1, 32).Select(i => i + "F").Concat(new string[] { "RF", "RF+1", "RF+2" }).ToList();
            for (int i = 0; i < storeys.Count; i++)
            {
                var storey = storeys[i];
                var bsPt1 = basePt.OffsetY(HEIGHT * i);
                DrainageSystemDiagram.DrawStoreyLine(storey, bsPt1, lineLen);
                for (int j = 0; j < COUNT; j++)
                {
                    var bsPt2 = bsPt1.OffsetX(OFFSET_X + (j + 1) * SPAN_X);
                    Dbg.ShowXLabel(bsPt2);
                }
            }
        }



    }
    [Feng]
    public class Sankaku1
    {
        static string ReadString()
        {
            var rst = AcHelper.Active.Editor.GetString("\n输入立管编号");
            if (rst.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK) return null;
            return rst.StringResult;
        }
        [Feng("👀")]
        public static void qus5uz()
        {
            Util1.FindText();
        }
        [Feng("❌")]
        public static void qus63i()
        {
            Dbg.DeleteTestGeometries();
        }
        [Feng("DrawDrainageSystemDiagram2")]
        public static void qus6ak()
        {
            DrainageService.DrawDrainageSystemDiagram2();
        }
        [Feng("保存geoData")]
        public static void qutpmu()
        {
            var geoData = DrainageService.CollectGeoData();
            var file = @"D:\DATA\temp\" + DateTime.Now.Ticks + ".json";
            File.WriteAllText(file, geoData.ToCadJson());
            Dbg.PrintLine(file);
        }
        [Feng("直接从geoData生成")]
        public static void qutpt9()
        {
            var file = @"D:\DATA\temp\637595412925029309.json";
            var geoData = File.ReadAllText(file).FromCadJson<DrainageGeoData>();
            DrainageService.TestDrawingDatasCreation(geoData);
        }
        [Feng("quw3jg")]
        public static void quw3jg()
        {

        }
        [Feng("quw29k")]
        public static void quw29k()
        {

        }

        public static List<Point2d> GetAlivePointsByNTS(List<Point2d> points, double radius)
        {
            var pts = points.Select(x => new GCircle(x, radius).ToCirclePolygon(6, false)).ToList();
            var flags = new bool[pts.Count];
            for (int i = 0; i < pts.Count; i++)
            {
                if (!flags[i])
                {
                    for (int j = 0; j < pts.Count; j++)
                    {
                        if (!flags[j])
                        {
                            if (i != j)
                            {
                                if (pts[i].Intersects(pts[j]))
                                {
                                    flags[i] = true;
                                    flags[j] = true;
                                }
                            }
                        }
                    }
                }
            }
            IEnumerable<Point2d> f()
            {
                for (int i = 0; i < pts.Count; i++)
                {
                    if (!flags[i])
                    {
                        yield return points[i];
                    }
                }
            }
            var q = f();
            return q.ToList();
        }


        public static void quu77p()
        {
            var file = @"D:\DATA\temp\637595412925029309.json";
            var geoData = File.ReadAllText(file).FromCadJson<DrainageGeoData>();
            {
                for (int i = 0; i < geoData.DLines.Count; i++)
                {
                    geoData.DLines[i] = geoData.DLines[i].Extend(5);
                }
            }
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var cadData = DrainageCadData.Create(geoData);
                var killer = GeoFac.CreateGeometryEx(cadData.VerticalPipes.Concat(cadData.WaterPorts).Concat(cadData.FloorDrains).ToList());
                var maxDis = 8000;
                var angleTolleranceDegree = 1;
                var lines = geoData.DLines.Where(x => x.Length > 0).Distinct().ToList();
                geoData.DLines.AddRange(GeoFac.AutoConn(lines, killer, maxDis, angleTolleranceDegree));
                DrainageService.DrawGeoData(geoData);
            }
        }



        [Feng("水管井")]
        public static void quqgdc()
        {
            //6.3.8	水管井的FL
            //若FL在水管井中，则认为该FL在出现的每层都安装了一个地漏。
            //水管井的判断：
            //空间名称为“水”、包含“水井”或“水管井”（持续更新）。
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var list = DrainageService.CollectRoomData(adb);
                foreach (var kv in list)
                {
                    if (kv.Key == "水" || kv.Key.Contains("水井") || kv.Key.Contains("水管井"))
                    {
                        Dbg.PrintLine(kv.Key);
                    }
                }
            }
        }
        [Feng("quqfmg")]
        public static void quqfmg()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var circles = adb.ModelSpace.OfType<Circle>().Select(x => x.ToGCircle()).ToList();
                var lines = adb.ModelSpace.OfType<Line>().Select(x => x.ToGLineSegment()).ToList();
                var cs = circles.Select(x => x.ToCirclePolygon(36)).ToList();
                var ls = lines.Select(x => x.ToLineString()).ToList();
                var gs = GeoFac.GroupGeometries(ToGeometries(cs, ls));
                var _gs = new List<List<Geometry>>();
                foreach (var g in gs)
                {
                    var _circles = g.Where(x => cs.Contains(x)).ToList();
                    var _lines = g.Where(x => ls.Contains(x)).ToList();
                    var segs = GeoFac.CreateGeometry(_lines).Difference(GeoFac.CreateGeometry(_circles)).ToDbObjects().OfType<Polyline>().SelectMany(x => x.ExplodeToDBObjectCollection().OfType<Line>()).Select(x => x.ToGLineSegment()).Where(x => x.IsValid).ToList();
                    var lst = new List<Geometry>();
                    lst.AddRange(_circles);
                    lst.AddRange(segs.Select(x => x.Extend(.1).ToLineString()));//延长一点点！
                    _gs.Add(lst);
                }
                foreach (var g in _gs)
                {
                    var _circles = g.Where(x => cs.Contains(x)).ToList();
                    foreach (var c in _circles)
                    {
                        var lst = g.ToList();
                        lst.Remove(c);
                        var f = GeoFac.CreateGeometrySelector(lst);
                        Dbg.PrintLine(f(c).Count);//OK,如果有地漏是串联的，那么这里会等于2，否则等于1
                    }

                }
            }
        }
        [Feng("在交点处打碎")]
        public static void quqqrp()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var seg1 = Dbg.SelectEntity<Line>(adb).ToGLineSegment();
                var seg2 = Dbg.SelectEntity<Line>(adb).ToGLineSegment();
                var geo = seg1.ToLineString().Union(seg2.ToLineString());//MultiLineString
                var segs = geo.ToDbCollection().OfType<Polyline>().SelectMany(x => x.ExplodeToDBObjectCollection().OfType<Line>()).Select(x => x.ToGLineSegment()).ToList();

                FengDbgTesting.AddLazyAction("", adb =>
                {
                    foreach (var seg in segs)
                    {
                        DU.DrawLineSegmentLazy(seg);
                    }
                });
            }
        }
        [Feng("qurx6s")]
        public static void qurx6s()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var circle = Dbg.SelectEntity<Circle>(adb);
                //DU.DrawGeometryLazy(circle.ToGCircle().ToCirclePolygon(6));
                DU.DrawGeometryLazy(circle.ToGCircle().ToCirclePolygon(6, false));
                //DU.DrawGeometryLazy(circle.ToGCircle().ToCirclePolygon(36));
            }
        }
        [Feng("quqdxf")]
        public static void quqdxf()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var circles = adb.ModelSpace.OfType<Circle>().Select(x => x.ToGCircle()).ToList();
                var lines = adb.ModelSpace.OfType<Line>().Select(x => x.ToGLineSegment()).ToList();
                var cs = circles.Select(x => x.ToCirclePolygon(36)).ToList();
                var ls = lines.Select(x => x.ToLineString()).ToList();
                var gs = GeoFac.GroupGeometries(ToGeometries(cs, ls));
                //Dbg.PrintLine(gs.Count);
                foreach (var g in gs)
                {
                    var _circles = g.Where(x => cs.Contains(x)).ToList();
                    var _lines = g.Where(x => ls.Contains(x)).ToList();
                    var segs = GeoFac.CreateGeometry(_lines).Difference(GeoFac.CreateGeometry(_circles)).ToDbObjects().OfType<Polyline>().SelectMany(x => x.ExplodeToDBObjectCollection().OfType<Line>()).Select(x => x.ToGLineSegment()).Where(x => x.IsValid).ToList();
                    FengDbgTesting.AddLazyAction("", adb =>
                    {
                        foreach (var c in _circles)
                        {
                            DU.DrawGeometryLazy(c);
                        }
                        foreach (var seg in segs)
                        {
                            DU.DrawLineSegmentLazy(seg);
                        }
                    });
                }
            }
        }
        public static List<Geometry> ToGeometries(IEnumerable<Geometry> geos1, IEnumerable<Geometry> geos2)
        {
            return geos1.Concat(geos2).ToList();
        }
        public static List<T> ToList<T>(IEnumerable<T> source1, IEnumerable<T> source2)
        {
            return source1.Concat(source2).ToList();
        }
        public static List<T> ToList<T>(IEnumerable<T> source1, IEnumerable<T> source2, IEnumerable<T> source3)
        {
            return source1.Concat(source2).Concat(source3).ToList();
        }
        [Feng("quqcqu")]
        public static void quqcqu()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var e1 = Dbg.SelectEntity<Line>(adb);
                var e2 = Dbg.SelectEntity<Circle>(adb);
                var g1 = e1.ToGLineSegment().ToLineString();
                var g2 = e2.ToGCircle().ToCirclePolygon(36);
                var g3 = g1.Difference(g2);
                FengDbgTesting.AddLazyAction("", _adb =>
                {
                    DU.DrawGeometryLazy(g3);
                    Dbg.PrintLine(g3.Intersects(g2));
                });
            }
        }
        [Feng("quqb3e")]
        public static void quqb3e()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                //Dbg.BuildAndSetCurrentLayer(db);
                var circles = adb.ModelSpace.OfType<Circle>().Select(x => x.ToGCircle()).ToList();
                var lines = adb.ModelSpace.OfType<Line>().Select(x => x.ToGLineSegment()).ToList();
                //[{'X':1744.5169050298846,'Y':2095.5695398475646,'Radius':656.30028012291064,'Center':{'Y':2095.5695398475646,'X':1744.5169050298846}}]
                Dbg.PrintText(circles.ToCadJson());
                //[{'type':'GLineSegment','values':[1823.533911180044,2017.6666691268156,6103.4608013348316,1256.789502112395]},{'type':'GLineSegment','values':[6103.4608013348316,1256.789502112395,6360.6679478744718,2249.0144581879167]},{'type':'GLineSegment','values':[6360.6679478744718,2249.0144581879167,5038.6232134830043,2326.1303835595991]},{'type':'GLineSegment','values':[5038.6232134830043,2326.1303835595991,4915.1637896213415,2819.6723318304757]},{'type':'GLineSegment','values':[4915.1637896213415,2819.6723318304757,6525.2805081162569,2819.6723318304757]}]
                Dbg.PrintText(lines.ToCadJson());
            }
        }
        [Feng("quqbet")]
        public static void quqbet()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var circles = "[{'X':1744.5169050298846,'Y':2095.5695398475646,'Radius':656.30028012291064,'Center':{'Y':2095.5695398475646,'X':1744.5169050298846}}]".FromCadJson<List<GCircle>>();
                var lines = "[{'type':'GLineSegment','values':[1823.533911180044,2017.6666691268156,6103.4608013348316,1256.789502112395]},{'type':'GLineSegment','values':[6103.4608013348316,1256.789502112395,6360.6679478744718,2249.0144581879167]},{'type':'GLineSegment','values':[6360.6679478744718,2249.0144581879167,5038.6232134830043,2326.1303835595991]},{'type':'GLineSegment','values':[5038.6232134830043,2326.1303835595991,4915.1637896213415,2819.6723318304757]},{'type':'GLineSegment','values':[4915.1637896213415,2819.6723318304757,6525.2805081162569,2819.6723318304757]}]".FromCadJson<List<GLineSegment>>();
                foreach (var e in circles)
                {
                    DU.DrawGeometryLazy(e);
                }
                foreach (var e in lines)
                {
                    DU.DrawLineSegmentLazy(e);
                }
                var nothing = nameof(FengDbgTesting.GetSegsToConnect);
                var h = GeoFac.LineGrouppingHelper.Create(lines);
                h.InitPointGeos(radius: 2.5);
                h.DoGroupingByPoint();
                h.CalcAlonePoints();
                h.DistinguishAlonePoints();
                foreach (var geo in h.GetAlonePoints())
                {
                    DU.DrawGeometryLazy(geo);
                }
                //去掉起点剩下的全是终点
            }
        }


        [Feng("qupz46")]
        public static void qupz46()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var e = new Leader();
                e.HasArrowHead = true;
                for (int i = 0; i < 3; i++)
                {
                    e.AppendVertex(Dbg.SelectPoint());
                }
                //e.Layer = "H-DIMS-DUCT";
                e.Dimasz = 200;
                //e.Dimtxt = 1000;
                //e.SetDimstyleData(AcHelper.Collections.Tables.GetDimStyle("TH-DIM100"));

                //Dbg.PrintLine(AcHelper.Collections.Tables.GetDimStyle("TH-DIM100").ObjectId.ToString());
                //e.SetDatabaseDefaults(db);
                DU.DrawEntityLazy(e);

            }
        }
        [Feng("quq0kg")]
        public static void quq0kg()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var e1 = Dbg.SelectEntity<Leader>(adb);
                Debugger.Break();
            }
        }
        [Feng("qupzpe")]
        public static void qupzpe()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                //var db = adb.Database;
                //Dbg.BuildAndSetCurrentLayer(db);

                var e1 = Dbg.SelectEntity<Leader>(adb);
                var e2 = Dbg.SelectEntity<Leader>(adb);
                //e2.Dimldrblk = e1.Dimldrblk;//boom
                //e2.DimensionStyle = e1.DimensionStyle;//boom

                //var e2=(Leader)e1.GetTransformedCopy(Matrix3d.Displacement(new Vector3d(100,0,0)));
                //e2.AppendVertex(Dbg.SelectPoint());
                //DU.DrawEntityLazy(e2);
            }
        }
        [Feng("qupz57")]
        public static void qupz57()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                //Dbg.BuildAndSetCurrentLayer(db);
                var e = Dbg.SelectEntity<Leader>(adb);
                Dbg.PrintLine(e.DimensionStyleName);
            }
        }
        static _nzm nzm => Dbg.nzm;
        [Feng(" aes test")]
        public static void qu7tls()
        {
            //nzm.OpenDir(@"Y:\");
            Dbg.PrintLine(nzm.AesEncrypt("xxx", "1234567812345678"));
            Dbg.PrintLine(nzm.AesDecrypt("bkPUyMh5oM1Aoe+tkd7IEA==", "1234567812345678"));
            Dbg.PrintLine(nzm.GetMd5String(nzm.EncodeUtf8("xxx")));
            Dbg.PrintLine(nzm.AesEncrypt("xxx", nzm.GetMd5String(nzm.EncodeUtf8("xxx"))));
        }
    }


    public class Sankaku
    {
        [Feng("🔴一些工具")]
        public static void qu0k4p()
        {
            FengDbgTest.qt8czw.AddButtons1(typeof(ThDebugClass.qu0jxf));
        }
        //        Dbg.FocusMainWindow();
        //using (Dbg.DocumentLock)
        //using (var adb = AcadDatabase.Active())
        //using (var tr = new DrawingTransaction(adb))
        //{
        //	var db = adb.Database;
        //    Dbg.BuildAndSetCurrentLayer(db);
        //}


        static _nzm nzm => Dbg.nzm;



        [Feng("ToObb")]
        public static void quiwqs()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var e = Dbg.SelectEntity<BlockReference>(adb);
                var pl = e.ToObb();
                DU.DrawEntityLazy(pl);
            }
        }

        [Feng("xxx")]
        public static void qu5x6r()
        {
            NewMethod3();
            //Dbg.FocusMainWindow();
            //using (Dbg.DocumentLock)
            //using (var adb = AcadDatabase.Active())
            //using (var tr = new DrawingTransaction(adb))
            //{
            //    var br=Dbg.SelectEntity<BlockReference>(adb);
            //    br.ObjectId.SetDynBlockValue("距离1", 1000.0);
            //    br.ObjectId.SetDynBlockValue("距离1", 2000.0);
            //    //br.ObjectId.SetDynBlockValue("可见性1", "侧墙通气管");
            //}
        }

        private static void NewMethod7()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var range = Dbg.SelectRange();
                var geoData = new RainSystemGeoData();
                geoData.Init();
                ThRainSystemService.AppendGravityWaterBuckets(adb, range, geoData);
                ThRainSystemService.AppendSideWaterBuckets(adb, range, geoData);
                foreach (var r in geoData.GravityWaterBuckets)
                {
                    DU.DrawRectLazy(r);
                }
            }
        }
        [Feng]
        public static void xx()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var basePt = Dbg.SelectPoint();
                DU.DrawBlockReference(blkName: "通气帽系统", basePt: basePt, layer: "W-DRAI-DOME-PIPE", cb: br =>
                {
                    br.ObjectId.SetDynBlockValue("距离1", 2000.0);
                    br.ObjectId.SetDynBlockValue("可见性1", "侧墙通气管");
                });
            }
        }

        //好奇怪。。。目前搞不定了
        //也可能是图块本身有问题
        //还真是。。。
        private static void NewMethod3()
        {
            ObjectId id = default;
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var basePt = Dbg.SelectPoint();
                DU.DrawBlockReference(blkName: "通气帽系统", basePt: basePt, layer: "W-DRAI-DOME-PIPE", cb: br =>
                {
                    //if (br.IsDynamicBlock)
                    //{
                    //    var props = br.DynamicBlockReferencePropertyCollection;
                    //    foreach (DynamicBlockReferenceProperty prop in props)
                    //    {
                    //        if (prop.ReadOnly) continue;
                    //        if (prop.PropertyName == "距离1") prop.Value = 2000.0;
                    //        //if (prop.PropertyName == "可见性1") prop.Value = "伸顶通气管";
                    //    }
                    //}
                    //br.ObjectId.SetDynBlockValue("可见性1", "侧墙通气管");
                    //br.ObjectId.SetDynBlockValue("距离1", 2000.0);
                    //br.ObjectId.SetDynBlockValue("可见性1", "伸顶通气管");

                    br.ResetBlock();

                    br.ObjectId.SetDynBlockValue("距离1", (double)1000.0);
                    //br.ResetBlock();
                    //br.ObjectId.SetDynBlockValue("距离1", (double)1000.0);
                    //br.ObjectId.SetDynBlockValue("可见性1", "侧墙通气管");
                    //br.ResetBlock();
                    //br.ObjectId.SetDynBlockValue("可见性1", "伸顶通气管");
                    //br.ResetBlock();
                    //br.ObjectId.SetDynBlockValue("距离1", (double)3000.0);
                    id = br.ObjectId;
                    Dbg.UpdateScreen();
                });
            }

            //using (Dbg.DocumentLock)
            //using (var adb = AcadDatabase.Active())
            //using (var tr = new DrawingTransaction(adb))
            //{
            //    var br = adb.Element<BlockReference>(id);
            //    br.ObjectId.SetDynBlockValue("可见性1", "侧墙通气管");
            //    br.ObjectId.SetDynBlockValue("距离1", 2000.0);
            //    br.ObjectId.SetDynBlockValue("可见性1", "伸顶通气管");
            //    Dbg.UpdateScreen();
            //}
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var br = adb.Element<BlockReference>(id);
                var o = adb.Element<BlockTableRecord>(br.BlockTableRecord);

                o.UpdateAnonymousBlocks();
                //br.ObjectId.SetDynBlockValue("可见性1", "侧墙通气管");
                Dbg.UpdateScreen();
            }
            //using (Dbg.DocumentLock)
            //using (var adb = AcadDatabase.Active())
            //using (var tr = new DrawingTransaction(adb))
            //{
            //    var br = adb.Element<BlockReference>(id);
            //    //br.ObjectId.SetDynBlockValue("可见性1", "伸顶通气管");
            //    Dbg.UpdateScreen();
            //}

        }
        [Feng("ThMEPWSS.Common.Utils.CreateFloorFraming()")]
        public static void quev3i()
        {
            ThMEPWSS.Common.Utils.CreateFloorFraming();
        }
        [Feng("ImportElementsFromStdDwg")]
        public static void quev3d()
        {
            ThRainSystemService.ImportElementsFromStdDwg();
        }
        [Feng("雨水管径100")]
        public static void quexpp()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                //DU.DrawBlockReference(blkName: "雨水管径100", basePt:Dbg.SelectPoint(), layer: "W-NOTE", props: new Dictionary<string, string>() { { "可见性", /*Dr.GetFloorDrainDN()*/"DN25" }, { "角度1", "90" } });
                //DU.DrawBlockReference(blkName: "雨水管径100", basePt: Dbg.SelectPoint(), layer: "W-NOTE", cb: br =>
                //{
                //    br.ObjectId.SetDynBlockValue("可见性", Dr.GetFloorDrainDN());
                //});

                //DU.DrawBlockReference(blkName: "雨水管径100",scale:.5, basePt: Dbg.SelectPoint(), layer: "W-NOTE", cb: br =>
                DU.DrawBlockReference(blkName: "雨水管径100", scale: 1, basePt: Dbg.SelectPoint().OffsetX(600), layer: "W-NOTE", cb: br =>
                {
                    br.ObjectId.SetDynBlockValue("可见性", Dr.GetFloorDrainDN());
                    //br.ObjectId.SetDynBlockValue("角度1", Math.PI / 2);
                    br.ObjectId.SetDynBlockValue("角度1", Math.PI);
                });
            }
        }
        [Feng]
        public static void test2()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var basePt = Dbg.SelectPoint();
                var dn = "DN25";
                //var angle = Math.PI / 2;
                var angle = Math.PI;
                DU.DrawBlockReference(blkName: "雨水管径100", scale: 1, basePt: basePt, layer: "W-NOTE", cb: br =>
                {
                    br.ObjectId.SetDynBlockValue("可见性", dn);
                    br.ObjectId.SetDynBlockValue("角度1", Math.PI);
                });
            }
        }
        [Feng]
        public static void test()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb, true))
            {
                var pt = Dbg.SelectPoint();
                var fbk = FastBlock.Create(adb);
                for (int i = 0; i < 1; i++)
                //for (int i = 0; i < 1; i++)
                {
                    var dn = "DN25";
                    var angle = Math.PI / 2;
                    //var angle = Math.PI;
                    //var angle = 0;
                    Dr.InsetDNBlock(pt, dn, angle, .5);
                }

            }
        }
        public static void quf6ek()
        {

            //for (int i = 0; i < 1000; i++)
            //{
            //    var dn = "DN25";
            //    var angle = Math.PI / 2;
            //    NewMethod9(pt, fbk, dn, angle);
            //}
        }




        public static ObjectId InsertBlockReference(ObjectId spaceId, string layer, string blockName, Point3d position, Scale3d scale, double rotateAngle, Dictionary<string, string> attNameValues)
        {
            Database db = spaceId.Database;//获取数据库对象
            //以读的方式打开块表
            BlockTable bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
            //如果没有blockName表示的块，则程序返回
            if (!bt.Has(blockName)) return ObjectId.Null;
            //以写的方式打开空间（模型空间或图纸空间）
            BlockTableRecord space = (BlockTableRecord)spaceId.GetObject(OpenMode.ForWrite);
            ObjectId btrId = bt[blockName];//获取块表记录的Id
            //打开块表记录
            BlockTableRecord record = (BlockTableRecord)btrId.GetObject(OpenMode.ForRead);
            //创建一个块参照并设置插入点
            BlockReference br = new BlockReference(position, bt[blockName]);
            br.ScaleFactors = scale;//设置块参照的缩放比例
            if (layer != null) br.Layer = layer;//设置块参照的层名
            br.Rotation = rotateAngle;//设置块参照的旋转角度
            space.AppendEntity(br);//为了安全，将块表状态改为读 
            //判断块表记录是否包含属性定义
            if (record.HasAttributeDefinitions)
            {
                //若包含属性定义，则遍历属性定义
                foreach (ObjectId id in record)
                {
                    //检查是否是属性定义
                    AttributeDefinition attDef = id.GetObject(OpenMode.ForRead) as AttributeDefinition;
                    if (attDef != null)
                    {
                        NewMethod8(attNameValues, db, br, attDef);
                    }
                }
            }
            db.TransactionManager.AddNewlyCreatedDBObject(br, true);
            return br.ObjectId;//返回添加的块参照的Id
        }

        private static void NewMethod8(Dictionary<string, string> d, Database db, BlockReference br, AttributeDefinition attDef)
        {
            var attribute = FastBlock.CreateAttribute(db, br, attDef);
            //判断是否包含指定的属性名称
            {
                var s = attDef.Tag.ToUpper();
                if (d.ContainsKey(s))
                {
                    //设置属性值
                    attribute.TextString = d[s].ToString();
                }
            }
            FastBlock.AppendAttribute(db, br, attribute);
        }


        //雨水斗对位研究
        private static void NewMethod2()
        {
            var r1 = "{'type':'GRect','values':[1292293.678676707,581253.83417658461,1392293.678676707,699247.960999981]}".FromCadJson<GRect>();
            var r2 = "{'type':'GRect','values':[1292293.678676707,431253.83417658461,1392293.678676707,549247.960999981]}".FromCadJson<GRect>();
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                //foreach (var e in adb.ModelSpace.OfType<BlockReference>().Where(x => x.GetEffectiveName() == "重力流雨水斗"))
                //{
                //    Dbg.ShowWhere(e);
                //}
                //foreach (var e in adb.ModelSpace.OfType<BlockReference>().Where(x => x.GetEffectiveName() == "GYYR"))
                //{
                //    Dbg.ShowWhere(e);
                //}
                //foreach (var e in adb.ModelSpace.OfType<Entity>().Where(e=>e.Layer=="W-RAIN-EQPM"))
                //{
                //    Dbg.ShowWhere(e);
                //}
                var pts1 = new List<Point2d>();
                var pts2 = new List<Point2d>();
                foreach (var e in adb.ModelSpace.OfType<BlockReference>().Where(x => x.GetEffectiveName() == "GYYR"))
                {
                    var bd = e.Bounds.ToGRect();
                    if (r1.ContainsRect(bd))
                    {
                        pts1.Add(bd.Center);
                    }
                }
                foreach (var e in adb.ModelSpace.OfType<BlockReference>().Where(x => x.GetEffectiveName() == "重力流雨水斗"))
                {
                    var bd = e.Bounds.ToGRect();
                    if (r1.ContainsRect(bd))
                    {
                        pts1.Add(bd.Center);
                    }
                }
                //foreach (var pt in pts1)
                //{
                //    Dbg.ShowWhere(pt);
                //}

                foreach (var e in adb.ModelSpace.OfType<Entity>().Where(e => e.Layer == "W-RAIN-EQPM"))
                {
                    var bd = e.Bounds.ToGRect();
                    if (r2.ContainsRect(bd))
                    {
                        pts2.Add(bd.Center);
                    }
                }
                //foreach (var pt in pts2)
                //{
                //    Dbg.ShowWhere(pt);
                //}

                var p1 = r1.LeftTop;
                var p2 = r2.LeftTop;
                var pts3 = pts1.Select(p => (p - p1).ToPoint2d()).ToList();
                var pts4 = pts2.Select(p => (p - p2).ToPoint2d()).ToList();
                for (int i = 0; i < pts3.Count; i++)
                {
                    for (int j = 0; j < pts4.Count; j++)
                    {
                        if (pts3[i].GetDistanceTo(pts4[j]) < 50)
                        {
                            Dbg.ShowWhere(pts1[i]);
                            Dbg.ShowWhere(pts2[j]);
                        }
                    }
                }
            }
        }

        private static void NewMethod1()
        {
            var p1 = new Point2d(0, 10);
            var p2 = new Point2d(10, 0);
            var p3 = new Point2d(10, 10);
            var p4 = Point2d.Origin;
            var line1 = Dbg.GeometryFactory.CreateLineString(new Coordinate[] { p3.ToNTSCoordinate(), p4.ToNTSCoordinate() });
            var line2 = Dbg.GeometryFactory.CreateLineString(new Coordinate[] { p1.ToNTSCoordinate(), p2.ToNTSCoordinate() });
            var geo = line1.Intersection(line2) as Point;
            Dbg.PrintLine(geo.ToString());
        }

        private static void NewMethod()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                {
                    var p1 = new Point2d(0, 10);
                    var p2 = new Point2d(10, 0);
                    var p3 = new Point2d(20, 0);
                    var line = Dbg.GeometryFactory.CreateLineString(new Coordinate[] { p2.ToNTSCoordinate(), p3.ToNTSCoordinate() });
                    var p = p1.ToNTSPoint();
                    Dbg.PrintLine(p.Distance(line));
                }

                {
                    var p1 = new Point2d(0, 10);
                    var p2 = new Point2d(10, 0);
                    var p3 = new Point2d(-10, 0);
                    var line = Dbg.GeometryFactory.CreateLineString(new Coordinate[] { p2.ToNTSCoordinate(), p3.ToNTSCoordinate() });
                    var p = p1.ToNTSPoint();
                    Dbg.PrintLine(p.Distance(line));
                }
            }
        }

        private static void qu0lsq()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                //var r1 = new GRect(0, 0, 10, 10);
                //var r2 = new GRect(5, 5, 20, 20);
                //var geo = GeometryFac.CreateGeometry(new Geometry[] { r1.ToPolygon(), r2.ToPolygon() });
                //DU.DrawGeometryLazy(geo);
                //Dbg.ChangeCadScreenTo(geo.EnvelopeInternal.ToGRect());

                var r1 = new GRect(0, 0, 10, 10).ToPolygon();
                var r2 = new GRect(5, 5, 20, 20).ToPolygon();
                //var geo = r1.Union(r2);
                var geo = r1.Intersection(r2);
                //var geo = r1.Difference(r2);
                DU.DrawGeometryLazy(geo);
                //Dbg.ChangeCadScreenTo(geo.EnvelopeInternal.ToGRect());

                Dbg.FocusMainWindow();
            }
        }
    }

    public class ListDict<K, V> : Dictionary<K, List<V>>
    {
        public void Add(K key, V value)
        {
            var d = this;
            if (!d.TryGetValue(key, out List<V> lst))
            {
                lst = new List<V>() { value };
                d[key] = lst;
            }
            else
            {
                lst.Add(value);
            }
        }
    }
    public class CountDict<K> : IEnumerable<KeyValuePair<K, int>>
    {
        Dictionary<K, int> d = new Dictionary<K, int>();
        public int this[K key]
        {
            get
            {
                d.TryGetValue(key, out int value); return value;
            }
            set
            {
                d[key] = value;
            }
        }

        public IEnumerator<KeyValuePair<K, int>> GetEnumerator()
        {
            foreach (var kv in d)
            {
                if (kv.Value > 0) yield return kv;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
    public static class qtdueu
    {
        public static string ToCadJson(this object obj)
        {
            return Util1.ToJson(obj);
        }
        public static T FromCadJson<T>(this string json)
        {
            return Util1.FromJson<T>(json);
        }
    }
    public class FengAttribute : Attribute
    {
        public string Title;
        public FengAttribute() { }
        public FengAttribute(string title) { this.Title = title; }
    }

    public static class Matrix2dUtil
    {
        public static readonly Matrix2d Identity = new Matrix2d(new double[] { 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0 });
    }
    public static class DrainageTest
    {

        [Feng("quimgi")]
        public static void quimgi()
        {

        }

        private static void NewMethod5()
        {
            var f = new Form();
            f.TopMost = true;
            f.Show();
            if (false) AddButton("", () =>
            {
                Dbg.PrintLine(f.Location.X);
                Dbg.PrintLine(f.Location.Y);
                Dbg.PrintLine(f.Width);
                Dbg.PrintLine(f.Height);
            });
            void setLeft(Form form)
            {
                form.Location = new System.Drawing.Point(-1896, 30);
                form.Width = 1854;
                form.Height = 976;
            }
            void setRight(Form form)
            {
                form.Location = new System.Drawing.Point(41, 30);
                form.Width = 1854;
                form.Height = 976;
            }
            setLeft(f);
            var _segs = NewMethod4();
            f.Paint += (s, e) =>
            {
                var g = e.Graphics;
                using var pen = new System.Drawing.Pen(System.Drawing.Color.Red);
                var m = Matrix2d.Mirroring(new Line2d(new Point2d(0, 0), new Point2d(10, 0)));
                m = m.PreMultiplyBy(Matrix2d.Displacement(new Vector2d(0, 800)));
                foreach (var seg in _segs)
                {
                    var sp = seg.StartPoint;
                    sp = sp.TransformBy(m);
                    var ep = seg.EndPoint;
                    ep = ep.TransformBy(m);
                    g.DrawLine(pen, sp.ToDrawingPoint(), ep.ToDrawingPoint());
                }
            };
        }

        [Feng("quizl8")]
        public static void quizl8()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var _segs = NewMethod4();

                foreach (var seg in _segs)
                {
                    DU.DrawLineSegmentLazy(seg);
                }

                var sv = new ThMEPEngineCore.Service.ThLaneLineCleanService();
                sv.ExtendDistance = 1;
                var colle = _segs.Select(x => x.ToCadLine()).ToCollection();
                var ret = sv.Clean(colle);
                foreach (Line e in ret)
                {
                    DU.DrawLineSegmentLazy(e.ToGLineSegment());
                }
            }
        }

        private static List<GLineSegment> NewMethod4()
        {
            var r = "{'type':'GRect','values':[521552.78763576248,867324.05193330813,533133.08130046073,876100.43981294858]}".FromCadJson<GRect>();
            var segs = loadsegs();
            var dlines = segs.Select(x => x.ToLineString()).ToGeometryList();
            var f = GeoFac.CreateGRectContainsSelector(dlines);
            var list = f(r);
            var ext = new Extents3d();
            foreach (var dline in list)
            {
                var seg = segs[dlines.IndexOf(dline)];
                ext.AddPoint(seg.StartPoint.ToPoint3d());
                ext.AddPoint(seg.EndPoint.ToPoint3d());
            }
            var w = ext.ToGRect();
            var targetWorld = GRect.Create(800, 600);

            //var m=Matrix2d.Displacement(-ext.MinPoint.ToPoint2d().ToVector2d());
            var v = -ext.MinPoint.ToPoint2d().ToVector2d();

            var p1 = ext.MaxPoint.ToPoint2D() + v;
            var p2 = ext.MinPoint.ToPoint2D() + v;
            var kx = targetWorld.Width / (p1.X - p2.X);
            var ky = targetWorld.Height / (p1.Y - p2.Y);

            var _segs = new List<GLineSegment>(segs.Count);
            foreach (var seg in segs)
            {
                //var sp=seg.StartPoint.TransformBy(m);
                //var ep = seg.EndPoint.TransformBy(m);
                var sp = seg.StartPoint + v;
                sp = new Point2d(sp.X * kx, sp.Y * ky);
                var ep = seg.EndPoint + v;
                ep = new Point2d(ep.X * kx, ep.Y * ky);
                _segs.Add(new GLineSegment(sp, ep));
            }

            return _segs;
        }

        [Feng("quirdk")]
        public static void quirdk()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var e = Dbg.SelectEntity<Entity>(adb);
                var ee = (Entity)e.Clone();
                DU.DrawRectLazy(ee.Bounds.ToGRect());
            }
        }
        [Feng("quim6x")]
        public static void quim6x()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                var segs = new List<GLineSegment>();
                foreach (var e in adb.ModelSpace.OfType<Entity>().Where(x => x.Layer == "W-DRAI-DOME-PIPE" && ThRainSystemService.IsTianZhengElement(x)))
                {
                    if (GeoAlgorithm.TryConvertToLineSegment(e, out GLineSegment seg))
                    {
                        segs.Add(seg);
                    }
                }
                var dlines = segs.Select(x => x.ToLineString()).ToList();
                Dbg.PrintText(segs.ToCadJson());
            }
        }
        [Feng("quizgv")]
        public static void quizgv()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            {
                var r = Dbg.SelectGRect();
                Dbg.PrintLine(r.ToCadJson());
            }
        }
        public static List<GLineSegment> loadsegs()
        {
            return File.ReadAllText(@"Y:\xxx.txt").FromCadJson<List<GLineSegment>>().Distinct().ToList();
        }
        [Feng("quhax8")]
        public static void quhax8()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                //立管
                {
                    var list = adb.ModelSpace.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-EQPM" && x.ObjectId.IsValid && x.GetEffectiveName() == "A$C58B12E6E").ToList();
                    Dbg.PrintLine(list.Count);
                }
                //label
                {
                    var list = adb.ModelSpace.OfType<DBText>().Where(x => x.Layer == "W-DRAI-NOTE").ToList();
                    //Dbg.PrintText(list.Select(x => x.TextString).ToJson());
                }
                //labelline
                {
                    var list = adb.ModelSpace.OfType<Line>().Where(x => x.Layer == "W-DRAI-NOTE").ToList();
                    Dbg.PrintLine(list.Count);
                }
                //dline
                {
                    var list = adb.ModelSpace.OfType<Entity>().Where(x => x.Layer == "W-DRAI-DOME-PIPE" && ThRainSystemService.IsTianZhengElement(x)).ToList();
                    Dbg.PrintLine(list.Count);
                }
            }
        }



        [Feng("天正信息提取")]
        public static void NewMethod3()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var e = Dbg.SelectEntity<Entity>(adb);
                Dbg.PrintLine(System.ComponentModel.TypeDescriptor.GetClassName(e.AcadObject));
                var dict = new Dictionary<string, object>();
                foreach (System.ComponentModel.PropertyDescriptor pi in System.ComponentModel.TypeDescriptor.GetProperties(e.AcadObject))
                {
                    var t = pi.PropertyType;
                    if (t == typeof(string) || t == typeof(double) || t == typeof(float) || t == typeof(int) || t == typeof(long) || t == typeof(Point2d) || t == typeof(Point3d))
                    {
                        var v = pi.GetValue(e.AcadObject);
                        dict[pi.Name] = v;
                    }
                }
                Dbg.PrintText(dict.ToCadJson());
            }
        }
        public static void quha91()
        {
            //光胜给的提取代码
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);
                var range = Dbg.SelectRange();
                var roomBuilder = new ThRoomBuilderEngine()
                {
                    RoomBoundaryLayerFilter = new List<string> { "AI-空间框线" },
                    RoomMarkLayerFilter = new List<string> { "AI-空间名称" },
                };
                var rooms = roomBuilder.BuildFromMS(db, range);

                foreach (var room in rooms)
                {
                    //内容是空的，算了，还是自己写吧
                    Dbg.PrintLine(room.Name);
                    Dbg.PrintLine(room.Tags.ToJson());
                }
            }
        }
        private static void qugqvl()
        {
            Dbg.FocusMainWindow();
            using (Dbg.DocumentLock)
            using (var adb = AcadDatabase.Active())
            using (var tr = new DrawingTransaction(adb))
            {
                var db = adb.Database;
                Dbg.BuildAndSetCurrentLayer(db);

                //var list=adb.ModelSpace.OfType<DBText>().Where(x => x.Layer == "W-DRAI-EQPM").ToList();
                //Dbg.PrintText(list.Select(x => x.TextString).ToJson());

                //var list = adb.ModelSpace.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-EQPM" && x.ObjectId.IsValid && x.GetEffectiveName()=="立管编号").ToList();
                //Dbg.PrintLine(list.Count);

                //var list = adb.ModelSpace.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-EQPM" && x.ObjectId.IsValid && x.GetEffectiveName() == "清扫口系统").ToList();
                //Dbg.PrintLine(list.Count);

                //var list = adb.ModelSpace.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-EQPM" && x.ObjectId.IsValid && x.GetEffectiveName() == "污废合流井编号").ToList();
                //Dbg.PrintLine(list.Count);
                //foreach (var e in list)
                //{
                //    Dbg.PrintLine(e.GetAttributesStrValue("-"));
                //}


                //Dbg.PrintLine(RXClass.GetClass(Dbg.SelectEntity<Entity>(adb).GetType()).DxfName);//tch_pipe是空的
                //Dbg.PrintLine(RXClass.GetClass(Dbg.SelectEntity<Entity>(adb).AcadObject.GetType()).DxfName);//报错

                //不管了，就这么判断吧
                //var e = Dbg.SelectEntity<Entity>(adb);
                //Dbg.PrintLine(System.ComponentModel.TypeDescriptor.GetClassName(e.AcadObject));//IComPipe
                //Dbg.PrintLine(TypeDescriptor.GetReflectionType(e.AcadObject).ToString());//System.__ComObject
                //Dbg.PrintLine(e.AcadObject.GetType().Assembly.FullName);
                //Dbg.PrintLine(TypeDescriptor.GetComponentName(e.AcadObject));//空的
                //Dbg.PrintLine(e.GetType().Assembly.FullName);//Acdbmgd,...

                //var list=adb.ModelSpace.OfType<Entity>().Where(x => ThRainSystemService.IsTianZhengElement(x)).ToList();
                //Dbg.PrintLine(list.Count);

                //var list = adb.ModelSpace.OfType<BlockReference>().Where(x => x.Layer == "W-DRAI-FLDR" && x.ObjectId.IsValid).ToList();
                //Dbg.PrintLine(list.Count);


            }
        }



        private static void NewMethod2(Database db)
        {
            var texts = new List<CText>();
            var visitor = new Visitor();
            visitor.Extract = (e, m) =>
            {
                if (e is MText mt)
                {
                    var bd = mt.Bounds;
                    if (bd is Extents3d ext)
                    {
                        ext.TransformBy(m);
                        var r = ext.ToGRect();
                        if (r.IsValid)
                        {
                            var text = mt.Contents;
                            //长这样
                            //"\\A1;Ah2","\\A1;Ah1"
                            if (text.ToLower().Contains("ah1") || text.ToLower().Contains("ah2"))
                            {
                                var ct = new CText() { Text = text, Boundary = r };
                                texts.Add(ct);
                            }
                        }
                    }
                }
            };
            Execute(db, visitor);
            foreach (var ct in texts)
            {
                DU.DrawRectLazy(ct.Boundary);
            }
            //Dbg.PrintLine(texts.Select(x => x.Text).ToJson());
        }
        [Feng("💰准备打开多张图纸")]
        public static void qtjr2w()
        {
            var files = Util1.getFiles();
            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i];
                AddButton((i + 1) + " " + Path.GetFileName(file), () =>
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.Open(file, false);
                });
            }
            AddButton("地上给水排水平面图模板_20210125", () =>
            {
                var file = @"E:\thepa_workingSpace\任务资料\任务2\210430\地上给水排水平面图模板_20210125.dwg";
                Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.Open(file, false);
            });
            AddButton("绘图说明_20210326", () =>
            {
                var file = @"E:\thepa_workingSpace\任务资料\任务2\210430\8#_210429\8#\设计区\绘图说明_20210326（反馈）.dwg";
                Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.Open(file, false);
            });

        }
        [Feng("💰准备打开多张图纸2")]
        public static void quhakv()
        {
            var root = @"E:\thepa_workingSpace\任务资料\任务3\图纸";
            var files = new string[]
            {
$@"{root}\01_蓝光钰泷府二期_框线\FS59P2BC_W20-地上给水排水平面图-副本.dwg",
$@"{root}\02_湖北交投颐和华府_框线\FS59OCRA_W20-3#-地上给排水及消防平面图.dwg",
$@"{root}\03_佳兆业滨江新城_框线\FS5BH1EW_W20-5#地上给水排水及消防平面图.dwg",
$@"{root}\04_蓝光未来阅璟_框线\FS5F8704_W20-地上给水排水平面图-送审版.dwg",
$@"{root}\05_清江山水四期_框线\FS55TMPH_W20-地上给水排水平面图.dwg",
$@"{root}\06_庭瑞君越观澜三期_框线\fs57grhn_w20-地上给水排水平面图.dwg",
$@"{root}\07_武汉二七滨江商务区南一片住宅地块_框线\FS5747SS_W20-地上给水排水平面图.dwg",
$@"{root}\08_合景红莲湖项目_框线\FS55TD78_W20-73#-地上给水排水平面图.dwg",
$@"{root}\09_长征村K2地块\FS5F46QE_W20-地上给水排水平面图-Z.dwg",
            };
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                AddButton((i + 1) + " " + Path.GetFileName(file), () =>
                      {
                          Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.Open(file, false);
                      });
            }
        }
        public static void AddButton(string name, Action f)
        {
            Util1.AddButton(name, f);
        }
        private static void Execute(Database db, Visitor visitor)
        {
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(db);
        }

        private static void NewMethod1(Database db)
        {
            var texts = new List<CText>();
            var mtexts = new List<CText>();

            var visitor = new Visitor();
            visitor.Extract = (e, m) =>
            {
                if (e is DBText dbt)
                {
                    //if (string.IsNullOrEmpty(dbt.TextString) || !dbt.TextString.ToLower().Contains("ah2")) return;
                    //var dbt2 = (DBText)dbt.Clone();
                    //dbt2.TransformBy(m);//eCannotScaleNonUniformly
                    //texts.Add(dbt);

                    var bd = dbt.Bounds;
                    if (bd is Extents3d ext)
                    {
                        ext.TransformBy(m);
                        var ct = new CText() { Text = dbt.TextString, Boundary = ext.ToGRect() };
                        texts.Add(ct);
                    }
                }
                else if (e is MText mt)
                {
                    //if (string.IsNullOrEmpty(mt.Contents) || !mt.Contents.ToLower().Contains("ah2")) return;
                    //var mt2 = (MText)mt.Clone();
                    //mt2.TransformBy(m);
                    //mtexts.Add(mt2);

                    var bd = mt.Bounds;
                    if (bd is Extents3d ext)
                    {
                        ext.TransformBy(m);
                        var ct = new CText() { Text = mt.Contents, Boundary = ext.ToGRect() };
                        mtexts.Add(ct);
                    }
                }
            };

            var ranges = new List<Geometry>();
            visitor.XClip = (xclip) =>
            {
                ranges.Add(xclip.Polygon.ToNTSPolygon());
            };

            Execute(db, visitor);


            Dbg.PrintLine(texts.Count);
            Dbg.PrintLine(mtexts.Count);


            if (ranges.Count > 0)
            {
                var _geo = ranges[0];
                for (int i = 1; i < ranges.Count; i++)
                {
                    //_geo = _geo.Intersection(ranges[i]);
                    _geo = _geo.Union(ranges[i]);
                }
                var bds = texts.Select(x => x.Boundary).Concat(mtexts.Select(x => x.Boundary)).Distinct().ToList();
                var geos = bds.Where(x => x.IsValid).Select(x => x.ToPolygon()).Cast<Geometry>().ToList();
                Dbg.PrintLine(geos.Count);
                var geo = GeoFac.CreateGeometry(geos);
                var f = GeoFac.CreateGeometrySelector(geos);
                var results = f(_geo);
                Dbg.PrintLine(results.Count);
            }
        }

        public class Visitor : ThBuildingElementExtractionVisitor
        {
            public Action<Entity, Matrix3d> Extract;
            public override void DoExtract(List<ThRawIfcBuildingElementData> elements, Entity dbObj, Matrix3d matrix)
            {
                Extract?.Invoke(dbObj, matrix);
                //if (dbObj is DBText dbText)
                //{
                //    elements.AddRange(HandleDbText(dbText, matrix));
                //}
                //else if (dbObj is MText mText)
                //{
                //    elements.AddRange(HandleMText(mText, matrix));
                //}
            }
            public Action<ThMEPXClipInfo> XClip;
            public override void DoXClip(List<ThRawIfcBuildingElementData> elements, BlockReference blockReference, Matrix3d matrix)
            {
                var xclip = blockReference.XClipInfo();
                if (xclip.IsValid)
                {
                    xclip.TransformBy(matrix);
                    //elements.RemoveAll(o => !xclip.Contains(GetTextPosition(o.Geometry)));
                    XClip?.Invoke(xclip);
                }
            }

        }
        private static void NewMethod(AcadDatabase adb, Database db)
        {
            if (false)
            {
                var gravityBucketEngine = new ThWGravityWaterBucketRecognitionEngine();
                gravityBucketEngine.Recognize(adb.Database, Dbg.SelectRange());
            }
            var list = new List<string>();
            var visitor = new BlockReferenceVisitor();
            visitor.IsTargetBlockReference = (br) =>
            {
                //__覆盖_A10-8地上平面_SEN23WUB$0$厨房250X250洞口
                var name = br.GetEffectiveName();
                //list.Add(name);
                if (name.Contains("厨房")) return true;
                return false;
            };
            var rs = new List<GRect>();
            visitor.HandleBlockReference = (br, m) =>
            {
                var e = br.GetTransformedCopy(m);
                rs.Add(e.Bounds.ToGRect());
            };
            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(db);
            //File.WriteAllText(@"Y:\xxxx.json", list.ToJson());
            foreach (var r in rs)
            {
                //Dbg.ShowWhere(r);
                DU.DrawRectLazy(r);
            }
        }
    }
    public class BlockReferenceVisitor : ThDistributionElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is BlockReference blkref)
            {
                _HandleBlockReference(elements, blkref, matrix);
            }
        }

        public override void DoXClip(List<ThRawIfcDistributionElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o => !IsContain(xclip, o.Geometry));
            }
        }

        public override bool CheckLayerValid(Entity curve)
        {
            return true;
        }
        public Func<BlockReference, bool> IsTargetBlockReference;
        public override bool IsDistributionElement(Entity entity)
        {
            if (entity is BlockReference blkref)
            {
                return IsTargetBlockReference(blkref);
                var name = blkref.GetEffectiveName();
                return (ThMEPEngineCore.Service.ThGravityWaterBucketLayerManager.IsGravityWaterBucketBlockName(name));
            }
            return false;
        }
        public Action<BlockReference, Matrix3d> HandleBlockReference;

        private void _HandleBlockReference(List<ThRawIfcDistributionElementData> elements, BlockReference blkref, Matrix3d matrix)
        {
            if (!blkref.ObjectId.IsValid) return;
            HandleBlockReference(blkref, matrix);
        }

        private bool IsContain(ThMEPEngineCore.Algorithm.ThMEPXClipInfo xclip, Entity ent)
        {
            if (ent is BlockReference br)
            {
                return xclip.Contains(br.GeometricExtents.ToRectangle());
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
    public static class DrawingExtensions
    {
        public static System.Drawing.Point ToDrawingPoint(this Point2d pt) => new System.Drawing.Point(Convert.ToInt32(pt.X), Convert.ToInt32(pt.Y));
        public static System.Drawing.Point ToDrawingPoint(this Point3d pt) => new System.Drawing.Point(Convert.ToInt32(pt.X), Convert.ToInt32(pt.Y));
        public static System.Drawing.Point ToDrawingPoint(this System.Windows.Point pt) => new System.Drawing.Point(Convert.ToInt32(pt.X), Convert.ToInt32(pt.Y));
        public static System.Windows.Point ToWindowsPoint(this Point2d pt) => new System.Windows.Point(pt.X, pt.Y);
        public static System.Windows.Point ToWindowsPoint(this Point3d pt) => new System.Windows.Point(pt.X, pt.Y);
        public static System.Windows.Point ToWindowsPoint(this System.Drawing.Point pt) => new System.Windows.Point(pt.X, pt.Y);
        public static System.Windows.Vector ToWindowVector(this Vector2d v) => new System.Windows.Vector(v.X, v.Y);
        public static System.Windows.Vector ToWindowVector(this Vector3d v) => new System.Windows.Vector(v.X, v.Y);
        public static Vector2d ToVector2d(this System.Windows.Vector v) => new Vector2d(v.X, v.Y);
        public static double GetAngle(this System.Windows.Vector v) => Math.Acos(v.X / v.Length);
        public static double GetDegreeAngle(this System.Windows.Vector v) => v.GetAngle().AngleToDegree();
    }
}

namespace ThMEPWSS.DebugNs
{
    using System.Diagnostics.Contracts;
    using System.Windows;
    using System.Windows.Media;
    using Dbg = ThMEPWSS.DebugNs.ThDebugTool;
    public static class quj50y
    {
        public static void quj51d()
        {
            Matrix scale = Matrix.Identity;
            scale.Scale(2, 2);

            Matrix transLate = Matrix.Identity;
            transLate.Translate(10, 20);

            var transForm = scale * transLate;

            Matrix transForm2 = Matrix.Identity;
            transForm2.Scale(2, 2);
            transForm2.Translate(10, 20);

            Contract.Assert(transForm == transForm2);
        }
        public static void quj4zb()
        {
            var transForm = Matrix.Identity;
            transForm.Scale(2.5, 3);

            var point = new Point(1, 10);
            var newPoint = transForm.Transform(point);

            Dbg.PrintLine(newPoint.ToString());
        }
    }
}