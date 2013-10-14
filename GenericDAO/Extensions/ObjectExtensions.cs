using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.ComponentModel;
using Omu.ValueInjecter;
using Core;
using Domain;

namespace Extensions {
    public static class ObjectExtensions {

        /// <summary>
        /// Return all properties that are of either built-in types or Enums
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="inherited"></param>
        /// <returns></returns>
        public static IEnumerable<PropertyInfo> GetPrimitiveTypeProperties(this Object obj,bool inherited=false) {

            return GetPrimitiveTypeProperties(obj.GetType(),inherited);
        }

        public static IEnumerable<PropertyInfo> GetPrimitiveTypeProperties(this Type type,bool inherited=false) {
            BindingFlags fieldFlags=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
            if(!inherited) {
                fieldFlags=fieldFlags|BindingFlags.DeclaredOnly;
            }
            return type.GetProperties(fieldFlags).Where(prop => prop.PropertyType.IsValueType||prop.PropertyType==typeof(string)||prop.PropertyType==typeof(DateTime)||prop.PropertyType.IsEnum);
        }

        public static String ToPropertyString(this Object obj) {
            IEnumerable<PropertyInfo> valueTypeProperties=GetPrimitiveTypeProperties(obj);
            if(valueTypeProperties.Any()) {
                return valueTypeProperties.Select(prop => String.Format("{0}: {1}",prop.Name,prop.GetValue(obj,null))).Aggregate((a,b) => String.Format("{0}\n{1}",a,b));
            } else {
                return "No value type properties in object";
            }
        }


        public static string ToXML(this object objectToSerialize) {
            MemoryStream mem=new MemoryStream();
            XmlSerializer ser=new XmlSerializer(objectToSerialize.GetType());
            ser.Serialize(mem,objectToSerialize);
            ASCIIEncoding ascii=new ASCIIEncoding();
            return ascii.GetString(mem.ToArray());
        }

        /// <summary>
        /// Copy an object, modify some properties using the <param>modifier</param> and return a new object of type T.
        /// </summary>
        /// <param name="original"></param>
        /// <param name="modifier"></param>
        /// <returns></returns>
        public static T Copy<T>(this T original,Action<T> modifier) {
            var copy=Activator.CreateInstance<T>();
            copy.InjectFrom(original);
            modifier.Invoke(copy);
            return copy;
        }


        /// <summary>
        /// Extract instances from the AuditLog
        /// </summary>
        /// <param name="xmlString"></param>
        /// <returns></returns>
        public static T To<T>(this string xmlString,string root="root") where T:HasId, new() {
            T obj=Activator.CreateInstance<T>();
            var props=typeof(T).GetProperties();
            using(XmlReader reader=XmlReader.Create(new StringReader(xmlString))) {
                reader.ReadToFollowing(root);
                while(reader.Read()) {
                    if(reader.NodeType==XmlNodeType.Element) {
                        for(int i=0;i<reader.AttributeCount;i++) {
                            reader.MoveToAttribute(i);
                            var prop=props.FirstOrDefault(p => p.Name==reader.Name);
                            if(prop!=null) {
                                TypeConverter converter=TypeDescriptor.GetConverter(prop.PropertyType);
                                object value;
                                if(converter.CanConvertFrom(typeof(string))) {
                                    try {
                                        value=converter.ConvertFromInvariantString(reader.Value);
                                        prop.SetValue(obj,value,null);
                                    } catch(FormatException) {
                                        // Ignore values that cannot be converted
                                    }
                                }
                            }
                        }
                    }
                }

            }

            return obj;
        }

		

		public static void Save<T>(this T obj,object extraParam=null) where T:class,HasId,new() {
            GenericDAO<T>.Save(obj,extraParam);
        }


        public static void Delete<T>(this T obj, object extraParam = null) where T : class,HasId, new()
        {
            GenericDAO<T>.Delete(obj, extraParam);
        }


        public static string ToIdString<T>(IEnumerable<T> objects) where T:HasId {
            return objects.Aggregate("",(str,obj) => String.Format("{0},{1}",str,obj.id));
        }



    }
}