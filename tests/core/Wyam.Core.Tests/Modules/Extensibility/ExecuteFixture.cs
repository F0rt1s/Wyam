﻿using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using NUnit.Framework;
using Wyam.Common.Documents;
using Wyam.Common.Execution;
using Wyam.Common.Modules;
using Wyam.Core.Execution;
using Wyam.Core.Modules.Extensibility;
using Wyam.Testing;

namespace Wyam.Core.Tests.Modules.Extensibility
{
    [TestFixture]
    [Parallelizable(ParallelScope.Self | ParallelScope.Children)]
    public class ExecuteFixture : BaseFixture
    {
        public class ExecuteTests : ExecuteFixture
        {
            [Test]
            public void DoesNotThrowForNullResultWithDocumentConfig()
            {
                // Given
                Engine engine = new Engine();
                Execute execute = new Execute((d, c) => null);
                engine.Pipelines.Add(execute);

                // When
                engine.Execute();

                // Then
            }

            [Test]
            public void DoesNotThrowForNullResultWithContextConfig()
            {
                // Given
                Engine engine = new Engine();
                Execute execute = new Execute(c => null);
                engine.Pipelines.Add(execute);

                // When
                engine.Execute();

                // Then
            }

            [Test]
            public void ThrowsForObjectResultWithContextConfig()
            {
                // Given
                Engine engine = new Engine();
                Execute execute = new Execute(c => 1);
                engine.Pipelines.Add(execute);

                // When, Then
                Assert.Throws<Exception>(() => engine.Execute());
            }

            [Test]
            public void ReturnsInputsForNullResultWithDocumentConfig()
            {
                // Given
                IExecutionContext context = Substitute.For<IExecutionContext>();
                IDocument[] inputs =
                {
                    Substitute.For<IDocument>(),
                    Substitute.For<IDocument>()
                };
                Execute execute = new Execute((d, c) => null);

                // When
                IEnumerable<IDocument> outputs = ((IModule) execute).Execute(inputs, context);

                // Then
                CollectionAssert.AreEqual(inputs, outputs);
            }

            [Test]
            public void ReturnsInputsForNullResultWithContextConfig()
            {
                // Given
                IExecutionContext context = Substitute.For<IExecutionContext>();
                IDocument[] inputs =
                {
                    Substitute.For<IDocument>(),
                    Substitute.For<IDocument>()
                };
                Execute execute = new Execute(c => null);

                // When
                IEnumerable<IDocument> outputs = ((IModule)execute).Execute(inputs, context);

                // Then
                CollectionAssert.AreEqual(inputs, outputs);
            }

            [Test]
            public void DoesNotRequireReturnValueForDocumentConfig()
            {
                // Given
                int a = 0;
                Engine engine = new Engine();
                Execute execute = new Execute((d, c) => { a = a + 1; });
                engine.Pipelines.Add(execute);

                // When
                engine.Execute();

                // Then
            }

            [Test]
            public void DoesNotRequireReturnValueForContextConfig()
            {
                // Given
                int a = 0;
                Engine engine = new Engine();
                Execute execute = new Execute(c => { a = a + 1; });
                engine.Pipelines.Add(execute);

                // When
                engine.Execute();

                // Then
            }

            [Test]
            public void ReturnsDocumentForSingleResultDocumentFromContextConfig()
            {
                // Given
                Engine engine = new Engine();
                IDocument document = Substitute.For<IDocument>();
                Execute execute = new Execute(c => document);
                engine.Pipelines.Add("Test", execute);

                // When
                engine.Execute();

                // Then
                CollectionAssert.AreEquivalent(new[] { document }, engine.Documents["Test"]);
            }

            [Test]
            public void ReturnsDocumentForSingleResultDocumentFromDocumentConfig()
            {
                // Given
                Engine engine = new Engine();
                IDocument document = Substitute.For<IDocument>();
                Execute execute = new Execute((d, c) => document);
                engine.Pipelines.Add("Test", execute);

                // When
                engine.Execute();

                // Then
                CollectionAssert.AreEquivalent(new[] { document }, engine.Documents["Test"]);
            }

            [Test]
            public void RunsSModuleAgainstEachInputDocument()
            {
                // Given
                IExecutionContext context = Substitute.For<IExecutionContext>();
                IDocument[] inputs =
                {
                    Substitute.For<IDocument>(),
                    Substitute.For<IDocument>()
                };
                IModule module = Substitute.For<IModule>();
                Execute execute = new Execute((d, c) => module);

                // When
                ((IModule)execute).Execute(inputs, context).ToList();

                // Then
                context.Received(2).Execute(Arg.Any<IEnumerable<IModule>>(), Arg.Any<IEnumerable<IDocument>>());
            }

            [Test]
            public void RunsModuleAgainstInputDocuments()
            {
                // Given
                IExecutionContext context = Substitute.For<IExecutionContext>();
                IDocument[] inputs =
                {
                    Substitute.For<IDocument>(),
                    Substitute.For<IDocument>()
                };
                IModule module = Substitute.For<IModule>();
                Execute execute = new Execute(c => module);

                // When
                ((IModule)execute).Execute(inputs, context).ToList();

                // Then
                context.Received(1).Execute(Arg.Any<IEnumerable<IModule>>(), Arg.Any<IEnumerable<IDocument>>());
            }

            [Test]
            public void ExecuteRunsModuleAgainstInputDocuments()
            {
                // Given
                IExecutionContext context = Substitute.For<IExecutionContext>();
                IDocument[] inputs =
                {
                    Substitute.For<IDocument>(),
                    Substitute.For<IDocument>()
                };
                IModule module = Substitute.For<IModule>();
                Execute execute = new Execute(c => module);

                // When
                ((IModule)execute).Execute(inputs, context).ToList();

                // Then
                context.Received(1).Execute(Arg.Any<IEnumerable<IModule>>(), Arg.Any<IEnumerable<IDocument>>());
            }

            [Test]
            public void SetsNewContentForInputDocuments()
            {
                // Given
                IExecutionContext context = Substitute.For<IExecutionContext>();
                IDocument[] inputs =
                {
                    Substitute.For<IDocument>(),
                    Substitute.For<IDocument>()
                };
                int count = 0;
                Execute execute = new Execute((d, c) => count++);

                // When
                ((IModule)execute).Execute(inputs, context).ToList();

                // Then
                context.Received(1).GetDocument(Arg.Any<IDocument>(), "0");
                context.Received(1).GetDocument(Arg.Any<IDocument>(), "1");
            }
        }
    }
}
