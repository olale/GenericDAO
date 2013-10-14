using System;
using System.Collections.Generic;
using System.Linq;
using System.Dynamic;
using Configuration;
using Extensions;

namespace Core {
    public class Dispatcher<T>:DynamicObject where T:class, new() {


        public static ReturnType ReturnTypeForDispatch {
            get;
            set;
        }


        public GenericDAO.ExceptionPolicy? Policy {
            get;
            set;
        }
        public int? Limit {
            get;
            set;
        }


        public GenericDAO.FetchRelatedObjectsPolicy? ScanForRelatedObjects {
            get;
            set;
        }

        internal Dispatcher(ReturnType returnType) {
            ReturnTypeForDispatch=returnType;
        }

        internal Dispatcher(GenericDAO.FetchRelatedObjectsPolicy scanForRelatedObjects) {
            ScanForRelatedObjects=scanForRelatedObjects;
        }

        internal Dispatcher(GenericDAO.ExceptionPolicy policy,ReturnType returnType,int limit) {
            ReturnTypeForDispatch=returnType;
            Policy=policy;
            Limit=limit;
        }

        /// <summary>
        /// The return type of a dynamic SP invocation is denoted by the generic type argument to the method
        /// </summary>
        /// <param name="binder"></param>
        /// <returns></returns>
        private static Type GetReturnType(InvokeMemberBinder binder) {
            var csharpBinder=binder.GetType().GetInterface("Microsoft.CSharp.RuntimeBinder.ICSharpInvokeOrInvokeMemberBinder");
            var typeArgs=(csharpBinder.GetProperty("TypeArguments").GetValue(binder,null) as IList<Type>);
            return typeArgs.Any()?typeArgs[0]:null;
        }

        private static object GetParameterObject(InvokeMemberBinder binder,
                                                                   object[] args) {
            bool anyNamedArguments=binder.CallInfo.ArgumentNames.Any();
            object paramObj=new ExpandoObject();
            if(anyNamedArguments) {
                // We expect that we only use named args, so we can populate the paramObj with all the parameter names and matching argument values
                if(binder.CallInfo.ArgumentNames.Count!=args.Length) {
                    throw new ArgumentException("cannot mix named and unnamed arguments: either provide an object with properties corresponding to parameters, or provide named parameters");
                }
                var paramDict=paramObj as IDictionary<string,object>;
                int i=0;
                foreach(var param in binder.CallInfo.ArgumentNames) {
                    paramDict[param]=args[i++];
                }
            } else {
                paramObj=args.Any()?args[0]:null;
            }
            return paramObj;
        }

        private void UpdateConfig(string storedProcedureName,SPConfigurator<T> oldConfig) {
            GenericDAO<T>.Configure(storedProcedureName,oldConfig.Copy(x => {
                x.StoredProcedureName=storedProcedureName;
                x.InitParamsGettersMap(storedProcedureName);
                if(Policy.HasValue) {
                    x.HasPolicy(Policy.Value);
                }
                if(ScanForRelatedObjects.HasValue) {
                    x.ScanForRelatedTypes(ScanForRelatedObjects.Value);
                }
            }));
        }

        private object GetResult(object paramObj,string storedProcedureName) {
            object result;
            var oldConfig=GenericDAO<T>.GetConfig(storedProcedureName);
            try {
                switch(ReturnTypeForDispatch) {
                case ReturnType.Static:
                if(Policy.HasValue||ScanForRelatedObjects.HasValue) {
                    UpdateConfig(storedProcedureName,
                                                          oldConfig);
                }
                result=Limit.HasValue?GenericDAO<T>.Get(storedProcedureName,
                                                   paramObj,Limit.Value):GenericDAO<T>.Get(storedProcedureName,
                                                   paramObj);
                break;
                case ReturnType.Dynamic:
                result=Limit.HasValue?GenericDAO<T>.GetObjects(storedProcedureName,
                                                   paramObj,Limit.Value):GenericDAO<T>.GetObjects(storedProcedureName,
                                                   paramObj);

                break;
                default:
                result=null;
                break;
                }
            } finally {
                GenericDAO<T>.Configure(storedProcedureName,oldConfig);
            }
            return result;
        }

        /// <summary>
        /// Use the generic type parameter of the given method to infer return type, and detect whether named parameters or a single parameter object is provided. 
        /// In case of named parameters, map them to a single ExpandoObject
        /// 
        /// We assume that Get*-methods should return IEnumerable, but other dynamic method invocations can specify their return types through a generic type argument
        /// </summary>            
        /// 
        /// <param name="binder"></param>
        /// <param name="args"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public override bool TryInvokeMember(InvokeMemberBinder binder,
                                             object[] args,
                                             out object result) {
            string procedureName=binder.Name;
            object paramObj=GetParameterObject(binder,args);
            string storedProcedureName=GenericDAO<T>.StoredProcedureFullName(procedureName);
            if(procedureName.StartsWith("Get")) {
                result=GetResult(paramObj,storedProcedureName);
            } else {
                Type returnType=GetReturnType(binder);
                result=GenericDAO<T>.ApplyInternal(storedProcedureName,
                                                     paramObj,
                                                     returnType);
            }
            // All invocations succeed here, but exceptions may be thrown in the underlying GenericDAO<> class
            return true;
        }
    }
}
