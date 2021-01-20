using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Transcriber.Core.Util
{
    public class FFmpegPercentageParser
    {

        private int? _totalDurationInSeconds = null;
        private int? _currentTimeProgressInSeconds = null;
        private Regex _durationRegEx;
        private Regex _currentTimeRegEx;

        public FFmpegPercentageParser()
        {
            _durationRegEx = new Regex(@"Duration: (?<duration>.*)\.\d{2}, start:");
            _currentTimeRegEx = new Regex(@"time=(?<current>.*)\.\d{2} bitrate=");
        }

        public void Parse(string s)
        {

            if (_totalDurationInSeconds == null)
            {
                Match matchDuration = _durationRegEx.Match(s);
                if (matchDuration.Success)
                {
                    string[] times = matchDuration.Groups["duration"].Value.Split(':');
                    _totalDurationInSeconds = SplitedStringToSeconds(times);
                }
            }
            else
            {
                Match matchCurrentTime = _currentTimeRegEx.Match(s);
                if (matchCurrentTime.Success)
                {
                    string[] times = matchCurrentTime.Groups["current"].Value.Split(':');
                    _currentTimeProgressInSeconds = SplitedStringToSeconds(times);
                }
            }
        }

        public int? GetPercentCompleted()
        {
            if (_totalDurationInSeconds != null && _currentTimeProgressInSeconds != null)
            {
                return (int)((double)_currentTimeProgressInSeconds / _totalDurationInSeconds * 100);
            }
            return null;
        }

        private int? SplitedStringToSeconds(string[] times)
        {
            int hours, minutes, seconds;
            if (int.TryParse(times[0], out hours)
                && int.TryParse(times[1], out minutes)
                && int.TryParse(times[2], out seconds))
            {
                return (hours * 3600) + (minutes * 60) + seconds;
            }
            return null;
        }
    }

}
