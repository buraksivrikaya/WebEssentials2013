﻿using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MadsKristensen.EditorExtensions.Classifications.Markdown;
using Microsoft.Html.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace WebEssentialsTests
{
    [TestClass]
    public class MarkdownClassifierTests
    {
        static List<Tuple<string, string>> Classify(string markdown, Span? subSpan = null)
        {
            var artifacts = new ArtifactCollection(new MarkdownCodeArtifactProcessor());
            artifacts.Build(markdown);
            var classifier = new MarkdownClassifier(artifacts, new MockClassificationTypeRegistry());
            var results = classifier.GetClassificationSpans(new SnapshotSpan(new MockSnapshot(markdown), subSpan ?? new Span(0, markdown.Length)))
                             .Select(cs => Tuple.Create(cs.ClassificationType.Classification, markdown.Substring(cs.Span.Start, cs.Span.Length)))
                             .ToList();
            return results;
        }

        static Func<string, Tuple<string, string>> CreateCreator(string classificationType) { return source => Tuple.Create(classificationType, source); }

        static readonly Func<string, Tuple<string, string>> Bold = CreateCreator(MarkdownClassificationTypes.MarkdownBold);
        static readonly Func<string, Tuple<string, string>> Code = CreateCreator(MarkdownClassificationTypes.MarkdownCode);
        static readonly Func<string, Tuple<string, string>> Header = CreateCreator(MarkdownClassificationTypes.MarkdownHeader);
        static readonly Func<string, Tuple<string, string>> Italic = CreateCreator(MarkdownClassificationTypes.MarkdownItalic);
        static readonly Func<string, Tuple<string, string>> Quote = CreateCreator(MarkdownClassificationTypes.MarkdownQuote);

        [TestMethod]
        public void TestBasicClassifications()
        {
            Classify(@"**bold**, _italic_
#Header
>> ##Header2
").Should().Equal(
                Bold("**bold**"),
                Italic("_italic_"),
                Header("#Header"),
                Header("##Header2"),
                Quote(" ##Header2")
            );

            Classify("**bold**").Should().Equal(Bold("**bold**"));
            Classify("#Header").Should().Equal(Header("#Header"));
            Classify("  >  >  >Quote!").Should().Equal(Quote("Quote!"));
        }

        [TestMethod]
        public void TestCodeBlocks()
        {
            Classify(@"`code`").Should().Equal(Code("`code`"));
            Classify(@"`code` _ab_").Should().Equal(Code("`code`"), Italic("_ab_"));
            Classify(@"**a** `code`").Should().Equal(Bold("**a**"), Code("`code`"));

            Classify(@"`code` ... `more _code_!`").Should().Equal(Code("`code`"), Code("`more _code_!`"));
            Classify(@"`int *a;` ... `int* b = a;`").Should().Equal(Code("`int *a;`"), Code("`int* b = a;`"));
            Classify(@"
```cs
var x = 2;
var y = 3;").Should().Equal(Code("var x = 2;"), Code("var y = 3;"));
            Classify(@"    font-weight: _bold_;").Should().Equal(Code("    font-weight: _bold_;"));
        }
        // TODO: Test quoted code blocks, test overlapping partial spans.
    }

    class MockClassificationTypeRegistry : IClassificationTypeRegistryService
    {
        public IClassificationType CreateClassificationType(string type, IEnumerable<IClassificationType> baseTypes)
        { throw new NotImplementedException(); }
        public IClassificationType CreateTransientClassificationType(params IClassificationType[] baseTypes)
        { throw new NotImplementedException(); }
        public IClassificationType CreateTransientClassificationType(IEnumerable<IClassificationType> baseTypes)
        { throw new NotImplementedException(); }

        public IClassificationType GetClassificationType(string type)
        {
            return new MockType(type);
        }
        class MockType : IClassificationType
        {
            public MockType(string type) { Classification = type; }

            public IEnumerable<IClassificationType> BaseTypes { get { yield break; } }
            public string Classification { get; private set; }

            public bool IsOfType(string type)
            {
                return Classification.Equals(type, StringComparison.OrdinalIgnoreCase) || BaseTypes.Any(b => b.IsOfType(type));
            }
        }
    }
}
