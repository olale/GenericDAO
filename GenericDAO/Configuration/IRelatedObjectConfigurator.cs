using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Data.SqlClient;
using System.Data;

namespace Configuration {
    public interface IRelatedObjectConfigurator<T,T1> : IRelatedObjectConfigurator, IConfigurator<T1>
        where T1: class, new()
    {
        /// <summary>
        /// specify the property to assign objects of type T to
        /// </summary>
        /// <param name="propertySelector"></param>
        /// <returns></returns>
        void Through(Expression<Func<T, T1>> propertySelector);

        /// <summary>
        /// Specify configuration expression
        /// </summary>
        /// <param name="config"></param>
        void By(Action<IRelatedObjectConfigurator<T, T1>> config);

    }

    public interface IRelatedObjectConfigurator
    {
        /// <summary>
        /// Internal use only
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        IEnumerable<string> GetFieldNamesUsedByThisRelatedObject(IDataReader reader);
        Action<object, IDataReader, IEnumerable<string>> Config { get; set; }
    }
}

