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
        public const string CONNECTIONSTRING = "Host=localhost;Port=5432;Pooling=true;Database=testdb;User Id=postgres;Password=postgres;";
    }
}
