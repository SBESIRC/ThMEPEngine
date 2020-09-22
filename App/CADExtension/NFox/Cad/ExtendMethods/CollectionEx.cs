﻿using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace NFox.Cad
{
    /// <summary>
    /// 集合扩展类
    /// </summary>
    public static class CollectionEx
    {
        /// <summary>
        /// 对象id迭代器转换为集合
        /// </summary>
        /// <param name="ids">对象id的迭代器</param>
        /// <returns>对象id集合</returns>
        public static ObjectIdCollection ToCollection(this IEnumerable<ObjectId> ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }
            ObjectIdCollection idCol = new ObjectIdCollection();
            foreach (ObjectId id in ids)
                idCol.Add(id);
            return idCol;
        }

        /// <summary>
        /// 实体迭代器转换为集合
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="objs">实体对象的迭代器</param>
        /// <returns>实体集合</returns>
        public static DBObjectCollection ToCollection<T>(this IEnumerable<T> objs) where T : DBObject
        {
            if (objs == null)
            {
                throw new ArgumentNullException(nameof(objs));
            }
            DBObjectCollection objCol = new DBObjectCollection();
            foreach (T obj in objs)
                objCol.Add(obj);
            return objCol;
        }

        /// <summary>
        /// double 数值迭代器转换为 double 数值集合
        /// </summary>
        /// <param name="doubles">double 数值迭代器</param>
        /// <returns>double 数值集合</returns>
        public static DoubleCollection ToCollection(this IEnumerable<double> doubles)
        {
            DoubleCollection doubleCol = new DoubleCollection();
            foreach (double d in doubles)
                doubleCol.Add(d);
            return doubleCol;
        }

        /// <summary>
        /// 二维点迭代器转换为二维点集合
        /// </summary>
        /// <param name="pts">二维点迭代器</param>
        /// <returns>二维点集合</returns>
        public static Point2dCollection ToCollection(this IEnumerable<Point2d> pts)
        {
            Point2dCollection ptCol = new Point2dCollection();
            foreach (Point2d pt in pts)
                ptCol.Add(pt);
            return ptCol;
        }

        /// <summary>
        /// 三维点迭代器转换为二维点集合
        /// </summary>
        /// <param name="pts">三维点迭代器</param>
        /// <returns>三维点集合</returns>
        public static Point3dCollection ToCollection(this IEnumerable<Point3d> pts)
        {
            Point3dCollection ptCol = new Point3dCollection();
            foreach (Point3d pt in pts)
                ptCol.Add(pt);
            return ptCol;
        }

        /// <summary>
        /// 对象id集合转换为对象id列表
        /// </summary>
        /// <param name="ids">对象id集合</param>
        /// <returns>对象id列表</returns>
        public static List<ObjectId> GetObjectIds(this ObjectIdCollection ids)
        {
            return ids.Cast<ObjectId>().ToList();
        }
    }
}