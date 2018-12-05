using System;
using System.Collections;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;

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
        public const string CurrentActivityPropertyName = "System.Diagnostics.Activity.Current";
        public const string OperationActivityPropertyName = "System.Diagnostics.Activity.OperationActivity";
        public const string ReceiveMessageActivityPropertyName = "System.ServiceModel.ReceiveMessageActivity";
        public const string SendRequestActivityPropertyName = "System.ServiceModel.SendRequestActivity";

        public static DiagnosticListener DiagnosticListener { get; private set; }

        // Constants to support working with AspNet Telemetery Correlation
        public const string AspNetActivityName = "Microsoft.AspNet.HttpReqIn";
        public const string RequestIdHttpHeaderName = "Request-Id";

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

        internal static void StartOperation(string typeName, long timestamp)
        {
            EnsureInitialized();
            var activity = new Activity(InvokeOperationActivityName);
            DiagnosticListener.StartActivity(activity, new { InvokerType = typeName, Timestamp = timestamp });
            OperationContext.Current.OutgoingMessageProperties.Add(OperationActivityPropertyName, activity);
        }

        internal static void StopOperation(long timestamp)
        {
            EnsureInitialized();
            // The operation activity might not be in the outgoing message properties if the OperationInvoker events were enabled after
            // an operation was started but before it completed.
            if (OperationContext.Current.OutgoingMessageProperties.TryGetValue(OperationActivityPropertyName, out Activity activity))
            {
                SetCurrentActivity(activity);
                DiagnosticListener.StopActivity(activity, new { Timestamp = timestamp });
                OperationContext.Current.OutgoingMessageProperties.Remove(OperationActivityPropertyName);
            }
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
            EnsureInitialized();
            if (DiagnosticListener.IsEnabled() && DiagnosticListener.IsEnabled(SendMessageActivityName))
            {
                var activity = new Activity(SendMessageActivityName);
                if (DiagnosticListener.IsEnabled(SendMessageActivityStartName))
                {
                    string action = message.Headers.Action ?? string.Empty;
                    DiagnosticListener.StartActivity(activity, new { Action = action });
                }
                else
                {
                    activity.Start();
                }

                var activityHeader = new ActivityMessageHeader(activity);
                message.Headers.Add(MessageHeader.CreateHeader(
                    ActivityMessageHeader.ActivityHeaderName,
                    ActivityMessageHeader.ActivityHeaderNamespace,
                    activityHeader,
                    ActivityMessageHeader.Serializer,
                    mustUnderstand: false));
                message.Properties.Add(SendRequestActivityPropertyName, activity);

                if (message.Headers.MessageId != null && message.Headers.MessageId.TryGetGuid(out Guid guid))
                {
                    lock (pendingActivities)
                    {
                        pendingActivities.Add(guid, activity);
                    }
                }
            }

            return message;
        }

        public static Message StartSendMessage(Message message)
        {
            EnsureInitialized();
            if (DiagnosticListener.IsEnabled() && DiagnosticListener.IsEnabled(SendMessageActivityName))
            {
                var activity = new Activity(SendMessageActivityName);
                if (DiagnosticListener.IsEnabled(SendMessageActivityStartName))
                {
                    string action = message.Headers.Action ?? string.Empty;
                    DiagnosticListener.StartActivity(activity, new { Action = action });
                }
                else
                {
                    activity.Start();
                }

                var activityHeader = new ActivityMessageHeader(activity);
                message.Headers.Add(MessageHeader.CreateHeader(
                    ActivityMessageHeader.ActivityHeaderName,
                    ActivityMessageHeader.ActivityHeaderNamespace,
                    activityHeader,
                    ActivityMessageHeader.Serializer,
                    mustUnderstand: false));
                message.Properties.Add(SendRequestActivityPropertyName, activity);
            }

            return message;
        }

        internal static void StopSendMessage(Message requestMessage, Message replyMessage)
        {
            EnsureInitialized();

            // This should restore the correct activity that existed before we started
            Activity activity;
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
                    DiagnosticListener.StopActivity(activity, new { Action = replyAction });
                }
                else
                {
                    activity.Stop();
                }
            }
        }

        internal static void StopSendMessage(Message replyMessage, Hashtable pendingActivities)
        {
            EnsureInitialized();

            // This should restore the correct activity that existed before we started
            Activity activity = null;
            if (replyMessage.Headers.RelatesTo.TryGetGuid(out Guid guid) && pendingActivities.ContainsKey(guid))
            {
                activity = (Activity)pendingActivities[guid];
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

            if (DiagnosticListener.IsEnabled(SendMessageActivityStopName))
            {
                string replyAction = replyMessage?.Headers.Action ?? string.Empty;
                DiagnosticListener.StopActivity(activity, new { Action = replyAction });
            }
            else
            {
                activity.Stop();
            }
        }

        public static Message StartReceiveMessage(Message message)
        {
            EnsureInitialized();
            if (DiagnosticListener.IsEnabled() && DiagnosticListener.IsEnabled(ReceiveMessageActivityName))
            {
                Activity currentActivity = Activity.Current;
                var activity = new Activity(ReceiveMessageActivityName);
                if (currentActivity == null || !currentActivity.OperationName.Equals(AspNetActivityName))
                {
                    // There wasn't a current activity from AspNet so we need to look for parent activity details in the request.
                    // If HttpTransport and correlation data is in HTTP headers, use them as the basis for parent Id and Baggage
                    // as the Http client was the last activity before sending the request, otherwise look for the correlation SOAP header.
                    if (!TryExtractHttpCorrelationHeaders(message, activity))
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

                // The Activity PropertyName is S.SM.ReceiveMessage. Check if S.SM.ReceiveMessage.Start is enabled
                // as StartActivity writes an event {OperationName}.Start. Otherwise just start the activity.
                if (DiagnosticListener.IsEnabled(ReceiveMessageActivityStartName))
                {
                    DiagnosticListener.StartActivity(activity, new { message.Headers.Action });
                }
                else
                {
                    activity.Start();
                }

                // Save the ReceiveMessage activity in the message property as we need to stop it later.
                message.Properties.Add(ReceiveMessageActivityPropertyName, activity);

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
            EnsureInitialized();

            // This should restore the correct activity that existed before we started
            Activity activity;
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
                    DiagnosticListener.StopActivity(activity, new { Action = replyAction });
                }
                else
                {
                    activity.Stop();
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

        private static bool TryExtractHttpCorrelationHeaders(Message message, Activity activity)
        {
            if (message.Properties.TryGetValue(HttpRequestMessageProperty.Name, out HttpRequestMessageProperty httpRequestMessageProperty))
            {
                var headers = httpRequestMessageProperty.Headers;
                var requestIDs = headers.GetValues(RequestIdHttpHeaderName);
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