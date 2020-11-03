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

                var ParamId = cmd.CreateParameter();
                ParamId.ParameterName = "@Id";
                ParamId.DbType = DbType.String;
                ParamId.Value = record.Id;
                ParamId.Direction = ParameterDirection.Input;

                var ParamHostname = cmd.CreateParameter();
                ParamHostname.ParameterName = "@Hostname";
                ParamHostname.DbType = DbType.String;
                ParamHostname.Value = record.HostName;
                ParamHostname.Direction = ParameterDirection.Input;

                var ParamExpiresOn = cmd.CreateParameter();
                ParamExpiresOn.ParameterName = "@ExpiresOn";
                ParamExpiresOn.DbType = DbType.DateTime;
                ParamExpiresOn.Value = record.ExpiresOn;
                ParamExpiresOn.Direction = ParameterDirection.Input;

                cmd.Parameters.Add(ParamId);
                cmd.Parameters.Add(ParamHostname);
                cmd.Parameters.Add(ParamExpiresOn);

                cmd.ExecuteNonQuery();
            });
        }

        public async Task<DatabaseProcessLockRecord> Read(string processId)
        {
            return await Task.Run(() =>
            {
                EnsureDependencies();

                var cmd = _ctx.Connection.CreateCommand();
                cmd.CommandText = _scripts.GetSelectScript();
                cmd.CommandType = CommandType.Text;

                var ParamId = cmd.CreateParameter();
                ParamId.ParameterName = "@Id";
                ParamId.DbType = DbType.String;
                ParamId.Value = processId;
                ParamId.Direction = ParameterDirection.Input;

                cmd.Parameters.Add(ParamId);

                using (var reader = cmd.ExecuteReader())
                {
                    if(reader.Read())
                        return new DatabaseProcessLockRecord
                        {
                            Id = reader.GetString(0),
                            HostName = reader.GetString(1),
                            ExpiresOn = reader.GetDateTime(2)
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

                var ParamId = cmd.CreateParameter();
                ParamId.ParameterName = "@Id";
                ParamId.DbType = DbType.String;
                ParamId.Value = record.Id;
                ParamId.Direction = ParameterDirection.Input;

                var ParamHostname = cmd.CreateParameter();
                ParamHostname.ParameterName = "@Hostname";
                ParamHostname.DbType = DbType.String;
                ParamHostname.Value = record.HostName;
                ParamHostname.Direction = ParameterDirection.Input;

                var ParamExpiresOn = cmd.CreateParameter();
                ParamExpiresOn.ParameterName = "@ExpiresOn";
                ParamExpiresOn.DbType = DbType.DateTime;
                ParamExpiresOn.Value = record.ExpiresOn;
                ParamExpiresOn.Direction = ParameterDirection.Input;

                cmd.Parameters.Add(ParamId);
                cmd.Parameters.Add(ParamHostname);
                cmd.Parameters.Add(ParamExpiresOn);

                cmd.ExecuteNonQuery();

                return record;
            });
        }

        public async Task Delete(string processId)
        {
            await Task.Run(() =>
            {
                EnsureDependencies();

                var cmd = _ctx.Connection.CreateCommand();
                cmd.CommandText = _scripts.GetDeleteScript();
                cmd.CommandType = CommandType.Text;

                var ParamId = cmd.CreateParameter();
                ParamId.ParameterName = "@Id";
                ParamId.DbType = DbType.String;
                ParamId.Value = processId;
                ParamId.Direction = ParameterDirection.Input;

                cmd.Parameters.Add(ParamId);

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
