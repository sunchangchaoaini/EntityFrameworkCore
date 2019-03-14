// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.EntityFrameworkCore.Query.NavigationExpansion.Visitors
{
    public abstract class LinqQueryVisitorBase : ExpressionVisitor
    {
        protected MethodInfo QueryableWhereMethodInfo { get; set; }
        protected MethodInfo QueryableSelectMethodInfo { get; set; }
        protected MethodInfo QueryableOrderByMethodInfo { get; set; }
        protected MethodInfo QueryableOrderByDescendingMethodInfo { get; set; }
        protected MethodInfo QueryableThenByMethodInfo { get; set; }
        protected MethodInfo QueryableThenByDescendingMethodInfo { get; set; }
        protected MethodInfo QueryableJoinMethodInfo { get; set; }
        protected MethodInfo QueryableGroupJoinMethodInfo { get; set; }
        protected MethodInfo QueryableSelectManyMethodInfo { get; set; }
        protected MethodInfo QueryableSelectManyWithResultOperatorMethodInfo { get; set; }

        protected MethodInfo QueryableGroupByKeySelector { get; set; }
        protected MethodInfo QueryableGroupByKeySelectorResultSelector { get; set; }
        protected MethodInfo QueryableGroupByKeySelectorElementSelector { get; set; }
        protected MethodInfo QueryableGroupByKeySelectorElementSelectorResultSelector { get; set; }

        protected MethodInfo QueryableFirstMethodInfo { get; set; }
        protected MethodInfo QueryableFirstOrDefaultMethodInfo { get; set; }
        protected MethodInfo QueryableSingleMethodInfo { get; set; }
        protected MethodInfo QueryableSingleOrDefaultMethodInfo { get; set; }

        protected MethodInfo QueryableFirstPredicateMethodInfo { get; set; }
        protected MethodInfo QueryableFirstOrDefaultPredicateMethodInfo { get; set; }
        protected MethodInfo QueryableSinglePredicateMethodInfo { get; set; }
        protected MethodInfo QueryableSingleOrDefaultPredicateMethodInfo { get; set; }

        protected MethodInfo QueryableAnyMethodInfo { get; set; }
        protected MethodInfo QueryableAnyPredicateMethodInfo { get; set; }
        protected MethodInfo QueryableAllMethodInfo { get; set; }
        protected MethodInfo QueryableContainsMethodInfo { get; set; }

        protected MethodInfo QueryableCountMethodInfo { get; set; }
        protected MethodInfo QueryableCountPredicateMethodInfo { get; set; }
        protected MethodInfo QueryableDistinctMethodInfo { get; set; }
        protected MethodInfo QueryableTakeMethodInfo { get; set; }
        protected MethodInfo QueryableSkipMethodInfo { get; set; }

        protected MethodInfo QueryableOfType { get; set; }

        protected MethodInfo QueryableDefaultIfEmpty { get; set; }
        protected MethodInfo QueryableDefaultIfEmptyWithDefaultValue { get; set; }

        protected MethodInfo EnumerableWhereMethodInfo { get; set; }
        protected MethodInfo EnumerableSelectMethodInfo { get; set; }

        protected MethodInfo EnumerableJoinMethodInfo { get; set; }
        protected MethodInfo EnumerableGroupJoinMethodInfo { get; set; }
        protected MethodInfo EnumerableSelectManyWithResultOperatorMethodInfo { get; set; }

        protected MethodInfo EnumerableGroupByKeySelector { get; set; }
        protected MethodInfo EnumerableGroupByKeySelectorResultSelector { get; set; }
        protected MethodInfo EnumerableGroupByKeySelectorElementSelector { get; set; }
        protected MethodInfo EnumerableGroupByKeySelectorElementSelectorResultSelector { get; set; }

        protected MethodInfo EnumerableFirstMethodInfo { get; set; }
        protected MethodInfo EnumerableFirstOrDefaultMethodInfo { get; set; }
        protected MethodInfo EnumerableSingleMethodInfo { get; set; }
        protected MethodInfo EnumerableSingleOrDefaultMethodInfo { get; set; }

        protected MethodInfo EnumerableFirstPredicateMethodInfo { get; set; }
        protected MethodInfo EnumerableFirstOrDefaultPredicateMethodInfo { get; set; }
        protected MethodInfo EnumerableSinglePredicateMethodInfo { get; set; }
        protected MethodInfo EnumerableSingleOrDefaultPredicateMethodInfo { get; set; }

        protected MethodInfo EnumerableDefaultIfEmptyMethodInfo { get; set; }

        protected MethodInfo EnumerableAnyMethodInfo { get; set; }
        protected MethodInfo EnumerableAnyPredicateMethodInfo { get; set; }
        protected MethodInfo EnumerableAllMethodInfo { get; set; }
        protected MethodInfo EnumerableContainsMethodInfo { get; set; }

        protected MethodInfo EnumerableCountMethodInfo { get; set; }
        protected MethodInfo EnumerableCountPredicateMethodInfo { get; set; }

        private bool IsExpressionOfFunc(Type type, int parameterCount)
            => type.IsGenericType
            && type.GetGenericTypeDefinition() == typeof(Expression<>)
            && type.GetGenericArguments()[0] is Type expressionTypeArgument
            && expressionTypeArgument.IsGenericType
            && expressionTypeArgument.Name.StartsWith(nameof(Func<object>))
            && expressionTypeArgument.GetGenericArguments().Count() == parameterCount + 1;

        private bool IsFunc(Type type, int parameterCount)
            => type.IsGenericType
            && type.Name.StartsWith(nameof(Func<object>))
            && type.GetGenericArguments().Count() == parameterCount + 1;

        protected LinqQueryVisitorBase()
        {
            var queryableMethods = typeof(Queryable).GetMethods().ToList();

            QueryableWhereMethodInfo = queryableMethods.Where(m => m.Name == nameof(Queryable.Where) && IsExpressionOfFunc(m.GetParameters()[1].ParameterType, 1)).Single();
            QueryableSelectMethodInfo = queryableMethods.Where(m => m.Name == nameof(Queryable.Select) && IsExpressionOfFunc(m.GetParameters()[1].ParameterType, 1)).Single();
            QueryableOrderByMethodInfo = queryableMethods.Where(m => m.Name == nameof(Queryable.OrderBy) && m.GetParameters().Count() == 2).Single();
            QueryableOrderByDescendingMethodInfo = queryableMethods.Where(m => m.Name == nameof(Queryable.OrderByDescending) && m.GetParameters().Count() == 2).Single();
            QueryableThenByMethodInfo = queryableMethods.Where(m => m.Name == nameof(Queryable.ThenBy) && m.GetParameters().Count() == 2).Single();
            QueryableThenByDescendingMethodInfo = queryableMethods.Where(m => m.Name == nameof(Queryable.ThenByDescending) && m.GetParameters().Count() == 2).Single();
            QueryableJoinMethodInfo = queryableMethods.Where(m => m.Name == nameof(Queryable.Join) && m.GetParameters().Count() == 5).Single();
            QueryableGroupJoinMethodInfo = queryableMethods.Where(m => m.Name == nameof(Queryable.GroupJoin) && m.GetParameters().Count() == 5).Single();

            QueryableSelectManyMethodInfo = queryableMethods.Where(m => m.Name == nameof(Queryable.SelectMany) && m.GetParameters().Count() == 2 && IsExpressionOfFunc(m.GetParameters()[1].ParameterType, 1)).Single();
            QueryableSelectManyWithResultOperatorMethodInfo = queryableMethods.Where(m => m.Name == nameof(Queryable.SelectMany) && m.GetParameters().Count() == 3 && IsExpressionOfFunc(m.GetParameters()[1].ParameterType, 1)).Single();

            QueryableGroupByKeySelector = queryableMethods.Where(m => m.Name == nameof(Queryable.GroupBy) && m.GetParameters().Count() == 2).Single();
            QueryableGroupByKeySelectorResultSelector = queryableMethods.Where(m => m.Name == nameof(Queryable.GroupBy) && m.GetParameters().Count() == 3 && IsExpressionOfFunc(m.GetParameters()[2].ParameterType, 2)).Single();
            QueryableGroupByKeySelectorElementSelector = queryableMethods.Where(m => m.Name == nameof(Queryable.GroupBy) && m.GetParameters().Count() == 3 && IsExpressionOfFunc(m.GetParameters()[2].ParameterType, 1)).Single();
            QueryableGroupByKeySelectorElementSelectorResultSelector = queryableMethods.Where(m => m.Name == nameof(Queryable.GroupBy) && m.GetParameters().Count() == 4 && IsExpressionOfFunc(m.GetParameters()[2].ParameterType, 1) && IsExpressionOfFunc(m.GetParameters()[3].ParameterType, 2)).Single();

            QueryableFirstMethodInfo = queryableMethods.Where(m => m.Name == nameof(Queryable.First) && m.GetParameters().Count() == 1).Single();
            QueryableFirstOrDefaultMethodInfo = queryableMethods.Where(m => m.Name == nameof(Queryable.FirstOrDefault) && m.GetParameters().Count() == 1).Single();
            QueryableSingleMethodInfo = queryableMethods.Where(m => m.Name == nameof(Queryable.Single) && m.GetParameters().Count() == 1).Single();
            QueryableSingleOrDefaultMethodInfo = queryableMethods.Where(m => m.Name == nameof(Queryable.SingleOrDefault) && m.GetParameters().Count() == 1).Single();

            QueryableFirstPredicateMethodInfo = queryableMethods.Where(m => m.Name == nameof(Queryable.First) && m.GetParameters().Count() == 2).Single();
            QueryableFirstOrDefaultPredicateMethodInfo = queryableMethods.Where(m => m.Name == nameof(Queryable.FirstOrDefault) && m.GetParameters().Count() == 2).Single();
            QueryableSinglePredicateMethodInfo = queryableMethods.Where(m => m.Name == nameof(Queryable.Single) && m.GetParameters().Count() == 2).Single();
            QueryableSingleOrDefaultPredicateMethodInfo = queryableMethods.Where(m => m.Name == nameof(Queryable.SingleOrDefault) && m.GetParameters().Count() == 2).Single();

            QueryableCountMethodInfo = queryableMethods.Where(m => m.Name == nameof(Queryable.Count) && m.GetParameters().Count() == 1).Single();
            QueryableCountPredicateMethodInfo = queryableMethods.Where(m => m.Name == nameof(Queryable.Count) && m.GetParameters().Count() == 2).Single();

            QueryableDistinctMethodInfo = queryableMethods.Where(m => m.Name == nameof(Queryable.Distinct) && m.GetParameters().Count() == 1).Single();
            QueryableTakeMethodInfo = queryableMethods.Where(m => m.Name == nameof(Queryable.Take) && m.GetParameters().Count() == 2).Single();
            QueryableSkipMethodInfo = queryableMethods.Where(m => m.Name == nameof(Queryable.Skip) && m.GetParameters().Count() == 2).Single();

            QueryableAnyMethodInfo = queryableMethods.Where(m => m.Name == nameof(Queryable.Any) && m.GetParameters().Count() == 1).Single();
            QueryableAnyPredicateMethodInfo = queryableMethods.Where(m => m.Name == nameof(Queryable.Any) && m.GetParameters().Count() == 2).Single();
            QueryableAllMethodInfo = queryableMethods.Where(m => m.Name == nameof(Queryable.All) && m.GetParameters().Count() == 2).Single();
            QueryableContainsMethodInfo = queryableMethods.Where(m => m.Name == nameof(Queryable.Contains) && m.GetParameters().Count() == 2).Single();

            QueryableOfType = queryableMethods.Where(m => m.Name == nameof(Queryable.OfType) && m.GetParameters().Count() == 1).Single();

            QueryableDefaultIfEmpty = queryableMethods.Where(m => m.Name == nameof(Queryable.DefaultIfEmpty) && m.GetParameters().Count() == 1).Single();
            QueryableDefaultIfEmptyWithDefaultValue = queryableMethods.Where(m => m.Name == nameof(Queryable.DefaultIfEmpty) && m.GetParameters().Count() == 2).Single();

            var enumerableMethods = typeof(Enumerable).GetMethods().ToList();

            EnumerableWhereMethodInfo = enumerableMethods.Where(m => m.Name == nameof(Enumerable.Where) && IsFunc(m.GetParameters()[1].ParameterType, 1)).Single();
            EnumerableSelectMethodInfo = enumerableMethods.Where(m => m.Name == nameof(Enumerable.Select) && IsFunc(m.GetParameters()[1].ParameterType, 1)).Single();

            EnumerableJoinMethodInfo = enumerableMethods.Where(m => m.Name == nameof(Enumerable.Join) && m.GetParameters().Count() == 5).Single();
            EnumerableGroupJoinMethodInfo = enumerableMethods.Where(m => m.Name == nameof(Enumerable.GroupJoin) && m.GetParameters().Count() == 5).Single();
            EnumerableSelectManyWithResultOperatorMethodInfo = enumerableMethods.Where(m => m.Name == nameof(Enumerable.SelectMany) && m.GetParameters().Count() == 3 && IsFunc(m.GetParameters()[1].ParameterType, 1)).Single();

            EnumerableGroupByKeySelector = enumerableMethods.Where(m => m.Name == nameof(Queryable.GroupBy) && m.GetParameters().Count() == 2).Single();
            EnumerableGroupByKeySelectorResultSelector = enumerableMethods.Where(m => m.Name == nameof(Queryable.GroupBy) && m.GetParameters().Count() == 3 && IsFunc(m.GetParameters()[2].ParameterType, 2)).Single();
            EnumerableGroupByKeySelectorElementSelector = enumerableMethods.Where(m => m.Name == nameof(Queryable.GroupBy) && m.GetParameters().Count() == 3 && IsFunc(m.GetParameters()[2].ParameterType, 1)).Single();
            EnumerableGroupByKeySelectorElementSelectorResultSelector = enumerableMethods.Where(m => m.Name == nameof(Queryable.GroupBy) && m.GetParameters().Count() == 4 && IsFunc(m.GetParameters()[2].ParameterType, 1) && IsFunc(m.GetParameters()[3].ParameterType, 2)).Single();

            EnumerableFirstMethodInfo = enumerableMethods.Where(m => m.Name == nameof(Enumerable.First) && m.GetParameters().Count() == 1).Single();
            EnumerableFirstOrDefaultMethodInfo = enumerableMethods.Where(m => m.Name == nameof(Enumerable.FirstOrDefault) && m.GetParameters().Count() == 1).Single();
            EnumerableSingleMethodInfo = enumerableMethods.Where(m => m.Name == nameof(Enumerable.Single) && m.GetParameters().Count() == 1).Single();
            EnumerableSingleOrDefaultMethodInfo = enumerableMethods.Where(m => m.Name == nameof(Enumerable.SingleOrDefault) && m.GetParameters().Count() == 1).Single();

            EnumerableFirstPredicateMethodInfo = enumerableMethods.Where(m => m.Name == nameof(Enumerable.First) && m.GetParameters().Count() == 2).Single();
            EnumerableFirstOrDefaultPredicateMethodInfo = enumerableMethods.Where(m => m.Name == nameof(Enumerable.FirstOrDefault) && m.GetParameters().Count() == 2).Single();
            EnumerableSinglePredicateMethodInfo = enumerableMethods.Where(m => m.Name == nameof(Enumerable.Single) && m.GetParameters().Count() == 2).Single();
            EnumerableSingleOrDefaultPredicateMethodInfo = enumerableMethods.Where(m => m.Name == nameof(Enumerable.SingleOrDefault) && m.GetParameters().Count() == 2).Single();

            EnumerableDefaultIfEmptyMethodInfo = enumerableMethods.Where(m => m.Name == nameof(Enumerable.DefaultIfEmpty) && m.GetParameters().Count() == 1).Single();

            EnumerableAnyMethodInfo = enumerableMethods.Where(m => m.Name == nameof(Enumerable.Any) && m.GetParameters().Count() == 1).Single();
            EnumerableAnyPredicateMethodInfo = enumerableMethods.Where(m => m.Name == nameof(Enumerable.Any) && m.GetParameters().Count() == 2).Single();
            EnumerableAllMethodInfo = enumerableMethods.Where(m => m.Name == nameof(Enumerable.All) && m.GetParameters().Count() == 2).Single();
            EnumerableContainsMethodInfo = enumerableMethods.Where(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Count() == 2).Single();

            EnumerableCountMethodInfo = enumerableMethods.Where(m => m.Name == nameof(Enumerable.Count) && m.GetParameters().Count() == 1).Single();
            EnumerableCountPredicateMethodInfo = enumerableMethods.Where(m => m.Name == nameof(Enumerable.Count) && m.GetParameters().Count() == 2).Single();
        }
    }
}
