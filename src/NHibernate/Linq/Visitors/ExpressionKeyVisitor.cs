using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using NHibernate.Param;
using Remotion.Linq.Parsing;

namespace NHibernate.Linq.Visitors
{
	/// <summary>
	/// Performs the equivalent of a ToString() on an expression. Swaps out constants for 
	/// parameters so that, for example:
	///		from c in Customers where c.City = "London"
	/// generate the same key as 
	///		from c in Customers where c.City = "Madrid"
	/// </summary>
	public class ExpressionKeyVisitor : RelinqExpressionVisitor
	{
		private readonly IDictionary<ConstantExpression, NamedParameter> _constantToParameterMap;
		readonly StringBuilder _string = new StringBuilder();

		private ExpressionKeyVisitor(IDictionary<ConstantExpression, NamedParameter> constantToParameterMap)
		{
			_constantToParameterMap = constantToParameterMap;
		}

		public static string Visit(Expression expression, IDictionary<ConstantExpression, NamedParameter> parameters)
		{
			var visitor = new ExpressionKeyVisitor(parameters);

			visitor.Visit(expression);

			return visitor.ToString();
		}

		public override string ToString()
		{
			return _string.ToString();
		}

		protected override Expression VisitBinary(BinaryExpression expression)
		{
			if (expression.Method != null)
			{
				_string.Append(expression.Method.DeclaringType.Name);
				_string.Append(".");
				VisitMethod(expression.Method);
			}
			else
			{
				_string.Append(expression.NodeType);
			}

			_string.Append("(");

			Visit(expression.Left);
			_string.Append(", ");
			Visit(expression.Right);

			_string.Append(")");

			return expression;
		}

		protected override Expression VisitConditional(ConditionalExpression expression)
		{
			Visit(expression.Test);
			_string.Append(" ? ");
			Visit(expression.IfTrue);
			_string.Append(" : ");
			Visit(expression.IfFalse);

			return expression;
		}

		protected override Expression VisitConstant(ConstantExpression expression)
		{
			NamedParameter param;

			if (_constantToParameterMap == null)
				throw new InvalidOperationException("Cannot visit a constant without a constant to parameter map.");
			if (_constantToParameterMap.TryGetValue(expression, out param))
			{
				// Nulls generate different query plans.  X = variable generates a different query depending on if variable is null or not.
				if (param.Value == null)
				{
					_string.Append("NULL");
				}
				else
				{
					var value = param.Value as IEnumerable;
					if (value != null && !(value is string) && !value.Cast<object>().Any())
					{
						_string.Append("EmptyList");
					}
					else
					{
						_string.Append(param.Name);
					}
				}
			}
			else
			{
				if (expression.Value == null)
				{
					_string.Append("NULL");
				}
				else
				{
					var value = expression.Value as IEnumerable;
					if (value != null  && !(value is string) && !(value is IQueryable))
					{
						_string.Append("{");
						_string.Append(String.Join(",", value.Cast<object>()));
						_string.Append("}");
					}
					else
					{
						_string.Append(expression.Value);
					}
				}
			}

			return base.VisitConstant(expression);
		}

		private T AppendCommas<T>(T expression) where T : Expression
		{
			Visit(expression);
			_string.Append(", ");

			return expression;
		}

		protected override Expression VisitLambda<T>(Expression<T> expression)
		{
			_string.Append('(');

			Visit(expression.Parameters, AppendCommas);
			_string.Append(") => (");
			Visit(expression.Body);
			_string.Append(')');

			return expression;
		}

		protected override Expression VisitMember(MemberExpression expression)
		{
			base.VisitMember(expression);

			_string.Append('.');
			_string.Append(expression.Member.Name);

			return expression;
		}

		protected override Expression VisitMethodCall(MethodCallExpression expression)
		{
			Visit(expression.Object);
			_string.Append('.');
			VisitMethod(expression.Method);
			_string.Append('(');
			ExpressionVisitor.Visit(expression.Arguments, AppendCommas);
			_string.Append(')');

			return expression;
		}

		protected override Expression VisitNew(NewExpression expression)
		{
			_string.Append("new ");
			_string.Append(expression.Constructor.DeclaringType.AssemblyQualifiedName);
			_string.Append('(');
			Visit(expression.Arguments, AppendCommas);
			_string.Append(')');

			return expression;
		}

		protected override Expression VisitParameter(ParameterExpression expression)
		{
			_string.Append(expression.Name);

			return expression;
		}

		protected override Expression VisitTypeBinary(TypeBinaryExpression expression)
		{
			_string.Append("IsType(");
			Visit(expression.Expression);
			_string.Append(", ");
			_string.Append(expression.TypeOperand.AssemblyQualifiedName);
			_string.Append(")");

			return expression;
		}

		protected override Expression VisitUnary(UnaryExpression expression)
		{
			_string.Append(expression.NodeType);
			_string.Append('(');
			Visit(expression.Operand);
			_string.Append(')');

			return expression;
		}

		protected override Expression VisitQuerySourceReference(Remotion.Linq.Clauses.Expressions.QuerySourceReferenceExpression expression)
		{
			_string.Append(expression.ReferencedQuerySource.ItemName);
			return expression;
		}

		private void VisitMethod(MethodInfo methodInfo)
		{
			_string.Append(methodInfo.Name);
			if (methodInfo.IsGenericMethod)
			{
				_string.Append('[');
				_string.Append(string.Join(",", methodInfo.GetGenericArguments().Select(a => a.AssemblyQualifiedName).ToArray()));
				_string.Append(']');
			}
		}
	}
}
