using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Configuration;
using Domain;
using Core;
using Extensions;

namespace Extensions {
    public static class CollectionExtensions {

        /// <summary>
        /// Merge elements in the ordered sequence <code>collection</code> based on <code>groupSelector</code>. Return results for each group using <code>resultSelector</code>.
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="groupSelector"></param>
        /// <param name="resultSelector"></param>
        /// <returns></returns>
        public static IEnumerable<TResult> GroupMerge<T,TResult>(this IEnumerable<T> collection,Func<T,T,int> orderingFunction,Func<IEnumerable<T>,T,bool> groupSelector,Func<IEnumerable<T>,TResult> resultSelector) {
            // Create groups by the group selector
            var groups=new List<List<T>>();
            foreach(T item in collection.OrderBy(item => item,orderingFunction)) {
                var groupForItem=groups.FirstOrDefault(group => groupSelector.Invoke(group,item));
                if(groupForItem!=null) {
                    groupForItem.Add(item);
                } else {
                    var newGroup=new List<T>();
                    newGroup.Add(item);
                    groups.Add(newGroup);
                }
            }

            // And project the results for each group using the resultSelector
            return groups.Select(group => resultSelector.Invoke(group));
        }
		
		public static IEnumerable<T> OrderBy<T>(this IEnumerable<T> objects, Func<T,T,int> orderingFunction) {
			return objects.OrderBy(t => t, new LambdaComparer<T> (orderingFunction));
		}

		
		public static IEnumerable<T> OrderBy<T,TKey>(this IEnumerable<T> objects, Func<T,TKey> keySelector, Func<TKey,TKey,int> orderingFunction)  {
			return objects.OrderBy(keySelector, new LambdaComparer<TKey> (orderingFunction));
		}


        public static IEnumerable<T> FetchAllRelated<T>(this IEnumerable<T> objects) where T:class,new() {
            return Prefetcher<T>.FetchAllRelated(objects);
        }

        public static IEnumerable<T> FetchRelated<T>(this IEnumerable<T> objects,params Expression<Func<T,object>>[] propertySelectors) where T:class,new() {
            return Prefetcher<T>.FetchRelated(objects,propertySelectors);
        }

		public static bool IsSubSetOf<T>(this IEnumerable<T> objects, IEnumerable<T> otherObjects) {
			return !objects.Except (otherObjects).Any ();
		}

        public static string ToIdString<T>(this IEnumerable<T> objects) where T: HasId {
            return objects.Select(c => c.id).ToIdString();
        }

        public static string ToIdString(this IEnumerable<int> objects) {
            return objects.Aggregate("",(str,obj) => string.IsNullOrEmpty(str)?obj.ToString():string.Format("{0},{1}",str,obj));
        }
    }
}
