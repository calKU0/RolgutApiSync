using RolgutXmlFromApi.DTOs;
using RolgutXmlFromApi.Helpers;
using RolgutXmlFromApi.Logging;
using RolgutXmlFromApi.Services;
using Serilog;
using System;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace RolgutXmlFromApi
{
    public partial class RolgutService : ServiceBase
    {
        // Settings
        private readonly GaskaApiSettings _apiSettings;

        private readonly FtpSettings _ftpSettings;
        private readonly TimeSpan _interval;
        private readonly int _logsExpirationDays;

        // Services
        private readonly GaskaApiService _apiService;

        private readonly FileService _fileService;

        private Timer _timer;
        private DateTime _lastProductDetailsSyncDate = DateTime.MinValue;
        private DateTime _lastRunTime;

        public RolgutService()
        {
            // App Settings initialization
            _apiSettings = AppSettingsLoader.LoadApiSettings();
            _ftpSettings = AppSettingsLoader.LoadFtpSettings();
            _interval = AppSettingsLoader.GetFetchInterval();
            _logsExpirationDays = AppSettingsLoader.GetLogsExpirationDays();

            // Services initialization
            _apiService = new GaskaApiService(_apiSettings);
            _fileService = new FileService();

            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // Serilog configuration and initialization
            LogConfig.Configure(_logsExpirationDays);

            _timer = new Timer(
                async _ => await TimerTickAsync(),
                null,
                TimeSpan.Zero,
                _interval
            );

            Log.Information("Service started. First run immediately. Interval: {Interval}", _interval);
        }

        protected override void OnStop()
        {
            Log.Information("Service stopped.");
            Log.CloseAndFlush();
        }

        private async Task TimerTickAsync()
        {
            try
            {
                _lastRunTime = DateTime.Now;

                // 1. Getting default info about products
                //await _apiService.SyncProducts();
                //Log.Information("Basic product sync completed.");

                // 2.Getting detailed info about products that are not in db yet
                //if (_lastProductDetailsSyncDate.Date < DateTime.Today)
                //{
                //    await _apiService.SyncProductDetails();
                //    _lastProductDetailsSyncDate = DateTime.Today;

                //    Log.Information("Detailed product sync completed.");
                //}

                // 3. Generate XML file
                await _fileService.GenerateXMLFile();
                Log.Information("XML generation completed.");

                // 4. TODO: Send XML file to FTP

                //Log.Information("Sending XML to FTP completed.");
                //DateTime nextRun = _lastRunTime.Add(_interval);
                //Log.Information("All processes completed. Next run scheduled at: {NextRun}", nextRun);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during API synchronization.");
            }
        }
    }
}