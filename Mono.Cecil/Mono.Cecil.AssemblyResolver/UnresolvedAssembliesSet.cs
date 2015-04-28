using System;
using System.Collections.Generic;

namespace Mono.Cecil.AssemblyResolver
{
    /*Telerik Authorship*/
    public interface IClonableCollection<T> : ICollection<T>
    {
        IClonableCollection<T> Clone();
    }

    /*Telerik Authorship*/
    internal class UnresolvedAssembliesCollection : IClonableCollection<string>
    {
        private static readonly HashSet<string> UnresolvableAssemblies = new HashSet<string>()
        { "mscorlib, Version=255.255.255.255, Culture=neutral, PublicKeyToken=b77a5c561934e089",
	      "mscorlib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"
		};

        private readonly HashSet<string> theSet;

        public UnresolvedAssembliesCollection()
        {
            this.theSet = new HashSet<string>();
        }

        public UnresolvedAssembliesCollection(IEnumerable<string> collection)
        {
            this.theSet = new HashSet<string>(collection);
        }

        public bool Contains(string item)
        {
            return UnresolvableAssemblies.Contains(item) || this.theSet.Contains(item);
        }

        public IClonableCollection<string> Clone()
        {
            return new UnresolvedAssembliesCollection(this.theSet);
        }

        public void Add(string item)
        {
            this.theSet.Add(item);
        }

        public void Clear()
        {
            this.theSet.Clear();
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            this.theSet.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return this.theSet.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(string item)
        {
            return this.theSet.Remove(item);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return this.theSet.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.theSet.GetEnumerator();
        }
    }
}
