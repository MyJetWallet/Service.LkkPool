using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using Service.LkkPool.Services;

namespace Service.LkkPool
{
    public class ApplicationLifetimeManager : ApplicationLifetimeManagerBase
    {
        private readonly ILogger<ApplicationLifetimeManager> _logger;
        private readonly PoolManager _poolManager;

        public ApplicationLifetimeManager(IHostApplicationLifetime appLifetime, ILogger<ApplicationLifetimeManager> logger,
            PoolManager poolManager)
            : base(appLifetime)
        {
            _logger = logger;
            _poolManager = poolManager;
        }

        protected override void OnStarted()
        {
            _logger.LogInformation("OnStarted has been called.");
            _poolManager.Start();
        }

        protected override void OnStopping()
        {
            _logger.LogInformation("OnStopping has been called.");
        }

        protected override void OnStopped()
        {
            _logger.LogInformation("OnStopped has been called.");
        }
    }
}
