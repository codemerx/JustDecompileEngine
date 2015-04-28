using System;
using Mono.Cecil;

namespace Telerik.JustDecompiler.Decompiler
{
    internal struct YieldExceptionHandlerInfo : IComparable<YieldExceptionHandlerInfo>
    {
        private readonly int tryBegin;
        private readonly int tryEnd;
        private readonly YieldExceptionHandlerType handlerType;
        private readonly MethodDefinition finallyMethodDef;
        private readonly int nextState;
        private readonly FieldReference enumeratorField;
        private readonly FieldReference disposableField;

        /// <summary>
        /// Gets the state at which the try begins.
        /// </summary>
        public int TryBeginState
        {
            get
            {
                return this.tryBegin;
            }
        }

        /// <summary>
        /// Gets the state at which the try ends.
        /// </summary>
        public int TryEndState
        {
            get
            {
                return this.tryEnd;
            }
        }

        /// <summary>
        /// Gets the type of the finally handler.
        /// </summary>
        public YieldExceptionHandlerType HandlerType
        {
            get
            {
                return this.handlerType;
            }
        }

        /// <summary>
        /// Gets the method in which resides the implementation of the finally block of the try/finally construct.
        /// </summary>
        public MethodDefinition FinallyMethodDefinition
        {
            get
            {
                return this.finallyMethodDef;
            }
        }

        /// <summary>
        /// Gets the value used for setting the state field.
        /// </summary>
        public int NextState
        {
            get
            {
                return this.nextState;
            }
        }

        /// <summary>
        /// Gets the enumerator field.
        /// </summary>
        public FieldReference EnumeratorField
        {
            get
            {
                return this.enumeratorField;
            }
        }

        /// <summary>
        /// Gets the disposable field.
        /// </summary>
        public FieldReference DisposableField
        {
            get
            {
                return this.disposableField;
            }
        }

        private YieldExceptionHandlerInfo(int tryBegin, int tryEnd)
        {
            this.tryBegin = tryBegin;
            this.tryEnd = tryEnd;
            this.handlerType = default(YieldExceptionHandlerType);
            this.finallyMethodDef = null;
            this.nextState = -1;
            this.enumeratorField = null;
            this.disposableField = null;
        }

        public YieldExceptionHandlerInfo(int tryBegin, int tryEnd, MethodDefinition finallyMethodDef)
            : this(tryBegin, tryEnd)
        {
            this.handlerType = YieldExceptionHandlerType.Method;
            this.finallyMethodDef = finallyMethodDef;
        }

        public YieldExceptionHandlerInfo(int tryBegin, int tryEnd, int nextState, FieldReference enumeratorField, FieldReference disposableField)
            : this(tryBegin, tryEnd)
        {
            this.handlerType = enumeratorField == null ? YieldExceptionHandlerType.SimpleConditionalDispose : YieldExceptionHandlerType.ConditionalDispose;
            this.nextState = nextState;
            this.enumeratorField = enumeratorField;
            this.disposableField = disposableField;
        }

        public int CompareTo(YieldExceptionHandlerInfo other)
        {
            if(this.TryEndState == other.TryEndState && this.TryBeginState == other.TryBeginState)
            {
                return 0;
            }

            int thisSize = this.tryEnd - this.tryBegin;
            int otherSize = other.tryEnd - other.tryBegin;

            if(thisSize != otherSize)
            {
                return thisSize.CompareTo(otherSize);
            }

            return this.tryBegin.CompareTo(other.tryEnd);
        }
    }
}
