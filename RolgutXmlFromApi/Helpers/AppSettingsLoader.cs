using RolgutXmlFromApi.DTOs;
using System;
using System.Configuration;

namespace RolgutXmlFromApi.Helpers
{
    public static class AppSettingsLoader
    {
        public static GaskaApiSettings LoadApiSettings()
        {
            return new GaskaApiSettings
            {
                BaseUrl = GetString("GaskaApiBaseUrl", required: true),
                Acronym = GetString("GaskaApiAcronym", required: true),
                Person = GetString("GaskaApiPerson", required: true),
                Password = GetString("GaskaApiPassword", required: true),
                ApiKey = GetString("GaskaApiKey", required: true),
                CategoryId = GetInt("GaskaApiCategoryId", 0),
                ProductsPerPage = GetInt("GaskaApiProductsPerPage", 1000),
                ProductsInterval = GetInt("GaskaApiProductsInterval", 1),
                ProductPerDay = GetInt("GaskaApiProductPerDay", 500),
                ProductInterval = GetInt("GaskaApiProductInterval", 10)
            };
        }

        public static int GetLogsExpirationDays() =>
            GetInt("LogsExpirationDays", 14);

        public static TimeSpan GetFetchInterval() =>
            TimeSpan.FromMinutes(GetInt("FetchIntervalMinutes", 60));

        // Helpers
        private static string GetString(string key, bool required = false)
        {
            var value = ConfigurationManager.AppSettings[key];

            if (required && string.IsNullOrWhiteSpace(value))
                throw new ConfigurationErrorsException($"Missing required appSetting: '{key}'");

            return value;
        }

        private static int GetInt(string key, int defaultValue)
        {
            var raw = ConfigurationManager.AppSettings[key];
            if (int.TryParse(raw, out int result))
                return result;

            return defaultValue;
        }
    }
}