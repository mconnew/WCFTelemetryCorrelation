using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.ServiceModel.TelemetryCorrelation.Tests
{
    internal class DiagnosticsHelper
    {
        internal static Subscription SubscribeToListener()
        {
            var subscription = new Subscription();
            var allListenerSubscription = DiagnosticListener.AllListeners.Subscribe((listener) =>
            {
                if (listener.Name == "System.ServiceModel.TelemetryCorrelation")
                {
                    subscription.RegisterDisposable(listener.Subscribe((KeyValuePair<string, object> evnt) =>
                    {
                        subscription.AddEvent(evnt);
                    }));
                }
            });

            subscription.RegisterDisposable(allListenerSubscription);
            return subscription;
        }
    }

    internal class Subscription : IDisposable
    {
        private bool _disposed;
        private List<IDisposable> _registeredDisposables = new List<IDisposable>();
        private List<KeyValuePair<string, object>> _events = new List<KeyValuePair<string, object>>();

        public IList<KeyValuePair<string, object>> Events
        {
            get
            {
                // Give time for all the events to be received
                System.Threading.Thread.Sleep(100);

                lock (_events)
                {
                    return new List<KeyValuePair<string, object>>(_events);
                }
            }
        }

        internal void AddEvent(KeyValuePair<string, object> evnt)
        {
            lock (_events)
            {
                _events.Add(evnt);
            }
        }

        internal void RegisterDisposable(IDisposable disposable)
        {
            _registeredDisposables.Add(disposable);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (IDisposable disposable in _registeredDisposables)
                {
                    disposable.Dispose();
                }

                _registeredDisposables.Clear();
                _disposed = true;
            }
        }
    }
}