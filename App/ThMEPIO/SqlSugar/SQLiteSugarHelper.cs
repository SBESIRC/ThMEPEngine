using Microsoft.Data.Sqlite;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPIO.SqlSugar
{
    public class SQLiteSugarHelper : SqlSugarHelper
    {
        protected override string ConnectStr => $@"Data Source = {_url};";
        protected override SqlSugarClient Instance => new SqlSugarClient(new ConnectionConfig()
        {
            ConnectionString = ConnectStr,
            DbType = DbType.Sqlite,//设置数据库类型
            IsAutoCloseConnection = true,//自动释放数据务，如果存在事务，在事务结束后释放
            InitKeyType = InitKeyType.SystemTable,//从数据库系统表读取主键信息中（InitKeyType.Attribute从实体特性中读取主键自增列信息）
        });

        public SQLiteSugarHelper(string url) : base(url)
        {

        }
    }
}
