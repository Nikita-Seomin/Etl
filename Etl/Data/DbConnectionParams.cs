namespace Etl.Data
{
    public class DbConnectionParams
    {
        public int TaskId { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Database { get; set; }

        // SSH параметры
        public string SshHost { get; set; }
        public int SshPort { get; set; }
        public string SshUsername { get; set; }
        public string SshPassword { get; set; }
    }
}