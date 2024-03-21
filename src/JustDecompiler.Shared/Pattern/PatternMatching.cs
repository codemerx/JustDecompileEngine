﻿using System;
using System.Collections;
using System.Collections.Generic;

using Mono.Cecil;
using Mono.Cecil.Cil;

using Telerik.JustDecompiler;
using Telerik.JustDecompiler.Ast;
using Ast = Telerik.JustDecompiler.Ast;
using Telerik.JustDecompiler.Ast.Statements;
using Telerik.JustDecompiler.Ast.Expressions;

namespace Telerik.JustDecompiler.Pattern {

	public struct MatchData {

		public readonly string Name;
		public readonly object Value;

		public MatchData (string name, object value)
		{
			Name = name;
			Value = value;
		}
	}

	public interface ICodePattern {
		bool Match (MatchContext context, object @object);
	}

	public static class Extensions {

		public static bool TryMatch (this ICodePattern self, MatchContext context, object @object)
		{
			if (self == null)
				return true;

			return self.Match (context, @object);
		}
	}

	public abstract class CodePattern : ICodePattern {

		public static MatchContext Match (ICodePattern pattern, object @object)
		{
			var context = new MatchContext ();
			context.Success = pattern.Match (context, @object);
			return context;
		}

		public abstract bool Match (MatchContext context, object @object);
	}

	public abstract class CodePattern<TNode> : CodePattern where TNode : class, ICodeNode {

		public Func<TNode, MatchData> Bind { get; set; }

		public override bool Match (MatchContext context, object node)
		{
			var current = node as TNode;
			if (current == null)
				return false;

			if (Bind != null)
				context.AddData (Bind (current));

			return OnMatch (context, current);
		}

		protected abstract bool OnMatch (MatchContext context, TNode node);
	}

	public class ExpressionStatement : CodePattern<Ast.Statements.ExpressionStatement> {

		public ICodePattern Expression { get; set; }

        protected override bool OnMatch(MatchContext context, Ast.Statements.ExpressionStatement node)
		{
			return Expression.TryMatch (context, node.Expression);
		}
	}

    public class Assignment : CodePattern<Ast.Expressions.BinaryExpression>
    {

		public ICodePattern Target { get; set; }
		public ICodePattern Expression { get; set; }

        protected override bool OnMatch(MatchContext context, Ast.Expressions.BinaryExpression node)
		{
			if (!Target.TryMatch (context, node.Left))
				return false;

			return Expression.TryMatch (context, node.Right);
		}
	}

    public class VariableReference : CodePattern<VariableReferenceExpression>
    {
        public ICodePattern Variable { get; set; }

        protected override bool OnMatch(MatchContext context, VariableReferenceExpression node)
        {
			return Variable.TryMatch (context, node.Variable);
		}
	}

    public class ContextVariableReference : CodePattern<VariableReferenceExpression>
    {
        public string Name { get; set; }

        protected override bool OnMatch(MatchContext context, VariableReferenceExpression node)
        {
			object data;
			if (!context.TryGetData (Name, out data))
				return false;

			return node.Variable == data;
		}
	}

	public class Binary : CodePattern<Ast.Expressions.BinaryExpression> {

		public ICodePattern Left { get; set; }
		public ICodePattern Operator { get; set; }
		public ICodePattern Right { get; set; }
        public bool? IsChecked { get; set; }

		protected override bool OnMatch (MatchContext context, BinaryExpression node)
		{
			if (!Left.TryMatch (context, node.Left))
				return false;

			if (!Operator.TryMatch (context, node.Operator))
				return false;

            if (!Right.TryMatch(context, node.Right))
            {
                return false;
            }

            if (IsChecked.HasValue)
            {
                return IsChecked.Value == node.IsChecked;
            }

            return true;
		}
	}

	public class Literal : CodePattern<Ast.Expressions.LiteralExpression> {

		object value;
		bool check_value;

		public object Value {
			get { return value; }
			set {
				this.value = value;
				check_value = true;
			}
		}

		protected override bool OnMatch (MatchContext context, Ast.Expressions.LiteralExpression node)
		{
			if (!check_value)
				return true;

			return Object.Equals (value, node.Value);
		}
	}

	public class SafeCast : CodePattern<Ast.Expressions.SafeCastExpression> {

		public ICodePattern TargetType { get; set; }
		public ICodePattern Expression { get; set; }

		protected override bool OnMatch (MatchContext context, SafeCastExpression node)
		{
			if (!TargetType.TryMatch (context, node.TargetType))
				return false;

			return Expression.TryMatch (context, node.Expression);
		}
	}

	public class Constant : CodePattern {

		public object Value { get; set; }
		public IEqualityComparer Comparer { get; set; }

		public override bool Match (MatchContext context, object @object)
		{
			var comparer = Comparer ?? EqualityComparer<object>.Default;
			return comparer.Equals (Value, @object);
		}
	}

	public class ContextData : CodePattern {

		public string Name { get; set; }
		public IEqualityComparer Comparer { get; set; }

		public override bool Match (MatchContext context, object @object)
		{
			object data;
			if (!context.TryGetData (Name, out data))
				return false;

			var comparer = Comparer ?? EqualityComparer<object>.Default;
			return comparer.Equals (data, @object);
		}
	}

	public class MatchContext {

		bool success = true;
		Dictionary<string, object> datas;

		public bool Success {
			get { return success; }
			set { success = value; }
		}

		public object this [string name] {
			get {
				return Store [name];
			}
		}

		Dictionary<string, object> Store {
			get {
				if (datas == null)
					datas = new Dictionary<string, object> ();

				return datas;
			}
		}

		public bool TryGetData (string name, out object value)
		{
			if (datas == null) {
				value = null;
				return false;
			}

			return Store.TryGetValue (name, out value);
		}

		public void AddData (MatchData data)
		{
			Store [data.Name] = data.Value;
		}
	}
}
