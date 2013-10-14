using System;
using System.Collections.Generic;
using System.Linq;
using Castle.DynamicProxy;
using System.Reflection;
using System.Runtime.Serialization;
using System.Dynamic;
using Attributes;
using Fasterflect;
using Extensions;
using Configuration;
using Core;

namespace Support {
    public class LazyLoadingInterceptor<T>:BaseInterceptor,IInterceptor where T:class, new() {


        private HashSet<string> PropertiesLoaded {
            get;
            set;
        }

        /// <summary>
        /// Initializes a new instance of the LazyLoadingInterceptor class.
        /// </summary>
        /// <param name="new"></param>
        public LazyLoadingInterceptor() {
            PropertiesLoaded=new HashSet<string>();
        }

        private bool IsPropertyLoaded(string propName) {
            return PropertiesLoaded.Contains(propName);
        }

        private void SetPropertyLoaded(string propName,bool loaded=true) {
            if(loaded) {
                PropertiesLoaded.Add(propName);
            } else {
                PropertiesLoaded.Remove(propName);
            }
        }


        private static string GetPropertyName(string getterName) {
            return getterName.Remove(0,
                                     4);
        }

        public void Intercept(IInvocation invocation) {
            object proxy=invocation.Proxy;
            // Get the target property access method
            var target=invocation.MethodInvocationTarget;
            string propertyName=GetPropertyName(target.Name);
            if(target.Name.StartsWith("get_")) {
                if(!IsPropertyLoaded(propertyName)) {
                    // Ignore the returned enumeration of elements from Prefetcher<T> as it is just the original sequence with properties set
                    Prefetcher<T>.FetchRelatedProperty(new List<T>() { proxy as T },typeof(T).GetProperty(propertyName));
                    SetPropertyLoaded(propertyName);
                }
            } else {
                // Setter invocation: update "property loaded" map with indication of whether non-default value set
                var setterValue=invocation.GetArgumentValue(0);
                SetPropertyLoaded(propertyName,setterValue!=setterValue.GetType().DefaultValue());
            }
            invocation.Proceed();
        }
    }
}
