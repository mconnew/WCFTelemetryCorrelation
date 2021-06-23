using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.ApplicationInsights.DependencyCollector;

namespace Microsoft.ServiceModel.TelemetryCorrelation
{
    class AppInsightLogger 
    {
        internal static IOperationHolder<T> StartOperation<T>(string name) where T : OperationTelemetry, new()
        {
            IOperationHolder<T> operationHolder;
            Debug.WriteLine($"AppInsightLogger.StartOperation before, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, Activity - Name: {name}, Activity.Current?.OperationName: {Activity.Current?.OperationName},  RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");

            //TODO: Temporary. Explore 
            var t = new T();
            var type = t.GetType();
            if (type.Name == "DependencyTelemetry")
            {

                var dependency = t as DependencyTelemetry;
                dependency.Type = "WCF Service Call";
                dependency.Name = "net.pipe or net.tcp, or http TBD";
                dependency.Target = name;
                //dependency.Data = name;
                operationHolder = GetTelemetryClient().StartOperation<T>((T)(OperationTelemetry)dependency);
            }
            else
            {
                operationHolder = GetTelemetryClient().StartOperation<T>(name);
            }

            Debug.WriteLine($"AppInsightLogger.StartOperation after, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, Activity - Name: {name}, Activity.Current?.OperationName: {Activity.Current?.OperationName}, RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");
            return operationHolder;
        }

        internal static IOperationHolder<T> StartOperation<T>(Activity activity) where T : OperationTelemetry, new()
        {
            Debug.WriteLine($"AppInsightLogger.StartOperation before, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId},  Activity - activity.OperationName: {activity.OperationName}, Activity.Current?.OperationName: {Activity.Current?.OperationName}, RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");
            var s =  GetTelemetryClient().StartOperation<T>(activity);
            Debug.WriteLine($"AppInsightLogger.StartOperation after, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId},  Activity - activity.OperationName: {activity.OperationName}, Activity.Current?.OperationName: {Activity.Current?.OperationName}, RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");
            return s;
        }

        internal static void StopOperation<TelemetryType>(IOperationHolder<TelemetryType> opHolder) where TelemetryType : OperationTelemetry, new()
        {
            Debug.WriteLine($"AppInsightLogger.StopOperation before, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId},  Activity - Activity.Current?.OperationName: {Activity.Current?.OperationName}, RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId} ");
            Debug.WriteLine($"AppInsightLogger.StopOperation before opHolder, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, opHolder.Telemetry.Id: {opHolder.Telemetry.Id}, opHolder.Telemetry.Context.Operation.Name: {opHolder.Telemetry.Context.Operation.Name}");

            TestTelemetryIdAndActivty<TelemetryType>(opHolder); 

            GetTelemetryClient().StopOperation<TelemetryType>(opHolder);

            Debug.WriteLine($"AppInsightLogger.StopOperation after, - Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId},  Activity.Current?.OperationName: {Activity.Current?.OperationName}, RootId: {Activity.Current?.RootId}, SpanId: {Activity.Current?.SpanId}, ParentSpanId: {Activity.Current?.ParentSpanId}, opHolder {opHolder.ToString()} ");
        }

        internal static void CustomLog(string message, string OpId)
        {
            TelemetryClient client = GetTelemetryClient();
            if (string.IsNullOrEmpty(OpId))
            {
            }
            else
            {
                client.Context.Operation.Id = OpId;
            }
            IOperationHolder<DependencyTelemetry> holder = client.StartOperation<DependencyTelemetry>("Custom operation from DesktopClient");
            holder.Telemetry.Type = "Custom";
            client.StopOperation<DependencyTelemetry>(holder);
        }
        internal static void TrackTrace(string message)
        {
            GetTelemetryClient().TrackTrace(message);
        }
        internal static void TrackEvent(string message)
        {
            GetTelemetryClient().TrackEvent(message);
        }
        internal static TelemetryClient GetTelemetryClient()
        {
            return new TelemetryClient(TelemetryConfiguration.Active);
        }

        internal static void Flush()
        {
            GetTelemetryClient().Flush();
        }

        private static bool isInitialized;
        private static bool isAvailable;

