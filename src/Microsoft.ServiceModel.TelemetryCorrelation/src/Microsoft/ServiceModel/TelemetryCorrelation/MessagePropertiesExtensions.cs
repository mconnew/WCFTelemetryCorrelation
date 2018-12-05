using System.Diagnostics;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceModel.TelemetryCorrelation
{
    internal static class MessagePropertiesExtensions
    {
        public static bool TryGetValue<TProperty>(this MessageProperties properties, string name, out TProperty property)
        {
            object o;
            if (properties.TryGetValue(name, out o))
            {
                // Property is non-null but also not castable to TProperty. Semantically this method is asking
                // "If there is a property with name {name} and type {TProperty}, get that property". If a
                // property exists with a matching name but the wrong type, the Try fails.
                if (o == null || o is TProperty)
                {
                    property = (TProperty)o;
                    return true;
                }
            }

            property = default(TProperty);
            return false;
        }

        public static bool TryGetRootActivity(this MessageProperties properties, out Activity activity)
        {
            return properties.TryGetValue(ActivityHelper.CurrentActivityPropertyName, out activity);
        }

        public static void RestoreCurrentActivity(this MessageProperties properties)
        {
            if (Activity.Current == null && 
                TryGetRootActivity(properties, out Activity rootActivity) &&
                rootActivity != null)
            {
                ActivityHelper.RestoreCurrentActivity(rootActivity);
            }
        }
    }
}
