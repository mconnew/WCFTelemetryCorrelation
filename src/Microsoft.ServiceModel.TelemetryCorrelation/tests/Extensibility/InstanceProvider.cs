// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.ServiceModel.TelemetryCorrelation.Tests.Extensibility
{
    class InstanceProvider : IInstanceProvider
    {
        Func<object> _creator = null;

        public InstanceProvider(DispatchRuntime dispatchRuntime)
        {
            ConstructorInfo constructor = null;
            if (dispatchRuntime.Type != null)
            {
                constructor = dispatchRuntime.Type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
            }

            if (dispatchRuntime.SingletonInstanceContext == null)
            {
                if (dispatchRuntime.Type != null && (dispatchRuntime.Type.IsAbstract || dispatchRuntime.Type.IsInterface))
                {
                    throw new InvalidOperationException("Service Type Not Creatable");
                }

                if (constructor == null)
                {
                    throw new InvalidOperationException("No Default Constructor");
                }
            }

            if (constructor != null)
            {
                if (dispatchRuntime.SingletonInstanceContext == null)
                {
                    _creator = GenerateCreateInstanceDelegate(constructor);
                }
            }
        }

        private Func<object> GenerateCreateInstanceDelegate(ConstructorInfo constructor)
        {
            return () => { return constructor.Invoke(null); };
        }

        public object GetInstance(InstanceContext instanceContext)
        {
            return _creator();
        }

        public object GetInstance(InstanceContext instanceContext, Message message)
        {
            return _creator();
        }

        public void ReleaseInstance(InstanceContext instanceContext, object instance)
        {
            var dispose = instance as IDisposable;
            if (dispose != null)
                dispose.Dispose();
        }
    }
}
