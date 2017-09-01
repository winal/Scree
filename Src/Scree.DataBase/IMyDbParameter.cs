using System;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Collections;
using System.Data.Common;
using System.Collections.Generic;

namespace Scree.DataBase
{
    public interface IMyDbParameter
    {
        SqlParameter Para { get; }
        string ParameterName { get; set; }
        int Size { get; set; }
        object Value { get; set; }
        SqlDbType DbType { get; set; }
        ParameterDirection Direction { get; set; }
    }
}
