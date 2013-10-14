using System;
using System.Collections.Generic;
using System.Linq;
using Core;

namespace Configuration {
    public interface ISPConfigurator<T>
            :IConfigurator<T>
            where T:class, new() {
        IRelatedObjectConfigurator<T,T1> Include<T1>() where T1:class, new();
        void ScanForRelatedTypes(Core.GenericDAO.FetchRelatedObjectsPolicy t=Core.GenericDAO.FetchRelatedObjectsPolicy.ScanFields);
        /// <summary>
        /// Wrap the configuration of the current SP in an Action 
        /// </summary>
        /// <param name="conf"></param>
        void By(Action<ISPConfigurator<T>> conf);

        /// <summary>
        /// Set the Exception policy for this SP
        /// </summary>
        /// <param name="policy"></param>
        /// <returns></returns>
        void HasPolicy(Core.GenericDAO.ExceptionPolicy policy);

    }
}

