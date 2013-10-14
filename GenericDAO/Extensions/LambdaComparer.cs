using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Configuration;
using Domain;
using Core;
using Extensions;

namespace Extensions
{
	public class LambdaComparer<T>: Comparer<T>
	{

		private Func<T,T,Int32> Comparer { get; set;}

		public LambdaComparer(Func<T,T,Int32> comparer){
			Comparer = comparer;
		}

		public override int Compare (T x, T y)
		{
			return Comparer (x, y);
		}
	}
}
