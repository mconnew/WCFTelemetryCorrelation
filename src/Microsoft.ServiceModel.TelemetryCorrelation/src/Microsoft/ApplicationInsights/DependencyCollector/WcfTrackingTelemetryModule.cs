using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.DependencyCollector
{
    //Based on modules from Application Insights, such as DependencyTrackingTelemetryModule. However, currently, do not do much in this module since do not rely on module to configure DiagnosticSourceBridgeEventListener etc.
    public class WcfTrackingTelemetryModule : ITelemetryModule, IDisposable
    {
        private readonly object lockObject = new object();

        private Subscription subcription;

        private bool disposed = false;

        /// <summary>Gets a value indicating whether this module has been initialized.</summary>
        internal bool IsInitialized { get; private set; } = false;

        internal static DiagnosticListener DiagnosticListener { get; private set; }

        public static bool IsEnabled { get; set; }

        /// <summary>
        /// IDisposable implementation.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Initialize method is called after all configuration properties have been loaded from the configuration.
        /// </summary>
        public void Initialize(TelemetryConfiguration configuration)
        {
            //            // Temporary fix to make sure that we initialize module once.
            //            // It should be removed when configuration reading logic is moved to Web SDK.

            if (!this.IsInitialized)
            {
                lock (this.lockObject)
                {
                    if (!this.IsInitialized)
                    {
                        try
                        {
                            this.subcription = DiagnosticsHelper.SubscribeToListener(); //TODO: Added this so that can remove DiagnosticListener in Tests code, and when Microsofot.ServiceModel.TelemetryCorrelation is used elsewhere.

                        }
                        catch (Exception exc)
                        {
                            string clrVersion;
#if NETSTANDARD
                                                        clrVersion = System.Reflection.Assembly.GetEntryAssembly().GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName;
#else
                            clrVersion = Environment.Version.ToString();
#endif
                        }

                        //PrepareFirstActivity(); //TODO: Comment out for now since I have Root Activity in tests

                        this.IsInitialized = true;
                        WcfTrackingTelemetryModule.IsEnabled = true;
                    }
                }
            }
        }

        /// <summary>
        /// IDisposable implementation.
        /// </summary>
        /// <param name="disposing">The method has been called directly or indirectly by a user's code.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    if (this.subcription != null)
                    {
                        this.subcription.Dispose();
                    }
                }

                this.disposed = true;
            }
        }

        /// <summary>
        /// When the first Activity is created in the process (on .NET Framework), it synchronizes DateTime.UtcNow 
        /// in order to make it's StartTime and duration precise, it may take up to 16ms. 
        /// Let's create the first Activity ever here, so we will not miss those 16ms on the first dependency tracking.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private static void PrepareFirstActivity()
        {
            using (var activity = new Activity("Microsoft.ApplicationInights.Init"))
            {
                activity.Start();
            }
        }
    }
}