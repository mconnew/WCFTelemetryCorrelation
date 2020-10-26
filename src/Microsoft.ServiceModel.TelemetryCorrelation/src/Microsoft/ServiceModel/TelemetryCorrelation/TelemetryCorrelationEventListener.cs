using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace Microsoft.ServiceModel.TelemetryCorrelation
{
    internal class DiagnosticSourceBridgeEventListener : EventListener
    {
        private const string EventSourceName = "Microsoft-Windows-Application ServiceModel-DiagnosticSource-Bridge";
        private static bool s_initialized = false;
        private static bool s_dsBridgeAvailable = false;
        private static object s_classLock = new object();
        private static DiagnosticSourceBridgeEventListener s_instance;

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name.Equals(EventSourceName))
            {
                ActivityHelper.EnsureInitialized();
                EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (!ActivityHelper.DiagnosticListener.IsEnabled())
            {
                return;
            }

            Activity activity;
            if(!ActivityHelper.TryGetRootActivityFromOperationContext(out activity) || activity == null)
            {
                activity = Activity.Current;
                if (activity == null)
                {
                    return;
                }
            }

            if(!ActivityHelper.DiagnosticListener.IsEnabled(eventData.EventName))
            {
                return;
            }

            ActivityHelper.SetCurrentActivity(activity);

            switch(eventData.EventName)
            {
                case "DispatchMessageInspectorAfterReceive":
                case "DispatchMessageInspectorBeforeSend":
                case "ClientMessageInspectorAfterReceive":
                case "ClientMessageInspectorBeforeSend":
                case "ParameterInspectorAfter":
                case "ParameterInspectorBefore":
                case "DispatchMessageFormatterDeserialize":
                case "DispatchMessageFormatterSerialize":
                case "ClientMessageFormatterDeserialize":
                case "ClientMessageFormatterSerialize":
                    ActivityHelper.WriteTimedEvent(eventData.EventName, GetStringFromEventData(eventData, "TypeName"), GetLongFromEventData(eventData, "Duration"));
                    break;
                case "DispatchSelectOperation":
                case "ClientSelectOperation":
                    ActivityHelper.SelectOperation(eventData.EventName, GetStringFromEventData(eventData, "TypeName"), GetStringFromEventData(eventData, "SelectedOperation"), GetLongFromEventData(eventData, "Duration"));
                    break;
                case "InvokeOperationStart":
                    ActivityHelper.StartOperation(GetStringFromEventData(eventData, "TypeName"), GetLongFromEventData(eventData, "Timestamp"));
                    break;
                case "InvokeOperationStop":
                    ActivityHelper.StopOperation(GetLongFromEventData(eventData, "Timestamp"));
                    break;
                case "InstanceProviderGet":
                case "InstanceProviderRelease":
                    ActivityHelper.InstanceProvider(eventData.EventName, GetStringFromEventData(eventData, "TypeName"), GetIntFromEventData(eventData, "InstanceHash"), GetLongFromEventData(eventData, "Duration"));
                    break;
                case "CallThrottled":
                case "InstanceThrottled":
                    ActivityHelper.Throttled(eventData.EventName, GetLongFromEventData(eventData, "Duration"));
                    break;
                case "Authentication":
                    ActivityHelper.Auth(eventData.EventName, GetStringFromEventData(eventData, "TypeName"), GetBoolFromEventData(eventData, "Authenticated"), GetLongFromEventData(eventData, "Duration"));
                    break;
                case "Authorization":
                    ActivityHelper.Auth(eventData.EventName, GetStringFromEventData(eventData, "TypeName"), GetBoolFromEventData(eventData, "Authorized"), GetLongFromEventData(eventData, "Duration"));
                    break;
            }
        }

        private bool GetBoolFromEventData(EventWrittenEventArgs eventData, string keyName)
        {
            int pos = eventData.PayloadNames.IndexOf(keyName);
            if (pos > -1)
            {
                var val = eventData.Payload[pos];
                if (val is bool)
                {
                    return (bool)val;
                }
            }

            return false;
        }

        private int GetIntFromEventData(EventWrittenEventArgs eventData, string keyName)
        {
            int pos = eventData.PayloadNames.IndexOf(keyName);
            if (pos > -1)
            {
                var val = eventData.Payload[pos];
                if (val is int)
                {
                    return (int)val;
                }
            }

            return -1;
        }

        private long GetLongFromEventData(EventWrittenEventArgs eventData, string keyName)
        {
            int pos = eventData.PayloadNames.IndexOf(keyName);
            if (pos > -1)
            {
                var val = eventData.Payload[pos];
                if(val is long)
                {
                    return (long)val;
                }
            }

            return -1;
        }


        private string GetStringFromEventData(EventWrittenEventArgs eventData, string keyName)
        {
            int pos = eventData.PayloadNames.IndexOf(keyName);
            if (pos > -1)
            {
                return eventData.Payload[pos].ToString();
            }

            return string.Empty;
        }

        internal static void Init()
        {
            if (!s_initialized)
            {
                lock (s_classLock)
                {
                    if (!s_initialized)
                    {
                        s_dsBridgeAvailable = typeof(System.ServiceModel.ChannelFactory).Assembly.GetType("System.ServiceModel.Diagnostics.DiagnosticSourceBridge", false) != null;
                        if (s_dsBridgeAvailable)
                        {
                            s_instance = new DiagnosticSourceBridgeEventListener();
                        }

                        s_initialized = true;
                    }
                }
            }
        }
    }
}