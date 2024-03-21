using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Extensions;
using Mono.Cecil.Mono.Cecil;
using Mono.Collections.Generic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Telerik.JustDecompiler.Ast;
using Telerik.JustDecompiler.Ast.Expressions;
using Telerik.JustDecompiler.Ast.Statements;
using Telerik.JustDecompiler.Decompiler;
using Telerik.JustDecompiler.Steps;

namespace Telerik.JustDecompiler.Languages
{
	public abstract class BaseImperativeLanguageWriter : BaseLanguageWriter
	{
		private Telerik.JustDecompiler.Languages.AttributeWriter attributeWriter;

		private readonly Stack<bool> shouldOmitSemicolon = new Stack<bool>();

		private readonly Stack<MethodReference> methodReferences = new Stack<MethodReference>();

		private const string DoublePositiveInfinityFieldSignature = "System.Double System.Double::PositiveInfinity";

		private const string DoubleNegativeInfinityFieldSignature = "System.Double System.Double::NegativeInfinity";

		private const string DoubleNanFieldSignature = "System.Double System.Double::NaN";

		private const string DoubleMaxValueFieldSignature = "System.Double System.Double::MaxValue";

		private const string DoubleMinValueFieldSignature = "System.Double System.Double::MinValue";

		private const string DoubleEpsilonFieldSignature = "System.Double System.Double::Epsilon";

		private const string FloatPositiveInfinityFieldSignature = "System.Single System.Single::PositiveInfinity";

		private const string FloatNegativeInfinityFieldSignature = "System.Single System.Single::NegativeInfinity";

		private const string FloatNanFieldSignature = "System.Single System.Single::NaN";

		private const string FloatMaxValueFieldSignature = "System.Single System.Single::MaxValue";

		private const string FloatMinValueFieldSignature = "System.Single System.Single::MinValue";

		private const string FloatEpsilonFieldSignature = "System.Single System.Single::Epsilon";

		protected Telerik.JustDecompiler.Languages.AttributeWriter AttributeWriter
		{
			get
			{
				if (this.attributeWriter == null)
				{
					this.attributeWriter = this.CreateAttributeWriter();
				}
				return this.attributeWriter;
			}
		}

		protected abstract string CharEnd
		{
			get;
		}

		protected abstract string CharStart
		{
			get;
		}

		protected abstract string GenericLeftBracket
		{
			get;
		}

		protected abstract string GenericRightBracket
		{
			get;
		}

		protected abstract string HexValuePrefix
		{
			get;
		}

		public abstract string IndexLeftBracket
		{
			get;
		}

		public abstract string IndexRightBracket
		{
			get;
		}

		public IKeyWordWriter KeyWordWriter
		{
			get;
			private set;
		}

		protected Stack<MethodReference> MethodReferences
		{
			get
			{
				return this.methodReferences;
			}
		}

		protected virtual bool RemoveBaseConstructorInvocation
		{
			get
			{
				return false;
			}
		}

		protected Stack<bool> ShouldOmitSemicolon
		{
			get
			{
				return this.shouldOmitSemicolon;
			}
		}

		protected virtual bool ShouldWriteOutAndRefOnInvocation
		{
			get
			{
				return false;
			}
		}

		protected virtual bool SupportsAutoProperties
		{
			get
			{
				return false;
			}
		}

		protected virtual bool SupportsSpecialNullable
		{
			get
			{
				return false;
			}
		}

		public BaseImperativeLanguageWriter(ILanguage language, IFormatter formatter, IExceptionFormatter exceptionFormatter, IWriterSettings settings) : base(language, formatter, exceptionFormatter, settings)
		{
			this.KeyWordWriter = this.CreateKeyWordWriter();
		}

		private bool CheckIfParameterIsByRef(MethodReference methodReference, int parameterIndex)
		{
			return methodReference.get_Parameters().get_Item(parameterIndex).get_ParameterType().get_IsByReference();
		}

		private ExpressionCollection CopyMethodParametersAsArguments(MethodDefinition method)
		{
			ExpressionCollection expressionCollection = new ExpressionCollection();
			if (method.get_HasParameters())
			{
				foreach (ParameterDefinition parameter in method.get_Parameters())
				{
					expressionCollection.Add(new ArgumentReferenceExpression(parameter, null));
				}
			}
			return expressionCollection;
		}

		protected abstract Telerik.JustDecompiler.Languages.AttributeWriter CreateAttributeWriter();

		protected abstract IKeyWordWriter CreateKeyWordWriter();

		protected void DoVisit(ICodeNode node)
		{
			base.Visit(node);
		}

		protected void EnterMethodInvocation(MethodReference methodReference)
		{
			this.methodReferences.Push(methodReference);
		}

		private OffsetSpan ExecuteAndGetOffsetSpan(Action toBeExecuted)
		{
			int num = this.formatter.CurrentPosition;
			EventHandler<int> eventHandler = (object sender, int currentPosition) => this.startPosition = currentPosition;
			EventHandler eventHandler1 = (object sender, EventArgs args) => this.formatter.FirstNonWhiteSpaceCharacterOnLineWritten -= eventHandler;
			this.formatter.FirstNonWhiteSpaceCharacterOnLineWritten += eventHandler;
			this.formatter.NewLineWritten += eventHandler1;
			toBeExecuted();
			this.formatter.FirstNonWhiteSpaceCharacterOnLineWritten -= eventHandler;
			this.formatter.NewLineWritten -= eventHandler1;
			return new OffsetSpan(num, this.formatter.CurrentPosition);
		}

		protected virtual string GetArgumentName(ParameterReference parameter)
		{
			string name;
			ParameterDefinition parameterDefinition = parameter.Resolve();
			if (!this.GetCurrentMethodContext().ParameterDefinitionToNameMap.TryGetValue(parameterDefinition, out name))
			{
				name = parameterDefinition.get_Name();
			}
			if (!base.Language.IsValidIdentifier(name))
			{
				name = base.Language.ReplaceInvalidCharactersInIdentifier(name);
			}
			if (base.Language.IsGlobalKeyword(name))
			{
				name = Utilities.EscapeNameIfNeeded(name, base.Language);
			}
			return name;
		}

		protected TypeReference GetBaseElementType(TypeReference type)
		{
			TypeReference elementType = type;
			while (elementType is ArrayType)
			{
				elementType = (elementType as ArrayType).get_ElementType();
			}
			return elementType;
		}

		private TypeReference GetCollidingType(TypeReference typeReference)
		{
			if (!(typeReference is TypeSpecification))
			{
				return typeReference;
			}
			TypeSpecification typeSpecification = typeReference as TypeSpecification;
			if (typeSpecification is PointerType)
			{
				return typeSpecification.get_ElementType();
			}
			if (typeSpecification is PinnedType)
			{
				if (!(typeSpecification.get_ElementType() is ByReferenceType))
				{
					return typeSpecification.get_ElementType();
				}
				return (typeSpecification.get_ElementType() as ByReferenceType).get_ElementType();
			}
			if (typeSpecification is ByReferenceType)
			{
				return typeSpecification.get_ElementType();
			}
			if (typeSpecification is ArrayType)
			{
				TypeReference elementType = typeSpecification.get_ElementType();
				while (elementType is ArrayType)
				{
					elementType = (elementType as ArrayType).get_ElementType();
				}
				return elementType;
			}
			GenericInstanceType genericInstanceType = typeSpecification as GenericInstanceType;
			if (genericInstanceType == null)
			{
				return typeSpecification;
			}
			if (!this.SupportsSpecialNullable || genericInstanceType.GetFriendlyFullName(base.Language).IndexOf("System.Nullable<") != 0 || !genericInstanceType.get_GenericArguments().get_Item(0).get_IsValueType())
			{
				return genericInstanceType;
			}
			TypeReference item = genericInstanceType.get_GenericArguments().get_Item(0);
			if (genericInstanceType.get_PostionToArgument().ContainsKey(0))
			{
				item = genericInstanceType.get_PostionToArgument()[0];
			}
			return item;
		}

		private string GetCollidingTypeName(TypeReference typeReference)
		{
			if (typeReference.get_Namespace() != "System")
			{
				return typeReference.get_Name();
			}
			return this.ToEscapedTypeString(typeReference);
		}

		private MethodSpecificContext GetCurrentMethodContext()
		{
			if (!(this.membersStack.Peek() is FieldDefinition))
			{
				return base.MethodContext;
			}
			FieldDefinition fieldDefinition = this.membersStack.Peek() as FieldDefinition;
			return base.GetMethodContext(this.TypeContext.AssignmentData[fieldDefinition.get_FullName()].AssignmentMethod);
		}

		private DefaultObjectExpression GetDefaultValueExpression(ParameterDefinition parameter)
		{
			TypeReference parameterType = parameter.get_ParameterType();
			if (parameterType.get_IsByReference())
			{
				parameterType = (parameterType as ByReferenceType).get_ElementType();
			}
			return new DefaultObjectExpression(parameterType, null);
		}

		private TypeReference GetMemberType(IMemberDefinition member)
		{
			if (member is MethodDefinition)
			{
				return ((MethodDefinition)member).get_FixedReturnType();
			}
			if (member is FieldDefinition)
			{
				return ((FieldDefinition)member).get_FieldType();
			}
			if (member is PropertyDefinition)
			{
				return ((PropertyDefinition)member).get_PropertyType();
			}
			if (!(member is EventDefinition))
			{
				throw new NotSupportedException();
			}
			return ((EventDefinition)member).get_EventType();
		}

		protected string GetMethodKeyWord(MethodDefinition method)
		{
			if (this.IsOperator(method))
			{
				return this.KeyWordWriter.Operator;
			}
			if (method.IsFunction())
			{
				return this.KeyWordWriter.Function;
			}
			return this.KeyWordWriter.Sub;
		}

		private string GetMethodOriginalName(MethodDefinition method)
		{
			if (!this.TypeContext.MethodDefinitionToNameMap.ContainsKey(method))
			{
				return method.get_Name();
			}
			return this.TypeContext.MethodDefinitionToNameMap[method];
		}

		protected virtual string GetOriginalFieldName(FieldDefinition field)
		{
			if (!this.TypeContext.BackingFieldToNameMap.ContainsKey(field))
			{
				return field.get_Name();
			}
			return this.TypeContext.BackingFieldToNameMap[field];
		}

		protected string GetParameterName(ParameterDefinition parameter)
		{
			string name = parameter.get_Name();
			if (base.MethodContext != null && (base.MethodContext.Method.get_Body().get_Instructions().Count<Instruction>() > 0 || base.MethodContext.Method.get_IsJustDecompileGenerated()) && base.MethodContext.Method == parameter.get_Method() && !base.MethodContext.ParameterDefinitionToNameMap.TryGetValue(parameter, out name))
			{
				name = parameter.get_Name();
			}
			return name;
		}

		private ICollection<string> GetUsedNamespaces()
		{
			HashSet<string> strs = new HashSet<string>();
			foreach (string usedNamespace in this.TypeContext.UsedNamespaces)
			{
				strs.Add(usedNamespace);
			}
			string[] strArray = this.TypeContext.CurrentType.get_Namespace().Split(new Char[] { '.' });
			if (strArray.Count<string>() > 0)
			{
				StringBuilder stringBuilder = new StringBuilder();
				for (int i = 0; i < (int)strArray.Length; i++)
				{
					if (i > 0)
					{
						stringBuilder.Append(".");
					}
					stringBuilder.Append(strArray[i]);
					string str = stringBuilder.ToString();
					if (!strs.Contains(str))
					{
						strs.Add(str);
					}
				}
			}
			return strs;
		}

		protected string GetVariableName(VariableReference variable)
		{
			string name;
			VariableDefinition variableDefinition = variable.Resolve();
			if (!this.GetCurrentMethodContext().VariableDefinitionToNameMap.TryGetValue(variableDefinition, out name))
			{
				name = variableDefinition.get_Name();
			}
			return name;
		}

		protected bool HasArguments(PropertyDefinition property)
		{
			MethodDefinition setMethod;
			int num = 0;
			if (property.get_GetMethod() == null)
			{
				setMethod = property.get_SetMethod();
				num = 1;
			}
			else
			{
				setMethod = property.get_GetMethod();
			}
			if (setMethod == null)
			{
				return false;
			}
			return setMethod.get_Parameters().get_Count() > num;
		}

