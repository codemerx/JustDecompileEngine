using System;
using System.IO;

namespace JustDecompile.Tools.MSBuildProjectBuilder
{
	public class ProjectFileCreatedEvent : EventArgs
	{
		public string Name { get; private set; }
		public string FullName { get; private set; }
		public bool HasErrors { get; private set; }

		public ProjectFileCreatedEvent(string fullName, bool hasErrors)
		{
			FullName = fullName;
			Name = Path.GetFileName(fullName);
			HasErrors = hasErrors;
		}
	}
}
