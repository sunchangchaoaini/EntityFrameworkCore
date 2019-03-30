﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Utilities;

namespace Microsoft.EntityFrameworkCore.Query.NavigationExpansion
{
    public enum NavigationTreeNodeExpansionMode
    {
        /// <summary>
        ///     Navigation doesn't need to be expanded
        /// </summary>
        NotNeeded,

        /// <summary>
        ///     Navigation needs to be expanded, but hasn't been expanded yet
        /// </summary>
        Pending,

        /// <summary>
        ///     Navigation had already been expanded
        /// </summary>
        Complete,
    };

    public class NavigationTreeNode
    {
        private NavigationTreeNode(
            [NotNull] INavigation navigation,
            [NotNull] NavigationTreeNode parent,
            bool optional,
            bool include)
        {
            Check.NotNull(navigation, nameof(navigation));
            Check.NotNull(parent, nameof(parent));

            Navigation = navigation;
            Parent = parent;
            Optional = optional;
            ToMapping = new List<string>();
            if (include)
            {
                ExpansionMode = NavigationTreeNodeExpansionMode.NotNeeded;
                Included = true;
            }
            else
            {
                ExpansionMode = NavigationTreeNodeExpansionMode.Pending;
                Included = false;
            }

            foreach (var parentFromMapping in parent.FromMappings)
            {
                var newMapping = parentFromMapping.ToList();
                newMapping.Add(navigation.Name);
                FromMappings.Add(newMapping);
            }
        }

        private NavigationTreeNode(
            List<string> fromMapping,
            bool optional)
        {
            Optional = optional;
            FromMappings.Add(fromMapping.ToList());
            ToMapping = fromMapping.ToList();
            ExpansionMode = NavigationTreeNodeExpansionMode.Complete;
            Included = false;
        }

        public INavigation Navigation { get; private set; }
        public bool Optional { get; private set; }
        public NavigationTreeNode Parent { get; private set; }
        public List<NavigationTreeNode> Children { get; private set; } = new List<NavigationTreeNode>();
        public NavigationTreeNodeExpansionMode ExpansionMode { get; set; }
        public bool Included { get; set; }

        public List<List<string>> FromMappings { get; set; } = new List<List<string>>();
        public List<string> ToMapping { get; set; }

        public static NavigationTreeNode CreateRoot(
            [NotNull] SourceMapping sourceMapping,
            [NotNull] List<string> fromMapping,
            bool optional)
        {
            Check.NotNull(sourceMapping, nameof(sourceMapping));
            Check.NotNull(fromMapping, nameof(fromMapping));

            return sourceMapping.NavigationTree ?? new NavigationTreeNode(fromMapping, optional);
        }

        public static NavigationTreeNode Create(
            [NotNull] SourceMapping sourceMapping,
            [NotNull] INavigation navigation,
            [NotNull] NavigationTreeNode parent,
            bool include)
        {
            Check.NotNull(sourceMapping, nameof(sourceMapping));
            Check.NotNull(navigation, nameof(navigation));
            Check.NotNull(parent, nameof(parent));

            var existingChild = parent.Children.Where(c => c.Navigation == navigation).SingleOrDefault();
            if (existingChild != null)
            {
                if (include && !existingChild.Included)
                {
                    existingChild.Included = true;
                }
                else if (!include && existingChild.ExpansionMode == NavigationTreeNodeExpansionMode.NotNeeded)
                {
                    existingChild.ExpansionMode = NavigationTreeNodeExpansionMode.Pending;
                }

                return existingChild;
            }

            // if (any) parent is optional, all children must be optional also
            // TODO: what about query filters?
            var optional = parent.Optional || !navigation.ForeignKey.IsRequired || !navigation.IsDependentToPrincipal();
            var result = new NavigationTreeNode(navigation, parent, optional, include);
            parent.Children.Add(result);

            return result;
        }

        public List<NavigationTreeNode> Flatten()
        {
            var result = new List<NavigationTreeNode>();
            result.Add(this);

            foreach (var child in Children)
            {
                result.AddRange(child.Flatten());
            }

            return result;
        }

        // TODO: just make property settable?
        public void MakeOptional()
        {
            Optional = true;
        }

        // TODO: get rid of it?
        public List<string> GeneratePath()
        {
            if (Parent == null)
            {
                return new List<string> { Navigation.Name };
            }
            else
            {
                var result = Parent.GeneratePath();
                result.Add(Navigation.Name);

                return result;
            }
        }

        public Expression BuildExpression(ParameterExpression root)
        {
            var result = (Expression)root;
            foreach (var accessorPathElement in ToMapping)
            {
                result = Expression.PropertyOrField(result, accessorPathElement);
            }

            return result;
        }

        // TODO: this shouldn't be needed eventually, temporary hack
        public List<INavigation> NavigationChain()
        {
            var result = Parent?.NavigationChain() ?? new List<INavigation>();
            result.Add(Navigation);

            return result;
        }
    }
}