using System.Collections.Generic;
using System.Data;
using Etl.Domain.Entities;
using Npgsql;
using Renci.SshNet; 

namespace Etl.Data
{
    public class DbContext : IDisposable
    {
        private readonly DbConnectionParams _connectionParams;
        private SshClient _sshClient;

        public DbContext(DbConnectionParams connectionParams)
        {
            _connectionParams = connectionParams;
            if (!string.IsNullOrEmpty(_connectionParams.SshHost))
            {
                ConnectSsh();
            }
        }

        private void ConnectSsh()
        {
            _sshClient = new SshClient(_connectionParams.SshHost, _connectionParams.SshPort, 
                                        _connectionParams.SshUsername, _connectionParams.SshPassword);
            _sshClient.Connect();
        }

        
        // Получить строку подключения к БД
        private string GetConnectionString()
        {
            if (_sshClient != null && _sshClient.IsConnected)
            {
                // перенаправление порта
                var localPort = new Random().Next(10000, 65000); // Случайный локальный порт
                var portForwarded = new ForwardedPortLocal("127.0.0.1", (uint)localPort, _connectionParams.Host, (uint)_connectionParams.Port);
                _sshClient.AddForwardedPort(portForwarded);
                portForwarded.Start();

                return $"Host=127.0.0.1;Port={localPort};Username={_connectionParams.Username};Password={_connectionParams.Password};Database={_connectionParams.Database};";
            }

            // Если SSH не используется
            return $"Host={_connectionParams.Host};Port={_connectionParams.Port};Username={_connectionParams.Username};Password={_connectionParams.Password};Database={_connectionParams.Database};";
        }

        public List<MappingRecord> GetMappingRecords()
        {
            var records = new List<MappingRecord>();
            string connectionString = GetConnectionString();

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new NpgsqlCommand($"""
                                                                SELECT 
                                                                    m.id, 
                                                                    m.source_column,
                                                                    m.element_type_id, 
                                                                    m.parent_id,
                                                                    m.target_field_id,
                                                                    m.loader_id,
                                                                    m.value,
                                                                    e.object_id
                                                               FROM 
                                                                   etl_editor.mappings m
                                                               LEFT JOIN object_editor.entities e ON e.id = m.target_field_id
                                                               WHERE m.task_id = {_connectionParams.TaskId}
                                                        """, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        records.Add(new MappingRecord
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            SourceColumn = reader["source_column"].ToString(),
                            ElementTypeId = reader["element_type_id"].ToString(),
                            ParentId = reader["parent_id"] != DBNull.Value ? Convert.ToInt32(reader["parent_id"]) : null,
                            TargetFieldId = reader["target_field_id"] != DBNull.Value ? Convert.ToInt32(reader["target_field_id"]) : null,
                            LoaderId = reader["loader_id"] != DBNull.Value ? Convert.ToInt32(reader["loader_id"]) : null,
                            Value = reader["value"] != DBNull.Value ? Convert.ToInt32(reader["value"]) : null,
                            ObjectId = reader["object_id"] != DBNull.Value ? Convert.ToInt32(reader["object_id"]) : null
                        });
                    }
                }
            }
            return records;
        }
        
        public void Dispose()
        {
            _sshClient?.Disconnect();
            _sshClient?.Dispose();
        }

        // Создает папку для данной БД, если ее нет и создает/(дропает и создает новую) папку для сущностей данной таски
        public void CreateOrReplaceTaskEntityDir(MappingRecord mappingRecord)
        {
            string? dirPath = Environment.GetEnvironmentVariable("FILE_STORAGE\\" + _connectionParams.Database + '\\' +
                                                                 _connectionParams.TaskId);
            if (dirPath != null) Directory.CreateDirectory(dirPath);
        }
    }
}