        public static bool TryRun(Action action)
        {
            Debug.Assert(action != null, "Action must not be null");
            if (!isInitialized)
            {
                isAvailable = Initialize();
            }

            if (isAvailable)
            {
                action.Invoke();
            }

            return isAvailable;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool Initialize()
        {
            try
            {
                Assembly.Load(new AssemblyName("System.Diagnostics.DiagnosticSource, Version=5.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51"));
                return true;
            }
            catch (System.IO.FileNotFoundException)
            {
                // This is a workaround that allows ApplicationInsights Core SDK run without DiagnosticSource.dll
                // so the ApplicationInsights.dll could be used alone to track telemetry, and will fall back to CallContext/AsyncLocal instead of Activity
                return false;
            }
            catch (System.IO.FileLoadException)
            {
                // Dll version, public token or culture is different
                return false;
            }
            finally
            {
                isInitialized = true;
            }
        }

        //This is same test that is done in Application Insights SDK. Detects whether there is issue with correlatign Stop wiht IOperationHolder
        private static void TestTelemetryIdAndActivty<T>(IOperationHolder<T> opHolder) where T : OperationTelemetry, new()
        {
            var operationTelemetry = opHolder.Telemetry;
            var currentActivity = Activity.Current;
            Debug.WriteLine($"TestTelemetryIdAndActivty enter, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, operationTelemetry.Id :{operationTelemetry.Id}, currentActivity.Id: {currentActivity.Id}, currentActivity.SpanId.ToHexString(): {currentActivity.SpanId.ToHexString()}");

            bool isActivityAvailable = false;
            isActivityAvailable = TryRun(() =>
            {
                //var currentActivity = Activity.Current;
                if (currentActivity == null
                || (Activity.DefaultIdFormat != ActivityIdFormat.W3C && operationTelemetry.Id != currentActivity.Id)
                || (Activity.DefaultIdFormat == ActivityIdFormat.W3C && operationTelemetry.Id != currentActivity.SpanId.ToHexString()))
                {
                    // W3COperationCorrelationTelemetryInitializer changes Id
                    // but keeps an original one in 'ai_legacyRequestId' property

                    if (!operationTelemetry.Properties.TryGetValue("ai_legacyRequestId", out var legacyId) ||
                        legacyId != currentActivity?.Id)
                    {
                        Debug.WriteLine($"TestTelemetryIdAndActivty failed and exit, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, operationTelemetry.Id :{operationTelemetry.Id}, currentActivity.Id: {currentActivity.Id}, currentActivity.SpanId.ToHexString(): {currentActivity.SpanId.ToHexString()}");

                        Debugger.Break();

                        // this is for internal error reporting
                        //CoreEventSource.Log.InvalidOperationToStopError();

                        //// this are details with unique ids for debugging
                        //CoreEventSource.Log.InvalidOperationToStopDetails(
                        //    string.Format(
                        //        CultureInfo.InvariantCulture,
                        //        "Telemetry Id '{0}' does not match current Activity '{1}'",
                        //        operationTelemetry.Id,
                        //        currentActivity?.Id));

                        return;
                    }
                }


                //this.telemetryClient.Track(operationTelemetry);

                //currentActivity?.Stop();

                //if (this.originalActivity != null &&
                //    Activity.Current != this.originalActivity &&
                //    this.originalActivity is Activity activity)
                //{
                //    Activity.Current = activity;
                //}
            });

            if (!isActivityAvailable)
            {
                //var currentActivity = Activity.Current;

                Debug.WriteLine($"TestTelemetryIdAndActivty failed and exit, Thread: {Thread.CurrentThread.ManagedThreadId}, Task: {Task.CurrentId}, operationTelemetry.Id :{operationTelemetry.Id}, currentActivity.Id: {currentActivity.Id}, currentActivity.SpanId.ToHexString(): {currentActivity.SpanId.ToHexString()}");

                Debugger.Break();
                //var currentOperationContext = CallContextHelpers.GetCurrentOperationContext();
                //if (currentOperationContext == null || operationTelemetry.Id != currentOperationContext.ParentOperationId)
                //{
                //    // this is for internal error reporting
                //    CoreEventSource.Log.InvalidOperationToStopError();

                //    // this are details with unique ids for debugging
                //    CoreEventSource.Log.InvalidOperationToStopDetails(
                //        string.Format(
                //            CultureInfo.InvariantCulture,
                //            "Telemetry Id '{0}' does not match current Activity '{1}'",
                //            operationTelemetry.Id,
                //            currentOperationContext?.ParentOperationId));

                return ;
                //}

                //this.telemetryClient.Track(operationTelemetry);

                //CallContextHelpers.RestoreOperationContext(this.ParentContext);
            }

        }
    }
}
