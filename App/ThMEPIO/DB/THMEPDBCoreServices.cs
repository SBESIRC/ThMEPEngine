using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPIO.DB
{
    public interface THMEPDBCoreServices
    {
        //查
        DataTable ExecuteDataTable(string tableName);
        
        //查
        DataTable GetTable(string sql);

        //增删改
        int ExecuteNonQuery(string sql);
    }
}
