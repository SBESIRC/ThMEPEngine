using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using SqlSugar;
using DbType = SqlSugar.DbType;

namespace ThMEPIO.SqlSugar
{
    public abstract class SqlSugarHelper
    {
        public SqlSugarHelper(string url)
        {
            this._url = url;
        }

        protected string _url;
        protected abstract string ConnectStr { get; }

        protected abstract SqlSugarClient Instance { get; }

        //增
        public void InsertTable<T>(T t) where T : class, new()
        {
            //Where(true/*不插入null值的列*/,false/*不插入主键值*/)
            this.Instance.Insertable(t).IgnoreColumns(true, false).ExecuteCommand();
        }

        //删
        public void DeleteTable<T>(Expression<Func<T, bool>> expressionWhere) where T : class, new()
        {
            this.Instance.Deleteable<T>().Where(expressionWhere).ExecuteCommand();
        }

        //改
        public int UpdateTable<T>(T t) where T : class, new()
        {
            return this.Instance.Updateable(t).IgnoreColumns(ignoreAllNullColumns: true).ExecuteCommand();
        }

        //查
        public ISugarQueryable<T> Queryable<T>(Expression<Func<T, bool>> expressionWhere) where T : class, new()
        {
            return this.Instance.Queryable<T>().Where(expressionWhere);
        }

        /// <summary>
        /// DbFirst
        /// 调用会生成Model文件至指定文件夹
        /// 请谨慎调用
        /// </summary>
        public void CreatModel(string directoryPath, string nameSpace = "Models")
        {
            this.Instance.DbFirst
                .IsCreateDefaultValue()
                .CreateClassFile(directoryPath, nameSpace);
        }

        //纯SQL
        public void ExecuteCommand(string sql, object parameters)
        {
            this.Instance.Ado.ExecuteCommand(sql, parameters);
        }
    }
}
