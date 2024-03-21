using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Application.Services
{
	[Nullable(0)]
	[NullableContext(1)]
	public sealed class Notification
	{
		private readonly IDictionary<string, IList<string>> _errorMessages = new Dictionary<string, IList<string>>();

		public bool IsInvalid
		{
			get
			{
				return this._errorMessages.Count > 0;
			}
		}

		public bool IsValid
		{
			get
			{
				return this._errorMessages.Count == 0;
			}
		}

		public IDictionary<string, string[]> ModelState
		{
			get
			{
				IDictionary<string, string[]> dictionary = this._errorMessages.ToDictionary<KeyValuePair<string, IList<string>>, string, string[]>((KeyValuePair<string, IList<string>> item) => item.Key, (KeyValuePair<string, IList<string>> item) => item.Value.ToArray<string>());
				return dictionary;
			}
		}

		public Notification()
		{
		}

		public void Add(string key, string message)
		{
			if (!this._errorMessages.ContainsKey(key))
			{
				this._errorMessages[key] = new List<string>();
			}
			this._errorMessages[key].Add(message);
		}
	}
}