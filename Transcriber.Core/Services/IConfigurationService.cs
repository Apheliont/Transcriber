using System;
using System.Collections.Generic;
using System.Text;

namespace Transcriber.Core.Services
{
    public interface IConfigurationService
    {
        Dictionary<string, string> GetLanguages();
    }
}
