// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using System.Runtime.CompilerServices;
using OperationContext = System.ServiceModel.OperationContext;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.ApplicationInsights.DependencyCollector;

namespace Microsoft.ServiceModel.TelemetryCorrelation
{
    internal class ActivityHelper
    {
        public const string ServiceModelListenerName = "System.ServiceModel.TelemetryCorrelation";
        public const string SendMessageActivityName = "System.ServiceModel.SendMessage";
        public const string SendMessageActivityStartName = "System.ServiceModel.SendMessage.Start";
        public const string SendMessageActivityStopName = "System.ServiceModel.SendMessage.Stop";  
        public const string ReceiveMessageActivityName = "System.ServiceModel.ReceiveMessage";
        public const string ReceiveMessageActivityStartName = "System.ServiceModel.ReceiveMessage.Start";
        public const string ReceiveMessageActivityStopName = "System.ServiceModel.ReceiveMessage.Stop";
        public const string InvokeOperationActivityName = "System.ServiceModel.InvokeOperation";
        public const string InvokeOperationActivityStartName = "System.ServiceModel.InvokeOperation.Start";
        public const string InvokeOperationActivityStopName = "System.ServiceModel.InvokeOperation.Stop";
        public const string CurrentActivityPropertyName = "System.Diagnostics.Activity.Current";
        public const string OperationActivityPropertyName = "System.Diagnostics.Activity.OperationActivity";
        public const string ReceiveMessageActivityPropertyName = "System.ServiceModel.ReceiveMessageActivity";
        public const string SendRequestActivityPropertyName = "System.ServiceModel.SendRequestActivity";

        public static DiagnosticListener DiagnosticListener { get; private set; }

        // Constants to support working with AspNet Telemetery Correlation
        public const string AspNetActivityName = "Microsoft.AspNet.HttpReqIn";
        public const string RequestIdHttpHeaderName = "Request-Id";
        public const string TraceParentHttpHeaderName = "traceparent";

        private static bool s_initialized = false;
        private static object s_classLock = new object();
        private static Action<Activity> s_setCurrent;

        internal static bool TryGetRootActivityFromOperationContext(out Activity activity)
        {
            var opCtx = OperationContext.Current;
            if (opCtx == null)
            {
                activity = null;
                return false;
            }
            else
            {
                return OperationContext.Current.OutgoingMessageProperties.TryGetRootActivity(out activity) ||
                       OperationContext.Current.IncomingMessageProperties.TryGetRootActivity(out activity);
            }
        }

        internal static void SetCurrentActivity(Activity activity)
        {
            EnsureInitialized();
            s_setCurrent(activity);
        }

