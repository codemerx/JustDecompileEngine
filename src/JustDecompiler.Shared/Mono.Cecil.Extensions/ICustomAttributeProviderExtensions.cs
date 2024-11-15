using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Mono.Cecil.Extensions
{
	public static class ICustomAttributeProviderExtensions
	{
	    /* AGPL */
		private static readonly string CompilerGeneratedAttributeName = typeof(CompilerGeneratedAttribute).FullName;
		/* End AGPL */
		
		public static bool HasCustomAttribute(this ICustomAttributeProvider attributeProvider, IEnumerable<string> attributeTypes)
		{
			if (attributeProvider == null || attributeTypes == null)
			{
				return false;
			}
			if (!attributeProvider.HasCustomAttributes)
			{
				return false;
			}
			foreach (CustomAttribute attr in attributeProvider.CustomAttributes)
			{
				foreach (string attributeType in attributeTypes)
				{
					if (attr.AttributeType.FullName == attributeType)
					{
						return true;
					}
				}
			}
			return false;
		}
		
		/* AGPL */
		public static bool HasCompilerGeneratedAttribute(this ICustomAttributeProvider attributeProvider)
		{
			if (attributeProvider.CustomAttributes == null)
				return false;

			foreach (var attribute in attributeProvider.CustomAttributes)
			{
				if (attribute.Constructor != null && attribute.AttributeType != null && attribute.AttributeType.FullName == CompilerGeneratedAttributeName)
				{
					return true;
				}
			}
			return false;
		}
		/* End AGPL */
	}
}
