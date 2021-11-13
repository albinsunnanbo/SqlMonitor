using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlMonitor.Helpers
{
    public static class ExtendedEvents
    {
        public static void CreateEventSession()
        {
            SqlRunner.ExecuteSqlNonQuery(Properties.Resources.CreateEventSession);
        }

        public static bool EventSessionExists
        {
            get
            {
                var count = SqlRunner.ExecuteSqlScalar($"select Count(*) from sys.server_event_sessions where name = '{Properties.Resources.SessionName}'");
                return count > 0;
            }
        }
        public static bool EventSessionActive
        {
            get
            {
                var count = SqlRunner.ExecuteSqlScalar($"select Count(*) from sys.dm_xe_sessions where name = '{Properties.Resources.SessionName}'");
                return count > 0;
            }
        }
        public static void DropEventSession()
        {
            SqlRunner.ExecuteSqlNonQuery($"DROP EVENT SESSION [{Properties.Resources.SessionName}] ON SERVER");
        }
        public static void StartEventSession()
        {
            SqlRunner.ExecuteSqlNonQuery($"ALTER EVENT SESSION [{Properties.Resources.SessionName}] ON SERVER STATE = Start;");
        }
        public static void StopEventSession()
        {
            SqlRunner.ExecuteSqlNonQuery($"ALTER EVENT SESSION [{Properties.Resources.SessionName}] ON SERVER STATE = Stop;");
        }

        //        select Count(*) from sys.server_event_sessions
        //where name = 'albin'

    }
}
