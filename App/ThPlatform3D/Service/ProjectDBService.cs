﻿using SqlSugar;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ThPlatform3D.Model.Project;

namespace ThPlatform3D.Service
{
    public class ProjectDBService
    {
        
        ConnectionConfig dbSqlServerConfig = new ConnectionConfig()
        {
            ConfigId="DBSqlServer",
            DbType = SqlSugar.DbType.SqlServer,
            ConnectionString = "Data Source= 172.16.0.2;Initial Catalog=Product_Project;User ID=ghost123;Password=12345abc123!",
            IsAutoCloseConnection =true,
        };
        ConnectionConfig dbMySqlConfig = new ConnectionConfig()
        {
            ConfigId = "DBMySql",
            DbType = SqlSugar.DbType.MySql,
            ConnectionString = string.Format("Server={0};Port={1};Database={2};Uid={3};Pwd={4};", "172.17.1.37", "3306", "thbim_project", "thbim_project", "5Z_7e6B8d54b"),
            IsAutoCloseConnection = true,
        };
        public ProjectDBService() 
        {
            
        }
        public List<DBProject> GetUserProjects(string userId)
        {
            var resPorject = new List<DBProject>();
            if (string.IsNullOrEmpty(userId))
                return resPorject;
            SqlSugarClient sqlClient = new SqlSugarClient(dbSqlServerConfig);
            SqlSugarProvider sqlDB = sqlClient.GetConnection(dbSqlServerConfig.ConfigId);
            //step1获取可以看的项目
            var dataTable = sqlClient.Ado.GetDataTable("SELECT [Id] FROM [Product_Project].[dbo].[AI_prjrole] where [ExecutorId]=@Uid", new { Uid = userId });
            var prjIds = new List<string>();
            foreach (DataRow dataRow in dataTable.Rows)
            {
                var id = dataRow[0].ToString();
                if (prjIds.Contains(id))
                    continue;
                prjIds.Add(id);
            }
            //step2根据项目获取项目的完整信息
            foreach (var prjId in prjIds)
            {
                Expressionable<DBProject> expressionable = new Expressionable<DBProject>();
                expressionable.And(c => c.Id == prjId);
                expressionable.And(c => c.MajorName == null);
                var prjs = sqlDB.Queryable<DBProject>().Where(expressionable.ToExpression()).ToList();
                if (prjs.Count < 1)
                    continue;
                var prj = prjs.FirstOrDefault();
                prj.SubProjects = new List<DBSubProject>();
                Expressionable<DBSubProject> subExp = new Expressionable<DBSubProject>();
                subExp.And(c => c.Id == prjId);
                var subPrjs = sqlDB.Queryable<DBSubProject>().Where(subExp.ToExpression()).Distinct().ToList();
                prj.SubProjects.AddRange(subPrjs);
                resPorject.Add(prj);
            }
            return resPorject;
        }
        public bool CADBindingProject(ProjectFile projectFile,out string msg) 
        {
            msg = "";
            try
            {
                SqlSugarClient sqlClient = new SqlSugarClient(dbMySqlConfig);
                SqlSugarProvider sqlDB = sqlClient.GetConnection(dbMySqlConfig.ConfigId);
                //需要保留原有效的项目文件Id
                var hisPrj = GetValidProject(projectFile.FileName);
                if (null != hisPrj)
                {
                    //删除旧数据
                    sqlDB.Updateable<ProjectFile>().SetColumns(it => 
                        new ProjectFile() { IsDel = 1,
                            UpdateTime = DateTime.Now,
                            UpdatedBy = projectFile.CreaterId,
                            UpdatedUserName = projectFile.CreaterName,
                        })
                        .Where(it => it.FileName == projectFile.FileName
                        && it.IsDel == 0).ExecuteCommand();
                    projectFile.ProjectFileId = hisPrj.ProjectFileId;
                }
                //插入新数据
                int res = sqlDB.Insertable<ProjectFile>(projectFile).ExecuteCommand();
                return res > 0;
            }
            catch (Exception ex) 
            {
                msg = ex.Message;
                return false;
            }
        }
        public ProjectFile GetValidProject(string fileName, string prjId, string subPrjId)
        {
            var sqlClient = new SqlSugarClient(dbMySqlConfig);
            SqlSugarProvider sqlDB = sqlClient.GetConnection(dbMySqlConfig.ConfigId);
            var expressionable = new Expressionable<ProjectFile>();
            expressionable.And(c => c.FileName == fileName);
            expressionable.And(c => c.PrjId == prjId);
            expressionable.And(c => c.SubPrjId == subPrjId);
            expressionable.And(c => c.IsDel == 0);
            var res = sqlDB.Queryable<ProjectFile>().Where(expressionable.ToExpression()).ToList();
            if (res.Count < 1)
                return null;
            return res.FirstOrDefault();
        }
        public ProjectFile GetValidProject(string fileName)
        {
            var sqlClient = new SqlSugarClient(dbMySqlConfig);
            SqlSugarProvider sqlDB = sqlClient.GetConnection(dbMySqlConfig.ConfigId);
            var expressionable = new Expressionable<ProjectFile>();
            expressionable.And(c => c.FileName == fileName);
            expressionable.And(c => c.IsDel == 0);
            var res = sqlDB.Queryable<ProjectFile>().Where(expressionable.ToExpression()).ToList();
            if (res.Count < 1)
                return null;
            return res.FirstOrDefault();
        }
    }
}