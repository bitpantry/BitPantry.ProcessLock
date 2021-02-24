using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.ProcessLock.Implementation.Database
{
    public class DatabaseProcessLockRepository
    {
        const string suffixChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        private bool _hasCheckedDatabase = false;
        private readonly IDatabaseProcessLockContext _ctx;

        private Scripts _scripts;

        public DatabaseProcessLockRepository(IDatabaseProcessLockContext ctx)
        {
            _ctx = ctx;
            _scripts = new Scripts(
                ctx.ServerType,
                ctx.UseTableNameSuffix
                    ? GetRandomTableNameSuffix()
                    : null);
        }

        private string GetRandomTableNameSuffix()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[8];
            var random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            return $"_{new String(stringChars)}";
        }

        public async Task Create(DatabaseProcessLockRecord record)
        {
            await Task.Run(() =>
            {
                EnsureDependencies();

                var cmd = _ctx.Connection.CreateCommand();
                cmd.CommandText = _scripts.GetInsertScript();
                cmd.CommandType = CommandType.Text;

                var paramProcessName = cmd.CreateParameter();
                paramProcessName.ParameterName = "@ProcessName";
                paramProcessName.DbType = DbType.String;
                paramProcessName.Value = record.ProcessName;
                paramProcessName.Direction = ParameterDirection.Input;

                var paramToken = cmd.CreateParameter();
                paramToken.ParameterName = "@Token";
                paramToken.DbType = DbType.String;
                paramToken.Value = record.Token;
                paramToken.Direction = ParameterDirection.Input;

                var ParamExpiresOn = cmd.CreateParameter();
                ParamExpiresOn.ParameterName = "@ExpiresOn";
                ParamExpiresOn.DbType = DbType.DateTime;
                ParamExpiresOn.Value = record.ExpiresOn;
                ParamExpiresOn.Direction = ParameterDirection.Input;

                var ParamLockDuration = cmd.CreateParameter();
                ParamLockDuration.ParameterName = "@LockDuration";
                ParamLockDuration.DbType = DbType.Int32;
                ParamLockDuration.Value = record.LockDuration;
                ParamLockDuration.Direction = ParameterDirection.Input;

                cmd.Parameters.Add(paramProcessName);
                cmd.Parameters.Add(paramToken);
                cmd.Parameters.Add(ParamExpiresOn);
                cmd.Parameters.Add(ParamLockDuration);

                cmd.ExecuteNonQuery();
            });
        }

        public async Task<DatabaseProcessLockRecord> ReadByToken(string token)
        {
            return await Task.Run(() =>
            {
                EnsureDependencies();

                var cmd = _ctx.Connection.CreateCommand();
                cmd.CommandText = _scripts.GetSelectByTokenSript();
                cmd.CommandType = CommandType.Text;

                var ParamToken = cmd.CreateParameter();
                ParamToken.ParameterName = "@Token";
                ParamToken.DbType = DbType.String;
                ParamToken.Value = token;
                ParamToken.Direction = ParameterDirection.Input;

                cmd.Parameters.Add(ParamToken);

                using (var reader = cmd.ExecuteReader())
                {
                    if(reader.Read())
                        return new DatabaseProcessLockRecord
                        {
                            ProcessName = reader.GetString(0),
                            Token = reader.GetString(1),
                            ExpiresOn = reader.GetDateTime(2),
                            LockDuration = reader.GetInt32(3)
                        };

                    return null;
                }
            });
        }

        public async Task<DatabaseProcessLockRecord> ReadByProcessName(string processName)
        {
            return await Task.Run(() =>
            {
                EnsureDependencies();

                var cmd = _ctx.Connection.CreateCommand();
                cmd.CommandText = _scripts.GetSelectByProcessNameScript();
                cmd.CommandType = CommandType.Text;

                var ParamProcessName = cmd.CreateParameter();
                ParamProcessName.ParameterName = "@ProcessName";
                ParamProcessName.DbType = DbType.String;
                ParamProcessName.Value = processName;
                ParamProcessName.Direction = ParameterDirection.Input;

                cmd.Parameters.Add(ParamProcessName);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        return new DatabaseProcessLockRecord
                        {
                            ProcessName = reader.GetString(0),
                            Token = reader.GetString(1),
                            ExpiresOn = reader.GetDateTime(2),
                            LockDuration = reader.GetInt32(3)
                        };

                    return null;
                }
            });
        }

        public async Task<DatabaseProcessLockRecord> Update(DatabaseProcessLockRecord record)
        {
            return await Task.Run(() =>
            {
                EnsureDependencies();

                var cmd = _ctx.Connection.CreateCommand();
                cmd.CommandText = _scripts.GetUpdateScript();
                cmd.CommandType = CommandType.Text;

                var ParamToken = cmd.CreateParameter();
                ParamToken.ParameterName = "@Token";
                ParamToken.DbType = DbType.String;
                ParamToken.Value = record.Token;
                ParamToken.Direction = ParameterDirection.Input;

                var ParamExpiresOn = cmd.CreateParameter();
                ParamExpiresOn.ParameterName = "@ExpiresOn";
                ParamExpiresOn.DbType = DbType.DateTime;
                ParamExpiresOn.Value = record.ExpiresOn;
                ParamExpiresOn.Direction = ParameterDirection.Input;

                var ParamLockDuration = cmd.CreateParameter();
                ParamLockDuration.ParameterName = "@LockDuration";
                ParamLockDuration.DbType = DbType.Int32;
                ParamLockDuration.Value = record.LockDuration;
                ParamLockDuration.Direction = ParameterDirection.Input;

                cmd.Parameters.Add(ParamToken);
                cmd.Parameters.Add(ParamExpiresOn);
                cmd.Parameters.Add(ParamLockDuration);

                cmd.ExecuteNonQuery();

                return record;
            });
        }

        public async Task Delete(string token)
        {
            await Task.Run(() =>
            {
                EnsureDependencies();

                var cmd = _ctx.Connection.CreateCommand();
                cmd.CommandText = _scripts.GetDeleteScript();
                cmd.CommandType = CommandType.Text;

                var ParamToken = cmd.CreateParameter();
                ParamToken.ParameterName = "@Token";
                ParamToken.DbType = DbType.String;
                ParamToken.Value = token;
                ParamToken.Direction = ParameterDirection.Input;

                cmd.Parameters.Add(ParamToken);

                cmd.ExecuteNonQuery();
            });
        }

        public async Task CreateTable()
        {
            await Task.Run(() =>
            {
                var cmd = _ctx.Connection.CreateCommand();
                cmd.CommandType = CommandType.Text;

                cmd.CommandText = _scripts.GetCreateTableScript();

                cmd.ExecuteNonQuery();
            });
        }

        public async Task<bool> DoesTableExist()
        {
            return await Task.Run(() =>
            {
                var cmd = _ctx.Connection.CreateCommand();
                cmd.CommandType = CommandType.Text;

                cmd.CommandText = _scripts.GetSelectTableScript();

                return (long)cmd.ExecuteScalar() > 0;
            });
        }

        public async Task ResetDatabase()
        {
            if (await DoesTableExist())
            {
                await Task.Run(() =>
                {
                    var cmd = _ctx.Connection.CreateCommand();
                    cmd.CommandType = CommandType.Text;

                    cmd.CommandText = _scripts.GetDropTableScript();

                    cmd.ExecuteNonQuery();
                });
            }
        }
        
        public void EnsureDependencies()
        {
            lock (_ctx)
            {
                // ensure open connection

                if (_ctx.Connection.State == ConnectionState.Closed)
                    _ctx.Connection.Open();

                // ensure database exists
                if (!_hasCheckedDatabase && !DoesTableExist().GetAwaiter().GetResult())
                    CreateTable().GetAwaiter().GetResult();

                _hasCheckedDatabase = true;
            }
        }

        public bool IsUniqueKeyViolatedException(Exception ex)
            => _ctx.IsUniqueKeyViolatedException(ex);
    }
}
