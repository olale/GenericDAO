using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Common;
using Support.NpgSql;
using Npgsql;
using Extensions;
using System.Reflection;
using Fasterflect;
using System.Dynamic;
using System.Data;
using Exceptions;
using Core;

namespace Configuration
{
	public class SPConfigurator<T> : Configurator, ISPConfigurator<T> where T : class, new()
	{

		[ThreadStatic]
		private readonly List<IRelatedObjectConfigurator> configurators = new List<IRelatedObjectConfigurator> ();

		public virtual T InjectFrom (IDataReader reader)
		{
			var returnObject = CreateObject<T> (reader);
			var remainingFields = reader.Names ().Except (GetFieldNamesUsedBy (reader));
			foreach (var c in configurators) {
				c.Config (returnObject,
				                     reader, remainingFields);
				remainingFields = remainingFields.Except (c.GetFieldNamesUsedByThisRelatedObject (reader));
			}
			return returnObject;
		}

		public bool ScanMultiple {
			get;
			set;
		}

		public IList<T> Process (IList<T> entities)
		{
			return ScanMultiple ? Prefetcher<T>.FetchAllRelated (entities).ToList () : entities;
		}

		public void ScanForRelatedTypes (Core.GenericDAO.FetchRelatedObjectsPolicy t = Core.GenericDAO.FetchRelatedObjectsPolicy.ScanFields)
		{
			if ((t & Core.GenericDAO.FetchRelatedObjectsPolicy.ScanFields) != 0) {
				initConfigs.Add ((command, fields) => IncludeAllRelatedObjectsFor (fields));
			}
			ScanMultiple = (t & Core.GenericDAO.FetchRelatedObjectsPolicy.UsePrefetch) != 0;
		}

		protected internal static readonly Dictionary<string, MemberSetter> setters = new Dictionary<string, MemberSetter> ();
		protected internal static readonly Dictionary<string, MemberGetter> getters = new Dictionary<string, MemberGetter> ();

		internal static MemberSetter GetStaticSetter (PropertyInfo prop)
		{
			if (!setters.ContainsKey (prop.Name)) {
				setters [prop.Name] = prop.DelegateForSetPropertyValue ();
			}
			return setters [prop.Name];
		}

		protected override MemberSetter GetSetter (PropertyInfo prop)
		{
			return SPConfigurator<T>.GetStaticSetter (prop);
		}

		protected internal static MemberGetter GetGetter (PropertyInfo prop)
		{
			if (!getters.ContainsKey (prop.Name)) {
				getters [prop.Name] = prop.DelegateForGetPropertyValue ();
			}
			return getters [prop.Name];
		}

		protected internal static MemberGetter GetGetter (string propertyName)
		{
			PropertyInfo prop = GetPropertiesFor<T> ().FirstOrDefault (p => p.Name == propertyName);
			return prop != default(PropertyInfo) ? GetGetter (prop) : default(MemberGetter);
		}

		/// <summary>
		/// A mapping between stored procedure parameters and getters for those parameters
		/// </summary>
		protected readonly Dictionary<string, Func<T, object, object>> paramsPropertiesMap = new Dictionary<string, Func<T, object, object>> ();

		public string StoredProcedureName {
			get;
			set;
		}

		/// <summary>
		/// Create a mapping between the parameters of the given SP and the properties of the current class T for all properties that match 
		/// SP parameter names. For other, non-nullable parameters, use an extra parameter to resolve the parameter value.
		/// 
		/// This method derives the parameters of the SP by querying the DB for the parameter names and produces lookup functions that invokes property getters for each parameter.
		/// </summary>
		/// <param name="storedProcedureName"></param>
		public void InitParamsGettersMap (string storedProcedureName)
		{
			using (NpgsqlCommand command = DBAccess.GetCommand(storedProcedureName)) {
				NpgsqlCommandBuilder.DeriveParameters (command);
				foreach (DbParameter param in command.Parameters) {
					if (param.Direction == ParameterDirection.Input || param.Direction == ParameterDirection.InputOutput) {
						// Remove the initial '@' from the parameter name
						string sqlParamName = param.ParameterName.Substring (1);
						MemberGetter getter = GetGetter (sqlParamName);

						if (getter != default(MemberGetter)) {
							// Fetch properties matching the parameter names of the SP
							paramsPropertiesMap [sqlParamName] = (o, x) => getter (o);
						} else if (!param.IsNullable) {
							// Parameters not available from class T may come from extra parameter objects
							paramsPropertiesMap [sqlParamName] = (o, x) => GetPropertyValue (x, sqlParamName);
						}
					} else {
						// Ignore all other parameter types
						//  throw new UnsupportedParameterException(storedProcedureName,param);
					}
				}
			}
		}

		protected internal static object GetPropertyValue (object o, string p)
		{
			object propValue = null;
			if (o != null) {
				if (typeof(IDictionary<string, object>).IsAssignableFrom (o.GetType ())) {
					// Dynamic ExpandoObject, lookup value in dictionary
					var dict = o as IDictionary<string, object>;
					if (dict.ContainsKey (p)) {
						propValue = dict [p];
					}
				} else {
					var prop = o.GetType ().GetProperty (p);
					if (prop != default(PropertyInfo)) {
						propValue = o.GetPropertyValue (p);
					}
				}
			}
			return propValue;
		}

