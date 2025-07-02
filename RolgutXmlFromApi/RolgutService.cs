using RolgutXmlFromApi.DTOs;
using RolgutXmlFromApi.Helpers;
using RolgutXmlFromApi.Logging;
using RolgutXmlFromApi.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RolgutXmlFromApi
{
    public partial class RolgutService : ServiceBase
    {
        private readonly GaskaApiSettings _apiSettings;
        private readonly TimeSpan _interval;
        private readonly int _logsExpirationDays;
        private readonly GaskaApiService _apiService;

        private Timer _timer;
        private DateTime _lastProductDetailsSyncDate = DateTime.MinValue;

        public RolgutService()
        {
            // Services initialization
            _apiSettings = AppSettingsLoader.LoadApiSettings();
            _interval = AppSettingsLoader.GetFetchInterval();
            _logsExpirationDays = AppSettingsLoader.GetLogsExpirationDays();

            _apiService = new GaskaApiService(_apiSettings);

            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // Serilog configuration and initialization
            LogConfig.Configure(_logsExpirationDays);
            Log.Information("Service is starting up...");

            Timer timer = new Timer(
                async _ => await TimerTickAsync(),
                null,
                TimeSpan.Zero,
                _interval
            );

            Log.Information("Service started.");
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
                // 1. Getting default info about products
                await _apiService.SyncProducts();
                Log.Information("Default product sync completed.");

                // 2. Getting detailed info about products that are not in db yet
                if (_lastProductDetailsSyncDate.Date < DateTime.Today)
                {
                    await _apiService.SyncProductDetails();
                    _lastProductDetailsSyncDate = DateTime.Today;

                    Log.Information("Detailed product sync completed.");
                }

                // 3. TODO: Generate XML file
                // 4. TODO: Send XML file to FTP

                // Log.Information("Successfully sent XML file.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during API synchronization");
            }
        }
    }
}