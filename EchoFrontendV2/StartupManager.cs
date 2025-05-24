using TestSQLLite;

namespace EchoFrontendV2
{
    public class StartupManager
    {
        public MemoryDB Database { get; private set; }
        public SessionManager SessionManager { get; private set; }

        private readonly RealtimeLogger _logger;

        public StartupManager(RealtimeLogger logger)
        {
            _logger = logger;
            SessionManager = new SessionManager();
            Database = new MemoryDB(_logger);
        }
    }

}
