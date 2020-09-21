﻿using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.GraphicsSystem;

namespace NFox.Cad
{
    /// <summary>
    /// 系统管理类
    /// <para>封装了一些系统 osmode、cmdecho、dimblk 系统变量</para>
    /// <para>封装了常用的 文档 编辑器 数据库等对象为静态变量</para>
    /// <para>封装了配置页面的注册表信息获取函数</para>
    /// </summary>
    public static class SystemManager
    {
        #region Goal

        /// <summary>
        /// 当前的数据库
        /// </summary>
        public static Database CurrentDatabase
        {
            get
            {
                return HostApplicationServices.WorkingDatabase;
            }
        }

        /// <summary>
        /// 当前文档
        /// </summary>
        public static Document ActiveDocument
        {
            get
            {
                return Application.DocumentManager.MdiActiveDocument;
            }
        }

        /// <summary>
        /// 编辑器对象
        /// </summary>
        public static Editor Editor
        {
            get
            {
                return ActiveDocument.Editor;
            }
        }

        /// <summary>
        /// 图形管理器
        /// </summary>
        public static Manager GsManager
        {
            get
            {
                return ActiveDocument.GraphicsManager;
            }
        }

        #endregion Goal

        #region Preferences

        /// <summary>
        /// 获取当前配置的数据
        /// </summary>
        /// <param name="subSectionName">小节名</param>
        /// <param name="propertyName">数据名</param>
        /// <returns>对象</returns>
        public static object GetCurrentProfileProperty(string subSectionName, string propertyName)
        {
            UserConfigurationManager ucm = Application.UserConfigurationManager;
            IConfigurationSection cpf = ucm.OpenCurrentProfile();
            IConfigurationSection ss = cpf.OpenSubsection(subSectionName);
            return ss.ReadProperty(propertyName, "");
        }

        /// <summary>
        /// 获取对话框配置的数据
        /// </summary>
        /// <param name="dialog">对话框对象</param>
        /// <returns>配置项</returns>
        public static IConfigurationSection GetDialogSection(object dialog)
        {
            UserConfigurationManager ucm = Application.UserConfigurationManager;
            IConfigurationSection ds = ucm.OpenDialogSection(dialog);
            return ds;
        }

        /// <summary>
        /// 获取公共配置的数据
        /// </summary>
        /// <param name="propertyName">数据名</param>
        /// <returns>配置项</returns>
        public static IConfigurationSection GetGlobalSection(string propertyName)
        {
            UserConfigurationManager ucm = Application.UserConfigurationManager;
            IConfigurationSection gs = ucm.OpenGlobalSection();
            IConfigurationSection ss = gs.OpenSubsection(propertyName);
            return ss;
        }

        #endregion Preferences

        #region Enum

        /// <summary>
        /// 命令行回显系统变量， <see langword="true"/> 为显示， <see langword="false"/> 为不显示
        /// </summary>
        public static bool CmdEcho
        {
            get
            {
                return (int)Application.GetSystemVariable("cmdecho") == 1;
            }
            set
            {
                Application.SetSystemVariable("cmdecho", Convert.ToInt16(value));
            }
        }

        #region Dimblk

        /// <summary>
        /// 标注箭头类型
        /// </summary>
        public enum DimblkType
        {
            /// <summary>
            /// 实心闭合
            /// </summary>
            Defult,

            /// <summary>
            /// 点
            /// </summary>
            Dot,

            /// <summary>
            /// 小点
            /// </summary>
            DotSmall,

            /// <summary>
            /// 空心点
            /// </summary>
            DotBlank,

            /// <summary>
            /// 原点标记
            /// </summary>
            Origin,

            /// <summary>
            /// 原点标记2
            /// </summary>
            Origin2,

            /// <summary>
            /// 打开
            /// </summary>
            Open,

            /// <summary>
            /// 直角
            /// </summary>
            Open90,

            /// <summary>
            /// 30度角
            /// </summary>
            Open30,

            /// <summary>
            /// 闭合
            /// </summary>
            Closed,

            /// <summary>
            /// 空心小点
            /// </summary>
            Small,

            /// <summary>
            /// 无
            /// </summary>
            None,

            /// <summary>
            /// 倾斜
            /// </summary>
            Oblique,

            /// <summary>
            /// 实心框
            /// </summary>
            BoxFilled,

            /// <summary>
            /// 方框
            /// </summary>
            BoxBlank,

            /// <summary>
            /// 空心闭合
            /// </summary>
            ClosedBlank,

            /// <summary>
            /// 实心基准三角形
            /// </summary>
            DatumFilled,

