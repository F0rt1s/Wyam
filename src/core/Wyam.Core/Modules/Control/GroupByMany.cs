﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Wyam.Common.Configuration;
using Wyam.Common.Documents;
using Wyam.Common.Meta;
using Wyam.Common.Modules;
using Wyam.Common.Execution;
using Wyam.Common.Util;
using Wyam.Core.Documents;
using Wyam.Core.Meta;

namespace Wyam.Core.Modules.Control
{
    /// <summary>
    /// Splits a sequence of documents into groups based on a specified function or metadata key
    /// that returns or contains a sequence of group keys.
    /// </summary>
    /// <remarks>
    /// This module forms groups from the output documents of the specified modules.
    /// If the function or metadata key returns or contains an enumerable, each item in the enumerable
    /// will become one of the grouping keys. If a document contains multiple group keys, it will
    /// be included in multiple groups. A good example is a tag engine where each document can contain
    /// any number of tags and you want to make groups for each tag including all the documents with that tag.
    /// Each input document is cloned for each group and metadata related 
    /// to the groups, including the sequence of documents for each group, 
    /// is added to each clone. For example, if you have 2 input documents
    /// and the result of grouping is 3 groups, this module will output 6 documents.
    /// </remarks>
    /// <metadata name="GroupDocuments" type="IEnumerable&lt;IDocument&gt;">Contains all the documents for the current group.</metadata>
    /// <metadata name="GroupKey" type="object">The key for the current group.</metadata>
    /// <category>Control</category>
    public class GroupByMany : IModule
    {
        private readonly DocumentConfig _key;
        private readonly IModule[] _modules;
        private Func<IDocument, IExecutionContext, bool> _predicate;

        /// <summary>
        /// Partitions the result of the specified modules into groups with matching keys 
        /// based on the key delegate.
        /// The input documents to GroupBy are used as 
        /// the initial input documents to the specified modules.
        /// </summary>
        /// <param name="key">A delegate that returns the group keys.</param>
        /// <param name="modules">Modules to execute on the input documents prior to grouping.</param>
        public GroupByMany(DocumentConfig key, params IModule[] modules)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            _key = key;
            _modules = modules;
        }

        /// <summary>
        /// Partitions the result of the specified modules into groups with matching keys 
        /// based on the value at the specified metadata key.
        /// If a document to group does not contain the specified metadata key, it is not included in any output groups.
        /// The input documents to GroupBy are used as 
        /// the initial input documents to the specified modules.
        /// </summary>
        /// <param name="keyMetadataKey">The key metadata key.</param>
        /// <param name="modules">Modules to execute on the input documents prior to grouping.</param>
        public GroupByMany(string keyMetadataKey, params IModule[] modules)
        {
            if (keyMetadataKey == null)
            {
                throw new ArgumentNullException(nameof(keyMetadataKey));
            }

            _key = (doc, ctx) => doc.Get(keyMetadataKey);
            _modules = modules;
            _predicate = (doc, ctx) => doc.ContainsKey(keyMetadataKey);
        }

        /// <summary>
        /// Limits the documents to be grouped to those that satisfy the supplied predicate.
        /// </summary>
        /// <param name="predicate">A delegate that should return a <c>bool</c>.</param>
        public GroupByMany Where(DocumentConfig predicate)
        {
            Func<IDocument, IExecutionContext, bool> currentPredicate = _predicate;
            _predicate = currentPredicate == null
                ? (Func<IDocument, IExecutionContext, bool>)(predicate.Invoke<bool>)
                : ((x, c) => currentPredicate(x, c) && predicate.Invoke<bool>(x, c));
            return this;
        }

        public IEnumerable<IDocument> Execute(IReadOnlyList<IDocument> inputs, IExecutionContext context)
        {
            ImmutableArray<IGrouping<object, IDocument>> groupings = context.Execute(_modules, inputs)
                .Where(x => _predicate?.Invoke(x, context) ?? true)    
                .GroupByMany(x => _key.Invoke<IEnumerable<object>>(x, context))
                .ToImmutableArray();
            if (groupings.Length == 0)
            {
                return inputs;
            }
            return inputs.SelectMany(input =>
            {
                return groupings.Select(x => context.GetDocument(input,
                    new MetadataItems
                    {
                        {Keys.GroupDocuments, x.ToImmutableArray()},
                        {Keys.GroupKey, x.Key}
                    })
                );
            });
        }
    }
}
