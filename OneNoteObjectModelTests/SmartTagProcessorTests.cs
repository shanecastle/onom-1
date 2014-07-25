﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using NUnit.Framework;
using OnenoteCapabilities;
using OneNoteObjectModel;
using List = NUnit.Framework.List;

namespace OneNoteObjectModelTests
{
    // tests cases for our poor man XML parsing. 
    [TestFixture]
    public class SmartTagXMLProcessingTests
    {
        public SmartTagXMLProcessingTests()
        {
            _cursor = new OneNotePageCursor();
        }

        private OneNotePageCursor _cursor;

        [Test] 
        public void TestNotTag()
        {
            var unprocessedTag = "notATag Tag2";
            Assert.That(!SmartTagAugmenter.IsSmartTag(unprocessedTag));
        }

        [Test] 
        public void TestSimpleTag()
        {
            var unprocessedTag = "#unprocessedTag Tag2";
            Assert.That(SmartTagAugmenter.IsSmartTag(unprocessedTag));

            var st = SmartTagAugmenter.SmartTagFromElement(unprocessedTag,null, _cursor);
            Assert.That(st.TextAfterTag(), Is.EqualTo("Tag2"));
            Assert.That(st.TagName() , Is.EqualTo("unprocessedTag"));
        }

        [Test] 
        public void TestPunctuation()
        {
            var unprocessedTag = "#unprocessedTag Tag2 - ! , fad|";
            Assert.That(SmartTagAugmenter.IsSmartTag(unprocessedTag));

            var st = SmartTagAugmenter.SmartTagFromElement(unprocessedTag,null, _cursor);
            Assert.That(st.TextAfterTag(), Is.EqualTo("Tag2 - ! , fad|"));
            Assert.That(st.TagName() , Is.EqualTo("unprocessedTag"));
        }

    }

[TestFixture]
    public class SmartTagProcessorTests
    {


    private TemporaryNoteBookHelper smartTagNoteBook;
        private OneNoteApp ona;

        [SetUp]
        public void Setup()
        {
            this.ona = new OneNoteApp();
            smartTagNoteBook = new TemporaryNoteBookHelper(ona, "SmartTag");
            this._templateNotebook = new TemporaryNoteBookHelper(ona, "SmartTagTemplates");
            this._settingsSmartTags = new SettingsSmartTags()
            {
                    TemplateNotebook = _templateNotebook.Get().name,
                    SmartTagStorageNotebook =  smartTagNoteBook.Get().name
            };

            // create template structure.
            var templateSection  = ona.CreateSection(_templateNotebook.Get(), _settingsSmartTags.TemplateSection);
            var smartTagTemplatePage = ona.CreatePage(templateSection, _settingsSmartTags.SmartTagTemplateName);


            // create smartTagModelTemplate
            // the copied page has a PageID, replace it with PageID from the newly created page.
            var filledInTempaltePage = string.Format(_smartTagTestsPageConent.smartTagModelTemplatePageWithDumbTodoTable, smartTagTemplatePage.ID, smartTagTemplatePage.name);
            ona.OneNoteApplication.UpdatePageContent(filledInTempaltePage);

            // create smartTag model structure.
            ona.CreateSection(smartTagNoteBook.Get(), _settingsSmartTags.SmartTagStorageSection);

            this.defaultSection = ona.CreateSection(smartTagNoteBook.Get(), "section_to_find_content");
            var p = ona.CreatePage(defaultSection, Guid.NewGuid().ToString());

            // the copied page has a PageID, replace it with PageID from the newly created page.
            var filledInPageHeader = string.Format(_smartTagTestsPageConent.pageWithOneProcessedAndOneUnProcessedSmartTagHeader, p.ID, p.name);
            this.pageContent = XDocument.Parse(filledInPageHeader+ _smartTagTestsPageConent.pageWithOneProcessedAndOneUnProcessedSmartTagBody);
            ona.OneNoteApplication.UpdatePageContent(pageContent.ToString());

            _cursor = new OneNotePageCursor();
            this.smartTagAugmenter = new SmartTagAugmenter(ona, _settingsSmartTags, new List<ISmartTagProcessor>());
        }


        // TODO: Come up with a better template for such tests.

        private SmartTagAugmenter smartTagAugmenter;
        private XDocument pageContent;
        private Section defaultSection;
        private TemporaryNoteBookHelper _templateNotebook;
        private SettingsSmartTags _settingsSmartTags;
        private readonly SmartTagTestsPageConent _smartTagTestsPageConent = new SmartTagTestsPageConent();
        private OneNotePageCursor _cursor;

    [Test]
        public void EnumerateSmartTags()
        {
            var smartTags = SmartTagAugmenter.GetSmartTags(pageContent, _cursor);
            Assert.That(smartTags.Count(), Is.EqualTo(2));
            Assert.That(smartTags.Count(st => st.IsProcessed()), Is.EqualTo(1));
        }

        [Test]
        public void TestAugmentPage()
        {
            var smartTags = SmartTagAugmenter.GetSmartTags(this.pageContent, _cursor);
            Assert.That(smartTags.Count(), Is.EqualTo(2));
            Assert.That(smartTags.Count(st => st.IsProcessed()), Is.EqualTo(1));

            this.smartTagAugmenter.AugmentPage(ona,pageContent, new OneNotePageCursor());

            var smartTagsPostAugement = SmartTagAugmenter.GetSmartTags(this.pageContent, _cursor);
            Assert.That(smartTagsPostAugement.Count(), Is.EqualTo(2));
            Assert.That(smartTagsPostAugement.All(st => st.IsProcessed()));
        }

        [Test]
        public void ProcessSmartTagAndLinkToOneNotePage()
        {
            var smartTag = SmartTagAugmenter.GetSmartTags(this.pageContent, _cursor).First(st=>!st.IsProcessed());
            Assert.That(!smartTag.IsProcessed());

            // Brittle test, manually executing augmentation.
            smartTagAugmenter.AddToModel(smartTag,this.pageContent);
            smartTagAugmenter.AddLinkToSmartTag(smartTag, this.pageContent, this.defaultSection,"DeadPage");

            Assert.That(smartTag.IsProcessed());
        }
        [Test]
        public void ProcessSmartTagAndLinkToURI()
        {
            var smartTag = SmartTagAugmenter.GetSmartTags(this.pageContent, _cursor).First(st=>!st.IsProcessed());
            Assert.That(!smartTag.IsProcessed());
            smartTagAugmenter.AddToModel(smartTag,this.pageContent);
            Assert.That(smartTag.IsProcessed());

            // Set link to URI.
            smartTagAugmenter.AddLinkToSmartTag(smartTag,this.pageContent, new Uri("http://helloworld"));
        }


        [TearDown]
        public void TearDown()
        {
            smartTagNoteBook.Dispose();
            _templateNotebook.Dispose();
        }
    }
}
