using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Pipeline.Lib.Util
{
    internal static class MemberInfoExtensions
    {
        public static bool IsSameAs(this MemberInfo? propertyInfo, MemberInfo? otherPropertyInfo)
            => propertyInfo == null
                ? otherPropertyInfo == null
                : (otherPropertyInfo != null
                    && (Equals(propertyInfo, otherPropertyInfo)
                        || (propertyInfo.Name == otherPropertyInfo.Name
                            && propertyInfo.DeclaringType != null
                            && otherPropertyInfo.DeclaringType != null
                            && (propertyInfo.DeclaringType == otherPropertyInfo.DeclaringType
                                || propertyInfo.DeclaringType.GetTypeInfo().IsSubclassOf(otherPropertyInfo.DeclaringType)
                                || otherPropertyInfo.DeclaringType.GetTypeInfo().IsSubclassOf(propertyInfo.DeclaringType)
                                || propertyInfo.DeclaringType.GetTypeInfo().ImplementedInterfaces.Contains(otherPropertyInfo.DeclaringType)
                                || otherPropertyInfo.DeclaringType.GetTypeInfo().ImplementedInterfaces
                                    .Contains(propertyInfo.DeclaringType)))));
    }
}
