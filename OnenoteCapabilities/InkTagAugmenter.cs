using System.Linq;
using System.Xml.Linq;
using OneNoteObjectModel;

namespace OnenoteCapabilities
{
    public class InkTagAugmenter : IPageAugmenter
    {
        public void AugmentPage(XDocument pageContentInXml, OneNotePageCursor cursor)
        {
            var inkTags = InkTag.Get(pageContentInXml).ToList();
            inkTags.ForEach(inkTag => inkTag.ToText());
            if (inkTags.Any())
            {
                // toText does not update the pageContent so do it explicitly.
                OneNoteApplication.Instance.UpdatePageContent(pageContentInXml);
            }
        }
    }
}