		public object CreateParameterObject (T obj, object extraArguments = null)
		{
			var paramObject = (IDictionary<string, object>)new ExpandoObject ();
			foreach (KeyValuePair<string, Func<T, object, object>> paramPropertyGetter in paramsPropertiesMap) {
				object value = paramPropertyGetter.Value.Invoke (obj, extraArguments);
				// Only pass non-default values as parameters, or?
				//if(value!=null && !value.Equals(value.GetType().DefaultValue())) {
				paramObject [paramPropertyGetter.Key] = value;
				//}
			}
			return paramObject;
		}

		private static bool IsGetProcedure (string storedProcedureName)
		{
			int offSet = GenericDAO<T>.StoredProcedureNamePrefix ().Length;
			return storedProcedureName.Substring (offSet, 3).Equals ("Get");
		}

		private void Init (string storedProcedureName)
		{
			StoredProcedureName = storedProcedureName;
			if (!IsGetProcedure (storedProcedureName)) {
				InitParamsGettersMap (storedProcedureName);
			}
		}

		public SPConfigurator (string storedProcedureName)
		{
			Init (storedProcedureName);
		}

		public SPConfigurator ()
		{

		}

		/// <summary>
		/// Configure the retrieval of all related objects that have properties with names that match the names available as fieldNames.
		/// 
		/// With a single SP invocation, 
		/// given a set of fields available for populating object properties, we fetch related objects as well
		/// </summary>
		/// <param name="fieldNames"></param>
		protected internal void IncludeAllRelatedObjectsFor (IEnumerable<string> fieldNames)
		{
			Type t = typeof(T);
			var properties = t.GetProperties ();
			var relatedObjectTypes = properties.Where (prop => prop.PropertyType.IsClass);
			var remainingFields = fieldNames.Except (properties.Select (p => p.Name));
			foreach (var relatedObjectType in relatedObjectTypes) {
				var relatedObjectPropertyNames = relatedObjectType.PropertyType.GetProperties ().Select (p => p.Name);
				IEnumerable<string> fieldsUsedByRelatedObject = relatedObjectPropertyNames.Intersect (remainingFields);
				if (fieldsUsedByRelatedObject.Any ()) {
					var relatedPropertyConfiguratorType = typeof(RelatedObjectConfigurator<,>).MakeGenericType (typeof(T),
					                                                                                                          relatedObjectType.PropertyType);
					configurators.Add ((IRelatedObjectConfigurator)Activator.CreateInstance (relatedPropertyConfiguratorType, (IEnumerable<PropertyInfo>)new PropertyInfo[] { relatedObjectType }));
					remainingFields = remainingFields.Except (fieldsUsedByRelatedObject);
				}
			}

		}

		/// <summary>
		/// Convenience method for Map(typeof(T).Name+"Id").To(x => x.Id)
		/// </summary>
		/// <returns></returns>
		public void UsingClassNameId ()
		{
			CustomFieldsToPropertiesMap [typeof(T).Name +
				"Id"] = typeof(T).GetProperty ("Id");

		}

		public FieldMappingConfigurator<T> Map (string fieldName)
		{
			return new FieldMappingConfigurator<T> (fieldName, this);
		}

		/// <summary>
		/// Set the Exception policy for this SP
		/// </summary>
		/// <param name="policy"></param>
		/// <returns></returns>
		public void HasPolicy (Core.GenericDAO.ExceptionPolicy policy)
		{
			Policy = policy;
		}

		/// <summary>
		/// Configure the inclusion of a related object
		/// </summary>
		/// <returns></returns>
		public IRelatedObjectConfigurator<T, T1> Include<T1> () where T1 : class, new()
		{
			Type relatedType = typeof(T1);
			Type t = typeof(T);
			var propertiesOfTypeT1 = t.GetProperties ().Where (prop => relatedType.Equals (prop.PropertyType));
			if (!propertiesOfTypeT1.Any ()) {
				throw new PropertyMissingException (t, relatedType);
			} else {
				var config = new RelatedObjectConfigurator<T, T1> (propertiesOfTypeT1);
				configurators.Add (config);
				return config;
			}
		}

		public void By (Action<ISPConfigurator<T>> conf)
		{
			conf (this);
		}

		protected override IEnumerable<Configurator> GetRelatedConfigurators ()
		{
			return configurators.Cast<Configurator> ();
		}

		public override IEnumerable<string> GetAllFieldsUsedBy (IDataReader reader)
		{
			IEnumerable<string> masterPropertyNames = GetFieldsToPropertiesMap<T> (reader).Select (x => x.Key);
			IEnumerable<Configurator> relatedConfigurators = GetRelatedConfigurators ();
			foreach (var config in relatedConfigurators) {
				IEnumerable<string> overlappingProperties = config.GetAllFieldsUsedBy (reader).Intersect (masterPropertyNames);
				if ((Policy &
					Core.GenericDAO.ExceptionPolicy.AbortOnDuplicateFields) != 0 &&
					overlappingProperties.Any ()) {
					throw new NonUniquePropertyNameException (GetDescription (), overlappingProperties);
				}
			}
			return masterPropertyNames.Concat (relatedConfigurators.SelectMany (c => c.GetAllFieldsUsedBy (reader)));
		}

		protected override IEnumerable<string> GetFieldNamesUsedBy (IDataReader reader)
		{
			return GetFieldsToPropertiesMap<T> (reader).Select (x => x.Key);
		}

		internal void Init (DbCommand command, IEnumerable<string> fieldNames)
		{
			if (!configured) {
				initConfigs.ForEach (c => c.Invoke (command, fieldNames));
				configured = true;
			}
		}

		internal override string GetDescription ()
		{
			return string.Format (@"SPConfigurator<{0}>(""{1}"")", typeof(T), StoredProcedureName);
		}
	}
}
