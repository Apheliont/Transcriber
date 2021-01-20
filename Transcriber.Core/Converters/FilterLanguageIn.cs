using MvvmCross.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Linq;

namespace Transcriber.Core.Converters
{
    public class FilterLanguageIn : MvxValueConverter<IEnumerable<KeyValuePair<string, string>>, IEnumerable<string>>
    {
        protected override IEnumerable<string> Convert(IEnumerable<KeyValuePair<string, string>> value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Select((KeyValuePair<string, string> pair) => { return pair.Key; });
        }
    }
}
