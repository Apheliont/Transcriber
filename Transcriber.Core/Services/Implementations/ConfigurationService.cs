using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;


namespace Transcriber.Core.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly IConfigurationRoot _config;
        public ConfigurationService()
        {
            _config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }
        public Dictionary<string, string> GetLanguages()
        {
            return _config.GetSection("LanguageEndpointURIs").GetChildren().ToDictionary(x => x.Key, x => x.Value);
        }

    }
}
