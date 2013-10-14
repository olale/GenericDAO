using System;
using System.Collections.Generic;
using System.Linq;
using Castle.DynamicProxy;
using System.Reflection;
using Extensions;
namespace Support
{
    public class LazyLoadingProxyGenerationHook: IProxyGenerationHook
    {

        /// <summary>
        /// 
        ///               Invoked by the generation process to notify that the whole process has completed.
        ///             
        /// </summary>
        public void MethodsInspected() {
        }

        /// <summary>
        /// 
        ///               Invoked by the generation process to notify that a member was not marked as virtual.
        ///             
        /// </summary>
        /// <param name="type">
        /// The type which declares the non-virtual member.
        /// </param>
        /// <param name="memberInfo">
        /// The non-virtual member.
        /// </param>
        /// <remarks>
        ///               This method gives an opportunity to inspect any non-proxyable member of a type that has 
        ///               been requested to be proxied, and if appropriate - throw an exception to notify the caller.
        ///             </remarks>
        public void NonProxyableMemberNotification(Type type,
                                                   MemberInfo memberInfo)
        {
        }

        /// <summary>
        /// 
        ///               Invoked by the generation process to determine if the specified method should be proxied.
        ///             
        /// Should only invoke methods that are getters or setters for properties that return user-created objects (or collections thereof)
        /// </summary>
        /// <param name="type">
        /// The type which declares the given method.
        /// </param>
        /// <param name="methodInfo">
        /// The method to inspect.
        /// </param>
        /// <returns>
        /// True if the given method should be proxied; false otherwise.
        /// </returns>
        public bool ShouldInterceptMethod(Type type,
                                          MethodInfo methodInfo)
        {
            return (methodInfo.Name.StartsWith("get_") || methodInfo.Name.StartsWith("set_")) && !methodInfo.ReturnType.IsSimpleValue();
        }

        public override bool Equals(object obj) {
            return obj.GetType().Equals(typeof(LazyLoadingProxyGenerationHook));
        }

        public override int GetHashCode() {
            return typeof(LazyLoadingProxyGenerationHook).GetHashCode();
        }
    }
}