        internal static void EnsureInitialized()
        {
            if(!s_initialized)
            {
                lock (s_classLock)
                {
                    if (!s_initialized)
                    {
                        try
                        {
                            var setCurrentMethodInfo = typeof(Activity).GetMethod("set_Current", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
                            s_setCurrent = (Action<Activity>)setCurrentMethodInfo.CreateDelegate(typeof(Action<Activity>));
                        }
                        catch
                        {
                            s_setCurrent = (activity) => { };
                        }

                         DiagnosticListener = new DiagnosticListener(ServiceModelListenerName);
                    }

                    s_initialized = true;
                }
            }
        }


        internal static IOperationHolder<T> StartActivity<T>(string action, string WcfMsg, object value) where T : OperationTelemetry, new()
        {
            IOperationHolder<T>  opHolder = null;

            if (WcfTrackingTelemetryModule.IsEnabled)
            {
                opHolder = AppInsightLogger.StartOperation<T>(action);
            }

            if (DiagnosticsHelper.NumListeners > 2) //there are 2 listeners set up in Microsoft.ServiceModel.TelemetryCorrelation, but if the client sets up, like we do in tests, then there will be more
            {
                //DiagnosticListener.StartActivity(activity, new { Action = action });
                DiagnosticListener.Write(WcfMsg, value);

            }

            return opHolder;

        }

        internal static IOperationHolder<T> StartActivity<T>(Activity activity, string WcfMsg, object value) where T : OperationTelemetry, new()
        {
            IOperationHolder<T> opHolder = null;

            if (WcfTrackingTelemetryModule.IsEnabled)
            {
                opHolder = AppInsightLogger.StartOperation<T>(activity);
            }

            if (DiagnosticsHelper.NumListeners > 2) //there are 2 listeners set up in Microsoft.ServiceModel.TelemetryCorrelation, but if the client sets up, like we do in tests, then there will be more
            {
                //DiagnosticListener.StartActivity(activity, new { Action = action });
                DiagnosticListener.Write(WcfMsg, value);
            }

            return opHolder;

        }

        internal static void StopActivity<T>(IOperationHolder<T> opHolder, string WcfMsg, object value) where T : OperationTelemetry, new()
        {

            if (WcfTrackingTelemetryModule.IsEnabled)
            {
                AppInsightLogger.StopOperation<T>(opHolder);
            }


            if (DiagnosticsHelper.NumListeners > 2) //there are 2 listeners set up in Microsoft.ServiceModel.TelemetryCorrelation, but if the client sets up, like we do in tests, then there will be more
            {
                //DiagnosticListener.StartActivity(activity, new { Action = action });
                DiagnosticListener.Write(WcfMsg, value);
            }
        }

        internal static void StartOperation(string typeName, long timestamp)
        {
            Debug.WriteLine($"ActivityHelper.StartOperation enter, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");

            EnsureInitialized();

            var activity = new Activity(InvokeOperationActivityName);

            if (DiagnosticListener.IsEnabled(InvokeOperationActivityName))
            {
                DiagnosticListener.Write(InvokeOperationActivityStartName, new { InvokerType = typeName, Timestamp = timestamp });

            }
            //if (DiagnosticListener.IsEnabled(InvokeOperationActivityName))
            //{
            //    //DiagnosticListener.StartActivity(activity, new { InvokerType = typeName, Timestamp = timestamp });
            //    // move this to after AppInsightLogger.StartOperation
            //    // OperationContext.Current.OutgoingMessageProperties.Add(OperationActivityPropertyName, activity);

            //    //new
            //    activity.DisplayName = typeName; .
            //    var opHolder = AppInsightLogger.StartOperation<RequestTelemetry>(activity);
            //    var activityAi = Activity.Current;
            //    OperationContext.Current.OutgoingMessageProperties.Add(OperationActivityPropertyName, activityAi);
            //    OperationContext.Current.OutgoingMessageProperties.Add("IOperationHolder", opHolder);
            //    Debug.WriteLine($"ActivityHelper.StartOperation opHolder.Telemetry.Context.Operation.Id: {opHolder.Telemetry.Context.Operation.Id}, " +
            //        $"opHolder.Telemetry.Context.Operation.ParentId {opHolder.Telemetry.Context.Operation.ParentId}");
            //    //end new

            //    Debug.WriteLine($"ActivityHelper.StartOperation exit, Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");

            //    //TODO: Is something like this necessary like in other methods in this class? //if (DiagnosticListener.IsEnabled(ReceiveMessageActivityStartName))
            //}
            //else
            //{
                activity.Start();
                OperationContext.Current.OutgoingMessageProperties.Add(OperationActivityPropertyName, activity);
            //}

        }

        internal static void StopOperation(long timestamp)
        {
            Debug.WriteLine($"ActivityHelper.StopOperation enter, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");

            EnsureInitialized();
            // The operation activity might not be in the outgoing message properties if the OperationInvoker events were enabled after
            // an operation was started but before it completed.
            if (OperationContext.Current.OutgoingMessageProperties.TryGetValue(OperationActivityPropertyName, out Activity activity))
            {
                SetCurrentActivity(activity);

                OperationContext.Current.OutgoingMessageProperties.Remove(OperationActivityPropertyName);

                if (DiagnosticListener.IsEnabled(InvokeOperationActivityName))
                {
                    DiagnosticListener.Write(InvokeOperationActivityStopName, new { Timestamp = timestamp });
                    //DiagnosticListener.StopActivity(activity, new { Timestamp = timestamp });

                }

                //if (DiagnosticListener.IsEnabled(InvokeOperationActivityName))
                //{
                //    if (OperationContext.Current.OutgoingMessageProperties.TryGetValue("IOperationHolder", out IOperationHolder<RequestTelemetry> opHolder))
                //    {

                //        AppInsightLogger.StopOperation<RequestTelemetry>(opHolder);

                //        Debug.WriteLine($"ActivityHelper.StopOperation after AppInsightLogger.StopOperation, Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");
                //        Debug.WriteLine($"ActivityHelper.StartOperation opHolder.Telemetry.Context.Operation.Id: {opHolder.Telemetry.Context.Operation.Id}, " +
                //            $"opHolder.Telemetry.Context.Operation.ParentId {opHolder.Telemetry.Context.Operation.ParentId}");

                //        OperationContext.Current.OutgoingMessageProperties.Remove("IOperationHolder");
                //    }
                //}
                //else
                //{
                activity.Stop();
                //}
            }

            Debug.WriteLine($"ActivityHelper.StopOperation exit, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");

        }

        internal static void Auth(string eventName, string typeName, bool success, long duration)
        {
            EnsureInitialized();
            DiagnosticListener.Write(eventName, new { TypeName = typeName, Success = success, Duration = new TimeSpan(duration).TotalMilliseconds });
        }

        internal static void SelectOperation(string eventName, string typeName, string selectedOperation, long duration)
        {
            EnsureInitialized();
            DiagnosticListener.Write(eventName, new { TypeName = typeName, SelectedOperation = selectedOperation, Duration = new TimeSpan(duration).TotalMilliseconds });
        }

        internal static void InstanceProvider(string eventName, string typeName, int instanceHash, long duration)
        {
            EnsureInitialized();
            DiagnosticListener.Write(eventName, new { TypeName = typeName, InstanceHash = instanceHash, Duration = new TimeSpan(duration).TotalMilliseconds });
        }

        internal static void Throttled(string eventName, long duration)
        {
            EnsureInitialized();
            DiagnosticListener.Write(eventName, new { ThrottleDuration = new TimeSpan(duration).TotalMilliseconds });
        }

        internal static void WriteTimedEvent(string eventName, string typeName, long duration)
        {
            EnsureInitialized();
            DiagnosticListener.Write(eventName, new { TypeName = typeName, Duration = new TimeSpan(duration).TotalMilliseconds });
        }

        public const string CorrelationContextHttpHeaderName = "Correlation-Context";
  
        public static Message StartSendMessage(Message message, Hashtable pendingActivities)
        {
            Debug.WriteLine($"ActivityHelper.StartSendMessage Enter, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId},");

            EnsureInitialized();
            if (DiagnosticListener.IsEnabled() && DiagnosticListener.IsEnabled(SendMessageActivityName))
            {
                IOperationHolder<DependencyTelemetry> opHolder = null;
                var activity = new Activity(SendMessageActivityName);
                if (DiagnosticListener.IsEnabled(SendMessageActivityStartName))
                {
                    string action = message.Headers.Action ?? string.Empty;

                    opHolder = StartActivity<DependencyTelemetry>(action, SendMessageActivityStartName, new { Action = action });

                    Debug.WriteLine($"ActivityHelper.StartSendMessage, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId},, opHolder.Telemetry.Context.Operation.Id: {opHolder.Telemetry.Context.Operation.Id}, " +
                        $"opHolder.Telemetry.Context.Operation.ParentId {opHolder.Telemetry.Context.Operation.ParentId}");
                }
                else
                {
                    activity.Start();
                }

                Debug.WriteLine($"ActivityHelper.StartSendMessage after activity, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, activity.Id: {activity.Id}, activity.ParentId: {activity.ParentId}");

                var activityAi = Activity.Current;
                if (activity.Id != activityAi.Id)
                    Debug.WriteLine($"ActivityHelper.StartSendMessage activity.Id != activityAi.Id");

                Debug.WriteLine($"ActivityHelper.StartSendMessage after Activity.Current, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, Activity.Current.Id: {activityAi.Id}, Activity.Current.ParentId: {activityAi.ParentId}");

                var activityHeader = new ActivityMessageHeader(activityAi);
                message.Headers.Add(MessageHeader.CreateHeader(
                    ActivityMessageHeader.ActivityHeaderName,
                    ActivityMessageHeader.ActivityHeaderNamespace,
                    activityHeader,
                    ActivityMessageHeader.Serializer,
                    mustUnderstand: false));
                message.Properties.Add(SendRequestActivityPropertyName, new Tuple<Activity, IOperationHolder<DependencyTelemetry>>(activityAi, opHolder));

                if (message.Headers.MessageId != null && message.Headers.MessageId.TryGetGuid(out Guid guid))
                {
                    lock (pendingActivities)
                    {
                        pendingActivities.Add(guid, new Tuple<Activity, IOperationHolder<DependencyTelemetry>>(activityAi, opHolder)); //TODO: What are pendingActivities used for?
                    }
                }
            }

            Debug.WriteLine($"ActivityHelper.StartSendMessage Exit, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId},");

            return message;
        }

        public static Message StartSendMessage(Message message)
        {
            Debug.WriteLine($"ActivityHelper.StartSendMessage Enter, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId},");

            EnsureInitialized();
            if (DiagnosticListener.IsEnabled() && DiagnosticListener.IsEnabled(SendMessageActivityName))
            {
                var activity = new Activity(SendMessageActivityName);
                IOperationHolder<DependencyTelemetry> opHolder = null;

                if (DiagnosticListener.IsEnabled(SendMessageActivityStartName))
                {
                    string action = message.Headers.Action ?? string.Empty;

                    opHolder = StartActivity<DependencyTelemetry>(action, SendMessageActivityStartName, new { Action = action });

                    Debug.WriteLine($"ActivityHelper.StartSendMessage, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, opHolder.Telemetry.Context.Operation.Id: {opHolder.Telemetry.Context.Operation.Id}, " +
                        $"opHolder.Telemetry.Context.Operation.ParentId {opHolder.Telemetry.Context.Operation.ParentId}");

                }
                else
                {
                    activity.Start();
                }


                Debug.WriteLine($"ActivityHelper.StartSendMessage after activity, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, activity.Id: {activity.Id}, activity.ParentId: {activity.ParentId}");

                var activityAi = Activity.Current;
                if (activity.Id != activityAi.Id)
                    Debug.WriteLine($"ActivityHelper.StartSendMessage activity.Id != activityAi.Id");

                Debug.WriteLine($"ActivityHelper.StartSendMessage, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, Activity.Current.Id: {activityAi.Id}, Activity.Current.ParentId: {activityAi.ParentId}");

                var activityHeader = new ActivityMessageHeader(activityAi);
                message.Headers.Add(MessageHeader.CreateHeader(
                    ActivityMessageHeader.ActivityHeaderName,
                    ActivityMessageHeader.ActivityHeaderNamespace,
                    activityHeader,
                    ActivityMessageHeader.Serializer,
                    mustUnderstand: false));
                message.Properties.Add(SendRequestActivityPropertyName, new Tuple<Activity, IOperationHolder<DependencyTelemetry>>(activityAi, opHolder));
            }

            Debug.WriteLine($"ActivityHelper.StartSendMessage, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, Exit");

            return message;
        }

        internal static void StopSendMessage(Message requestMessage, Message replyMessage)
        {
            Debug.WriteLine($"ActivityHelper.StopSendMessage Enter, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId},");

            EnsureInitialized();

            // This should restore the correct activity that existed before we started
            Tuple<Activity, IOperationHolder<DependencyTelemetry>> activity;
            if (requestMessage == null)
            {
                Debugger.Break();
                return;
            }

            if (requestMessage.Properties.TryGetValue(SendRequestActivityPropertyName, out activity))
            {
                if (DiagnosticListener.IsEnabled(SendMessageActivityStopName))
                {
                    string replyAction = replyMessage?.Headers.Action ?? string.Empty;

                    IOperationHolder<DependencyTelemetry> opHolder = activity.Item2 as IOperationHolder<DependencyTelemetry>;

                    StopActivity<DependencyTelemetry>(opHolder, SendMessageActivityStopName, new { Action = replyAction });

                    Debug.WriteLine($"ActivityHelper.StopSendMessage, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, opHolder.Telemetry.Context.Operation.Id: {opHolder.Telemetry.Context.Operation.Id}, " +
                       $"opHolder.Telemetry.Context.Operation.ParentId {opHolder.Telemetry.Context.Operation.ParentId}");

                }
                else
                {
                    activity.Item1.Stop();
                }
            }

            Debug.WriteLine($"ActivityHelper.StopSendMessage Exit, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId},");

        }

        internal static void StopSendMessage(Message replyMessage, Hashtable pendingActivities)
        {
            Debug.WriteLine($"ActivityHelper.StopSendMessage Enter, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId},");
            
            EnsureInitialized();

            // This should restore the correct activity that existed before we started
            Tuple<Activity, IOperationHolder<DependencyTelemetry>> activity; activity = null;

            if (replyMessage.Headers.RelatesTo.TryGetGuid(out Guid guid) && pendingActivities.ContainsKey(guid))
            {
                activity = (Tuple <Activity, IOperationHolder <DependencyTelemetry>>) pendingActivities[guid];
                lock (pendingActivities)
                {
                    pendingActivities.Remove(guid);
                }
            }

            if (activity == null)
            {
                Debugger.Break();
                return;
            }

            ActivityHelper.SetCurrentActivity(activity.Item1);

            if (DiagnosticListener.IsEnabled(SendMessageActivityStopName))
            {
                string replyAction = replyMessage?.Headers.Action ?? string.Empty;
                IOperationHolder<DependencyTelemetry> opHolder = activity.Item2 as IOperationHolder<DependencyTelemetry>;

                StopActivity<DependencyTelemetry>(opHolder, SendMessageActivityStopName, new { Action = replyAction });

            }
            else
            {
                activity.Item1.Stop();
            }

            Debug.WriteLine($"ActivityHelper.StopSendMessage Exit, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId},");
        }

        public static Message StartReceiveMessage(Message message)
        {
            Debug.WriteLine($"ActivityHelper.StartReceiveMessage enter, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");

            IOperationHolder<RequestTelemetry> opHolder = null;

            EnsureInitialized();
            if (DiagnosticListener.IsEnabled() && DiagnosticListener.IsEnabled(ReceiveMessageActivityName))
            {
                Activity currentActivity = Activity.Current;
                string action = message.Headers.Action ?? string.Empty;
                var activity = new Activity(action);
                if (currentActivity == null || !currentActivity.OperationName.Equals(AspNetActivityName))
                {
                    // There wasn't a current activity from AspNet so we need to look for parent activity details in the request.
                    // If HttpTransport and correlation data is in HTTP headers, use them as the basis for parent Id and Baggage
                    // as the Http client was the last activity before sending the request, otherwise look for the correlation SOAP header.
                    if (!TryExtractHttpCorrelationHeaders(message, activity)) //Verified that has the Request-Id in Http Headers if HTTP
                    {
                        // There were no HTTP headers so look for a SOAP header
                        if (TryGetActivityMessageHeader(message, out var activityHeader))
                        {
                            activity.SetParentId(activityHeader.Id);
                            if (activityHeader.Baggage != null) 
                            {
                                foreach (var item in activityHeader.Baggage)
                                {
                                    activity.AddBaggage(item.Key, item.Value);
                                }
                            }
                        }
                    }
                }

                Debug.WriteLine($"ActivityHelper.StartReceiveMessage after TryGetHeader line 358, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");

                // The Activity PropertyName is S.SM.ReceiveMessage. Check if S.SM.ReceiveMessage.Start is enabled
                // as StartActivity writes an event {OperationName}.Start. Otherwise just start the activity.
                if (DiagnosticListener.IsEnabled(ReceiveMessageActivityStartName))
                {
                    opHolder = StartActivity<RequestTelemetry>(activity, ReceiveMessageActivityStartName, new { message.Headers.Action });

                    Debug.WriteLine($"ActivityHelper.StartReceiveMessage Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId},, opHolder.Telemetry.Context.Operation, Id: {opHolder.Telemetry.Context.Operation.Id}, " +
                        $"ParentId {opHolder.Telemetry.Context.Operation.ParentId}");
                }
                else
                {
                    activity.Start();
                }

                // Save the ReceiveMessage activity in the message property as we need to stop it later.
                message.Properties.Add(ReceiveMessageActivityPropertyName, new Tuple<Activity, IOperationHolder<RequestTelemetry>>(activity, opHolder));

            }

            // Save Activity.Current in message properties so it can be restored later as Activity.Current is stored in
            // an AsyncLocal and WCF does not flow the ExecutionContext.
            if (Activity.Current != null)
            {
                message.Properties.Add(CurrentActivityPropertyName, Activity.Current);
            }

            return message;
        }

        public static void StopReceiveMessage(Message requestMessage, Message replyMessage)
        {

            Debug.WriteLine($"ActivityHelper.StopReceiveMessage enter, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, Activity - RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");

            EnsureInitialized();

            // This should restore the correct activity that existed before we started
            Tuple<Activity, IOperationHolder<RequestTelemetry>> activity;

            if (requestMessage == null)
            {
                Debugger.Break();
                return;
            }

            if (requestMessage.Properties.TryGetValue(ReceiveMessageActivityPropertyName, out activity))
            {
                if (DiagnosticListener.IsEnabled(ReceiveMessageActivityStopName))
                {
                    string replyAction = replyMessage?.Headers.Action ?? string.Empty;
                   
                    IOperationHolder<RequestTelemetry> opHolder = activity.Item2;

                    StopActivity<RequestTelemetry>(opHolder, ReceiveMessageActivityStopName, new { Action = replyAction });

                    Debug.WriteLine($"ActivityHelper.StopReceiveMessage Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, ,opHolder.Telemetry.Context.Operation.Id: {opHolder.Telemetry.Context.Operation.Id}, " +
                        $"opHolder.Telemetry.Context.Operation.ParentId {opHolder.Telemetry.Context.Operation.ParentId}");
                }
                else
                {
                    activity.Item1.Stop();
                }
            }
        }

        private static bool TryGetActivityMessageHeader(Message message, out ActivityMessageHeader activityHeader)
        {
            int headerIndex = message.Headers.FindHeader(
                ActivityMessageHeader.ActivityHeaderName,
                ActivityMessageHeader.ActivityHeaderNamespace);

            if (headerIndex != -1)
            {
                activityHeader = message.Headers.GetHeader<ActivityMessageHeader>(
                    headerIndex, ActivityMessageHeader.Serializer);
                return true;
            }

            activityHeader = null;
            return false;
        }

        private static bool TryGetIOperationHolderMessageProperty<T>(Message message, out IOperationHolder<T> opHolder)
        {
            if (message.Properties.ContainsKey("IOperationHolder"))
            {
                _ = message.Properties.TryGetValue("IOperationHolder", out opHolder);
                return true;
            }

            opHolder = null;
            return false;

        }

        private static bool TryExtractHttpCorrelationHeaders(Message message, Activity activity)
        {
            if (message.Properties.TryGetValue(HttpRequestMessageProperty.Name, out HttpRequestMessageProperty httpRequestMessageProperty))
            {
                var headers = httpRequestMessageProperty.Headers;
                var requestIDs = headers.GetValues(TraceParentHttpHeaderName);
                if (!string.IsNullOrEmpty(requestIDs?[0]))
                {
                    // there may be several Request-Id headers, but we only read the first one
                    activity.SetParentId(requestIDs[0]);

                    // Header format - Correlation-Context: key1=value1, key2=value2
                    var baggages = headers.GetValues(CorrelationContextHttpHeaderName);
                    if (baggages != null)
                    {
                        // there may be several Correlation-Context headers
                        foreach (var item in baggages)
                        {
                            foreach (var pair in item.Split(','))
                            {
                                if (NameValueHeaderValue.TryParse(pair, out NameValueHeaderValue baggageItem))
                                {
                                    activity.AddBaggage(baggageItem.Name, baggageItem.Value);
                                }
                            }
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        public static void RestoreCurrentActivity(Activity root)
        {
            EnsureInitialized();
            Debug.Assert(root != null);

            // workaround to restore the root activity, because we don't
            // have a way to change the Activity.Current
            var childActivity = new Activity(root.OperationName);
            childActivity.SetParentId(root.Id);
            childActivity.SetStartTime(root.StartTimeUtc);
            foreach (var item in root.Baggage)
            {
                childActivity.AddBaggage(item.Key, item.Value);
            }

            childActivity.Start();
        }
    }
}