            /// <summary>
            /// 基准三角形
            /// </summary>
            DatumBlank,

            /// <summary>
            /// 完整标记
            /// </summary>
            Integral,

            /// <summary>
            /// 建筑标记
            /// </summary>
            ArchTick,
        }

        /// <summary>
        /// 标注箭头属性
        /// </summary>
        public static DimblkType Dimblk
        {
            get
            {
                string s = (string)Application.GetSystemVariable("dimblk");
                if (string.IsNullOrEmpty(s))
                {
                    return DimblkType.Defult;
                }
                else
                {
                    return s.ToEnum<DimblkType>();
                }
            }
            set
            {
                string s = GetDimblkName(value);
                Application.SetSystemVariable("dimblk", s);
            }
        }
        /// <summary>
        /// 获取标注箭头名
        /// </summary>
        /// <param name="dimblk">标注箭头类型</param>
        /// <returns>箭头名</returns>
        public static string GetDimblkName(DimblkType dimblk)
        {
            return
                dimblk == DimblkType.Defult
                ?
                "."
                :
                "_" + dimblk.GetName();
        }
        /// <summary>
        /// 获取标注箭头ID
        /// </summary>
        /// <param name="dimblk">标注箭头类型</param>
        /// <returns>箭头ID</returns>
        public static ObjectId GetDimblkId(DimblkType dimblk)
        {
            DimblkType oldDimblk = Dimblk;
            Dimblk = dimblk;
            ObjectId id = HostApplicationServices.WorkingDatabase.Dimblk;
            Dimblk = oldDimblk;
            return id;
        }

        #endregion Dimblk

        #region OsMode

        /// <summary>
        /// 捕捉模式系统变量类型
        /// </summary>
        public enum OSModeType
        {
            /// <summary>
            /// 无
            /// </summary>
            None = 0,

            /// <summary>
            /// 端点
            /// </summary>
            End = 1,

            /// <summary>
            /// 中点
            /// </summary>
            Middle = 2,

            /// <summary>
            /// 圆心
            /// </summary>
            Center = 4,

            /// <summary>
            /// 节点
            /// </summary>
            Node = 8,

            /// <summary>
            /// 象限点
            /// </summary>
            Quadrant = 16,

            /// <summary>
            /// 交点
            /// </summary>
            Intersection = 32,

            /// <summary>
            /// 插入点
            /// </summary>
            Insert = 64,

            /// <summary>
            /// 垂足
            /// </summary>
            Pedal = 128,

            /// <summary>
            /// 切点
            /// </summary>
            Tangent = 256,

            /// <summary>
            /// 最近点
            /// </summary>
            Nearest = 512,

            /// <summary>
            /// 几何中心
            /// </summary>
            Quick = 1024,

            /// <summary>
            /// 外观交点
            /// </summary>
            Appearance = 2048,

            /// <summary>
            /// 延伸
            /// </summary>
            Extension = 4096,

            /// <summary>
            /// 平行
            /// </summary>
            Parallel = 8192
        }

        /// <summary>
        /// 捕捉模式系统变量
        /// </summary>
        public static OSModeType OSMode
        {
            get
            {
                return (OSModeType)Convert.ToInt16(Application.GetSystemVariable("osmode"));
            }
            set
            {
                Application.SetSystemVariable("osmode", (int)value);
            }
        }

        /// <summary>
        /// 追加捕捉模式
        /// </summary>
        /// <param name="osm1">原系统变量</param>
        /// <param name="osm2">要追加的模式</param>
        public static void Append(this OSModeType osm1, OSModeType osm2)
        {
            osm1 |= osm2;
        }

        /// <summary>
        /// 检查系统变量的模式是否相同
        /// </summary>
        /// <param name="osm1">原模式</param>
        /// <param name="osm2">要比较的模式</param>
        /// <returns>等于要比较的模式返回 <see langword="true"/>，反之返回 <see langword="false"/></returns>
        public static bool Check(this OSModeType osm1, OSModeType osm2)
        {
            return (osm1 & osm2) == osm2;
        }

        /// <summary>
        /// 取消捕捉模式
        /// </summary>
        /// <param name="osm1">原模式</param>
        /// <param name="osm2">要取消的模式</param>
        public static void Remove(this OSModeType osm1, OSModeType osm2)
        {
            osm1 ^= osm2;
        }

        #endregion OsMode

        private static T ToEnum<T>(this string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }

        private static string GetName<T>(this T value)
        {
            return Enum.GetName(typeof(T), value);
        }

        #endregion Enum
    }
}