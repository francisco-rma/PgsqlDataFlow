using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PgsqlDataFlow
{
    public static class Constants
    {
        public const int CHUNKSIZE = 100000;
        public const string CONNECTIONSTRING = "Host=localhost;Port=5432;Pooling=true;Database=db_dw_cloud_local;User Id=postgres;Password=password;";
        public const string CUSTOMCONNECTIONSTRING = "Host=localhost;Port=5432;Pooling=true;Database=db_dw_cloud_local;User Id=postgres;Password=password;Read Buffer Size = 16000;Write Buffer Size = 16000";
    }
}
