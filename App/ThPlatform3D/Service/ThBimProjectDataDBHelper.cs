using SqlSugar;
using System.Collections.Generic;
using ThPlatform3D.Model;
using ThPlatform3D.Model.MysqlModel;

namespace ThPlatform3D.Service
{
    public static class ThBimProjectDataDBHelper
    {
        private static string ConnStr = "server= 172.17.1.37;Initial Catalog=thbim_project;Persist Security Info=True;uid=thbim_project;pwd=5Z_7e6B8d54b;";

        private static SqlSugarClient CreateConnection(string connStr)
        {
            var config = new ConnectionConfig()
            {
                DbType = DbType.MySql,
                ConnectionString = ConnStr,
                IsAutoCloseConnection = true,
            };
            return new SqlSugarClient(config);
        }

        //public static void Query(string docFullName)
        //{
        //    var db = CreateConnection(ConnStr);
        //    var dt = db.Ado.GetDataTable("select * from ProjectFiles where FileName=@fileName", new List<SugarParameter>()
        //    {
        //        new SugarParameter("@fileName",docFullName)
        //    });           
        //}

        public static List<ProjectFile> QueryProjectFiles(string docFullName)
        {
            var db = CreateConnection(ConnStr); 
            return db.Queryable<ProjectFile>()
                .Where(o=>o.FileName==docFullName)
                .Where(o=>o.IsDel==0)
                .ToList();
        }

        public static void CreateClass()
        {
            var db = CreateConnection(ConnStr);
            db.DbFirst.Where(c => c == "ProjectFile" || c == "PlaneViewInfo" ||
            c == "ElevationViewInfo" || c == "SectionViewInfo").CreateClassFile(@"D:\Temp");
        }

        public static List<PlaneViewInfo> QueryPlaneViewInfosByFileName(this string docFullName)
        {
            var db = CreateConnection(ConnStr);       
            return db.Queryable<PlaneViewInfo>().Where(o => o.FileName == docFullName).ToList();
        }

        public static List<ElevationViewInfo> QueryElevationViewInfosByFileName(this string docFullName)
        {
            var db = CreateConnection(ConnStr);
            return db.Queryable<ElevationViewInfo>().Where(o => o.FileName == docFullName).ToList();
        }

        public static List<SectionViewInfo> QuerySectionViewInfosByFileName(this string docFullName)
        {
            var db = CreateConnection(ConnStr);
            return db.Queryable<SectionViewInfo>().Where(o => o.FileName == docFullName).ToList();
        }

        public static List<PlaneViewInfo> QueryPlaneViewInfosById(this string id)
        {
            var db = CreateConnection(ConnStr);
            return db.Queryable<PlaneViewInfo>().Where(o => o.Id == id).ToList();
        }

        public static List<ElevationViewInfo> QueryElevationViewInfosById(this string id)
        {
            var db = CreateConnection(ConnStr);
            return db.Queryable<ElevationViewInfo>().Where(o => o.Id == id).ToList();
        }

        public static List<SectionViewInfo> QuerySectionViewInfosById(this string id)
        {
            var db = CreateConnection(ConnStr);
            return db.Queryable<SectionViewInfo>().Where(o => o.Id == id).ToList();
        }

        public static void InsertToMySqlDb(this PlaneViewInfo planeViewInfo)
        {
            var db = CreateConnection(ConnStr);
            db.Insertable(planeViewInfo).ExecuteCommand();
        }
        public static void InsertToMySqlDb(this SectionViewInfo sectionViewInfo)
        {
            var db = CreateConnection(ConnStr);
            db.Insertable(sectionViewInfo).ExecuteCommand();
        }
        public static void InsertToMySqlDb(this ElevationViewInfo elevationViewInfo)
        {
            var db = CreateConnection(ConnStr);
            db.Insertable(elevationViewInfo).ExecuteCommand();
        }

        public static void UpdateToMySqlDb(this PlaneViewInfo planeViewInfo)
        {
            var db = CreateConnection(ConnStr);
            db.Updateable(planeViewInfo).ExecuteCommand();
        }
        public static void UpdateToMySqlDb(this SectionViewInfo sectionViewInfo)
        {
            var db = CreateConnection(ConnStr);
            db.Updateable(sectionViewInfo).ExecuteCommand();
        }
        public static void UpdateToMySqlDb(this ElevationViewInfo elevationViewInfo)
        {
            var db = CreateConnection(ConnStr);
            db.Updateable(elevationViewInfo).ExecuteCommand();
        }

        public static void DeleteToMySqlDb(this PlaneViewInfo planeViewInfo)
        {
            var db = CreateConnection(ConnStr);
            db.Deleteable(planeViewInfo).ExecuteCommand();
        }
        public static void DeleteToMySqlDb(this SectionViewInfo sectionViewInfo)
        {
            var db = CreateConnection(ConnStr);
            db.Deleteable(sectionViewInfo).ExecuteCommand();
        }
        public static void DeleteToMySqlDb(this ElevationViewInfo elevationViewInfo)
        {
            var db = CreateConnection(ConnStr);
            db.Deleteable(elevationViewInfo).ExecuteCommand();
        }
    }
}
