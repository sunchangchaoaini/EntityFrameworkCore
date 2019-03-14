// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Query.Expressions.Internal;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Query.NavigationExpansion.Visitors;

namespace Microsoft.EntityFrameworkCore.Query.NavigationExpansion
{
    public class NavigationExpansionExpression : Expression, IPrintable
    {
        private MethodInfo _queryableSelectMethodInfo
            = typeof(Queryable).GetMethods().Where(m => m.Name == nameof(Queryable.Select) && m.GetParameters()[1].ParameterType.GetGenericArguments()[0].GetGenericArguments().Count() == 2).Single();

        private MethodInfo _enumerableSelectMethodInfo
            = typeof(Enumerable).GetMethods().Where(m => m.Name == nameof(Enumerable.Select) && m.GetParameters()[1].ParameterType.GetGenericArguments().Count() == 2).Single();

        private Type _returnType;

        public override ExpressionType NodeType => ExpressionType.Extension;
        public override Type Type => _returnType;
        public override bool CanReduce => true;
        public override Expression Reduce()
        {
            if (!State.ApplyPendingSelector
                && State.PendingCardinalityReducingOperator == null
                && State.MaterializeCollectionNavigation == null
                && State.PendingOrderings.Count == 0)
            {
                //// TODO: hack to workaround type discrepancy that can happen sometimes when rerwriting collection navigations
                //if (Operand.Type != _returnType)
                //{
                //    return Convert(Operand, _returnType);
                //}

                return Operand;
            }

            var result = Operand;
            var parameter = Parameter(result.Type.GetSequenceType());

            foreach (var pendingOrdering in State.PendingOrderings)
            {
                var remappedKeySelectorBody = new ExpressionReplacingVisitor(pendingOrdering.keySelector.Parameters[0], State.CurrentParameter).Visit(pendingOrdering.keySelector.Body);
                var newSelectorBody = new NavigationPropertyUnbindingVisitor(State.CurrentParameter).Visit(remappedKeySelectorBody);
                var newSelector = Lambda(newSelectorBody, State.CurrentParameter);
                var orderingMethod = pendingOrdering.method.MakeGenericMethod(State.CurrentParameter.Type, newSelectorBody.Type);
                result = Call(orderingMethod, result, newSelector);
            }

            if (State.ApplyPendingSelector)
            {
                var pendingSelector = (LambdaExpression)new NavigationPropertyUnbindingVisitor(State.CurrentParameter).Visit(State.PendingSelector);

                // we can't get body type using lambda.Body.Type because in some cases (SelectMany) we manually set the lambda type (IEnumerable<Entity>) where the body itself is IQueryable
                // TODO: this might be problem in other places!
                var pendingSelectorBodyType = pendingSelector.Type.GetGenericArguments()[1];

                var pendingSelectMathod = result.Type.IsGenericType && (result.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>) || result.Type.GetGenericTypeDefinition() == typeof(IOrderedEnumerable<>))
                    ? _enumerableSelectMethodInfo.MakeGenericMethod(parameter.Type, pendingSelectorBodyType)
                    : _queryableSelectMethodInfo.MakeGenericMethod(parameter.Type, pendingSelectorBodyType);

                result = Call(pendingSelectMathod, result, pendingSelector);
                parameter = Parameter(result.Type.GetSequenceType());
            }

            if (State.PendingCardinalityReducingOperator != null)
            {
                var terminatingOperatorMethodInfo = State.PendingCardinalityReducingOperator.MakeGenericMethod(parameter.Type);

                result = Call(terminatingOperatorMethodInfo, result);
            }

            if (State.MaterializeCollectionNavigation != null)
            {
                result = Call(
                    NavigationExpansionHelpers.MaterializeCollectionNavigationMethodInfo.MakeGenericMethod(
                        State.MaterializeCollectionNavigation.GetTargetType().ClrType),
                    result,
                    Constant(State.MaterializeCollectionNavigation));
            }

            if (_returnType != result.Type && _returnType.IsGenericType)
            {
                if (_returnType.GetGenericTypeDefinition() == typeof(IOrderedQueryable<>))
                {
                    var toOrderedQueryableMethodInfo = typeof(NavigationExpansionExpression).GetMethod(nameof(NavigationExpansionExpression.ToOrderedQueryable)).MakeGenericMethod(parameter.Type);

                    return Call(toOrderedQueryableMethodInfo, result);
                }
                else if(_returnType.GetGenericTypeDefinition() == typeof(IOrderedEnumerable<>))
                {
                    var toOrderedEnumerableMethodInfo = typeof(NavigationExpansionExpression).GetMethod(nameof(NavigationExpansionExpression.ToOrderedEnumerable)).MakeGenericMethod(parameter.Type);

                    return Call(toOrderedEnumerableMethodInfo, result);
                }
            }

            return result;
        }

        public Expression Operand { get; }

        public NavigationExpansionExpressionState State { get; private set; }

        public NavigationExpansionExpression(
            Expression operand,
            NavigationExpansionExpressionState state,
            Type returnType)
        {
            Operand = operand;
            State = state;
            _returnType = returnType;
        }

        public void Print([NotNull] ExpressionPrinter expressionPrinter)
        {
            expressionPrinter.Visit(Operand);

            if (State.ApplyPendingSelector)
            {
                expressionPrinter.StringBuilder.Append(".PendingSelect(");
                expressionPrinter.Visit(State.PendingSelector);
                expressionPrinter.StringBuilder.Append(")");
            }

            if (State.PendingCardinalityReducingOperator != null)
            {
                expressionPrinter.StringBuilder.Append(".Pending" + State.PendingCardinalityReducingOperator.Name);
            }
        }

        public static IOrderedQueryable<TElement> ToOrderedQueryable<TElement>(IQueryable<TElement> source)
            => new IOrderedQueryableAdapter<TElement>(source);

        private class IOrderedQueryableAdapter<TElement> : IOrderedQueryable<TElement>
        {
            IQueryable<TElement> _source;

            public IOrderedQueryableAdapter(IQueryable<TElement> source)
            {
                _source = source;
            }

            public Type ElementType => _source.ElementType;

            public Expression Expression => _source.Expression;

            public IQueryProvider Provider => _source.Provider;

            public IEnumerator<TElement> GetEnumerator()
                => _source.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => ((IEnumerable)_source).GetEnumerator();
        }

        public static IOrderedEnumerable<TElement> ToOrderedEnumerable<TElement>(IEnumerable<TElement> source)
            => new IOrderedEnumerableAdapter<TElement>(source);

        private class IOrderedEnumerableAdapter<TElement> : IOrderedEnumerable<TElement>
        {
            IEnumerable<TElement> _source;

            public IOrderedEnumerableAdapter(IEnumerable<TElement> source)
            {
                _source = source;
            }

            public IOrderedEnumerable<TElement> CreateOrderedEnumerable<TKey>(Func<TElement, TKey> keySelector, IComparer<TKey> comparer, bool descending)
                => descending
                ? _source.OrderByDescending(keySelector, comparer)
                : _source.OrderBy(keySelector, comparer);

            public IEnumerator<TElement> GetEnumerator()
                => _source.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => ((IEnumerable)_source).GetEnumerator();
        }
    }
}
