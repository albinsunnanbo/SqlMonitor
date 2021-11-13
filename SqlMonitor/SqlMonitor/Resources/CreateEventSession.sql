CREATE EVENT SESSION [SqlMonitorSqlDeadlockMonitor] ON SERVER 
-- Dead locks
ADD EVENT sqlserver.database_xml_deadlock_report(
    ACTION(package0.collect_system_time,sqlserver.client_app_name,sqlserver.client_hostname,sqlserver.database_name,sqlserver.request_id,sqlserver.session_id,sqlserver.sql_text,sqlserver.username)),
-- Dead locks (second version, listen to both since we're "one event behind"), see https://itsalljustelectrons.blogspot.com/2017/01/Hide-And-Seek-With-Extended-Events.html
ADD EVENT sqlserver.xml_deadlock_report(
    ACTION(package0.collect_system_time,sqlserver.client_app_name,sqlserver.client_hostname,sqlserver.database_name,sqlserver.request_id,sqlserver.session_id,sqlserver.sql_text,sqlserver.username)),

-- Failing queries
ADD EVENT sqlserver.error_reported(
    ACTION(package0.collect_system_time,sqlserver.client_app_name,sqlserver.client_hostname,sqlserver.database_name,sqlserver.request_id,sqlserver.session_id,sqlserver.sql_text,sqlserver.username)
    WHERE ([severity]>(10))), --0-10 is information level, skip

-- Application Abort (A.K.A. The Time Out) / [result]=(2)
-- Long Running Queries                    / [duration]>(10000000)
ADD EVENT sqlserver.rpc_completed(SET collect_output_parameters=(1),collect_statement=(1)
    ACTION(package0.collect_system_time,sqlserver.client_app_name,sqlserver.client_hostname,sqlserver.database_name,sqlserver.request_id,sqlserver.session_id,sqlserver.sql_text,sqlserver.username)
    WHERE ([result]=(2) OR [duration]>(10000000)) -- 10 seconds
    AND ([database_name] <> 'MyExcludeDatabase')
    )

WITH (STARTUP_STATE=OFF,TRACK_CAUSALITY=ON)
