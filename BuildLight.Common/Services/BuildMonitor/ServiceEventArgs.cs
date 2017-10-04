using System;

namespace BuildLight.Common.Services.BuildMonitor
{
    public enum ServiceEventCode
    {
        Starting,
        Ending,
        BeginningQuery,
        CompletedQuery,
        QueryError
    }

    public class ServiceEventArgs : EventArgs
    {
        public ServiceEventCode EventCode { get; set; }
        public ServiceEventArgs(ServiceEventCode serviceEventCode)
        {
            EventCode = serviceEventCode;
        }
    }
}