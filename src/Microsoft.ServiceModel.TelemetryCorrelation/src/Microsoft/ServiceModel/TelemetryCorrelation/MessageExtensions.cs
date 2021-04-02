// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceModel.TelemetryCorrelation
{
    internal static class MessageExtensions
    {
        public static void RestoreCurrentActivity(this Message message)
        {
            message.Properties.RestoreCurrentActivity();
        }

        public static bool TryGetRootActivity(this Message message, out Activity activity)
        {
            return message.Properties.TryGetRootActivity(out activity);
        }
    }
}
