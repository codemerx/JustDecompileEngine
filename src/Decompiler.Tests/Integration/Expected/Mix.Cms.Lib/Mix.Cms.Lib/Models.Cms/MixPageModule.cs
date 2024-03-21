using System;
using System.Runtime.CompilerServices;

namespace Mix.Cms.Lib.Models.Cms
{
	public class MixPageModule
	{
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

		public string Image
		{
			get;
			set;
		}

		public DateTime? LastModified
		{
			get;
			set;
		}

		public virtual Mix.Cms.Lib.Models.Cms.MixModule MixModule
		{
			get;
			set;
		}

		public virtual Mix.Cms.Lib.Models.Cms.MixPage MixPage
		{
			get;
			set;
		}

		public string ModifiedBy
		{
			get;
			set;
		}

		public int ModuleId
		{
			get;
			set;
		}

		public int PageId
		{
			get;
			set;
		}

		public int Position
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

		public string Status
		{
			get;
			set;
		}

		public MixPageModule()
		{
		}
	}
}