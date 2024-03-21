using Microsoft.EntityFrameworkCore.Storage;
using Mix.Cms.Lib;
using Mix.Cms.Lib.Models.Cms;
using Mix.Cms.Lib.ViewModels.MixPortalPagePortalPages;
using Mix.Cms.Lib.ViewModels.MixPortalPageRoles;
using Mix.Domain.Core.ViewModels;
using Mix.Domain.Data.Repository;
using Mix.Domain.Data.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Mix.Cms.Lib.ViewModels.MixPortalPages
{
	public class UpdateRolePermissionViewModel : ViewModelBase<MixCmsContext, MixPortalPage, UpdateRolePermissionViewModel>
	{
		[JsonProperty("childPages")]
		public List<Mix.Cms.Lib.ViewModels.MixPortalPagePortalPages.UpdateViewModel> ChildPages { get; set; } = new List<Mix.Cms.Lib.ViewModels.MixPortalPagePortalPages.UpdateViewModel>();

		[JsonProperty("createdBy")]
		public string CreatedBy
		{
			get;
			set;
		}

		[JsonProperty("createdDateTime")]
		public DateTime CreatedDateTime
		{
			get;
			set;
		}

		[JsonProperty("description")]
		public string Descriotion
		{
			get;
			set;
		}

		[JsonProperty("icon")]
		public string Icon
		{
			get;
			set;
		}

		[JsonProperty("id")]
		public int Id
		{
			get;
			set;
		}

		[JsonProperty("lastModified")]
		public DateTime? LastModified
		{
			get;
			set;
		}

		[JsonProperty("level")]
		public int Level
		{
			get;
			set;
		}

		[JsonProperty("modifiedBy")]
		public string ModifiedBy
		{
			get;
			set;
		}

		[JsonProperty("navPermission")]
		public Mix.Cms.Lib.ViewModels.MixPortalPageRoles.ReadViewModel NavPermission
		{
			get;
			set;
		}

		[JsonProperty("priority")]
		public int Priority
		{
			get;
			set;
		}

		[JsonProperty("specificulture")]
		public string Specificulture
		{
			get;
			set;
		}

		[JsonProperty("status")]
		public MixEnums.MixContentStatus Status
		{
			get;
			set;
		}

		[JsonProperty("textDefault")]
		public string TextDefault
		{
			get;
			set;
		}

		[JsonProperty("textKeyword")]
		public string TextKeyword
		{
			get;
			set;
		}

		[JsonProperty("title")]
		public string Title
		{
			get;
			set;
		}

		[JsonProperty("url")]
		public string Url
		{
			get;
			set;
		}

		public UpdateRolePermissionViewModel()
		{
		}

		public UpdateRolePermissionViewModel(MixPortalPage model, MixCmsContext _context = null, IDbContextTransaction _transaction = null) : base(model, _context, _transaction)
		{
		}

		public override void ExpandView(MixCmsContext _context = null, IDbContextTransaction _transaction = null)
		{
			RepositoryResponse<List<Mix.Cms.Lib.ViewModels.MixPortalPagePortalPages.UpdateViewModel>> modelListBy = ViewModelBase<MixCmsContext, MixPortalPageNavigation, Mix.Cms.Lib.ViewModels.MixPortalPagePortalPages.UpdateViewModel>.Repository.GetModelListBy((MixPortalPageNavigation n) => n.ParentId == this.Id, _context, _transaction);
			if (modelListBy.get_IsSucceed())
			{
				this.ChildPages = (
					from c in modelListBy.get_Data()
					orderby c.Priority
					select c).ToList<Mix.Cms.Lib.ViewModels.MixPortalPagePortalPages.UpdateViewModel>();
			}
		}
	}
}