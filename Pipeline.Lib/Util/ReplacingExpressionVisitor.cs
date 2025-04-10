//using System;
//using System.Collections.Generic;
//using System.Diagnostics.CodeAnalysis;
//using System.Linq;
//using System.Linq.Expressions;
//using System.Text;
//using System.Threading.Tasks;

//namespace Pipeline.Lib.Util
//{
//    public class ReplacingExpressionVisitor : ExpressionVisitor
//    {
//        private readonly Expression[] _originals;
//        private readonly Expression[] _replacements;

//        public static Expression Replace([NotNull] Expression original, [NotNull] Expression replacement, [NotNull] Expression tree)
//        {
//            ArgumentNullException.ThrowIfNull(original, nameof(original));
//            ArgumentNullException.ThrowIfNull(replacement, nameof(replacement));
//            ArgumentNullException.ThrowIfNull(tree, nameof(tree));

//            return new ReplacingExpressionVisitor(new[] { original }, new[] { replacement }).Visit(tree);
//        }

//        public ReplacingExpressionVisitor([NotNull] Expression[] originals, [NotNull] Expression[] replacements)
//        {
//            ArgumentNullException.ThrowIfNull(originals, nameof(originals));
//            ArgumentNullException.ThrowIfNull(replacements, nameof(replacements));

//            _originals = originals;
//            _replacements = replacements;
//        }

//        public override Expression Visit(Expression expression)
//        {
//            if (expression == null)
//            {
//                return expression;
//            }

//            // We use two arrays rather than a dictionary because hash calculation here can be prohibitively expensive
//            // for deep trees. Locality of reference makes arrays better for the small number of replacements anyway.
//            for (var i = 0; i < _originals.Length; i++)
//            {
//                if (expression.Equals(_originals[i]))
//                {
//                    return _replacements[i];
//                }
//            }

//            return base.Visit(expression);
//        }

//        protected override Expression VisitMember(MemberExpression memberExpression)
//        {
//            ArgumentNullException.ThrowIfNull(memberExpression, nameof(memberExpression));

//            var innerExpression = Visit(memberExpression.Expression);

//            if (innerExpression is NewExpression newExpression)
//            {
//                var index = newExpression.Members?.IndexOf(memberExpression.Member);
//                if (index >= 0)
//                {
//                    return newExpression.Arguments[index.Value];
//                }
//            }

//            if (innerExpression is MemberInitExpression memberInitExpression
//                && memberInitExpression.Bindings.SingleOrDefault(
//                    mb => mb.Member.IsSameAs(memberExpression.Member)) is MemberAssignment memberAssignment)
//            {
//                return memberAssignment.Expression;
//            }

//            return memberExpression.Update(innerExpression);
//        }

//        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
//        {
//            ArgumentNullException.ThrowIfNull(methodCallExpression, nameof(methodCallExpression));

//            if (methodCallExpression.TryGetEFPropertyArguments(out var entityExpression, out var propertyName))
//            {
//                var newEntityExpression = Visit(entityExpression);
//                if (newEntityExpression is NewExpression newExpression)
//                {
//                    var index = newExpression.Members?.Select(m => m.Name).IndexOf(propertyName);
//                    if (index >= 0)
//                    {
//                        return newExpression.Arguments[index.Value];
//                    }
//                }

//                if (newEntityExpression is MemberInitExpression memberInitExpression
//                    && memberInitExpression.Bindings.SingleOrDefault(
//                        mb => mb.Member.Name == propertyName) is MemberAssignment memberAssignment)
//                {
//                    return memberAssignment.Expression;
//                }

//                return methodCallExpression.Update(null, new[] { newEntityExpression, methodCallExpression.Arguments[1] });
//            }

//            return base.VisitMethodCall(methodCallExpression);
//        }
//    }
//}
