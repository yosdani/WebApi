using Microsoft.Extensions.Options;
using static Webdoc.Common.Support.LanguageSupport;
using Webdoc.Common.Exceptions;
using Webdoc.Common.Settings;
using Webdoc.Library.Entities;
using Webdoc.Library;
using WebApi.Support;

namespace WebApi.Support
{
    public class WebRestApi
    {
        private static Context[] contexts;
        private static int currentDBConIndex = 1;
        private static Timer _cleanupTimer, _dbConTimer;
        private static object _locker_firewall = new object(), _locker_timer = new object(), _locker_dbcons = new object();
        private static IServiceProvider services = null;
        private static string currentEnvironment;
        internal const string dataPath = "Backstage", dataPathReplacement = "Storage";
        private static readonly MultilingualException exception_singletonError = new MultilingualException(new LanguageObject("Can't set once a value has already been set", "Não é possível definir uma vez que um valor já foi definido"));
        private static readonly MultilingualException exception_dbConError = new MultilingualException(new LanguageObject("Cannot connect to the database", "Não é possível estabelecer ligação à base de dados"));

        internal static byte[] Resource_RegistrationFooter { get; private set; }

        internal static DisposableLazy<Context> GetLazyContext(bool readOnly = true, Connectionretrier retrier = null) => new DisposableLazy<Context>(() => new Context(ConnectionString().Result, readOnly, retrier));

        internal static async Task<string> ConnectionString()
        {
            return Settings.DataAccess.DBConnections[0];
        }

        internal static IServiceProvider Services
        {
            get => services;
            set
            {
                if (services != null)
                    throw exception_singletonError;
                services = value;
            }
        }
        public static Microsoft.AspNetCore.Hosting.IHostingEnvironment HostingEnvironment
        {
            get
            {
                return services.GetService(typeof(Microsoft.AspNetCore.Hosting.IHostingEnvironment)) as Microsoft.AspNetCore.Hosting.IHostingEnvironment;
            }
        }
        internal static AppSettings Settings
        {
            get
            {
                //This works to get file changes.
                IOptionsMonitor<AppSettings> item = services.GetService(typeof(IOptionsMonitor<AppSettings>)) as IOptionsMonitor<AppSettings>;
                AppSettings value = item.CurrentValue;
                return value;
            }
        }

        internal static DBKeysSettings DBKeys
        {
            get
            {
                //This works to get file changes.
                IOptionsMonitor<DBKeysSettings> item = services.GetRequiredService<IOptionsMonitor<DBKeysSettings>>();
                DBKeysSettings value = item.CurrentValue;
                return value;
            }
        }

        internal static void ConfigureDBConnections()
        {
            string[] dbCons;
            lock (_locker_dbcons)
            {
                AppSettings settings;
                if (contexts == null && (dbCons = (settings = Settings).DataAccess.DBConnections)?.Any() == true && (contexts = dbCons.Select(con => new Context(con, retrier: settings.DataAccess.ConnectionRetrier)).ToArray()).Length > 1 && settings.DataAccess.ConnectionPicker.ResetConnectionMinutes > 0)
                {
                    _dbConTimer = new Timer(ResetDBConnections);
                    _dbConTimer.Change(TimeSpan.Zero, TimeSpan.FromMinutes(settings.DataAccess.ConnectionPicker.ResetConnectionMinutes));

                }
            }

        }

        private static void ResetDBConnections(object state)
        {
            if (currentDBConIndex > 0)
                currentDBConIndex = -1;
        }
        internal static string CurrentEnvironment
        {
            get => currentEnvironment;
            set
            {
                if (currentEnvironment != null)
                    throw exception_singletonError;
                currentEnvironment = value;
            }
        }
    }
}

  