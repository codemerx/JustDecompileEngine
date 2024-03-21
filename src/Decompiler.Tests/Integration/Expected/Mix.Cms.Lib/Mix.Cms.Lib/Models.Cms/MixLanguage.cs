using System;
using System.Runtime.CompilerServices;

namespace Mix.Cms.Lib.Models.Cms
{
	public class MixLanguage
	{
		public string Category
		{
			get;
			set;
		}

		public string CreatedBy
		{
			get;
			set;
		}

		public DateTime CreatedDateTime
		{
			get;
			set;
		}

		public int DataType
		{
			get;
			set;
		}

		public string DefaultValue
		{
			get;
			set;
		}

		public string Description
		{
			get;
			set;
		}

		public int Id
		{
			get;
			set;
		}

		public string Keyword
		{
			get;
			set;
		}

		public DateTime? LastModified
		{
			get;
			set;
		}

		public string ModifiedBy
		{
			get;
			set;
		}

		public int Priority
		{
			get;
			set;
		}

		public string Specificulture
		{
			get;
			set;
		}

		public virtual MixCulture SpecificultureNavigation
		{
			get;
			set;
		}

		public string Status
		{
			get;
			set;
		}

		public string Value
		{
			get;
			set;
		}

		public MixLanguage()
		{
		}
	}
}