using System;
using System.Runtime.CompilerServices;

namespace Mix.Cms.Lib.Models.Cms
{
	public class MixTemplate
	{
		public string Content
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

		public string Extension
		{
			get;
			set;
		}

		public string FileFolder
		{
			get;
			set;
		}

		public string FileName
		{
			get;
			set;
		}

		public string FolderType
		{
			get;
			set;
		}

		public int Id
		{
			get;
			set;
		}

		public DateTime? LastModified
		{
			get;
			set;
		}

		public string MobileContent
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

		public string Scripts
		{
			get;
			set;
		}

		public string SpaContent
		{
			get;
			set;
		}

		public string Status
		{
			get;
			set;
		}

		public string Styles
		{
			get;
			set;
		}

		public virtual MixTheme Theme
		{
			get;
			set;
		}

		public int ThemeId
		{
			get;
			set;
		}

		public string ThemeName
		{
			get;
			set;
		}

		public MixTemplate()
		{
		}
	}
}