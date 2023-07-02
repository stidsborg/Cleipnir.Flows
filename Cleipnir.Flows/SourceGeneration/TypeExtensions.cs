using System;

namespace Cleipnir.Flows.SourceGeneration
{
    internal static class TypeExtensions
    {
        public static bool IsSubclassOfRawGeneric(this Type subclass, Type openEndedGeneric)
        {
            Type? curr = subclass;
            while (curr != null && curr != typeof(object))
            {
                var cur = curr.IsGenericType ? curr.GetGenericTypeDefinition() : subclass;
                if (openEndedGeneric == cur)
                {
                    return true;
                }
                curr = curr.BaseType;
            }
            return false;
        }
    }
}
