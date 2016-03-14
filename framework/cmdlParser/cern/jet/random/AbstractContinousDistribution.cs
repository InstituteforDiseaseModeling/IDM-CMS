/*
Copyright � 1999 CERN - European Organization for Nuclear Research.
Permission to use, copy, modify, distribute and sell this software and its documentation for any purpose
is hereby granted without fee, provided that the above copyright notice appear in all copies and
that both that copyright notice and this permission notice appear in supporting documentation.
CERN makes no representations about the suitability of this software for any purpose.
It is provided "as is" without expressed or implied warranty.*/
using System;
namespace cern.jet.random
{

	/// <summary> Abstract base class for all continous distributions.
	///
	/// </summary>
	/// <author>  wolfgang.hoschek@cern.ch
	/// </author>
	/// <version>  1.0, 09/24/99
	/// </version>
	[Serializable]
	public abstract class AbstractContinousDistribution:AbstractDistribution
	{
		/// <summary> Makes this class non instantiable, but still let's others inherit from it.</summary>
		protected internal AbstractContinousDistribution()
		{
		}
	}
}