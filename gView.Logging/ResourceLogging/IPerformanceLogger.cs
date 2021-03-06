﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace gView.Framework.Logging.ResourceLogging
{
    public interface IPerformanceLogger
    {
        Task Init(string initialParameter);

        void Log(IPerformanceLoggerItem item);

        Task Flush();
    }
}
