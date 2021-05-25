// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.ApplicationInsights.DependencyCollector;
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

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
            Debug.WriteLine($"DiagnosticSourceBridgeEventListener OnEventWritten enter, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");

            //if (!ActivityHelper.DiagnosticListener.IsEnabled())
            //{
            //    return;
            //}

            //TODO: Review if should replace above with this?
            if (!WcfTrackingTelemetryModule.IsEnabled)
            {
                return;
            }

            Activity activity;
            if(!ActivityHelper.TryGetRootActivityFromOperationContext(out activity) || activity == null)
            {
                Debug.WriteLine($"DiagnosticSourceBridgeEventListener OnEventWritten TryGetRootActivityFromOperationContext failed , Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");

                activity = Activity.Current;
                if (activity == null)
                {
                    Debug.WriteLine($"DiagnosticSourceBridgeEventListener OnEventWritten Activity.Current null , Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");

                    return;
                }
            }

            //TODO: Review if should comment out.
            //if (!ActivityHelper.DiagnosticListener.IsEnabled(eventData.EventName))
            //{
            //    return;
            //}

            //TODO: IsFinished property is not public, but this should work to determine if already stopped, which occurs in ActivityHelper.StopOperation.
            //Investigate whether this is an issue. May not be because duration information is not collected for DispatchMessageFormatterSerialize etc. as is done in original code.
            if (activity.Duration > new TimeSpan(0)) 
            {
                //Debugger.Break();
                Debug.WriteLine($"WARNING: DiagnosticSourceBridgeEventListener OnEventWritten befire ActivityHelper.SetCurrentActivity , Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, " +
                    $"eventData.EventName: {eventData.EventName}, activity.RootId: {activity.RootId}, activity.SpanId: {activity.SpanId}, activity.ParentId: {activity.ParentId}, " +
                    $"activity.Durarion {activity.Duration}, Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");
                //activity.Start();
            }
            else
            {
                ActivityHelper.SetCurrentActivity(activity);
            }

            Debug.WriteLine($"DiagnosticSourceBridgeEventListener OnEventWritten after SetCurrentActivity , Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");
            Debug.WriteLine($"DiagnosticSourceBridgeEventListener OnEventWritten eventData.EventName {eventData.EventName}");
            Debug.WriteLine($"DiagnosticSourceBridgeEventListener OnEventWritten (OperationContext.Current != null): {OperationContext.Current != null}");
            if ((OperationContext.Current != null) && OperationContext.Current.OutgoingMessageProperties.TryGetValue(ActivityHelper.OperationActivityPropertyName, out activity))
            {
                Debug.WriteLine($"DiagnosticSourceBridgeEventListener OnEventWritten, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, activity.Id: {activity.Id}, activity.ParentId: {activity.ParentId}");

            }

            switch (eventData.EventName)
            {
                //case "Message":
                //    Debugger.Break();
                //    break;
                case "DispatchMessageInspectorAfterReceive":
                case "DispatchMessageInspectorBeforeSend":
                case "ClientMessageInspectorAfterReceive":
                case "ClientMessageInspectorBeforeSend":
                case "ParameterInspectorAfter":
                case "ParameterInspectorBefore":
                case "DispatchMessageFormatterDeserialize":
                case "DispatchMessageFormatterSerialize":
                    ////TODO: Added the two lines below for testing temporarily
                    //Debugger.Break();
                    //ActivityHelper.WriteTimedEvent(eventData.EventName, GetStringFromEventData(eventData, "TypeName"), GetLongFromEventData(eventData, "Duration"));
                    //break;
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