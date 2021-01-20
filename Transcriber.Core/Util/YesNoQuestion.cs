using System;
using System.Collections.Generic;
using System.Text;

namespace Transcriber.Core.Util
{
    public class YesNoQuestion
    {
        public Action<bool> YesNoCallback { get; set; }
        public string Question { get; set; }
    }
}
