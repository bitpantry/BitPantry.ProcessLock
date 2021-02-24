using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.ProcessLock.Implementation.Database
{
    internal class Scripts
    {
        private const string CreateTableScriptName = "createTable";
        private const string SelectTableScriptName = "selectTable";
        private const string DropTableScriptName = "dropTable";

        private DatabaseProcessLockServerType _serverType;
        private readonly string _tableName;
        private readonly Dictionary<string, string> _scripts;

        public Scripts(DatabaseProcessLockServerType serverType, string tableNameSuffix) 
        { 
            _serverType = serverType;
            _tableName = $"ProcessLock{tableNameSuffix}";

            _scripts = new Dictionary<string, string>
            {
                // CREATE TABLE

                { $"{DatabaseProcessLockServerType.Sqlite}_{CreateTableScriptName}",
                        $"CREATE TABLE {_tableName} (" +
                        "ProcessName TEXT PRIMARY KEY, " +
                        "Token TEXT NOT NULL, " +
                        "ExpiresOn TEXT NOT NULL, " +
                        "LockDuration TEXT NOT NULL)"
                },
                { $"{DatabaseProcessLockServerType.SqlServer}_{CreateTableScriptName}",
                        $"CREATE TABLE [dbo].[{_tableName}] " +
                        "( " +
                        "[ProcessName][varchar](200) NOT NULL, " +
                        "[Token][varchar](200) NOT NULL, " +
                        "[ExpiresOn] [datetime] NOT NULL, " +
                        "[LockDuration] [int] NOT NULL, " +
                        $"CONSTRAINT[PK_{_tableName}] PRIMARY KEY CLUSTERED([ProcessName] ASC)" +
                        "WITH(PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON[PRIMARY] " +
                        ") ON[PRIMARY]" },

                // SELECT TABLE

                { $"{DatabaseProcessLockServerType.Sqlite}_{SelectTableScriptName}", $"SELECT COUNT(name) FROM sqlite_master WHERE type='table' AND name='{_tableName}'" },
                { $"{DatabaseProcessLockServerType.SqlServer}_{SelectTableScriptName}",
                        "SELECT CAST(COUNT(*) AS BIGINT) " +
                        "FROM INFORMATION_SCHEMA.TABLES " +
                        "WHERE TABLE_SCHEMA = 'dbo' " +
                        $"AND TABLE_NAME = '{_tableName}'" },

                // DROP TABLE

                { $"{DatabaseProcessLockServerType.Sqlite}_{DropTableScriptName}", $"DROP TABLE {_tableName}" },
                { $"{DatabaseProcessLockServerType.SqlServer}_{DropTableScriptName}", $"DROP TABLE [dbo].[{_tableName}]" }

            };

        }

        public string GetSelectTableScript() => GetScript(SelectTableScriptName);
        public string GetCreateTableScript() => GetScript(CreateTableScriptName);
        public string GetDropTableScript() => GetScript(DropTableScriptName);
        public string GetInsertScript()
            => $"INSERT INTO {_tableName}(ProcessName, Token, ExpiresOn, LockDuration) VALUES(@ProcessName, @Token, @ExpiresOn, @LockDuration)";

        public string GetSelectByTokenSript()
            => $"SELECT ProcessName, Token, ExpiresOn, LockDuration FROM {_tableName} WHERE Token = @Token";

        public string GetSelectByProcessNameScript()
            => $"SELECT ProcessName, Token, ExpiresOn, LockDuration FROM {_tableName} WHERE ProcessName = @ProcessName";

        public string GetUpdateScript()
            => $"UPDATE {_tableName} SET ExpiresOn = @ExpiresOn, LockDuration = @LockDuration WHERE Token = @Token";

        public string GetDeleteScript()
            => $"DELETE FROM {_tableName} WHERE Token = @Token";

        private string GetScript(string scriptName) => _scripts[$"{_serverType}_{scriptName}"];
    }
}