		private bool HasNoEmptyStatements(StatementCollection statements)
		{
			bool flag;
			using (IEnumerator<Statement> enumerator = statements.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current is EmptyStatement)
					{
						continue;
					}
					flag = true;
					return flag;
				}
				return false;
			}
			return flag;
		}

		private bool HaveSameConstraints(GenericParameter outerParameter, GenericParameter innerParameter)
		{
			if (innerParameter.get_HasNotNullableValueTypeConstraint() ^ outerParameter.get_HasNotNullableValueTypeConstraint())
			{
				return false;
			}
			if (innerParameter.get_HasReferenceTypeConstraint() ^ outerParameter.get_HasReferenceTypeConstraint())
			{
				return false;
			}
			if ((!innerParameter.get_HasDefaultConstructorConstraint() ? false : !innerParameter.get_HasNotNullableValueTypeConstraint()) ^ (!outerParameter.get_HasDefaultConstructorConstraint() ? false : !outerParameter.get_HasNotNullableValueTypeConstraint()))
			{
				return false;
			}
			if (innerParameter.get_HasConstraints() ^ outerParameter.get_HasConstraints())
			{
				return false;
			}
			if (innerParameter.get_Constraints().get_Count() != outerParameter.get_Constraints().get_Count())
			{
				return false;
			}
			List<TypeReference> typeReferences = new List<TypeReference>(innerParameter.get_Constraints());
			List<TypeReference> typeReferences1 = new List<TypeReference>(outerParameter.get_Constraints());
			typeReferences.Sort((TypeReference x, TypeReference y) => x.get_FullName().CompareTo(y.get_FullName()));
			typeReferences1.Sort((TypeReference x, TypeReference y) => x.get_FullName().CompareTo(y.get_FullName()));
			for (int i = 0; i < typeReferences.Count; i++)
			{
				if (typeReferences[i].get_FullName() != typeReferences1[i].get_FullName())
				{
					return false;
				}
			}
			return true;
		}

		protected virtual bool IsComplexTarget(Expression target)
		{
			if (target.CodeNodeType == CodeNodeType.UnaryExpression)
			{
				UnaryExpression unaryExpression = target as UnaryExpression;
				if (unaryExpression.Operator == UnaryOperator.AddressDereference && unaryExpression.Operand.CodeNodeType == CodeNodeType.ArgumentReferenceExpression && (unaryExpression.Operand as ArgumentReferenceExpression).ExpressionType.get_IsByReference())
				{
					return false;
				}
			}
			if (target.CodeNodeType == CodeNodeType.BinaryExpression || target.CodeNodeType == CodeNodeType.UnaryExpression || target.CodeNodeType == CodeNodeType.ArrayCreationExpression || target.CodeNodeType == CodeNodeType.ObjectCreationExpression)
			{
				return true;
			}
			return target.CodeNodeType == CodeNodeType.LambdaExpression;
		}

		protected bool IsImplemented(TypeDefinition type, TypeDefinition baseType)
		{
			bool flag;
			if (baseType == null || type == null)
			{
				return true;
			}
			if (!baseType.get_IsAbstract())
			{
				return true;
			}
			Mono.Collections.Generic.Collection<MethodDefinition>.Enumerator enumerator = baseType.get_Methods().GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					MethodDefinition current = enumerator.get_Current();
					if (!current.get_IsAbstract() || type.get_Methods().FirstOrDefault<MethodDefinition>((MethodDefinition x) => current.HasSameSignatureWith(x)) != null)
					{
						continue;
					}
					flag = false;
					return flag;
				}
				return true;
			}
			finally
			{
				enumerator.Dispose();
			}
			return flag;
		}

		private bool IsIndexerPropertyHiding(PropertyDefinition property)
		{
			bool flag;
			TypeDefinition typeDefinition;
			TypeDefinition typeDefinition1;
			MethodDefinition methodDefinition = (property.get_GetMethod() != null ? property.get_GetMethod() : property.get_SetMethod());
			if (property.get_DeclaringType().get_BaseType() != null)
			{
				typeDefinition = property.get_DeclaringType().get_BaseType().Resolve();
			}
			else
			{
				typeDefinition = null;
			}
			TypeDefinition typeDefinition2 = typeDefinition;
		Label2:
			while (typeDefinition2 != null)
			{
				Mono.Collections.Generic.Collection<PropertyDefinition>.Enumerator enumerator = typeDefinition2.get_Properties().GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						PropertyDefinition current = enumerator.get_Current();
						if (current.IsPrivate() || !current.IsIndexer())
						{
							continue;
						}
						MethodDefinition methodDefinition1 = (current.get_GetMethod() != null ? current.get_GetMethod() : current.get_SetMethod());
						if (methodDefinition1.get_Parameters().get_Count() != methodDefinition.get_Parameters().get_Count())
						{
							continue;
						}
						bool flag1 = true;
						int num = 0;
						while (num < methodDefinition1.get_Parameters().get_Count())
						{
							if (methodDefinition1.get_Parameters().get_Item(num).get_ParameterType().get_FullName() == methodDefinition.get_Parameters().get_Item(num).get_ParameterType().get_FullName())
							{
								num++;
							}
							else
							{
								flag1 = false;
								break;
							}
						}
						if (!flag1)
						{
							continue;
						}
						flag = true;
						return flag;
					}
					goto Label0;
				}
				finally
				{
					enumerator.Dispose();
				}
				return flag;
			}
			return false;
		Label0:
			if (typeDefinition2.get_BaseType() != null)
			{
				typeDefinition1 = typeDefinition2.get_BaseType().Resolve();
			}
			else
			{
				typeDefinition1 = null;
			}
			typeDefinition2 = typeDefinition1;
			goto Label2;
		}

		private bool IsMethodHiding(MethodDefinition method)
		{
			bool flag;
			TypeDefinition typeDefinition;
			TypeDefinition typeDefinition1;
			if (method.get_DeclaringType().get_BaseType() != null)
			{
				typeDefinition = method.get_DeclaringType().get_BaseType().Resolve();
			}
			else
			{
				typeDefinition = null;
			}
			TypeDefinition typeDefinition2 = typeDefinition;
		Label2:
			while (typeDefinition2 != null)
			{
				Mono.Collections.Generic.Collection<MethodDefinition>.Enumerator enumerator = typeDefinition2.get_Methods().GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						MethodDefinition current = enumerator.get_Current();
						if (current.get_IsPrivate() || !(current.get_Name() == method.get_Name()) || current.get_Parameters().get_Count() != method.get_Parameters().get_Count())
						{
							continue;
						}
						bool flag1 = true;
						int num = 0;
						while (num < current.get_Parameters().get_Count())
						{
							if (current.get_Parameters().get_Item(num).get_ParameterType().get_FullName() == method.get_Parameters().get_Item(num).get_ParameterType().get_FullName())
							{
								num++;
							}
							else
							{
								flag1 = false;
								break;
							}
						}
						if (!flag1)
						{
							continue;
						}
						flag = true;
						return flag;
					}
					goto Label0;
				}
				finally
				{
					enumerator.Dispose();
				}
				return flag;
			}
			return false;
		Label0:
			if (typeDefinition2.get_BaseType() != null)
			{
				typeDefinition1 = typeDefinition2.get_BaseType().Resolve();
			}
			else
			{
				typeDefinition1 = null;
			}
			typeDefinition2 = typeDefinition1;
			goto Label2;
		}

		private bool IsNewDelegate(ObjectCreationExpression node)
		{
			if (node.Constructor == null || node.Constructor.get_DeclaringType() == null)
			{
				return false;
			}
			TypeDefinition typeDefinition = node.Constructor.get_DeclaringType().Resolve();
			if (typeDefinition != null && typeDefinition.get_BaseType() != null && typeDefinition.get_BaseType().get_FullName() == typeof(MulticastDelegate).FullName)
			{
				return true;
			}
			return false;
		}

		private bool IsNull(Expression node)
		{
			if (node.CodeNodeType != CodeNodeType.LiteralExpression)
			{
				return false;
			}
			return (object)(node as LiteralExpression).Value == (object)null;
		}

		private bool IsOperator(MethodDefinition method)
		{
			string str;
			if (!method.get_IsOperator())
			{
				return false;
			}
			if (!base.Language.TryGetOperatorName(method.get_OperatorName(), out str))
			{
				return false;
			}
			return true;
		}

		protected bool IsPostUnaryOperator(UnaryOperator op)
		{
			if ((int)op - (int)UnaryOperator.PostDecrement <= (int)UnaryOperator.LogicalNot)
			{
				return true;
			}
			return false;
		}

		private bool IsPropertyHiding(PropertyDefinition property)
		{
			bool flag;
			TypeDefinition typeDefinition;
			TypeDefinition typeDefinition1;
			if (property.get_DeclaringType().get_BaseType() != null)
			{
				typeDefinition = property.get_DeclaringType().get_BaseType().Resolve();
			}
			else
			{
				typeDefinition = null;
			}
			TypeDefinition typeDefinition2 = typeDefinition;
		Label2:
			while (typeDefinition2 != null)
			{
				Mono.Collections.Generic.Collection<PropertyDefinition>.Enumerator enumerator = typeDefinition2.get_Properties().GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						PropertyDefinition current = enumerator.get_Current();
						if (current.IsPrivate() || !(current.get_Name() == property.get_Name()))
						{
							continue;
						}
						flag = true;
						return flag;
					}
					goto Label0;
				}
				finally
				{
					enumerator.Dispose();
				}
				return flag;
			}
			return false;
		Label0:
			if (typeDefinition2.get_BaseType() != null)
			{
				typeDefinition1 = typeDefinition2.get_BaseType().Resolve();
			}
			else
			{
				typeDefinition1 = null;
			}
			typeDefinition2 = typeDefinition1;
			goto Label2;
		}

		protected bool IsReferenceFromMscorlib(TypeReference reference)
		{
			if (reference.get_Scope().get_Name() == "mscorlib" || reference.get_Scope().get_Name() == "CommonLanguageRuntimeLibrary")
			{
				return true;
			}
			return reference.get_Scope().get_Name() == "System.Runtime";
		}

		protected virtual bool IsTypeNameInCollision(string typeName)
		{
			if (this.IsTypeNameInCollisionWithParameters(typeName))
			{
				return true;
			}
			if (this.IsTypeNameInCollisionWithNamespace(typeName))
			{
				return true;
			}
			if (this.IsTypeNameInCollisionWithOtherType(typeName))
			{
				return true;
			}
			if (this.IsTypeNameInCollisionWithMembers(typeName))
			{
				return true;
			}
			if (this.IsTypeNameinCollisionWithVariables(typeName))
			{
				return true;
			}
			return false;
		}

		private bool IsTypeNameInCollisionWithMembers(string typeName)
		{
			return this.TypeContext.VisibleMembersNames.Contains(typeName);
		}

		private bool IsTypeNameInCollisionWithNamespace(string typeName)
		{
			HashSet<string> strs;
			string[] strArray = this.TypeContext.CurrentType.get_Namespace().Split(new Char[] { '.' });
			if (strArray.Count<string>() > 0)
			{
				StringBuilder stringBuilder = new StringBuilder();
				for (int i = 0; i < (int)strArray.Length; i++)
				{
					if (i > 0)
					{
						stringBuilder.Append(".");
					}
					stringBuilder.Append(strArray[i]);
					string str = stringBuilder.ToString();
					if (this.ModuleContext.NamespaceHieararchy.TryGetValue(str, out strs) && strs.Contains(typeName))
					{
						return true;
					}
				}
			}
			return false;
		}

		private bool IsTypeNameInCollisionWithOtherType(string typeName)
		{
			List<string> strs;
			ICollection<string> usedNamespaces = this.GetUsedNamespaces();
			if (this.ModuleContext.CollisionTypesData.TryGetValue(typeName, out strs) && strs.Intersect<string>(usedNamespaces).Count<string>() > 1)
			{
				return true;
			}
			return false;
		}

		private bool IsTypeNameInCollisionWithParameters(string typeName)
		{
			bool flag;
			if (base.CurrentMethod != null)
			{
				Mono.Collections.Generic.Collection<ParameterDefinition>.Enumerator enumerator = base.CurrentMethod.get_Parameters().GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						ParameterDefinition current = enumerator.get_Current();
						if (base.Language.IdentifierComparer.Compare(current.get_Name(), typeName) != 0)
						{
							continue;
						}
						flag = true;
						return flag;
					}
					return false;
				}
				finally
				{
					enumerator.Dispose();
				}
				return flag;
			}
			return false;
		}

		private bool IsTypeNameinCollisionWithVariables(string typeName)
		{
			if (base.MethodContext == null)
			{
				return false;
			}
			return base.MethodContext.VariableNamesCollection.Contains(typeName);
		}

		protected bool IsTypeParameterRedeclaration(GenericParameter genericParameter)
		{
			TypeReference owner = genericParameter.get_Owner() as TypeReference;
			if (owner != null && owner.get_IsNested())
			{
				TypeReference declaringType = owner.get_DeclaringType();
				if (declaringType.get_HasGenericParameters() && genericParameter.get_Position() < declaringType.get_GenericParameters().get_Count())
				{
					return true;
				}
			}
			return false;
		}

		protected void LeaveMethodInvocation()
		{
			this.methodReferences.Pop();
		}

		private void NormalizeNameContainingGenericSymbols(string[] tokensCollection, char genericSymbol, StringBuilder stringBuilder)
		{
			for (int i = 0; i < (int)tokensCollection.Length; i++)
			{
				if (i > 0)
				{
					stringBuilder.Append(genericSymbol);
				}
				if (!this.NormalizeNameIfContainingGenericSymbols(tokensCollection[i], stringBuilder))
				{
					string[] strArray = tokensCollection[i].Split(new Char[] { ',' });
					for (int j = 0; j < (int)strArray.Length; j++)
					{
						if (j > 0)
						{
							stringBuilder.Append(", ");
						}
						stringBuilder.Append(base.Language.ReplaceInvalidCharactersInIdentifier(strArray[j]));
					}
				}
			}
		}

		private bool NormalizeNameIfContainingGenericSymbols(string name, StringBuilder stringBuilder)
		{
			bool flag = false;
			string[] strArray = name.Split(new Char[] { '<' });
			string[] strArray1 = name.Split(new Char[] { '>' });
			if ((int)strArray.Length > 1)
			{
				flag = true;
				this.NormalizeNameContainingGenericSymbols(strArray, '<', stringBuilder);
			}
			else if ((int)strArray1.Length > 1)
			{
				flag = true;
				this.NormalizeNameContainingGenericSymbols(strArray1, '>', stringBuilder);
			}
			return flag;
		}

		protected virtual void PostWriteGenericParametersConstraints(IGenericDefinition generic)
		{
		}

		protected virtual void PostWriteMethodReturnType(MethodDefinition method)
		{
		}

		protected bool ShouldWriteConstraintsAsComment(GenericParameter genericParameter)
		{
			TypeReference owner = genericParameter.get_Owner() as TypeReference;
			if (owner == null)
			{
				return false;
			}
			TypeReference declaringType = owner.get_DeclaringType();
			if (declaringType == null || !declaringType.get_HasGenericParameters() || declaringType.get_GenericParameters().get_Count() <= genericParameter.get_Position())
			{
				return false;
			}
			GenericParameter item = declaringType.get_GenericParameters().get_Item(genericParameter.get_Position());
			if (item == null)
			{
				return false;
			}
			if (this.HaveSameConstraints(item, genericParameter))
			{
				return false;
			}
			return true;
		}

		protected void StartInitializer(InitializerExpression node)
		{
			if (node.IsMultiLine)
			{
				this.WriteLine();
				return;
			}
			this.WriteSpace();
		}

		internal abstract string ToEscapedTypeString(TypeReference reference);

		protected abstract string ToString(BinaryOperator op, bool isOneSideNull = false);

		protected abstract string ToString(UnaryOperator op);

		public abstract string ToTypeString(TypeReference type);

		private bool TryWriteMethodAsOperator(MethodDefinition method)
		{
			string genericName;
			if (!method.get_IsOperator())
			{
				return false;
			}
			if (!base.Language.TryGetOperatorName(method.get_OperatorName(), out genericName))
			{
				return false;
			}
			bool flag = false;
			if (method.get_OperatorName() == "Implicit" || method.get_OperatorName() == "Explicit")
			{
				if (method.get_OperatorName() != "Implicit")
				{
					this.WriteKeyword(this.KeyWordWriter.Explicit);
				}
				else
				{
					this.WriteKeyword(this.KeyWordWriter.Implicit);
				}
				if (genericName == "")
				{
					genericName = method.get_ReturnType().GetGenericName(base.Language, this.GenericLeftBracket, this.GenericRightBracket);
				}
				this.WriteSpace();
				flag = true;
			}
			if (!flag && this.KeyWordWriter.Sub == null)
			{
				this.WriteMethodReturnType(method);
				this.WriteSpace();
			}
			this.WriteKeyword(this.KeyWordWriter.Operator);
			this.WriteSpace();
			int currentPosition = this.formatter.CurrentPosition;
			this.WriteReference(genericName, method);
			int num = this.formatter.CurrentPosition - 1;
			this.currentWritingInfo.MemberDeclarationToCodePostionMap[method] = new OffsetSpan(currentPosition, num);
			return true;
		}

		protected abstract bool TypeSupportsExplicitStaticMembers(TypeDefinition type);

		public override void Visit(ICodeNode node)
		{
			if (!this.isStopped)
			{
				this.WriteCodeNodeLabel(node);
				if (node == null || node.CodeNodeType == CodeNodeType.EmptyStatement)
				{
					this.DoVisit(node);
				}
				else
				{
					OffsetSpan offsetSpan = this.ExecuteAndGetOffsetSpan(() => this.u003cu003en__0(node));
					if (node != null)
					{
						this.currentWritingInfo.CodeMappingInfo.Add(node, new OffsetSpan(offsetSpan.StartOffset, offsetSpan.EndOffset - 1));
						if (node is Expression)
						{
							try
							{
								foreach (Instruction mappedInstruction in (node as Expression).MappedInstructions)
								{
									this.currentWritingInfo.CodeMappingInfo.Add(mappedInstruction, offsetSpan);
								}
							}
							catch (ArgumentException argumentException)
							{
								base.OnExceptionThrown(argumentException);
							}
						}
					}
				}
			}
		}

		public override void Visit(IEnumerable collection)
		{
			bool flag = false;
			foreach (ICodeNode codeNode in collection)
			{
				if (codeNode.CodeNodeType != CodeNodeType.EmptyStatement || codeNode is Statement && (codeNode as Statement).Label != "")
				{
					if (!flag)
					{
						flag = true;
					}
					else
					{
						this.WriteLine();
					}
				}
				this.Visit(codeNode);
			}
		}

		private void VisitAddressDereferenceExpression(UnaryExpression node)
		{
			if (node.Operand.CodeNodeType == CodeNodeType.ArgumentReferenceExpression && node.Operand.ExpressionType.get_IsByReference())
			{
				this.Visit(node.Operand);
				return;
			}
			this.WriteKeyword(this.KeyWordWriter.Dereference);
			this.Visit(node.Operand);
		}

		public override void VisitAnonymousObjectCreationExpression(AnonymousObjectCreationExpression node)
		{
			this.WriteKeyword(this.KeyWordWriter.New);
			if (this.KeyWordWriter.ObjectInitializer != null)
			{
				this.WriteSpace();
				this.WriteKeyword(this.KeyWordWriter.ObjectInitializer);
			}
			this.StartInitializer(node.Initializer);
			this.Visit(node.Initializer);
		}

		public override void VisitAnonymousPropertyInitializerExpression(AnonymousPropertyInitializerExpression node)
		{
			this.WritePropertyName(node.Property);
		}

		public override void VisitArgumentReferenceExpression(ArgumentReferenceExpression node)
		{
			this.Write(this.GetArgumentName(node.Parameter));
		}

		public override void VisitArrayAssignmentFieldReferenceExpression(ArrayAssignmentFieldReferenceExpression node)
		{
			this.Visit(node.Field);
		}

		public override void VisitArrayAssignmentVariableReferenceExpression(ArrayVariableReferenceExpression node)
		{
			this.Visit(node.Variable);
		}

		public override void VisitArrayCreationExpression(ArrayCreationExpression node)
		{
			this.WriteKeyword(this.KeyWordWriter.New);
			this.WriteSpace();
			this.WriteReferenceAndNamespaceIfInCollision(this.GetBaseElementType(node.ElementType));
			bool flag = Utilities.IsInitializerPresent(node.Initializer);
			this.WriteArrayDimensions(node.Dimensions, node.ElementType, flag);
			if (flag)
			{
				this.StartInitializer(node.Initializer);
				this.Visit(node.Initializer);
			}
		}

		public override void VisitArrayLengthExpression(ArrayLengthExpression node)
		{
			bool flag = this.IsComplexTarget(node.Target);
			if (flag)
			{
				this.WriteToken("(");
			}
			base.Visit(node.Target);
			if (flag)
			{
				this.WriteToken(")");
			}
			this.WriteToken(".");
			this.WriteReference("Length", null);
		}

		public override void VisitArrayVariableDeclarationExpression(ArrayVariableDeclarationExpression node)
		{
			this.Visit(node.Variable);
		}

		public override void VisitAutoPropertyConstructorInitializerExpression(AutoPropertyConstructorInitializerExpression node)
		{
			if (node.Target == null)
			{
				this.WriteReferenceAndNamespaceIfInCollision(node.Property.get_DeclaringType());
			}
			else
			{
				if (!(node.Target is ThisReferenceExpression))
				{
					throw new ArgumentException();
				}
				this.Visit(node.Target);
			}
			this.WriteToken(".");
			this.WritePropertyName(node.Property);
		}

		public override void VisitAwaitExpression(AwaitExpression node)
		{
			this.WriteKeyword(this.KeyWordWriter.Await);
			this.WriteSpace();
			this.Visit(node.Expression);
		}

		public override void VisitBaseReferenceExpression(BaseReferenceExpression node)
		{
			this.WriteKeyword(this.KeyWordWriter.Base);
		}

		public override void VisitBinaryExpression(BinaryExpression node)
		{
			bool flag;
			this.Visit(node.Left);
			this.WriteSpace();
			flag = (this.IsNull(node.Left) ? true : this.IsNull(node.Right));
			string str = this.ToString(node.Operator, flag);
			if (!base.Language.IsOperatorKeyword(str))
			{
				this.Write(str);
			}
			else
			{
				this.WriteKeyword(str);
			}
			if (node.Right.CodeNodeType != CodeNodeType.InitializerExpression)
			{
				this.WriteSpace();
			}
			else
			{
				this.StartInitializer(node.Right as InitializerExpression);
			}
			this.WriteRightPartOfBinaryExpression(node);
		}

		public override void VisitBlockExpression(BlockExpression node)
		{
			this.WriteToken("{ ");
			this.VisitList(node.Expressions);
			this.WriteToken(" }");
		}

		public override void VisitBlockStatement(BlockStatement node)
		{
			this.WriteBlock(() => {
				this.Visit(node.Statements);
				if (node.Statements.Count != 0 && this.HasNoEmptyStatements(node.Statements))
				{
					this.WriteLine();
				}
			}, node.Label);
		}

		public override void VisitCanCastExpression(CanCastExpression node)
		{
			if (this.KeyWordWriter.IsType != null)
			{
				this.WriteKeyword(this.KeyWordWriter.IsType);
				this.WriteSpace();
			}
			this.Visit(node.Expression);
			this.WriteSpace();
			this.WriteKeyword(this.KeyWordWriter.Is);
			this.WriteSpace();
			this.WriteReferenceAndNamespaceIfInCollision(node.TargetType);
		}

		public override void VisitCaseGotoStatement(CaseGotoStatement node)
		{
			this.VisitGotoStatement(node);
		}

		public override void VisitDelegateCreationExpression(DelegateCreationExpression node)
		{
			if (!node.TypeIsImplicitlyInferable)
			{
				this.WriteKeyword(this.KeyWordWriter.New);
				this.WriteSpace();
				this.WriteReference(node.Type);
				this.Write("(");
			}
			if (node.MethodExpression.CodeNodeType != CodeNodeType.LambdaExpression)
			{
				this.Write(node.Target);
				this.Write(".");
			}
			this.Write(node.MethodExpression);
			if (!node.TypeIsImplicitlyInferable)
			{
				this.Write(")");
			}
		}

		public override void VisitDelegateInvokeExpression(DelegateInvokeExpression node)
		{
			bool flag = this.IsComplexTarget(node.Target);
			bool flag1 = false;
			if (flag)
			{
				this.WriteToken("(");
				if (node.Target.CodeNodeType == CodeNodeType.LambdaExpression && (node.Target as LambdaExpression).HasType)
				{
					flag1 = true;
					this.WriteToken("(");
					this.WriteReferenceAndNamespaceIfInCollision((node.Target as LambdaExpression).ExpressionType);
					this.WriteToken(")");
					this.WriteToken("(");
				}
			}
			this.Visit(node.Target);
			if (flag)
			{
				if (flag1)
				{
					this.WriteToken(")");
				}
				this.WriteToken(")");
			}
			this.WriteToken("(");
			this.EnterMethodInvocation(node.InvokeMethodReference);
			this.VisitMethodParameters(node.Arguments);
			this.LeaveMethodInvocation();
			this.WriteToken(")");
		}

		public override void VisitDoWhileStatement(DoWhileStatement node)
		{
			this.WriteKeyword(this.KeyWordWriter.Do);
			this.WriteLine();
			this.Visit(node.Body);
			this.WriteLine();
			this.WriteKeyword(this.KeyWordWriter.LoopWhile);
			this.WriteSpace();
			this.WriteSpecialBetweenParenthesis(node.Condition);
			this.WriteEndOfStatement();
		}

		public override void VisitEnumExpression(EnumExpression node)
		{
			if (node.FieldName == null)
			{
				this.WriteKeyword(this.KeyWordWriter.Null);
				return;
			}
			this.WriteReferenceAndNamespaceIfInCollision(node.ExpressionType);
			this.Write(".");
			string fieldName = this.GetFieldName(node.Field);
			this.WriteReference(fieldName, node.Field);
		}

		public override void VisitEventReferenceExpression(EventReferenceExpression node)
		{
			if (node.Target == null)
			{
				this.WriteReferenceAndNamespaceIfInCollision(node.Event.get_DeclaringType());
			}
			else
			{
				bool flag = this.IsComplexTarget(node.Target);
				if (flag)
				{
					this.WriteToken("(");
				}
				this.Visit(node.Target);
				if (flag)
				{
					this.WriteToken(")");
				}
			}
			this.WriteToken(".");
			this.WriteReference(node.Event.get_Name(), node.Event);
		}

		public override void VisitExpressionStatement(ExpressionStatement node)
		{
			this.Visit(node.Expression);
			if (this.ShouldOmitSemicolon.Count == 0 || !this.ShouldOmitSemicolon.Peek())
			{
				this.WriteEndOfStatement();
			}
		}

		protected void VisitExtensionMethodParameters(IList<Expression> list)
		{
			this.VisitMethodParametersInternal(list, true);
		}

		public override void VisitFieldInitializerExpression(FieldInitializerExpression node)
		{
			this.WriteFieldName(node.Field);
		}

		public override void VisitFieldReferenceExpression(FieldReferenceExpression node)
		{
			if (node.Target == null)
			{
				this.WriteReferenceAndNamespaceIfInCollision(node.Field.get_DeclaringType());
			}
			else
			{
				bool flag = this.IsComplexTarget(node.Target);
				if (flag)
				{
					this.WriteToken("(");
				}
				this.Visit(node.Target);
				if (flag)
				{
					this.WriteToken(")");
				}
			}
			this.WriteToken(".");
			this.WriteFieldName(node.Field);
		}

		public override void VisitFixedStatement(FixedStatement expression)
		{
			this.WriteKeyword(this.KeyWordWriter.Fixed);
			this.WriteSpace();
			this.WriteBetweenParenthesis(expression.Expression);
			this.WriteLine();
			this.Visit(expression.Body);
			this.WriteSpecialEndBlock(this.KeyWordWriter.Fixed);
		}

		public override void VisitFromClause(FromClause node)
		{
			this.WriteKeyword(this.KeyWordWriter.LinqFrom);
			this.WriteSpace();
			this.Visit(node.Identifier);
			this.WriteSpace();
			this.WriteKeyword(this.KeyWordWriter.LinqIn);
			this.WriteSpace();
			this.Visit(node.Collection);
		}

		public override void VisitGotoStatement(GotoStatement node)
		{
			this.WriteKeyword(this.KeyWordWriter.GoTo);
			this.WriteSpace();
			this.Write(node.TargetLabel);
			this.WriteEndOfStatement();
		}

		public override void VisitGroupClause(GroupClause node)
		{
			this.WriteKeyword(this.KeyWordWriter.LinqGroup);
			this.WriteSpace();
			this.Visit(node.Expression);
			this.WriteSpace();
			this.WriteKeyword(this.KeyWordWriter.LinqBy);
			this.WriteSpace();
			this.Visit(node.GroupKey);
		}

		public override void VisitIfElseIfStatement(IfElseIfStatement node)
		{
			for (int i = 0; i < node.ConditionBlocks.Count; i++)
			{
				if (i != 0)
				{
					this.WriteLine();
					this.WriteKeyword(this.KeyWordWriter.ElseIf);
				}
				else
				{
					this.WriteKeyword(this.KeyWordWriter.If);
				}
				this.WriteSpace();
				KeyValuePair<Expression, BlockStatement> item = node.ConditionBlocks[i];
				this.WriteBetweenParenthesis(item.Key);
				if (this.KeyWordWriter.Then != null)
				{
					this.WriteSpace();
					this.WriteKeyword(this.KeyWordWriter.Then);
				}
				this.WriteLine();
				item = node.ConditionBlocks[i];
				this.Visit(item.Value);
			}
			if (node.Else != null)
			{
				this.WriteLine();
				this.WriteKeyword(this.KeyWordWriter.Else);
				this.WriteLine();
				this.Visit(node.Else);
			}
			this.WriteSpecialEndBlock(this.KeyWordWriter.If);
		}

		public override void VisitIfStatement(IfStatement node)
		{
			this.WriteKeyword(this.KeyWordWriter.If);
			this.WriteSpace();
			this.WriteBetweenParenthesis(node.Condition);
			if (this.KeyWordWriter.Then != null)
			{
				this.WriteSpace();
				this.WriteKeyword(this.KeyWordWriter.Then);
			}
			this.WriteLine();
			this.Visit(node.Then);
			if (node.Else == null)
			{
				this.WriteSpecialEndBlock(this.KeyWordWriter.If);
				return;
			}
			this.WriteLine();
			this.WriteKeyword(this.KeyWordWriter.Else);
			this.WriteLine();
			this.Visit(node.Else);
			this.WriteSpecialEndBlock(this.KeyWordWriter.If);
		}

		protected override void VisitIIndexerExpression(IIndexerExpression node)
		{
			bool flag = this.IsComplexTarget(node.Target);
			if (flag)
			{
				this.WriteToken("(");
			}
			this.Visit(node.Target);
			if (flag)
			{
				this.WriteToken(")");
			}
			this.WriteToken(this.IndexLeftBracket);
			this.VisitList(node.Indices);
			this.WriteToken(this.IndexRightBracket);
		}

		public override void VisitInitializerExpression(InitializerExpression node)
		{
			this.WriteToken("{");
			if (!node.IsMultiLine)
			{
				this.WriteSpace();
				this.VisitList(node.Expressions);
				this.WriteSpace();
			}
			else
			{
				this.Indent();
				this.WriteLine();
				this.VisitMultilineList(node.Expressions);
				this.WriteLine();
				this.Outdent();
			}
			this.WriteToken("}");
		}

		public override void VisitIntoClause(IntoClause node)
		{
			this.WriteKeyword(this.KeyWordWriter.LinqInto);
			this.WriteSpace();
			this.Visit(node.Identifier);
		}

		public override void VisitJoinClause(JoinClause node)
		{
			this.WriteKeyword(this.KeyWordWriter.LinqJoin);
			this.WriteSpace();
			this.Visit(node.InnerIdentifier);
			this.WriteSpace();
			this.WriteKeyword(this.KeyWordWriter.LinqIn);
			this.WriteSpace();
			this.Visit(node.InnerCollection);
			this.WriteSpace();
			this.WriteKeyword(this.KeyWordWriter.LinqOn);
			this.WriteSpace();
			this.Visit(node.OuterKey);
			this.WriteSpace();
			this.WriteKeyword(this.KeyWordWriter.LinqEquals);
			this.WriteSpace();
			this.Visit(node.InnerKey);
		}

		public override void VisitLambdaExpression(LambdaExpression node)
		{
			if (node.IsAsync && this.KeyWordWriter.Async != null)
			{
				this.WriteKeyword(this.KeyWordWriter.Async);
				this.WriteSpace();
			}
		}

		public override void VisitLambdaParameterExpression(LambdaParameterExpression node)
		{
			if (!node.DisplayType)
			{
				this.Write(this.GetArgumentName(node.Parameter));
				return;
			}
			this.WriteTypeAndName(node.ExpressionType, this.GetArgumentName(node.Parameter));
		}

		public override void VisitLetClause(LetClause node)
		{
			this.WriteKeyword(this.KeyWordWriter.LinqLet);
			this.WriteSpace();
			this.Visit(node.Identifier);
			this.Write(" = ");
			this.Visit(node.Expression);
		}

		protected void VisitList(IList<Expression> list)
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (i > 0)
				{
					this.WriteToken(",");
					this.WriteSpace();
				}
				this.Visit(list[i]);
			}
		}

		public override void VisitLiteralExpression(LiteralExpression node)
		{
			this.WriteLiteralInLanguageSyntax(node.Value);
		}

		public override void VisitLockStatement(LockStatement expression)
		{
			this.WriteKeyword(this.KeyWordWriter.Lock);
			this.WriteSpace();
			this.WriteSpecialBetweenParenthesis(expression.Expression);
			this.WriteLine();
			this.Visit(expression.Body);
			this.WriteSpecialEndBlock(this.KeyWordWriter.Lock);
		}

		public override void VisitMakeRefExpression(MakeRefExpression node)
		{
			this.WriteKeyword("__makeref");
			this.WriteToken("(");
			this.Visit(node.Expression);
			this.WriteToken(")");
		}

		public override void VisitMethodInvocationExpression(MethodInvocationExpression node)
		{
			bool isExtensionMethod = false;
			if (node.MethodExpression.CodeNodeType == CodeNodeType.MethodReferenceExpression)
			{
				MethodReferenceExpression methodExpression = node.MethodExpression;
				MethodReference method = methodExpression.Method;
				if (method != null && !method.get_HasThis() && methodExpression.MethodDefinition != null)
				{
					isExtensionMethod = methodExpression.MethodDefinition.get_IsExtensionMethod();
				}
			}
			if (!isExtensionMethod)
			{
				if (node.MethodExpression.Target != null)
				{
					this.WriteMethodTarget(node.MethodExpression.Target);
				}
				if (!node.MethodExpression.Method.get_HasThis() && (node.MethodExpression.MethodDefinition != null && !node.MethodExpression.MethodDefinition.get_IsExtensionMethod() || node.MethodExpression.MethodDefinition == null))
				{
					this.WriteReferenceAndNamespaceIfInCollision(node.MethodExpression.Method.get_DeclaringType());
					this.WriteToken(".");
				}
			}
			else if (node.Arguments.Count > 0)
			{
				this.WriteMethodTarget(node.Arguments[0]);
			}
			this.WriteMethodReference(node.MethodExpression);
			bool flag = false;
			this.WriteToken((flag ? this.IndexLeftBracket : "("));
			if (node.MethodExpression == null)
			{
				this.VisitMethodParameters(node.Arguments);
			}
			else
			{
				this.EnterMethodInvocation(node.MethodExpression.Method);
				if (isExtensionMethod)
				{
					this.VisitExtensionMethodParameters(node.Arguments);
				}
				else
				{
					this.VisitMethodParameters(node.Arguments);
				}
				this.LeaveMethodInvocation();
			}
			this.WriteToken((flag ? this.IndexRightBracket : ")"));
		}

		protected void VisitMethodParameters(IList<Expression> list)
		{
			this.VisitMethodParametersInternal(list, false);
		}

		private void VisitMethodParametersInternal(IList<Expression> list, bool isExtensionMethod)
		{
			for (int i = (isExtensionMethod ? 1 : 0); i < list.Count; i++)
			{
				if (i > 0 && !isExtensionMethod || i > 1 & isExtensionMethod)
				{
					this.WriteToken(",");
					this.WriteSpace();
				}
				bool flag = false;
				if (list[i].CodeNodeType == CodeNodeType.ArgumentReferenceExpression && (list[i] as ArgumentReferenceExpression).Parameter.get_ParameterType().get_IsByReference())
				{
					flag = true;
				}
				if (list[i] is UnaryExpression && (list[i] as UnaryExpression).Operator == UnaryOperator.AddressReference && this.MethodReferences.Count > 0)
				{
					flag = true;
				}
				if (list[i].CodeNodeType == CodeNodeType.MethodInvocationExpression && (list[i] as MethodInvocationExpression).IsByReference)
				{
					flag = this.CheckIfParameterIsByRef(this.MethodReferences.Peek(), i);
				}
				if (list[i].CodeNodeType == CodeNodeType.VariableReferenceExpression && (list[i] as VariableReferenceExpression).Variable.get_VariableType().get_IsByReference())
				{
					flag = this.CheckIfParameterIsByRef(this.MethodReferences.Peek(), i);
				}
				if (flag)
				{
					MethodDefinition methodDefinition = this.MethodReferences.Peek().Resolve();
					if (methodDefinition != null)
					{
						if (this.ShouldWriteOutAndRefOnInvocation)
						{
							this.WriteOutOrRefKeyWord(methodDefinition.get_Parameters().get_Item(i));
							this.WriteSpace();
						}
					}
					else if (this.ShouldWriteOutAndRefOnInvocation)
					{
						this.WriteKeyword(this.KeyWordWriter.ByRef);
						this.WriteSpace();
					}
					if (!(list[i] is UnaryExpression))
					{
						goto Label1;
					}
					this.Visit((list[i] as UnaryExpression).Operand);
					goto Label0;
				}
			Label1:
				this.Visit(list[i]);
			Label0:
			}
		}

		public override void VisitMethodReferenceExpression(MethodReferenceExpression node)
		{
			if (node.Target == null)
			{
				this.WriteReferenceAndNamespaceIfInCollision(node.Method.get_DeclaringType());
				this.WriteToken(".");
			}
			else
			{
				this.WriteMethodTarget(node.Target);
			}
			this.WriteMethodReference(node);
		}

		private void VisitMultilineList(ExpressionCollection expressions)
		{
			for (int i = 0; i < expressions.Count; i++)
			{
				this.Visit(expressions[i]);
				if (i != expressions.Count - 1)
				{
					this.WriteToken(",");
					this.WriteLine();
				}
			}
		}

		public override void VisitObjectCreationExpression(ObjectCreationExpression node)
		{
			if (this.IsNewDelegate(node))
			{
				this.WriteDelegateCreation(node);
				return;
			}
			this.WriteKeyword(this.KeyWordWriter.New);
			this.WriteSpace();
			if (node.Constructor == null)
			{
				this.WriteReferenceAndNamespaceIfInCollision(node.Type);
			}
			else
			{
				this.WriteConstructorInvocation(node);
			}
			this.WriteToken("(");
			this.EnterMethodInvocation(node.Constructor);
			this.VisitMethodParameters(node.Arguments);
			this.LeaveMethodInvocation();
			this.WriteToken(")");
			if (node.Initializer != null)
			{
				if (node.Initializer.InitializerType == InitializerType.ObjectInitializer)
				{
					if (this.KeyWordWriter.ObjectInitializer != null)
					{
						this.WriteSpace();
						this.WriteKeyword(this.KeyWordWriter.ObjectInitializer);
					}
				}
				else if (node.Initializer.InitializerType == InitializerType.CollectionInitializer && this.KeyWordWriter.CollectionInitializer != null)
				{
					this.WriteSpace();
					this.WriteKeyword(this.KeyWordWriter.CollectionInitializer);
				}
				this.StartInitializer(node.Initializer);
				this.Visit(node.Initializer);
			}
		}

		public override void VisitOrderByClause(OrderByClause node)
		{
			this.WriteKeyword(this.KeyWordWriter.LinqOrderBy);
			this.WriteSpace();
			for (int i = 0; i < node.ExpressionToOrderDirectionMap.Count; i++)
			{
				if (i > 0)
				{
					this.Write(", ");
				}
				this.Visit(node.ExpressionToOrderDirectionMap[i].Key);
				if (node.ExpressionToOrderDirectionMap[i].Value == OrderDirection.Descending)
				{
					this.WriteSpace();
					this.WriteKeyword(this.KeyWordWriter.LinqDescending);
				}
			}
		}

		public override void VisitParenthesesExpression(ParenthesesExpression parenthesesExpression)
		{
			this.WriteToken("(");
			this.Visit(parenthesesExpression.Expression);
			this.WriteToken(")");
		}

		public override void VisitPropertyInitializerExpression(PropertyInitializerExpression node)
		{
			this.WritePropertyName(node.Property);
		}

		public override void VisitPropertyReferenceExpression(PropertyReferenceExpression node)
		{
			if (node.Target == null)
			{
				this.WriteReferenceAndNamespaceIfInCollision(node.DeclaringType);
			}
			else
			{
				bool flag = this.IsComplexTarget(node.Target);
				if (flag)
				{
					this.WriteToken("(");
				}
				this.Visit(node.Target);
				if (flag)
				{
					this.WriteToken(")");
				}
			}
			if (node.IsIndexer)
			{
				this.WriteIndexerArguments(node);
				return;
			}
			this.WriteToken(".");
			this.WritePropertyName(node.Property);
		}

		public override void VisitReturnExpression(ReturnExpression node)
		{
			this.WriteKeyword(this.KeyWordWriter.Return);
			if (node.Value != null)
			{
				this.WriteSpace();
				this.Visit(node.Value);
			}
		}

		public override void VisitSelectClause(SelectClause node)
		{
			this.WriteKeyword(this.KeyWordWriter.LinqSelect);
			this.WriteSpace();
			this.Visit(node.Expression);
		}

		public override void VisitShortFormReturnExpression(ShortFormReturnExpression node)
		{
			if (node.Value != null)
			{
				this.Visit(node.Value);
			}
		}

		public override void VisitSizeOfExpression(SizeOfExpression node)
		{
			this.WriteKeyword(this.KeyWordWriter.SizeOf);
			this.WriteToken("(");
			this.WriteReferenceAndNamespaceIfInCollision(node.Type);
			this.WriteToken(")");
		}

		public override void VisitStackAllocExpression(StackAllocExpression node)
		{
			this.WriteKeyword(this.KeyWordWriter.Stackalloc);
			this.WriteSpace();
			this.WriteReference((node.ExpressionType as PointerType).get_ElementType());
			this.WriteToken(this.IndexLeftBracket);
			this.Visit(node.Expression);
			this.WriteToken(this.IndexRightBracket);
		}

		public override void VisitThisReferenceExpression(ThisReferenceExpression node)
		{
			this.WriteKeyword(this.KeyWordWriter.This);
		}

		public override void VisitThrowExpression(ThrowExpression node)
		{
			this.WriteKeyword(this.KeyWordWriter.Throw);
			if (node.Expression != null)
			{
				this.WriteSpace();
				this.Visit(node.Expression);
			}
		}

		public override void VisitTryStatement(TryStatement node)
		{
			this.WriteKeyword(this.KeyWordWriter.Try);
			this.WriteLine();
			this.Visit(node.Try);
			if (node.CatchClauses.Count != 0)
			{
				this.WriteLine();
				this.Visit(node.CatchClauses);
			}
			if (node.Finally != null)
			{
				this.WriteLine();
				this.WriteKeyword(this.KeyWordWriter.Finally);
				this.WriteLine();
				this.Visit(node.Finally);
			}
			this.WriteSpecialEndBlock(this.KeyWordWriter.Try);
		}

		public override void VisitTypeOfExpression(TypeOfExpression node)
		{
			this.WriteKeyword(this.KeyWordWriter.TypeOf);
			this.WriteToken("(");
			this.WriteGenericReference(node.Type);
			this.WriteToken(")");
		}

		public override void VisitTypeReferenceExpression(TypeReferenceExpression node)
		{
			this.WriteReferenceAndNamespaceIfInCollision(node.Type);
		}

		public override void VisitUnaryExpression(UnaryExpression node)
		{
			if (node.Operator == UnaryOperator.AddressDereference)
			{
				this.VisitAddressDereferenceExpression(node);
				return;
			}
			this.Visit(node.Operand);
		}

		public override void VisitVariableDeclarationExpression(VariableDeclarationExpression node)
		{
			this.WriteVariableTypeAndName(node.Variable);
		}

		public override void VisitVariableReferenceExpression(VariableReferenceExpression node)
		{
			this.Write(this.GetVariableName(node.Variable));
		}

		public override void VisitWhereClause(WhereClause node)
		{
			this.WriteKeyword(this.KeyWordWriter.LinqWhere);
			this.WriteSpace();
			this.Visit(node.Condition);
		}

		public override void VisitWhileStatement(WhileStatement node)
		{
			this.WriteKeyword(this.KeyWordWriter.While);
			this.WriteSpace();
			this.WriteSpecialBetweenParenthesis(node.Condition);
			this.WriteLine();
			this.Visit(node.Body);
			this.WriteSpecialEndBlock(this.KeyWordWriter.While);
		}

		protected override void Write(FieldDefinition field)
		{
			if (this.TryWriteEnumField(field))
			{
				return;
			}
			this.WriteFieldDeclaration(field);
			if (this.TypeContext.AssignmentData.ContainsKey(field.get_FullName()) && this.TypeContext.AssignmentData[field.get_FullName()] != null)
			{
				this.WriteSpace();
				this.WriteToken("=");
				this.WriteSpace();
				this.Visit(this.TypeContext.AssignmentData[field.get_FullName()].AssignmentExpression);
			}
			this.WriteEndOfStatement();
		}

		protected override void Write(Statement statement)
		{
			this.Visit(statement);
		}

		protected override void Write(EventDefinition @event)
		{
			bool flag;
			if (@event.get_AddMethod() == null || @event.get_AddMethod().get_Body() != null)
			{
				flag = (@event.get_RemoveMethod() == null ? false : @event.get_RemoveMethod().get_Body() == null);
			}
			else
			{
				flag = true;
			}
			bool flag1 = true;
			if (!flag)
			{
				flag1 = this.TypeContext.AutoImplementedEvents.Contains(@event);
			}
			this.WriteEventDeclaration(@event, flag1);
			if (flag1)
			{
				this.WriteEndOfStatement();
				return;
			}
			int currentPosition = this.formatter.CurrentPosition;
			this.formatter.WriteStartBlock();
			this.WriteLine();
			this.WriteBlock(() => {
				this.WriteEventMethods(@event);
				this.WriteLine();
			}, "");
			this.WriteSpecialEndBlock(this.KeyWordWriter.Event);
			this.currentWritingInfo.MemberDefinitionToFoldingPositionMap[@event] = new OffsetSpan(currentPosition, this.formatter.CurrentPosition - 1);
			this.formatter.WriteEndBlock();
		}

		protected override void Write(PropertyDefinition property)
		{
			if (property.ShouldStaySplit() && !base.MethodContextsMissing)
			{
				this.WriteSplitProperty(property);
				return;
			}
			if (base.Language.SupportsInlineInitializationOfAutoProperties && this.TypeContext.AutoImplementedProperties.Contains(property) && this.TypeContext.AssignmentData.ContainsKey(property.get_FullName()) && this.TypeContext.AssignmentData[property.get_FullName()] != null)
			{
				this.WriteInitializedAutoProperty(property, this.TypeContext.AssignmentData[property.get_FullName()].AssignmentExpression);
				return;
			}
			this.WritePropertyDeclaration(property);
			int currentPosition = this.formatter.CurrentPosition;
			this.formatter.WriteStartBlock();
			this.WriteLine();
			this.WriteBlock(() => {
				this.WritePropertyMethods(property, false);
				this.WriteLine();
			}, "");
			if (this.KeyWordWriter.Property != null)
			{
				this.WriteSpecialEndBlock(this.KeyWordWriter.Property);
			}
			this.currentWritingInfo.MemberDefinitionToFoldingPositionMap[property] = new OffsetSpan(currentPosition, this.formatter.CurrentPosition - 1);
			this.formatter.WriteEndBlock();
		}

		protected override void Write(MethodDefinition method)
		{
			if (base.MethodContext != null && base.MethodContext.IsDestructor)
			{
				this.WriteDestructor(method);
				return;
			}
			this.WriteMethod(method);
		}

		protected override void Write(Expression expression)
		{
			this.Visit(expression);
		}

		protected virtual void WriteAddOn(EventDefinition @event)
		{
			uint num = @event.get_AddMethod().get_MetadataToken().ToUInt32();
			this.membersStack.Push(@event.get_AddMethod());
			int currentPosition = this.formatter.CurrentPosition;
			this.AttributeWriter.WriteMemberAttributesAndNewLine(@event.get_AddMethod(), new String[] { "System.Runtime.CompilerServices.CompilerGeneratedAttribute" }, false);
			base.AddMemberAttributesOffsetSpan(num, currentPosition, this.formatter.CurrentPosition);
			int currentPosition1 = this.formatter.CurrentPosition;
			int num1 = this.formatter.CurrentPosition;
			this.WriteMoreRestrictiveMethodVisibility(@event.get_AddMethod(), @event.get_RemoveMethod());
			int currentPosition2 = this.formatter.CurrentPosition;
			this.WriteKeyword(this.KeyWordWriter.AddOn);
			this.WriteEventAddOnParameters(@event);
			int num2 = this.formatter.CurrentPosition - 1;
			this.currentWritingInfo.MemberDeclarationToCodePostionMap[@event.get_AddMethod()] = new OffsetSpan(currentPosition2, num2);
			this.WriteLine();
			this.Write(base.GetStatement(@event.get_AddMethod()));
			this.WriteSpecialEndBlock(this.KeyWordWriter.AddOn);
			this.currentWritingInfo.MemberDefinitionToFoldingPositionMap.Add(@event.get_AddMethod(), new OffsetSpan(currentPosition1, this.formatter.CurrentPosition - 1));
			this.currentWritingInfo.MemberTokenToDecompiledCodeMap.Add(num, new OffsetSpan(num1, this.formatter.CurrentPosition - 1));
			this.membersStack.Pop();
		}

		protected void WriteAndMapParameterToCode(Action write, int index)
		{
			int currentPosition = this.formatter.CurrentPosition;
			write();
			IMemberDefinition memberDefinition = this.membersStack.Peek();
			OffsetSpan offsetSpan = new OffsetSpan(currentPosition, this.formatter.CurrentPosition);
			this.currentWritingInfo.CodeMappingInfo.Add(memberDefinition, index, offsetSpan);
		}

		protected void WriteAndMapVariableToCode(Action write, VariableDefinition variable)
		{
			int currentPosition = this.formatter.CurrentPosition;
			write();
			OffsetSpan offsetSpan = new OffsetSpan(currentPosition, this.formatter.CurrentPosition);
			try
			{
				this.currentWritingInfo.CodeMappingInfo.Add(variable, offsetSpan);
			}
			catch (ArgumentException argumentException)
			{
				base.OnExceptionThrown(argumentException);
			}
		}

		protected virtual void WriteArrayDimensions(ExpressionCollection dimensions, TypeReference arrayType, bool isInitializerPresent)
		{
			ArrayType arrayType1 = null;
			List<int> nums = new List<int>();
			for (TypeReference i = arrayType; i is ArrayType; i = arrayType1.get_ElementType())
			{
				arrayType1 = i as ArrayType;
				nums.Add(arrayType1.get_Dimensions().get_Count());
			}
			this.WriteToken(this.IndexLeftBracket);
			for (int j = 0; j < dimensions.Count; j++)
			{
				if (j > 0)
				{
					this.WriteToken(",");
					if (!isInitializerPresent)
					{
						this.WriteToken(" ");
					}
				}
				if (!isInitializerPresent)
				{
					this.Visit(dimensions[j]);
				}
			}
			this.WriteToken(this.IndexRightBracket);
			foreach (int num in nums)
			{
				this.WriteToken(this.IndexLeftBracket);
				for (int k = 1; k < num; k++)
				{
					this.WriteToken(",");
				}
				this.WriteToken(this.IndexRightBracket);
			}
		}

		protected override void WriteAttributes(IMemberDefinition member, IEnumerable<string> ignoredAttributes = null)
		{
			MethodDefinition methodDefinition = member as MethodDefinition;
			if (methodDefinition != null && methodDefinition.IsAsync())
			{
				string[] strArray = new String[] { "System.Diagnostics.DebuggerStepThroughAttribute", "System.Runtime.CompilerServices.AsyncStateMachineAttribute" };
				if (ignoredAttributes != null)
				{
					ignoredAttributes = new List<string>(ignoredAttributes);
					((List<string>)ignoredAttributes).AddRange(strArray);
				}
				else
				{
					ignoredAttributes = new List<string>(strArray);
				}
			}
			this.AttributeWriter.WriteMemberAttributesAndNewLine(member, ignoredAttributes, (this.TypeContext.CurrentType != member ? false : this.TypeContext.IsWinRTImplementation));
		}

		protected virtual void WriteBaseConstructorInvokation(MethodInvocationExpression baseConstructorInvokation)
		{
		}

		protected virtual void WriteBaseTypeInheritColon()
		{
		}

		protected void WriteBetweenParenthesis(Expression expression)
		{
			this.WriteToken("(");
			this.Visit(expression);
			this.WriteToken(")");
		}

		public void WriteBitwiseOr()
		{
			this.WriteKeyword(this.ToString(BinaryOperator.BitwiseOr, false));
		}

		protected virtual void WriteBlock(Action action, string label)
		{
		}

		protected override void WriteBodyInternal(IMemberDefinition member)
		{
			this.membersStack.Push(member);
			if (member is MethodDefinition)
			{
				MethodDefinition methodDefinition = (MethodDefinition)member;
				if (methodDefinition.get_Body() != null)
				{
					Statement statement = base.GetStatement(methodDefinition);
					if (base.MethodContext.Method.get_IsConstructor() && !base.MethodContext.Method.get_IsStatic() && base.MethodContext.CtorInvokeExpression != null && !this.RemoveBaseConstructorInvocation)
					{
						this.WriteBaseConstructorInvokation(base.MethodContext.CtorInvokeExpression);
					}
					this.Write(statement);
				}
				else
				{
					this.WriteEndOfStatement();
				}
			}
			else if (member is PropertyDefinition)
			{
				this.WritePropertyMethods((PropertyDefinition)member, false);
			}
			else if (member is EventDefinition)
			{
				EventDefinition eventDefinition = (EventDefinition)member;
				if (eventDefinition.get_AddMethod() != null && eventDefinition.get_AddMethod().get_Body() == null || eventDefinition.get_RemoveMethod() != null && eventDefinition.get_RemoveMethod().get_Body() == null)
				{
					return;
				}
				this.WriteEventMethods(eventDefinition);
			}
			this.membersStack.Pop();
		}

		protected void WriteCodeNodeLabel(ICodeNode node)
		{
			if (node is Statement && node.CodeNodeType != CodeNodeType.BlockStatement)
			{
				Statement statement = node as Statement;
				this.WriteLabel(statement.Label);
				if (node.CodeNodeType != CodeNodeType.EmptyStatement && statement.Label != "")
				{
					this.WriteLine();
				}
			}
		}

		protected virtual void WriteConstructorGenericConstraint()
		{
			this.WriteKeyword(this.KeyWordWriter.New);
		}

		private void WriteConstructorInvocation(ObjectCreationExpression node)
		{
			if (node.Constructor.get_DeclaringType() is TypeSpecification)
			{
				GenericInstanceType declaringType = node.Constructor.get_DeclaringType() as GenericInstanceType;
				if (declaringType != null && this.SupportsSpecialNullable && declaringType.GetFriendlyFullName(base.Language).IndexOf("System.Nullable<") == 0 && declaringType.get_GenericArguments().get_Item(0).get_IsValueType())
				{
					TypeReference item = declaringType.get_GenericArguments().get_Item(0);
					if (declaringType.get_PostionToArgument().ContainsKey(0))
					{
						item = declaringType.get_PostionToArgument()[0];
					}
					this.WriteReferenceAndNamespaceIfInCollision(item);
					this.WriteToken("?");
					return;
				}
			}
			if (node.Constructor.get_DeclaringType().get_Namespace() == "System")
			{
				this.WriteReferenceAndNamespaceIfInCollision(node.Constructor.get_DeclaringType());
				return;
			}
			if (node.Constructor.get_DeclaringType().get_DeclaringType() == null)
			{
				bool flag = this.IsTypeNameInCollision(node.Constructor.get_DeclaringType().get_Name());
				this.WriteNamespace(node.Constructor.get_DeclaringType().GetElementType(), flag);
				this.WriteConstructorNameAndGenericArguments(node, true, 0);
				return;
			}
			TypeReference typeReference = node.Constructor.get_DeclaringType().get_DeclaringType();
			if (node.Constructor.get_DeclaringType().get_IsGenericInstance())
			{
				GenericInstanceType genericInstanceType = node.Constructor.get_DeclaringType() as GenericInstanceType;
				if (typeReference.get_HasGenericParameters())
				{
					GenericInstanceType genericInstanceType1 = new GenericInstanceType(typeReference);
					Mono.Collections.Generic.Collection<TypeReference> collection = new Mono.Collections.Generic.Collection<TypeReference>(genericInstanceType.get_GenericArguments());
					Mono.Collections.Generic.Collection<TypeReference> collection1 = new Mono.Collections.Generic.Collection<TypeReference>(genericInstanceType1.get_GenericArguments());
					int count = typeReference.get_GenericParameters().get_Count();
					for (int i = 0; i < count; i++)
					{
						genericInstanceType1.AddGenericArgument(genericInstanceType.get_GenericArguments().get_Item(i));
						genericInstanceType1.get_GenericArguments().Add(genericInstanceType.get_GenericArguments().get_Item(i));
					}
					this.WriteReferenceAndNamespaceIfInCollision(genericInstanceType1);
					this.Write(".");
					if (genericInstanceType.get_GenericArguments().get_Count() - count <= 0)
					{
						this.WriteConstructorNameAndGenericArguments(node, false, 0);
					}
					else
					{
						this.WriteConstructorNameAndGenericArguments(node, true, count);
					}
					genericInstanceType.get_GenericArguments().Clear();
					genericInstanceType.get_GenericArguments().AddRange(collection);
					genericInstanceType1.get_GenericArguments().Clear();
					genericInstanceType1.get_GenericArguments().AddRange(collection1);
					return;
				}
			}
			this.WriteReferenceAndNamespaceIfInCollision(typeReference);
			this.Write(".");
			this.WriteConstructorNameAndGenericArguments(node, true, 0);
		}

		private void WriteConstructorName(MethodDefinition method)
		{
			string typeName = this.GetTypeName(method.get_DeclaringType());
			if (this.KeyWordWriter.Constructor != null)
			{
				if (this.KeyWordWriter.Sub != null)
				{
					this.WriteKeyword(this.KeyWordWriter.Sub);
					this.WriteSpace();
				}
				typeName = this.KeyWordWriter.Constructor;
			}
			int currentPosition = this.formatter.CurrentPosition;
			this.WriteReference(typeName, method);
			int num = this.formatter.CurrentPosition - 1;
			this.currentWritingInfo.MemberDeclarationToCodePostionMap[method] = new OffsetSpan(currentPosition, num);
		}

		private void WriteConstructorNameAndGenericArguments(ObjectCreationExpression node, bool writeGenericArguments = true, int startArgumentIndex = 0)
		{
			string typeName = this.GetTypeName(node.Constructor.get_DeclaringType());
			this.WriteReference(typeName, node.Constructor);
			if (writeGenericArguments && node.ExpressionType.get_IsGenericInstance())
			{
				this.WriteToken(this.GenericLeftBracket);
				Mono.Collections.Generic.Collection<TypeReference> genericArguments = (node.ExpressionType as GenericInstanceType).get_GenericArguments();
				for (int i = startArgumentIndex; i < genericArguments.get_Count(); i++)
				{
					if (i > startArgumentIndex)
					{
						this.WriteToken(",");
						this.WriteSpace();
					}
					this.WriteReferenceAndNamespaceIfInCollision(genericArguments.get_Item(i));
				}
				this.WriteToken(this.GenericRightBracket);
			}
		}

		protected override void WriteDelegate(TypeDefinition delegateDefinition)
		{
			this.WriteTypeVisiblity(delegateDefinition);
			this.WriteSpace();
			this.WriteKeyword(this.KeyWordWriter.Delegate);
			this.WriteSpace();
			MethodDefinition item = delegateDefinition.get_Methods().get_Item(0);
			for (int i = 1; i < delegateDefinition.get_Methods().get_Count() && item.get_Name() != "Invoke"; i++)
			{
				item = delegateDefinition.get_Methods().get_Item(i);
			}
			if (this.KeyWordWriter.Sub == null || this.KeyWordWriter.Function == null)
			{
				this.WriteMethodReturnType(item);
				this.WriteSpace();
			}
			else
			{
				this.WriteKeyword(this.GetMethodKeyWord(item));
				this.WriteSpace();
			}
			this.WriteGenericName(delegateDefinition);
			this.WriteToken("(");
			this.WriteParameters(item);
			this.WriteToken(")");
			this.PostWriteMethodReturnType(item);
			if (delegateDefinition.get_HasGenericParameters())
			{
				this.PostWriteGenericParametersConstraints(delegateDefinition);
			}
			this.WriteEndOfStatement();
		}

		protected void WriteDelegateArgument(ObjectCreationExpression node)
		{
			if (node.Arguments[0].CodeNodeType == CodeNodeType.LiteralExpression && (node.Arguments[0] as LiteralExpression).Value == null || !(node.Arguments[1] is MethodReferenceExpression) || (node.Arguments[1] as MethodReferenceExpression).Target != null)
			{
				this.Write(node.Arguments[1]);
				return;
			}
			this.Write(node.Arguments[0]);
			this.WriteToken(".");
			this.WriteMethodReference(node.Arguments[1] as MethodReferenceExpression);
		}

		protected virtual void WriteDelegateCreation(ObjectCreationExpression node)
		{
			this.WriteKeyword(this.KeyWordWriter.New);
			this.WriteSpace();
			this.WriteReferenceAndNamespaceIfInCollision(node.ExpressionType);
			this.WriteToken("(");
			this.WriteDelegateArgument(node);
			this.WriteToken(")");
		}

		protected abstract void WriteDestructor(MethodDefinition method);

		private void WriteDoubleConstantValue(IMemberDefinition currentMember, double value, string fieldName)
		{
			TypeDefinition typeDefinition = null;
			if (currentMember != null)
			{
				typeDefinition = (!(currentMember is TypeDefinition) ? currentMember.get_DeclaringType().get_Module().get_TypeSystem().get_Double().Resolve() : (currentMember as TypeDefinition).get_Module().get_TypeSystem().get_Double().Resolve());
			}
			if (currentMember.get_FullName() == fieldName)
			{
				if (Double.IsInfinity(value) || Double.IsNaN(value))
				{
					this.WriteSpecialDoubleConstants(value, currentMember);
					return;
				}
				this.WriteLiteral(value.ToString("R", CultureInfo.InvariantCulture));
				return;
			}
			FieldDefinition fieldDefinition = null;
			if (typeDefinition != null)
			{
				fieldDefinition = typeDefinition.get_Fields().First<FieldDefinition>((FieldDefinition x) => x.get_FullName() == fieldName);
				this.WriteReferenceAndNamespaceIfInCollision(typeDefinition);
				this.WriteToken(".");
			}
			if (fieldDefinition != null)
			{
				this.WriteReference(fieldDefinition.get_Name(), fieldDefinition);
				return;
			}
			this.WriteToken(fieldName.Substring(fieldName.IndexOf("::") + 2));
		}

		private void WriteDoubleInfinity(IMemberDefinition infinityMember, double value)
		{
			FieldDefinition fieldDefinition = infinityMember.get_DeclaringType().get_Fields().First<FieldDefinition>((FieldDefinition x) => x.get_FullName() == "System.Double System.Double::Epsilon");
			if (!Double.IsPositiveInfinity(value))
			{
				this.WriteLiteral("-1");
			}
			else
			{
				this.WriteLiteral("1");
			}
			this.WriteSpace();
			this.Write("/");
			this.WriteSpace();
			this.WriteReference("Epsilon", fieldDefinition);
		}

		private void WriteDoubleLiteral(object value)
		{
			double num = (Double)value;
			IMemberDefinition memberDefinition = null;
			if (this.membersStack.Count > 0)
			{
				memberDefinition = this.membersStack.Peek();
			}
			if (Double.IsPositiveInfinity(num))
			{
				this.WriteDoubleConstantValue(memberDefinition, num, "System.Double System.Double::PositiveInfinity");
				return;
			}
			if (Double.IsNegativeInfinity(num))
			{
				this.WriteDoubleConstantValue(memberDefinition, num, "System.Double System.Double::NegativeInfinity");
				return;
			}
			if (Double.IsNaN(num))
			{
				this.WriteDoubleConstantValue(memberDefinition, num, "System.Double System.Double::NaN");
				return;
			}
			if (MaxValue == num)
			{
				this.WriteDoubleConstantValue(memberDefinition, num, "System.Double System.Double::MaxValue");
				return;
			}
			if (MinValue == num)
			{
				this.WriteDoubleConstantValue(memberDefinition, num, "System.Double System.Double::MinValue");
				return;
			}
			this.WriteLiteral(num.ToString(CultureInfo.InvariantCulture));
		}

		private void WriteDoubleNan(IMemberDefinition nanMember)
		{
			FieldDefinition fieldDefinition = nanMember.get_DeclaringType().get_Fields().First<FieldDefinition>((FieldDefinition x) => x.get_FullName() == "System.Double System.Double::PositiveInfinity");
			this.WriteReference("PositiveInfinity", fieldDefinition);
			this.WriteSpace();
			this.Write("/");
			this.WriteSpace();
			this.WriteReference("PositiveInfinity", fieldDefinition);
		}

		protected virtual void WriteEmptyMethodEndOfStatement(MethodDefinition method)
		{
			this.WriteEndOfStatement();
		}

		protected virtual void WriteEnumBaseTypeInheritColon()
		{
			this.WriteBaseTypeInheritColon();
		}

		protected virtual void WriteEscapeCharLiteral(char p)
		{
		}

		protected virtual void WriteEventAddOnParameters(EventDefinition @event)
		{
		}

		protected override void WriteEventDeclaration(EventDefinition @event)
		{
			this.WriteEventDeclaration(@event, true);
		}

		private void WriteEventDeclaration(EventDefinition @event, bool isAutoGenerated)
		{
			MethodDefinition moreVisibleMethod = @event.get_AddMethod().GetMoreVisibleMethod(@event.get_RemoveMethod());
			this.WriteMethodVisibilityAndSpace(moreVisibleMethod);
			if (this.TypeSupportsExplicitStaticMembers(moreVisibleMethod.get_DeclaringType()) && moreVisibleMethod.get_IsStatic())
			{
				this.WriteKeyword(this.KeyWordWriter.Static);
				this.WriteSpace();
			}
			if (!isAutoGenerated && this.KeyWordWriter.Custom != null)
			{
				this.WriteKeyword(this.KeyWordWriter.Custom);
				this.WriteSpace();
			}
			if (@event.IsVirtual() && !@event.get_DeclaringType().get_IsInterface() && this.WriteEventKeywords(@event))
			{
				this.WriteSpace();
			}
			this.WriteKeyword(this.KeyWordWriter.Event);
			this.WriteSpace();
			this.WriteEventTypeAndName(@event);
			this.WriteEventInterfaceImplementations(@event);
		}

		protected virtual void WriteEventInterfaceImplementations(EventDefinition @event)
		{
		}

		private bool WriteEventKeywords(EventDefinition @event)
		{
			if (!@event.IsNewSlot())
			{
				if (@event.IsFinal())
				{
					this.WriteKeyword(this.KeyWordWriter.SealedMethod);
					this.WriteSpace();
				}
				this.WriteKeyword(this.KeyWordWriter.Override);
				return true;
			}
			if (@event.IsAbstract())
			{
				this.WriteKeyword(this.KeyWordWriter.AbstractMember);
				return true;
			}
			if (@event.IsFinal())
			{
				return false;
			}
			this.WriteKeyword(this.KeyWordWriter.Virtual);
			return true;
		}

		protected void WriteEventMethods(EventDefinition eventDef)
		{
			if (eventDef.get_AddMethod() != null)
			{
				this.WriteAddOn(eventDef);
			}
			if (eventDef.get_RemoveMethod() != null)
			{
				if (eventDef.get_AddMethod() != null)
				{
					this.WriteLine();
				}
				this.WriteRemoveOn(eventDef);
			}
			eventDef.get_InvokeMethod();
		}

		protected virtual void WriteEventRemoveOnParameters(EventDefinition @event)
		{
		}

		protected virtual void WriteEventTypeAndName(EventDefinition @event)
		{
			this.WriteTypeAndName(@event.get_EventType(), @event.get_Name(), @event);
		}

		private void WriteExternAndSpaceIfNecessary(MethodDefinition method)
		{
			if (method.IsExtern() && this.KeyWordWriter.Extern != null)
			{
				this.WriteKeyword(this.KeyWordWriter.Extern);
				this.WriteSpace();
			}
		}

		protected virtual void WriteFieldDeclaration(FieldDefinition field)
		{
			if (!this.TypeContext.BackingFieldToNameMap.ContainsKey(field) && this.ModuleContext.RenamedMembers.Contains(field.get_MetadataToken().ToUInt32()))
			{
				this.WriteComment(this.GetOriginalFieldName(field));
				this.WriteLine();
			}
			this.WriteMemberVisibility(field);
			this.WriteSpace();
			if (field.get_IsInitOnly())
			{
				this.WriteKeyword(this.KeyWordWriter.ReadOnly);
				this.WriteSpace();
			}
			if (field.get_IsLiteral())
			{
				this.WriteKeyword(this.KeyWordWriter.Const);
				this.WriteSpace();
			}
			else if (this.TypeSupportsExplicitStaticMembers(field.get_DeclaringType()) && field.get_IsStatic())
			{
				this.WriteKeyword(this.KeyWordWriter.Static);
				this.WriteSpace();
			}
			this.WriteFieldTypeAndName(field);
			if (field.get_HasConstant())
			{
				this.WriteSpace();
				this.WriteToken("=");
				this.WriteSpace();
				int currentPosition = this.formatter.CurrentPosition;
				TypeDefinition typeDefinition = field.get_FieldType().Resolve();
				if (typeDefinition == null || !typeDefinition.get_IsEnum())
				{
					this.WriteLiteralInLanguageSyntax(field.get_Constant().get_Value());
				}
				else
				{
					LiteralExpression literalExpression = new LiteralExpression(field.get_Constant().get_Value(), field.get_DeclaringType().get_Module().get_TypeSystem(), null);
					Expression enumExpression = EnumHelper.GetEnumExpression(typeDefinition, literalExpression, field.get_DeclaringType().get_Module().get_TypeSystem());
					this.Write(enumExpression);
				}
				this.currentWritingInfo.CodeMappingInfo.Add(field, new OffsetSpan(currentPosition, this.formatter.CurrentPosition));
			}
		}

		protected virtual void WriteFieldName(FieldReference field)
		{
			this.WriteReference(this.GetFieldName(field), field);
		}

		protected virtual void WriteFieldTypeAndName(FieldDefinition field)
		{
			string fieldName = this.GetFieldName(field);
			this.WriteTypeAndName(field.get_FieldType(), fieldName, field);
		}

		protected virtual void WriteFire(EventDefinition @event)
		{
			uint num = @event.get_InvokeMethod().get_MetadataToken().ToUInt32();
			int currentPosition = this.formatter.CurrentPosition;
			this.AttributeWriter.WriteMemberAttributesAndNewLine(@event.get_InvokeMethod(), new String[] { "System.Runtime.CompilerServices.CompilerGeneratedAttribute" }, false);
			base.AddMemberAttributesOffsetSpan(num, currentPosition, this.formatter.CurrentPosition);
			int currentPosition1 = this.formatter.CurrentPosition;
			this.WriteMethodVisibilityAndSpace(@event.get_InvokeMethod());
			int num1 = this.formatter.CurrentPosition;
			this.WriteKeyword(this.KeyWordWriter.Fire);
			int currentPosition2 = this.formatter.CurrentPosition;
			this.currentWritingInfo.MemberDeclarationToCodePostionMap[@event.get_InvokeMethod()] = new OffsetSpan(num1, currentPosition2);
			this.WriteToken("(");
			this.WriteParameters(@event.get_InvokeMethod());
			this.WriteToken(")");
			this.WriteLine();
			this.Write(base.GetStatement(@event.get_InvokeMethod()));
			this.WriteSpecialEndBlock(this.KeyWordWriter.Fire);
			this.currentWritingInfo.MemberTokenToDecompiledCodeMap.Add(num, new OffsetSpan(currentPosition1, this.formatter.CurrentPosition - 1));
		}

		private void WriteFloatInfinity(IMemberDefinition infinityMember, double value)
		{
			FieldDefinition fieldDefinition = infinityMember.get_DeclaringType().get_Fields().First<FieldDefinition>((FieldDefinition x) => x.get_FullName() == "System.Single System.Single::Epsilon");
			if (!Double.IsPositiveInfinity(value))
			{
				this.WriteLiteral("-1");
			}
			else
			{
				this.WriteLiteral("1");
			}
			this.WriteSpace();
			this.Write("/");
			this.WriteSpace();
			this.WriteReference("Epsilon", fieldDefinition);
		}

		private void WriteFloatInfinityValue(IMemberDefinition currentMember, float value, string fieldName)
		{
			TypeDefinition typeDefinition = null;
			if (currentMember != null)
			{
				typeDefinition = (!(currentMember is TypeDefinition) ? currentMember.get_DeclaringType().get_Module().get_TypeSystem().get_Single().Resolve() : (currentMember as TypeDefinition).get_Module().get_TypeSystem().get_Single().Resolve());
			}
			if (currentMember.get_FullName() == fieldName)
			{
				if (Single.IsInfinity(value) || Single.IsNaN(value))
				{
					this.WriteSpecialFloatValue(value, currentMember);
					return;
				}
				this.WriteLiteral(String.Concat(value.ToString("R", CultureInfo.InvariantCulture), base.Language.FloatingLiteralsConstant));
				return;
			}
			FieldDefinition fieldDefinition = null;
			if (typeDefinition != null)
			{
				fieldDefinition = typeDefinition.get_Fields().First<FieldDefinition>((FieldDefinition x) => x.get_FullName() == fieldName);
				this.WriteReferenceAndNamespaceIfInCollision(typeDefinition);
				this.WriteToken(".");
			}
			if (fieldDefinition != null)
			{
				this.WriteReference(fieldDefinition.get_Name(), fieldDefinition);
				return;
			}
			this.WriteToken(fieldName.Substring(fieldName.IndexOf("::") + 2));
		}

		private void WriteFloatLiteral(object value)
		{
			float single = (Single)value;
			IMemberDefinition memberDefinition = null;
			if (this.membersStack.Count > 0)
			{
				memberDefinition = this.membersStack.Peek();
			}
			if (Single.IsPositiveInfinity(single))
			{
				this.WriteFloatInfinityValue(memberDefinition, single, "System.Single System.Single::PositiveInfinity");
				return;
			}
			if (Single.IsNegativeInfinity(single))
			{
				this.WriteFloatInfinityValue(memberDefinition, single, "System.Single System.Single::NegativeInfinity");
				return;
			}
			if (MinValue == single)
			{
				this.WriteFloatInfinityValue(memberDefinition, single, "System.Single System.Single::MinValue");
				return;
			}
			if (MaxValue == single)
			{
				this.WriteFloatInfinityValue(memberDefinition, single, "System.Single System.Single::MaxValue");
				return;
			}
			if (Single.IsNaN(single))
			{
				this.WriteFloatInfinityValue(memberDefinition, single, "System.Single System.Single::NaN");
				return;
			}
			this.WriteLiteral(String.Concat(single.ToString("R", CultureInfo.InvariantCulture), base.Language.FloatingLiteralsConstant));
		}

		private void WriteFloatNan(IMemberDefinition nanMember)
		{
			FieldDefinition fieldDefinition = nanMember.get_DeclaringType().get_Fields().First<FieldDefinition>((FieldDefinition x) => x.get_FullName() == "System.Single System.Single::PositiveInfinity");
			this.WriteReference("PositiveInfinity", fieldDefinition);
			this.WriteSpace();
			this.Write("/");
			this.WriteSpace();
			this.WriteReference("PositiveInfinity", fieldDefinition);
		}

		protected void WriteGenericInstance(GenericInstanceType genericInstance, int startingArgument = 0)
		{
			this.WriteReference(this.GetTypeName(genericInstance), genericInstance.get_ElementType());
			this.WriteToken(this.GenericLeftBracket);
			for (int i = startingArgument; i < genericInstance.get_GenericArguments().get_Count(); i++)
			{
				if (i > startingArgument)
				{
					this.WriteToken(",");
					this.WriteSpace();
				}
				TypeReference item = genericInstance.get_GenericArguments().get_Item(i);
				if (genericInstance.get_PostionToArgument().ContainsKey(i))
				{
					item = genericInstance.get_PostionToArgument()[i];
				}
				this.WriteReferenceAndNamespaceIfInCollision(item);
			}
			this.WriteToken(this.GenericRightBracket);
		}

		protected virtual void WriteGenericInstanceMethod(GenericInstanceMethod genericMethod)
		{
			this.WriteGenericInstanceMethodWithArguments(genericMethod, genericMethod.get_GenericArguments());
		}

		protected void WriteGenericInstanceMethodWithArguments(GenericInstanceMethod genericMethod, Mono.Collections.Generic.Collection<TypeReference> genericArguments)
		{
			MethodReference elementMethod = genericMethod.get_ElementMethod();
			this.WriteReference(this.GetMethodName(elementMethod), elementMethod);
			if (genericMethod.HasAnonymousArgument())
			{
				return;
			}
			if (genericArguments.get_Count() == 0)
			{
				return;
			}
			this.WriteToken(this.GenericLeftBracket);
			for (int i = 0; i < genericArguments.get_Count(); i++)
			{
				if (i > 0)
				{
					this.WriteToken(",");
					this.WriteSpace();
				}
				this.WriteReferenceAndNamespaceIfInCollision(genericArguments.get_Item(i));
			}
			this.WriteToken(this.GenericRightBracket);
		}

		private void WriteGenericInstanceTypeArguments(IGenericInstance genericInstance)
		{
			this.WriteToken(this.GenericLeftBracket);
			for (int i = 0; i < genericInstance.get_GenericArguments().get_Count(); i++)
			{
				if (i > 0)
				{
					this.WriteToken(",");
					this.WriteSpace();
				}
				TypeReference item = genericInstance.get_GenericArguments().get_Item(i);
				if (genericInstance.get_PostionToArgument().ContainsKey(i))
				{
					item = genericInstance.get_PostionToArgument()[i];
				}
				this.WriteTypeReferenceNavigationName(item);
			}
			this.WriteToken(this.GenericRightBracket);
		}

		protected virtual void WriteGenericName(IGenericDefinition genericDefinition)
		{
			IGenericParameterProvider genericParameterProvider;
			string typeName;
			if (!(genericDefinition is MethodDefinition))
			{
				typeName = this.GetTypeName(genericDefinition as TypeDefinition);
				genericParameterProvider = genericDefinition as TypeDefinition;
			}
			else
			{
				typeName = this.GetMethodName(genericDefinition as MethodDefinition);
				genericParameterProvider = genericDefinition as MethodDefinition;
			}
			this.WriteReference(typeName, genericDefinition);
			BaseImperativeLanguageWriter baseImperativeLanguageWriter = this;
			this.WriteGenericParametersToDefinition(genericParameterProvider, new Action<GenericParameter>(baseImperativeLanguageWriter.WriteGenericParameterConstraints), true);
		}

		protected virtual void WriteGenericParameterConstraints(GenericParameter parameter)
		{
		}

		private void WriteGenericParametersToDefinition(IGenericParameterProvider genericDefinition, Action<GenericParameter> writeParamConstraints, bool renameInvalidCharacters)
		{
			int count = 0;
			if (genericDefinition is TypeDefinition)
			{
				TypeDefinition declaringType = (genericDefinition as TypeDefinition).get_DeclaringType();
				if (declaringType != null && declaringType.get_HasGenericParameters())
				{
					count = declaringType.get_GenericParameters().get_Count();
				}
			}
			if (count < genericDefinition.get_GenericParameters().get_Count())
			{
				this.WriteToken(this.GenericLeftBracket);
				while (count < genericDefinition.get_GenericParameters().get_Count())
				{
					GenericParameter item = genericDefinition.get_GenericParameters().get_Item(count);
					if (item.get_IsCovariant())
					{
						this.WriteKeyword(this.KeyWordWriter.Covariant);
						this.WriteSpace();
					}
					if (item.get_IsContravariant())
					{
						this.WriteKeyword(this.KeyWordWriter.Contravariant);
						this.WriteSpace();
					}
					this.WriteReference((renameInvalidCharacters ? GenericHelper.ReplaceInvalidCharactersName(base.Language, item.get_Name()) : item.get_Name()), null);
					if ((item.get_HasConstraints() || item.get_HasDefaultConstructorConstraint() || item.get_HasReferenceTypeConstraint() || item.get_HasNotNullableValueTypeConstraint()) && writeParamConstraints != null)
					{
						writeParamConstraints(item);
					}
					if (count != genericDefinition.get_GenericParameters().get_Count() - 1)
					{
						this.WriteToken(",");
						this.WriteSpace();
					}
					count++;
				}
				this.WriteToken(this.GenericRightBracket);
			}
		}

		public virtual void WriteGenericReference(TypeReference type)
		{
			if (type.get_IsNested() && !type.get_IsGenericParameter())
			{
				this.WriteGenericReference(type.get_DeclaringType());
				this.WriteToken(".");
			}
			if (type is TypeSpecification)
			{
				this.WriteTypeSpecification(type as TypeSpecification, 0);
				return;
			}
			if (!type.get_IsNested())
			{
				this.WriteNamespaceIfTypeInCollision(type);
			}
			if (type.get_IsGenericParameter())
			{
				this.WriteReference(this.ToEscapedTypeString(type), null);
				return;
			}
			TypeDefinition typeDefinition = type.Resolve();
			if (typeDefinition == null || !typeDefinition.get_HasGenericParameters())
			{
				if (type.get_Namespace() != "System")
				{
					this.WriteReference(this.GetTypeName(type), type);
					return;
				}
				this.WriteReference(this.ToEscapedTypeString(type), type);
				return;
			}
			this.WriteReference(this.GetTypeName(typeDefinition), typeDefinition);
			this.WriteToken(this.GenericLeftBracket);
			for (int i = 1; i < typeDefinition.get_GenericParameters().get_Count(); i++)
			{
				this.WriteToken(",");
			}
			this.WriteToken(this.GenericRightBracket);
		}

		protected void WriteGetMethod(PropertyDefinition property, bool isAutoImplemented)
		{
			uint num = property.get_GetMethod().get_MetadataToken().ToUInt32();
			this.membersStack.Push(property.get_GetMethod());
			int currentPosition = this.formatter.CurrentPosition;
			this.AttributeWriter.WriteMemberAttributesAndNewLine(property.get_GetMethod(), new String[] { "System.Runtime.CompilerServices.CompilerGeneratedAttribute" }, false);
			base.AddMemberAttributesOffsetSpan(num, currentPosition, this.formatter.CurrentPosition);
			int currentPosition1 = this.formatter.CurrentPosition;
			int num1 = this.formatter.CurrentPosition;
			this.WriteMoreRestrictiveMethodVisibility(property.get_GetMethod(), property.get_SetMethod());
			int currentPosition2 = this.formatter.CurrentPosition;
			this.WriteKeyword(this.KeyWordWriter.Get);
			int num2 = this.formatter.CurrentPosition - 1;
			this.currentWritingInfo.MemberDeclarationToCodePostionMap[property.get_GetMethod()] = new OffsetSpan(currentPosition2, num2);
			if (property.get_GetMethod().get_Body() == null || this.SupportsAutoProperties & isAutoImplemented)
			{
				this.WriteEndOfStatement();
			}
			else
			{
				this.WriteLine();
				this.Write(base.GetStatement(property.get_GetMethod()));
			}
			this.WriteSpecialEndBlock(this.KeyWordWriter.Get);
			this.currentWritingInfo.MemberDefinitionToFoldingPositionMap.Add(property.get_GetMethod(), new OffsetSpan(currentPosition1, this.formatter.CurrentPosition - 1));
			this.currentWritingInfo.MemberTokenToDecompiledCodeMap.Add(num, new OffsetSpan(num1, this.formatter.CurrentPosition - 1));
			this.membersStack.Pop();
		}

		protected void WriteIndexerArguments(PropertyReferenceExpression node)
		{
			this.WriteToken(this.IndexLeftBracket);
			for (int i = 0; i < node.Arguments.Count; i++)
			{
				Expression item = node.Arguments[i];
				if (i > 0)
				{
					this.WriteToken(",");
					this.WriteSpace();
				}
				this.Write(item);
			}
			this.WriteToken(this.IndexRightBracket);
		}

		protected abstract void WriteIndexerKeywords();

		protected void WriteInheritComma()
		{
			this.WriteToken(",");
			this.WriteSpace();
		}

		private void WriteInitializedAutoProperty(PropertyDefinition property, Expression assignment)
		{
			this.WritePropertyDeclaration(property);
			this.WriteBeginBlock(true);
			this.WriteSpace();
			this.WritePropertyMethods(property, true);
			this.WriteSpace();
			this.WriteEndBlock(property.get_Name());
			this.WriteSpace();
			this.WriteToken("=");
			this.WriteSpace();
			this.Visit(assignment);
			this.WriteEndOfStatement();
		}

		protected virtual void WriteInterfacesInheritColon(TypeDefinition type)
		{
		}

		protected void WriteLabel(string label)
		{
			if (label != "")
			{
				this.Outdent();
				this.Write(label);
				this.WriteToken(":");
				this.Indent();
			}
		}

		public void WriteLiteralInLanguageSyntax(object value)
		{
			if (value == null)
			{
				this.WriteKeyword(this.KeyWordWriter.Null);
				return;
			}
			switch (Type.GetTypeCode(value.GetType()))
			{
				case TypeCode.Boolean:
				{
					this.WriteKeyword(((Boolean)value ? this.KeyWordWriter.True : this.KeyWordWriter.False));
					return;
				}
				case TypeCode.Char:
				{
					this.WriteEscapeCharLiteral((Char)value);
					return;
				}
				case TypeCode.SByte:
				case TypeCode.Byte:
				case TypeCode.Decimal:
				case TypeCode.DateTime:
				case TypeCode.Object | TypeCode.DateTime:
				{
					this.WriteLiteral(value.ToString());
					return;
				}
				case TypeCode.Int16:
				{
					short num = (Int16)value;
					if (num < 0xff || !base.Settings.WriteLargeNumbersInHex)
					{
						this.WriteLiteral(num.ToString());
						return;
					}
					this.WriteLiteral(String.Concat(this.HexValuePrefix, num.ToString("X").ToLowerInvariant()));
					return;
				}
				case TypeCode.UInt16:
				{
					ushort num1 = (UInt16)value;
					if (num1 < 0xff || !base.Settings.WriteLargeNumbersInHex)
					{
						this.WriteLiteral(num1.ToString());
						return;
					}
					this.WriteLiteral(String.Concat(this.HexValuePrefix, num1.ToString("X").ToLowerInvariant()));
					return;
				}
				case TypeCode.Int32:
				{
					int num2 = (Int32)value;
					if (num2 < 0xff || !base.Settings.WriteLargeNumbersInHex)
					{
						this.WriteLiteral(num2.ToString());
						return;
					}
					this.WriteLiteral(String.Concat(this.HexValuePrefix, num2.ToString("X").ToLowerInvariant()));
					return;
				}
				case TypeCode.UInt32:
				{
					uint num3 = (UInt32)value;
					if (num3 < 0xff || !base.Settings.WriteLargeNumbersInHex)
					{
						this.WriteLiteral(num3.ToString());
						return;
					}
					this.WriteLiteral(String.Concat(this.HexValuePrefix, num3.ToString("X").ToLowerInvariant()));
					return;
				}
				case TypeCode.Int64:
				{
					long num4 = (Int64)value;
					if (num4 < (long)0xff || !base.Settings.WriteLargeNumbersInHex)
					{
						this.WriteLiteral(String.Concat(num4.ToString(), "L"));
						return;
					}
					this.WriteLiteral(String.Concat(this.HexValuePrefix, num4.ToString("X").ToLowerInvariant(), "L"));
					return;
				}
				case TypeCode.UInt64:
				{
					ulong num5 = (UInt64)value;
					if (num5 < (long)0xff || !base.Settings.WriteLargeNumbersInHex)
					{
						this.WriteLiteral(String.Concat(num5.ToString(), "L"));
						return;
					}
					this.WriteLiteral(String.Concat(this.HexValuePrefix, num5.ToString("X").ToLowerInvariant(), "L"));
					return;
				}
				case TypeCode.Single:
				{
					this.WriteFloatLiteral(value);
					return;
				}
				case TypeCode.Double:
				{
					this.WriteDoubleLiteral(value);
					return;
				}
				case TypeCode.String:
				{
					this.WriteLiteral("\"");
					this.WriteEscapeLiteral(value.ToString());
					this.WriteLiteral("\"");
					return;
				}
				default:
				{
					this.WriteLiteral(value.ToString());
					return;
				}
			}
		}

		protected string WriteLogicalToken(BinaryOperator logical)
		{
			if (logical == BinaryOperator.LogicalOr)
			{
				return this.ToString(BinaryOperator.LogicalOr, false);
			}
			if (logical != BinaryOperator.LogicalAnd)
			{
				return String.Empty;
			}
			return this.ToString(BinaryOperator.LogicalAnd, false);
		}

		public override void WriteMemberNavigationName(object memberDefinition)
		{
			if (memberDefinition is MethodReference)
			{
				this.WriteMethodReferenceNavigationName(memberDefinition as MethodReference);
				return;
			}
			if (memberDefinition is TypeReference)
			{
				this.WriteTypeReferenceNavigationName(memberDefinition as TypeReference);
				return;
			}
			if (memberDefinition is IMemberDefinition)
			{
				IMemberDefinition memberDefinition1 = memberDefinition as IMemberDefinition;
				string nonGenericName = GenericHelper.GetNonGenericName(memberDefinition1.get_Name());
				if (Utilities.IsExplicitInterfaceImplementataion(memberDefinition1))
				{
					string[] strArray = nonGenericName.Split(new Char[] { '.' });
					StringBuilder stringBuilder = new StringBuilder();
					for (int i = 0; i < (int)strArray.Length; i++)
					{
						if (i > 0)
						{
							stringBuilder.Append('.');
						}
						if (!base.Settings.RenameInvalidMembers)
						{
							stringBuilder.Append(strArray[i]);
						}
						else if (!this.NormalizeNameIfContainingGenericSymbols(strArray[i], stringBuilder))
						{
							stringBuilder.Append(base.Language.ReplaceInvalidCharactersInIdentifier(strArray[i]));
						}
					}
					nonGenericName = stringBuilder.ToString();
				}
				else if (base.Settings.RenameInvalidMembers)
				{
					nonGenericName = base.Language.ReplaceInvalidCharactersInIdentifier(nonGenericName);
				}
				this.formatter.Write(nonGenericName);
				this.formatter.Write(" : ");
				this.WriteTypeReferenceNavigationName(this.GetMemberType(memberDefinition1));
			}
		}

		protected void WriteMemberVisibility(IVisibilityDefinition member)
		{
			if (member.get_IsPrivate())
			{
				this.WriteKeyword(this.KeyWordWriter.Private);
				return;
			}
			if (member.get_IsPublic())
			{
				this.WriteKeyword(this.KeyWordWriter.Public);
				return;
			}
			if (member.get_IsFamily())
			{
				this.WriteKeyword(this.KeyWordWriter.Protected);
				return;
			}
			if (member.get_IsAssembly())
			{
				this.WriteKeyword(this.KeyWordWriter.Internal);
				return;
			}
			if (member.get_IsFamilyOrAssembly() || member.get_IsFamilyAndAssembly())
			{
				this.WriteKeyword(this.KeyWordWriter.Protected);
				this.WriteSpace();
				this.WriteKeyword(this.KeyWordWriter.Internal);
				return;
			}
			if ((!(member is MethodDefinition) || !(member as MethodDefinition).get_IsCompilerControlled()) && (!(member is FieldDefinition) || !(member as FieldDefinition).get_IsCompilerControlled()))
			{
				throw new NotSupportedException();
			}
			this.WriteComment("privatescope");
			this.WriteLine();
			this.WriteKeyword(this.KeyWordWriter.Internal);
		}

		protected void WriteMethod(MethodDefinition method)
		{
			Statement statement;
			this.membersStack.Push(method);
			bool flag = false;
			try
			{
				if (method.get_Body() == null || method.get_Body().get_Instructions().get_Count() <= 0 && !method.get_IsJustDecompileGenerated())
				{
					statement = null;
				}
				else
				{
					statement = base.GetStatement(method);
				}
				Statement statement1 = statement;
				this.WriteMethodDeclaration(method, false);
				if (method.get_Body() == null)
				{
					this.WriteEmptyMethodEndOfStatement(method);
					this.membersStack.Pop();
					return;
				}
				else if (method.get_Body().get_Instructions().get_Count() != 0 || method.get_IsJustDecompileGenerated())
				{
					if (base.MethodContext != null && base.MethodContext.Method.get_IsConstructor() && base.MethodContext.CtorInvokeExpression != null)
					{
						this.WriteBaseConstructorInvokation(base.MethodContext.CtorInvokeExpression);
					}
					bool flag1 = method.IsExtern();
					int currentPosition = 0;
					if (!method.get_IsAbstract() && !flag1)
					{
						currentPosition = this.formatter.CurrentPosition;
						this.formatter.WriteStartBlock();
						flag = true;
					}
					this.WriteLine();
					this.Write(statement1);
					if (this.KeyWordWriter.Sub != null && this.KeyWordWriter.Function != null && this.KeyWordWriter.Operator != null)
					{
						this.WriteSpecialEndBlock(this.GetMethodKeyWord(method));
					}
					if (!method.get_IsAbstract() && !flag1)
					{
						this.currentWritingInfo.MemberDefinitionToFoldingPositionMap[method] = new OffsetSpan(currentPosition, this.formatter.CurrentPosition - 1);
						this.formatter.WriteEndBlock();
					}
				}
				else
				{
					this.WriteBeginBlock(false);
					this.WriteLine();
					this.WriteEndBlock(this.GetMethodName(method));
					this.membersStack.Pop();
					return;
				}
			}
			catch (Exception exception)
			{
				if (flag)
				{
					this.formatter.WriteEndBlock();
				}
				this.membersStack.Pop();
				throw;
			}
			this.membersStack.Pop();
		}

		protected override void WriteMethodDeclaration(MethodDefinition method, bool writeDocumentation = false)
		{
			if (!method.get_IsConstructor() && !method.get_HasOverrides() && this.ModuleContext.RenamedMembers.Contains(method.get_MetadataToken().ToUInt32()))
			{
				this.WriteComment(GenericHelper.GetNonGenericName(this.GetMethodOriginalName(method)));
				this.WriteLine();
			}
			if (!method.get_DeclaringType().get_IsInterface())
			{
				if (!method.get_IsStatic() || !method.get_IsConstructor())
				{
					this.WriteMethodVisibilityAndSpace(method);
				}
				if (method.get_IsVirtual() && this.WriteMethodKeywords(method))
				{
					this.WriteSpace();
				}
				if (this.TypeSupportsExplicitStaticMembers(method.get_DeclaringType()) && method.get_IsStatic())
				{
					this.WriteKeyword(this.KeyWordWriter.Static);
					this.WriteSpace();
				}
				if (!method.get_IsConstructor() && (!method.get_IsVirtual() || method.get_IsNewSlot()) && this.IsMethodHiding(method))
				{
					this.WriteKeyword(this.KeyWordWriter.Hiding);
					this.WriteSpace();
				}
				this.WriteExternAndSpaceIfNecessary(method);
				if (method.get_IsUnsafe() && this.KeyWordWriter.Unsafe != String.Empty)
				{
					this.WriteKeyword(this.KeyWordWriter.Unsafe);
					this.WriteSpace();
				}
				if (this.KeyWordWriter.Async != null && method.IsAsync())
				{
					this.WriteKeyword(this.KeyWordWriter.Async);
					this.WriteSpace();
				}
				if (method.get_IsConstructor())
				{
					this.WriteConstructorName(method);
				}
				else if (!this.TryWriteMethodAsOperator(method))
				{
					this.WriteMethodName(method);
				}
			}
			else
			{
				this.WriteMethodName(method);
			}
			this.WriteToken("(");
			this.WriteParameters(method);
			this.WriteToken(")");
			this.PostWriteMethodReturnType(method);
			if (method.get_HasGenericParameters())
			{
				this.PostWriteGenericParametersConstraints(method);
			}
			if (!method.get_IsGetter() && !method.get_IsSetter())
			{
				this.WriteMethodInterfaceImplementations(method);
			}
		}

		protected virtual void WriteMethodInterfaceImplementations(MethodDefinition method)
		{
		}

		private bool WriteMethodKeywords(MethodDefinition method)
		{
			if (method.get_IsNewSlot())
			{
				if (method.get_IsAbstract())
				{
					this.WriteKeyword(this.KeyWordWriter.AbstractMember);
					return true;
				}
				if (method.get_IsFinal() || method.get_DeclaringType().get_IsSealed())
				{
					return false;
				}
				this.WriteKeyword(this.KeyWordWriter.Virtual);
				return true;
			}
			if (method.get_IsFinal())
			{
				this.WriteKeyword(this.KeyWordWriter.SealedMethod);
				this.WriteSpace();
			}
			this.WriteKeyword(this.KeyWordWriter.Override);
			if (method.get_IsAbstract())
			{
				this.WriteSpace();
				this.WriteKeyword(this.KeyWordWriter.AbstractMember);
			}
			return true;
		}

		private void WriteMethodName(MethodDefinition method)
		{
			if (this.KeyWordWriter.Sub == null || this.KeyWordWriter.Function == null)
			{
				this.WriteMethodReturnType(method);
				this.WriteSpace();
			}
			else
			{
				this.WriteKeyword(this.GetMethodKeyWord(method));
				this.WriteSpace();
			}
			int currentPosition = this.formatter.CurrentPosition;
			this.WriteGenericName(method);
			int num = this.formatter.CurrentPosition - 1;
			this.currentWritingInfo.MemberDeclarationToCodePostionMap[method] = new OffsetSpan(currentPosition, num);
		}

		protected virtual void WriteMethodReference(MethodReferenceExpression methodReferenceExpression)
		{
			MethodReference method = methodReferenceExpression.Method;
			if (method is GenericInstanceMethod)
			{
				this.WriteGenericInstanceMethod(method as GenericInstanceMethod);
				return;
			}
			this.WriteReference(this.GetMethodName(method), method);
		}

		private void WriteMethodReferenceNavigationName(MethodReference method)
		{
			string name = method.get_Name();
			bool flag = (!method.get_IsConstructor() ? false : (object)method.get_DeclaringType() != (object)null);
			if (flag)
			{
				name = method.get_DeclaringType().get_Name();
			}
			string[] strArray = GenericHelper.GetNonGenericName(name).Split(new Char[] { '.' });
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < (int)strArray.Length; i++)
			{
				if (i > 0)
				{
					stringBuilder.Append('.');
				}
				if (!base.Settings.RenameInvalidMembers)
				{
					stringBuilder.Append(strArray[i]);
				}
				else if (!this.NormalizeNameIfContainingGenericSymbols(strArray[i], stringBuilder))
				{
					stringBuilder.Append(base.Language.ReplaceInvalidCharactersInIdentifier(strArray[i]));
				}
			}
			this.formatter.Write(stringBuilder.ToString());
			if (method is GenericInstanceMethod)
			{
				this.WriteGenericInstanceTypeArguments((GenericInstanceMethod)method);
			}
			else if (method.get_HasGenericParameters())
			{
				this.WriteGenericParametersToDefinition(method, null, base.Settings.RenameInvalidMembers);
			}
			this.formatter.Write("(");
			if (method.get_HasParameters())
			{
				for (int j = 0; j < method.get_Parameters().get_Count(); j++)
				{
					if (j > 0)
					{
						this.formatter.Write(", ");
					}
					ParameterDefinition item = method.get_Parameters().get_Item(j);
					this.WriteTypeReferenceNavigationName(item.get_ParameterType());
				}
			}
			this.formatter.Write(")");
			if (!flag)
			{
				this.formatter.Write(" : ");
				this.WriteTypeReferenceNavigationName(method.get_FixedReturnType());
			}
		}

		protected virtual void WriteMethodReturnType(MethodDefinition method)
		{
			this.WriteReferenceAndNamespaceIfInCollision(method.get_ReturnType());
		}

		protected void WriteMethodTarget(Expression expression)
		{
			bool flag = this.IsComplexTarget(expression);
			if (flag)
			{
				this.WriteToken("(");
			}
			this.Visit(expression);
			if (flag)
			{
				this.WriteToken(")");
			}
			this.WriteToken(".");
		}

		protected virtual bool WriteMethodVisibility(MethodDefinition method)
		{
			if (method.get_DeclaringType().get_IsInterface())
			{
				return false;
			}
			this.WriteMemberVisibility(method);
			return true;
		}

		protected void WriteMethodVisibilityAndSpace(MethodDefinition currentMethod)
		{
			if (this.WriteMethodVisibility(currentMethod))
			{
				this.WriteSpace();
			}
		}

		protected void WriteMoreRestrictiveMethodVisibility(MethodDefinition currentMethod, MethodDefinition otherMethod)
		{
			if (otherMethod == null)
			{
				return;
			}
			if (currentMethod.get_IsPrivate() && !otherMethod.get_IsPrivate())
			{
				this.WriteMethodVisibilityAndSpace(currentMethod);
			}
			if ((currentMethod.get_IsFamily() || currentMethod.get_IsAssembly()) && (otherMethod.get_IsPublic() || otherMethod.get_IsFamilyOrAssembly()))
			{
				this.WriteMethodVisibilityAndSpace(currentMethod);
			}
			if (currentMethod.get_IsFamilyOrAssembly() && otherMethod.get_IsPublic())
			{
				this.WriteMethodVisibilityAndSpace(currentMethod);
			}
		}

		public virtual void WriteNamespaceIfTypeInCollision(TypeReference reference)
		{
			if (reference.get_IsPrimitive() && reference.get_MetadataType() != 24 && reference.get_MetadataType() != 25 || reference.get_MetadataType() == 14 || reference.get_MetadataType() == 28)
			{
				return;
			}
			TypeReference collidingType = this.GetCollidingType(reference);
			while (collidingType.get_DeclaringType() != null)
			{
				collidingType = this.GetCollidingType(collidingType.get_DeclaringType());
			}
			if (this.IsTypeNameInCollision(this.GetCollidingTypeName(collidingType)))
			{
				this.WriteNamespace(collidingType, true);
			}
		}

		protected virtual void WriteOptional(ParameterDefinition parameter)
		{
		}

		protected virtual void WriteOutOrRefKeyWord(ParameterDefinition parameter)
		{
			if (parameter.IsOutParameter())
			{
				this.WriteKeyword(this.KeyWordWriter.Out);
				return;
			}
			this.WriteKeyword(this.KeyWordWriter.ByRef);
		}

		protected virtual void WriteParameters(MethodDefinition method)
		{
			for (int i = 0; i < method.get_Parameters().get_Count(); i++)
			{
				ParameterDefinition item = method.get_Parameters().get_Item(i);
				if (i > 0)
				{
					this.WriteToken(",");
					this.WriteSpace();
				}
				if (i == 0 && this.KeyWordWriter.ExtensionThis != null && method.get_IsExtensionMethod())
				{
					this.WriteKeyword(this.KeyWordWriter.ExtensionThis);
					this.WriteSpace();
				}
				if (this.AttributeWriter.WriteParameterAttributes(item, (!this.TypeContext.IsWinRTImplementation ? false : (object)this.TypeContext.CurrentType == (object)method.get_DeclaringType())) > 0)
				{
					this.WriteSpace();
				}
				if (item.get_IsOptional())
				{
					this.WriteOptional(item);
				}
				TypeReference parameterType = item.get_ParameterType();
				if (parameterType is ByReferenceType)
				{
					this.WriteOutOrRefKeyWord(item);
					this.WriteSpace();
					parameterType = (parameterType as TypeSpecification).get_ElementType();
				}
				else if (this.KeyWordWriter.ByVal != null)
				{
					this.WriteKeyword(this.KeyWordWriter.ByVal);
					this.WriteSpace();
				}
				this.WriteParameterTypeAndName(parameterType, this.GetParameterName(item), item);
				if (item.get_IsOptional())
				{
					this.WriteSpace();
					this.Write("=");
					this.WriteSpace();
					if (!item.get_HasDefault())
					{
						this.Write(this.GetDefaultValueExpression(item));
					}
					else
					{
						this.WriteLiteralInLanguageSyntax(item.get_Constant().get_Value());
					}
				}
			}
		}

		protected virtual void WriteParameterTypeAndName(TypeReference typeReference, string name, ParameterDefinition reference)
		{
		}

		protected abstract bool WritePropertyAsIndexer(PropertyDefinition property);

		protected override void WritePropertyDeclaration(PropertyDefinition property)
		{
			MethodDefinition moreVisibleMethod = property.get_GetMethod().GetMoreVisibleMethod(property.get_SetMethod());
			if (property.IsIndexer())
			{
				this.WriteIndexerKeywords();
			}
			if (this.ModuleContext.RenamedMembers.Contains(property.get_MetadataToken().ToUInt32()))
			{
				this.WriteComment(property.get_Name());
				this.WriteLine();
			}
			if (!property.IsExplicitImplementation())
			{
				this.WriteMethodVisibilityAndSpace(moreVisibleMethod);
			}
			if (!property.IsVirtual() || property.IsNewSlot())
			{
				bool flag = property.IsIndexer();
				if (flag && this.IsIndexerPropertyHiding(property) || !flag && this.IsPropertyHiding(property))
				{
					this.WriteKeyword(this.KeyWordWriter.Hiding);
					this.WriteSpace();
				}
			}
			if (property.IsVirtual() && !property.get_DeclaringType().get_IsInterface() && this.WritePropertyKeywords(property))
			{
				this.WriteSpace();
			}
			this.WriteReadOnlyWriteOnlyProperty(property);
			if (this.TypeSupportsExplicitStaticMembers(property.get_DeclaringType()) && property.IsStatic())
			{
				this.WriteKeyword(this.KeyWordWriter.Static);
				this.WriteSpace();
			}
			if (this.KeyWordWriter.Property != null)
			{
				this.WriteKeyword(this.KeyWordWriter.Property);
				this.WriteSpace();
			}
			if (property.IsIndexer() && this.WritePropertyAsIndexer(property))
			{
				return;
			}
			this.WritePropertyTypeAndNameWithArguments(property);
			this.WritePropertyInterfaceImplementations(property);
		}

		protected virtual void WritePropertyInterfaceImplementations(PropertyDefinition property)
		{
		}

		private bool WritePropertyKeywords(PropertyDefinition property)
		{
			if (!property.IsNewSlot())
			{
				if (property.IsFinal())
				{
					this.WriteKeyword(this.KeyWordWriter.SealedMethod);
					this.WriteSpace();
				}
				this.WriteKeyword(this.KeyWordWriter.Override);
				return true;
			}
			if (property.IsAbstract())
			{
				this.WriteKeyword(this.KeyWordWriter.AbstractMember);
				return true;
			}
			if (property.IsFinal() || property.get_DeclaringType().get_IsSealed())
			{
				return false;
			}
			this.WriteKeyword(this.KeyWordWriter.Virtual);
			return true;
		}

		protected void WritePropertyMethods(PropertyDefinition property, bool inline = false)
		{
			bool flag = this.TypeContext.AutoImplementedProperties.Contains(property);
			if (property.get_GetMethod() != null)
			{
				this.WriteGetMethod(property, flag);
			}
			if (property.get_SetMethod() != null)
			{
				if (property.get_GetMethod() != null)
				{
					if (!inline)
					{
						this.WriteLine();
					}
					else
					{
						this.WriteSpace();
					}
				}
				this.WriteSetMethod(property, flag);
			}
		}

		protected virtual void WritePropertyName(PropertyDefinition property)
		{
			this.WriteReference(this.GetPropertyName(property), property);
		}

		protected void WritePropertyParameters(PropertyDefinition property)
		{
			MethodDefinition setMethod;
			int num = 0;
			if (property.get_GetMethod() == null)
			{
				setMethod = property.get_SetMethod();
				num = 1;
			}
			else
			{
				setMethod = property.get_GetMethod();
			}
			if (setMethod == null)
			{
				return;
			}
			for (int i = 0; i < setMethod.get_Parameters().get_Count() - num; i++)
			{
				ParameterDefinition item = setMethod.get_Parameters().get_Item(i);
				if (i > 0)
				{
					this.WriteToken(",");
					this.WriteSpace();
				}
				TypeReference parameterType = item.get_ParameterType();
				if (parameterType is ByReferenceType)
				{
					this.WriteOutOrRefKeyWord(item);
					this.WriteSpace();
					parameterType = (parameterType as TypeSpecification).get_ElementType();
				}
				else if (this.KeyWordWriter.ByVal != null)
				{
					this.WriteKeyword(this.KeyWordWriter.ByVal);
					this.WriteSpace();
				}
				string name = item.get_Name();
				if (base.MethodContext != null && base.MethodContext.Method == item.get_Method() && base.MethodContext.ParameterDefinitionToNameMap.ContainsKey(item))
				{
					name = base.MethodContext.ParameterDefinitionToNameMap[item];
				}
				this.WriteParameterTypeAndName(parameterType, name, item);
			}
		}

		protected virtual void WritePropertyTypeAndNameWithArguments(PropertyDefinition property)
		{
		}

		protected virtual void WriteReadOnlyWriteOnlyProperty(PropertyDefinition property)
		{
		}

		private void WriteReference(TypeReference reference)
		{
			string escapedTypeString;
			if (reference.get_DeclaringType() != null && !reference.get_IsGenericParameter())
			{
				TypeReference declaringType = reference.get_DeclaringType();
				if (!reference.get_IsGenericInstance())
				{
					this.WriteReference(declaringType);
					this.Write(".");
				}
				else
				{
					GenericInstanceType genericInstanceType = reference as GenericInstanceType;
					if (declaringType.get_HasGenericParameters())
					{
						GenericInstanceType genericInstanceType1 = new GenericInstanceType(declaringType);
						Mono.Collections.Generic.Collection<TypeReference> collection = new Mono.Collections.Generic.Collection<TypeReference>(genericInstanceType.get_GenericArguments());
						Mono.Collections.Generic.Collection<TypeReference> collection1 = new Mono.Collections.Generic.Collection<TypeReference>(genericInstanceType1.get_GenericArguments());
						int count = declaringType.get_GenericParameters().get_Count();
						for (int i = 0; i < count; i++)
						{
							genericInstanceType1.AddGenericArgument(genericInstanceType.get_GenericArguments().get_Item(i));
							genericInstanceType1.get_GenericArguments().Add(genericInstanceType.get_GenericArguments().get_Item(i));
						}
						this.WriteReference(genericInstanceType1);
						this.Write(".");
						if (genericInstanceType.get_GenericArguments().get_Count() - count <= 0)
						{
							this.WriteReference(this.GetTypeName(reference), reference);
						}
						else
						{
							this.WriteTypeSpecification(genericInstanceType, count);
						}
						genericInstanceType.get_GenericArguments().Clear();
						genericInstanceType.get_GenericArguments().AddRange(collection);
						genericInstanceType1.get_GenericArguments().Clear();
						genericInstanceType1.get_GenericArguments().AddRange(collection1);
						return;
					}
					this.WriteReference(declaringType);
					this.Write(".");
				}
			}
			if (reference is TypeSpecification)
			{
				this.WriteTypeSpecification(reference as TypeSpecification, 0);
				return;
			}
			if (reference.get_Namespace() == "System")
			{
				escapedTypeString = this.ToEscapedTypeString(reference);
				if (!this.IsReferenceFromMscorlib(reference))
				{
					escapedTypeString = Utilities.EscapeTypeNameIfNeeded(escapedTypeString, base.Language);
				}
			}
			else
			{
				escapedTypeString = this.GetTypeName(reference);
			}
			if (reference.get_HasGenericParameters() || !base.Language.IsEscapedWord(escapedTypeString) && !base.Language.IsValidIdentifier(escapedTypeString))
			{
				if (!(reference is TypeDefinition) || !reference.get_HasGenericParameters())
				{
					escapedTypeString = this.GetTypeName(reference);
				}
				else
				{
					escapedTypeString = this.GetTypeName(reference);
					int num = 0;
					if (reference.get_DeclaringType() != null && reference.get_DeclaringType().get_HasGenericParameters())
					{
						num = reference.get_DeclaringType().get_GenericParameters().get_Count();
					}
					if (num < reference.get_GenericParameters().get_Count())
					{
						escapedTypeString = String.Concat(escapedTypeString, this.GenericLeftBracket);
						for (int j = num; j < reference.get_GenericParameters().get_Count(); j++)
						{
							if (j > num)
							{
								escapedTypeString = String.Concat(escapedTypeString, ", ");
							}
							escapedTypeString = (base.Language.IsValidIdentifier(reference.get_GenericParameters().get_Item(j).get_Name()) ? String.Concat(escapedTypeString, reference.get_GenericParameters().get_Item(j).get_Name()) : String.Concat(escapedTypeString, base.Language.ReplaceInvalidCharactersInIdentifier(reference.get_GenericParameters().get_Item(j).get_Name())));
						}
						escapedTypeString = String.Concat(escapedTypeString, this.GenericRightBracket);
					}
				}
			}
			if (reference.get_IsGenericParameter())
			{
				this.WriteReference(escapedTypeString, null);
				return;
			}
			this.WriteReference(escapedTypeString, reference);
		}

		public virtual void WriteReferenceAndNamespaceIfInCollision(TypeReference reference)
		{
			this.WriteNamespaceIfTypeInCollision(reference);
			this.WriteReference(reference);
		}

		protected virtual void WriteRemoveOn(EventDefinition @event)
		{
			uint num = @event.get_RemoveMethod().get_MetadataToken().ToUInt32();
			this.membersStack.Push(@event.get_RemoveMethod());
			int currentPosition = this.formatter.CurrentPosition;
			this.AttributeWriter.WriteMemberAttributesAndNewLine(@event.get_RemoveMethod(), new String[] { "System.Runtime.CompilerServices.CompilerGeneratedAttribute" }, false);
			base.AddMemberAttributesOffsetSpan(num, currentPosition, this.formatter.CurrentPosition);
			int currentPosition1 = this.formatter.CurrentPosition;
			int num1 = this.formatter.CurrentPosition;
			this.WriteMoreRestrictiveMethodVisibility(@event.get_RemoveMethod(), @event.get_AddMethod());
			int currentPosition2 = this.formatter.CurrentPosition;
			this.WriteKeyword(this.KeyWordWriter.RemoveOn);
			this.WriteEventRemoveOnParameters(@event);
			int num2 = this.formatter.CurrentPosition - 1;
			this.currentWritingInfo.MemberDeclarationToCodePostionMap[@event.get_RemoveMethod()] = new OffsetSpan(currentPosition2, num2);
			this.WriteLine();
			this.Write(base.GetStatement(@event.get_RemoveMethod()));
			this.WriteSpecialEndBlock(this.KeyWordWriter.RemoveOn);
			this.currentWritingInfo.MemberDefinitionToFoldingPositionMap.Add(@event.get_RemoveMethod(), new OffsetSpan(currentPosition1, this.formatter.CurrentPosition - 1));
			this.currentWritingInfo.MemberTokenToDecompiledCodeMap.Add(num, new OffsetSpan(num1, this.formatter.CurrentPosition - 1));
			this.membersStack.Pop();
		}

		protected virtual void WriteRightPartOfBinaryExpression(BinaryExpression binaryExpression)
		{
			this.Visit(binaryExpression.Right);
		}

		protected void WriteSetMethod(PropertyDefinition property, bool isAutoImplemented)
		{
			uint num = property.get_SetMethod().get_MetadataToken().ToUInt32();
			this.membersStack.Push(property.get_SetMethod());
			int currentPosition = this.formatter.CurrentPosition;
			this.AttributeWriter.WriteMemberAttributesAndNewLine(property.get_SetMethod(), new String[] { "System.Runtime.CompilerServices.CompilerGeneratedAttribute" }, false);
			base.AddMemberAttributesOffsetSpan(num, currentPosition, this.formatter.CurrentPosition);
			int currentPosition1 = this.formatter.CurrentPosition;
			int num1 = this.formatter.CurrentPosition;
			this.WriteMoreRestrictiveMethodVisibility(property.get_SetMethod(), property.get_GetMethod());
			int currentPosition2 = this.formatter.CurrentPosition;
			this.WriteKeyword(this.KeyWordWriter.Set);
			int num2 = this.formatter.CurrentPosition - 1;
			this.currentWritingInfo.MemberDeclarationToCodePostionMap[property.get_SetMethod()] = new OffsetSpan(currentPosition2, num2);
			if (this.KeyWordWriter.ByVal != null)
			{
				this.WriteToken("(");
				this.WriteKeyword(this.KeyWordWriter.ByVal);
				this.WriteSpace();
				this.WriteTypeAndName(property.get_PropertyType(), "value", null);
				this.WriteToken(")");
			}
			if (property.get_SetMethod().get_Body() == null || this.SupportsAutoProperties & isAutoImplemented)
			{
				this.WriteEndOfStatement();
			}
			else
			{
				this.WriteLine();
				this.Write(base.GetStatement(property.get_SetMethod()));
			}
			this.WriteSpecialEndBlock(this.KeyWordWriter.Set);
			this.currentWritingInfo.MemberDefinitionToFoldingPositionMap.Add(property.get_SetMethod(), new OffsetSpan(currentPosition1, this.formatter.CurrentPosition - 1));
			this.currentWritingInfo.MemberTokenToDecompiledCodeMap.Add(num, new OffsetSpan(num1, this.formatter.CurrentPosition - 1));
			this.membersStack.Pop();
		}

		protected void WriteSingleGenericParameterConstraintsList(GenericParameter genericParameter)
		{
			bool flag = false;
			if (genericParameter.get_HasNotNullableValueTypeConstraint())
			{
				if (flag)
				{
					this.WriteToken(",");
					this.WriteSpace();
				}
				flag = true;
				this.WriteKeyword(this.KeyWordWriter.Struct);
			}
			if (genericParameter.get_HasReferenceTypeConstraint())
			{
				if (flag)
				{
					this.WriteToken(",");
					this.WriteSpace();
				}
				flag = true;
				this.WriteKeyword(this.KeyWordWriter.Class);
			}
			foreach (TypeReference constraint in genericParameter.get_Constraints())
			{
				if (genericParameter.get_HasNotNullableValueTypeConstraint() && constraint.get_FullName() == "System.ValueType")
				{
					continue;
				}
				if (flag)
				{
					this.WriteToken(",");
					this.WriteSpace();
				}
				this.WriteReferenceAndNamespaceIfInCollision(constraint);
				flag = true;
			}
			if (genericParameter.get_HasDefaultConstructorConstraint() && !genericParameter.get_HasNotNullableValueTypeConstraint())
			{
				if (flag)
				{
					this.WriteToken(",");
					this.WriteSpace();
				}
				flag = true;
				this.WriteConstructorGenericConstraint();
			}
		}

		protected virtual void WriteSpecialBetweenParenthesis(Expression expression)
		{
		}

		protected virtual void WriteSpecialBetweenParenthesis(Action action)
		{
		}

		private void WriteSpecialDoubleConstants(double value, IMemberDefinition currentMember)
		{
			if (Double.IsNaN(value))
			{
				this.WriteDoubleNan(currentMember);
			}
			if (Double.IsInfinity(value))
			{
				this.WriteDoubleInfinity(currentMember, value);
			}
		}

		protected virtual void WriteSpecialEndBlock(string statementName)
		{
		}

		private void WriteSpecialFloatValue(float value, IMemberDefinition currentMember)
		{
			if (Single.IsNaN(value))
			{
				this.WriteFloatNan(currentMember);
			}
			if (Single.IsInfinity(value))
			{
				this.WriteFloatInfinity(currentMember, (double)value);
			}
		}

		private void WriteSplitProperty(PropertyDefinition property)
		{
			MetadataToken metadataToken;
			MethodDefinition moreVisibleMethod = property.get_GetMethod().GetMoreVisibleMethod(property.get_SetMethod());
			this.WriteMethodVisibilityAndSpace(moreVisibleMethod);
			if (this.KeyWordWriter.Property != null)
			{
				this.WriteKeyword(this.KeyWordWriter.Property);
				this.WriteSpace();
			}
			this.WritePropertyTypeAndNameWithArguments(property);
			this.WritePropertyInterfaceImplementations(property);
			this.WriteBeginBlock(false);
			this.WriteLine();
			this.Indent();
			if (property.get_GetMethod() != null)
			{
				metadataToken = property.get_GetMethod().get_MetadataToken();
				uint num = metadataToken.ToUInt32();
				int currentPosition = this.formatter.CurrentPosition;
				this.AttributeWriter.WriteMemberAttributesAndNewLine(property.get_GetMethod(), new String[] { "System.Runtime.CompilerServices.CompilerGeneratedAttribute" }, false);
				base.AddMemberAttributesOffsetSpan(num, currentPosition, this.formatter.CurrentPosition);
				this.membersStack.Push(property.get_GetMethod());
				int currentPosition1 = this.formatter.CurrentPosition;
				this.WriteSplitPropertyGetter(property);
				this.currentWritingInfo.MemberTokenToDecompiledCodeMap.Add(num, new OffsetSpan(currentPosition1, this.formatter.CurrentPosition - 1));
				this.membersStack.Pop();
				this.WriteLine();
			}
			if (property.get_SetMethod() != null)
			{
				metadataToken = property.get_SetMethod().get_MetadataToken();
				uint num1 = metadataToken.ToUInt32();
				int currentPosition2 = this.formatter.CurrentPosition;
				this.AttributeWriter.WriteMemberAttributesAndNewLine(property.get_SetMethod(), new String[] { "System.Runtime.CompilerServices.CompilerGeneratedAttribute" }, false);
				base.AddMemberAttributesOffsetSpan(num1, currentPosition2, this.formatter.CurrentPosition);
				this.membersStack.Push(property.get_SetMethod());
				int num2 = this.formatter.CurrentPosition;
				this.WriteSplitPropertySetter(property);
				this.currentWritingInfo.MemberTokenToDecompiledCodeMap.Add(num1, new OffsetSpan(num2, this.formatter.CurrentPosition - 1));
				this.membersStack.Pop();
				this.WriteLine();
			}
			this.Outdent();
			this.WriteEndBlock("Property");
			FieldDefinition compileGeneratedBackingField = Utilities.GetCompileGeneratedBackingField(property);
			if (compileGeneratedBackingField != null)
			{
				this.WriteLine();
				this.WriteLine();
				this.Write(compileGeneratedBackingField);
			}
			if (property.get_GetMethod() != null)
			{
				this.WriteLine();
				this.WriteLine();
				this.Write(property.get_GetMethod());
			}
			if (property.get_SetMethod() != null)
			{
				this.WriteLine();
				this.WriteLine();
				this.Write(property.get_SetMethod());
			}
		}

		private void WriteSplitPropertyGetter(PropertyDefinition property)
		{
			this.WriteKeyword(this.KeyWordWriter.Get);
			this.WriteBeginBlock(false);
			this.WriteLine();
			this.Indent();
			this.Write(new ExpressionStatement(new ReturnExpression(new MethodInvocationExpression(new MethodReferenceExpression(null, property.get_GetMethod(), null), null)
			{
				Arguments = this.CopyMethodParametersAsArguments(property.get_GetMethod())
			}, null)));
			this.WriteLine();
			this.Outdent();
			this.WriteEndBlock("Get");
		}

		private void WriteSplitPropertySetter(PropertyDefinition property)
		{
			this.WriteKeyword(this.KeyWordWriter.Set);
			this.WriteBeginBlock(false);
			this.WriteLine();
			this.Indent();
			this.Write(new ExpressionStatement(new MethodInvocationExpression(new MethodReferenceExpression(null, property.get_SetMethod(), null), null)
			{
				Arguments = this.CopyMethodParametersAsArguments(property.get_SetMethod())
			}));
			this.WriteLine();
			this.Outdent();
			this.WriteEndBlock("Set");
		}

		protected void WriteTokenBetweenSpace(string token)
		{
			this.WriteSpace();
			this.WriteToken(token);
			this.WriteSpace();
		}

		protected virtual void WriteTypeAndName(TypeReference typeReference, string name, object reference)
		{
		}

		protected virtual void WriteTypeAndName(TypeReference typeReference, string name)
		{
		}

		protected virtual bool WriteTypeBaseTypes(TypeDefinition type, bool isPartial = false)
		{
			bool flag = false;
			if (type.get_BaseType() != null && type.get_BaseType().get_FullName() != "System.Object" && (!isPartial || this.IsImplemented(type, type.get_BaseType().Resolve())))
			{
				flag = true;
				this.WriteBaseTypeInheritColon();
				this.WriteReferenceAndNamespaceIfInCollision(type.get_BaseType());
			}
			return flag;
		}

		protected virtual void WriteTypeBaseTypesAndInterfaces(TypeDefinition type, bool isPartial)
		{
			bool flag = false;
			if (!type.get_IsEnum() && !type.get_IsValueType())
			{
				flag = this.WriteTypeBaseTypes(type, isPartial);
			}
			if (!type.get_IsEnum())
			{
				this.WriteTypeInterfaces(type, isPartial, flag);
			}
		}

		protected override string WriteTypeDeclaration(TypeDefinition type, bool isPartial = false)
		{
			string nonGenericName = GenericHelper.GetNonGenericName(type.get_Name());
			if (this.ModuleContext.RenamedMembers.Contains(type.get_MetadataToken().ToUInt32()))
			{
				this.WriteComment(nonGenericName);
				this.WriteLine();
			}
			string empty = String.Empty;
			this.WriteTypeVisiblity(type);
			this.WriteSpace();
			if (isPartial)
			{
				this.WriteKeyword(this.KeyWordWriter.Partial);
				this.WriteSpace();
			}
			if (type.get_IsEnum())
			{
				empty = this.KeyWordWriter.Enum;
			}
			else if (!type.get_IsValueType())
			{
				if (type.get_IsClass())
				{
					if (!type.get_IsStaticClass())
					{
						if (type.get_IsSealed())
						{
							this.WriteKeyword(this.KeyWordWriter.SealedType);
							this.WriteSpace();
						}
						if (type.get_IsAbstract())
						{
							this.WriteKeyword(this.KeyWordWriter.AbstractType);
							this.WriteSpace();
						}
						empty = this.KeyWordWriter.Class;
					}
					else
					{
						this.WriteTypeStaticKeywordAndSpace();
						empty = this.KeyWordWriter.StaticClass;
					}
				}
				if (type.get_IsInterface())
				{
					empty = this.KeyWordWriter.Interface;
				}
			}
			else
			{
				empty = this.KeyWordWriter.Struct;
			}
			this.WriteKeyword(empty);
			this.WriteSpace();
			int currentPosition = this.formatter.CurrentPosition;
			this.WriteGenericName(type);
			int num = this.formatter.CurrentPosition - 1;
			this.currentWritingInfo.MemberDeclarationToCodePostionMap[type] = new OffsetSpan(currentPosition, num);
			if (!type.get_IsDefaultEnum() && type.get_IsEnum())
			{
				TypeReference fieldType = type.get_Fields().get_Item(0).get_FieldType();
				if (fieldType.get_Name() != "Int32")
				{
					this.WriteEnumBaseTypeInheritColon();
					this.WriteReferenceAndNamespaceIfInCollision(fieldType);
				}
			}
			this.WriteTypeBaseTypesAndInterfaces(type, isPartial);
			if (type.get_HasGenericParameters())
			{
				this.PostWriteGenericParametersConstraints(type);
			}
			return empty;
		}

		protected abstract void WriteTypeInterfaces(TypeDefinition type, bool isPartial, bool baseTypeWritten);

		protected override void WriteTypeOpeningBlock(TypeDefinition type)
		{
			this.WriteLine();
			this.Indent();
		}

		private void WriteTypeReferenceNavigationName(TypeReference type)
		{
			if (type.get_IsOptionalModifier() || type.get_IsRequiredModifier())
			{
				this.WriteTypeReferenceNavigationName((type as IModifierType).get_ElementType());
				return;
			}
			if (type.get_IsByReference())
			{
				type = (type as ByReferenceType).get_ElementType();
				this.WriteTypeReferenceNavigationName(type);
				this.formatter.Write("&");
				return;
			}
			if (type.get_IsPointer())
			{
				type = (type as PointerType).get_ElementType();
				this.WriteTypeReferenceNavigationName(type);
				this.formatter.Write("*");
				return;
			}
			if (type.get_IsArray())
			{
				int count = (type as ArrayType).get_Dimensions().get_Count();
				type = (type as ArrayType).get_ElementType();
				this.WriteTypeReferenceNavigationName(type);
				this.formatter.Write(this.IndexLeftBracket);
				this.formatter.Write(new String(',', count - 1));
				this.formatter.Write(this.IndexRightBracket);
				return;
			}
			string nonGenericName = GenericHelper.GetNonGenericName(type.get_Name());
			if (base.Settings.RenameInvalidMembers)
			{
				nonGenericName = base.Language.ReplaceInvalidCharactersInIdentifier(nonGenericName);
			}
			this.formatter.Write(nonGenericName);
			if (type is GenericInstanceType)
			{
				this.WriteGenericInstanceTypeArguments((GenericInstanceType)type);
				return;
			}
			if (type.get_HasGenericParameters())
			{
				this.WriteGenericParametersToDefinition(type, null, base.Settings.RenameInvalidMembers);
			}
		}

		protected virtual void WriteTypeSpecification(TypeSpecification typeSpecification, int startingArgument = 0)
		{
			TypeReference i;
			ArrayType arrayType = null;
			if (typeSpecification is PointerType)
			{
				this.WriteReference(typeSpecification.get_ElementType());
				this.WriteToken("*");
				return;
			}
			if (typeSpecification is PinnedType)
			{
				if (!(typeSpecification.get_ElementType() is ByReferenceType))
				{
					this.WriteReference(typeSpecification.get_ElementType());
					return;
				}
				this.WriteReference((typeSpecification.get_ElementType() as ByReferenceType).get_ElementType());
				this.WriteToken("*");
				return;
			}
			if (typeSpecification is ByReferenceType)
			{
				this.WriteReference(typeSpecification.get_ElementType());
				this.WriteToken("&");
				return;
			}
			ArrayType arrayType1 = typeSpecification as ArrayType;
			if (arrayType1 != null)
			{
				List<int> nums = new List<int>();
				for (i = typeSpecification.get_ElementType(); i is ArrayType; i = arrayType.get_ElementType())
				{
					arrayType = i as ArrayType;
					nums.Add(arrayType.get_Dimensions().get_Count());
				}
				this.WriteReference(i);
				this.WriteToken(this.IndexLeftBracket);
				if (arrayType1.get_Dimensions() != null)
				{
					for (int j = 1; j < arrayType1.get_Dimensions().get_Count(); j++)
					{
						this.WriteToken(",");
					}
				}
				this.WriteToken(this.IndexRightBracket);
				foreach (int num in nums)
				{
					this.WriteToken(this.IndexLeftBracket);
					for (int k = 1; k < num; k++)
					{
						this.WriteToken(",");
					}
					this.WriteToken(this.IndexRightBracket);
				}
				return;
			}
			GenericInstanceType genericInstanceType = typeSpecification as GenericInstanceType;
			if (genericInstanceType == null)
			{
				if (typeSpecification.get_MetadataType() == 32)
				{
					this.Write(typeSpecification.get_Name());
					return;
				}
				if (!typeSpecification.get_IsRequiredModifier())
				{
					throw new NotSupportedException();
				}
				if (this.isWritingComment && (typeSpecification as RequiredModifierType).get_ModifierType().get_FullName() == "System.Runtime.CompilerServices.IsVolatile")
				{
					this.WriteVolatileType(typeSpecification.get_ElementType());
					return;
				}
				this.WriteReference(typeSpecification.get_ElementType());
				return;
			}
			if (!this.SupportsSpecialNullable || genericInstanceType.GetFriendlyFullName(base.Language).IndexOf("System.Nullable<") != 0 || !genericInstanceType.get_GenericArguments().get_Item(0).get_IsValueType())
			{
				this.WriteGenericInstance(genericInstanceType, startingArgument);
				return;
			}
			TypeReference item = genericInstanceType.get_GenericArguments().get_Item(0);
			if (genericInstanceType.get_PostionToArgument().ContainsKey(0))
			{
				item = genericInstanceType.get_PostionToArgument()[0];
			}
			this.WriteReference(item);
			this.WriteToken("?");
		}

		protected virtual void WriteTypeStaticKeywordAndSpace()
		{
			this.WriteKeyword(this.KeyWordWriter.Static);
			this.WriteSpace();
		}

		protected void WriteTypeVisiblity(TypeDefinition type)
		{
			if (this.TypeContext.IsWinRTImplementation)
			{
				this.WriteKeyword(this.KeyWordWriter.Public);
				return;
			}
			if (type.get_IsPublic())
			{
				this.WriteKeyword(this.KeyWordWriter.Public);
				return;
			}
			if (type.get_IsNestedPublic())
			{
				this.WriteKeyword(this.KeyWordWriter.Public);
				return;
			}
			if (type.get_IsNestedFamily())
			{
				this.WriteKeyword(this.KeyWordWriter.Protected);
				return;
			}
			if (type.get_IsNestedPrivate())
			{
				this.WriteKeyword(this.KeyWordWriter.Private);
				return;
			}
			if (type.get_IsNestedAssembly())
			{
				this.WriteKeyword(this.KeyWordWriter.Internal);
				return;
			}
			if (!type.get_IsNestedFamilyOrAssembly() && !type.get_IsNestedFamilyAndAssembly())
			{
				if (!type.get_IsNotPublic())
				{
					throw new NotSupportedException();
				}
				this.WriteKeyword(this.KeyWordWriter.Internal);
				return;
			}
			this.WriteKeyword(this.KeyWordWriter.Protected);
			this.WriteSpace();
			this.WriteKeyword(this.KeyWordWriter.Internal);
		}

		protected virtual void WriteVariableTypeAndName(VariableDefinition variable)
		{
		}

		protected virtual void WriteVolatileType(TypeReference reference)
		{
		}
	}
}