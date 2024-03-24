using Markdig.Syntax;
using SiteBuilder;

namespace SiteBuilder
{


    public class Indexable
    {
        public string? filename;
        public string? filecontent;
        public MarkdownDocument? markdown;
        public Metadata? metadata;

        public string GetOutFileName(bool bIncludePath = false)
        {
            if (filename == null)
            {
                throw new Exception("Indexable file name is null");
            }

            // file is something like ./content\BasicsOfJson.html
            FileInfo fi = new FileInfo(filename);

            if (bIncludePath)
            {
                return "output/" + fi.Name.Replace(".md", ".html");
            }
            else
            {
                return fi.Name.Replace(".md", ".html");
            }
        }
    }
}
