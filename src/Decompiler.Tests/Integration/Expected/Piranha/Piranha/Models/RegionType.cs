using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Piranha.Models
{
	[Serializable]
	public sealed class RegionType
	{
		public bool Collection
		{
			get;
			set;
		}

		public string Description
		{
			get;
			set;
		}

		public RegionDisplayMode Display
		{
			get;
			set;
		}

		public IList<FieldType> Fields { get; set; } = new List<FieldType>();

		public string Icon
		{
			get;
			set;
		}

		public string Id
		{
			get;
			set;
		}

		public bool ListExpand { get; set; } = true;

		public string ListTitleField
		{
			get;
			set;
		}

		public string ListTitlePlaceholder
		{
			get;
			set;
		}

		public string Title
		{
			get;
			set;
		}

		public RegionType()
		{
		}
	}
}