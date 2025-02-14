using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PgsqlDataFlow.Extensions
{
    internal static class DateTimeExtensions
    {
        public static DateTime SetKind(this DateTime date, DateTimeKind kind)
        {
            return new DateTime(date.Ticks, kind);
        }

        public static DateTime Max(this DateTime date, DateTime dateCompare)
        {
            int timeline = DateTime.Compare(date, dateCompare);
            return timeline < 0 ? dateCompare : date;
        }
    }
}
