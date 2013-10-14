using System;
using System.Collections.Generic;
using System.Linq;
using Support;

namespace Attributes {

    [AttributeUsage(AttributeTargets.Property,AllowMultiple=false)]
    public class MapByAttribute: Attribute {

        public string StoredProcedureName {get;set;}

        /// <summary>
        /// A class that is a subclass of the generic class GenericDAO.Support.ClassMapping that has been instantiated 
        /// with the containing type and the property type to provide a concrete implementation of the mapping
        /// </summary>
        public Type Mapper {get;set;}

        /// <summary>
        /// Initializes a new instance of the MapByAttribute class.
        /// 
        /// <param name="storedProcedureName">the stored procedure used to retrieve objects of the related type</param>
        /// <param name="mapper">the class used to map between objects of the class containing this attribute, and related objects accessed through the property</param>
        /// </summary>
        public MapByAttribute(string storedProcedureName,Type mapper) {
            StoredProcedureName=storedProcedureName;
            if (!typeof(ClassMapping).IsAssignableFrom(mapper)) {
                throw new ArgumentException(string.Format(@"{0} is not a ClassMapping", mapper));
            } else {
                Mapper=mapper;
            }
        }

    }
}
