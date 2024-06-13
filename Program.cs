using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Markdown.ColorCode;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;


void Usage()
{
    Console.WriteLine(@"--index    create output\index.html");
    Console.WriteLine(@"--update   update all indexed pages to .\output");
    Console.WriteLine(@"--refresh  refresh browser window");
    Console.WriteLine(@"--all      --update and --index and --refresh");
    Console.WriteLine(@"--watch    watch for file changes, regenerate the page which changed");
    Console.WriteLine(@"           regenerate the index, refresh the browser");
}

if (args.Length == 0)
{
    Usage();
    return;
}

SiteBuilder.Builder S = new SiteBuilder.Builder();

if (args[0] == "--index" )
{
    S.RecreateIndex();
}
else if (args[0] == "--update")
{
    S.RecreateIndex();
    S.UpdateIndexedPages();
}
else if (args[0] == "--refresh")
{
    S.RefreshBrowserWindows();
}
else if (args[0] == "--all")
{
    S.RecreateIndex();
    S.UpdateIndexedPages();
    S.RefreshBrowserWindows();
}
else if (args[0] == "--watch" )
{
    S.Watch();
}
else
{
    Usage();
}

return;

namespace SiteBuilder
{
    partial class Builder
    {

        string CONTENT_DIR = @"./content";
        string CONTENT_PATTERN = @"*.md";
        bool bAddSidebarLevelNumbers = false;

        private MarkdownPipeline GetPipeline()
        {
            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseYamlFrontMatter()
                .UseGridTables()
                .UsePipeTables()
                .UseColorCode(
                    HtmlFormatterType.Css
                )
                .EnableTrackTrivia()
                .UseDiagrams()
                .Use<YamlExpanderExtension>()
                .Build();

            return pipeline;
        }

        private List<Indexable> GetIndexables(bool verbose = false)
        {
            // read each of the *.md files in ./content, retrieve the TITLE and DESCRIPTION fields
            var files = Directory.GetFiles(CONTENT_DIR, CONTENT_PATTERN);

            var pipeline = GetPipeline();

            List<Indexable> indexables = new List<Indexable>();

            foreach (string file in files)
            {
                Indexable Ix = new Indexable();
                Ix.filename = file;

                FileStream? stream = null;
                while (IsFileLocked(file, out stream))
                {
                    System.Threading.Thread.Sleep(200);
                    Console.WriteLine("sleeping 2 200");
                }
                if (stream != null)
                {
                    stream.Close();
                }

                Ix.filecontent = File.ReadAllText(file);

                Ix.markdown = Markdig.Markdown.Parse(Ix.filecontent, pipeline);

                var yamlBlock = Ix.markdown.Descendants<YamlFrontMatterBlock>().FirstOrDefault();

                if (yamlBlock == null)
                {
                    if (verbose)
                    {
                        Console.WriteLine("index excluding {0}, has no metadata", file);
                    }
                    continue;
                }

                var YamlLines = yamlBlock.Lines.ToString();

                if (YamlLines == null)
                {
                    if (verbose)
                    {
                        Console.WriteLine("index excluding {0}, has no metadata", file);
                    }
                    continue;
                }

                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                try
                {
                    Ix.metadata = deserializer.Deserialize<Metadata>(YamlLines);
                }
                catch (YamlDotNet.Core.SemanticErrorException Ex)
                {
                    // "Could not find expected key" means with this:
                    // index:true
                    // there MUST be a space after the :

                    Console.WriteLine(Ex.Message);
                    throw;
                }

                // if index=false but draft=true add it to the list, will generate .html but
                // not include in index
                if (!Ix.metadata.Index && !Ix.metadata.Draft)
                {
                    if (verbose)
                    {
                        Console.WriteLine("index excluding {0}, index != true", file);
                    }
                    continue;
                }

                if (Ix.metadata.Title == "")
                {
                    if (verbose)
                    {
                        Console.WriteLine("index excluding {0}, no title", file);
                    }
                    continue;
                }

                if (Ix.metadata.Description == "")
                {
                    if (verbose)
                    {
                        Console.WriteLine("index excluding {0}, no description", file);
                    }
                    continue;
                }

                indexables.Add(Ix);
            }

            indexables.Sort(delegate (Indexable x, Indexable y)
            {
                // lowest order number is last on page so negate
                return -x!.metadata!.Order.CompareTo(y!.metadata!.Order);
            });

            return indexables;
        }

        private void Write(string filename, List<String> head, List<string> body)
        {
            using (StreamWriter stream = new StreamWriter(filename))
            {
                stream.WriteLine("<!DOCTYPE html>");
                stream.WriteLine("<html lang=\"en-US\">");

                stream.WriteLine("<head>");
                foreach (string line in head)
                {
                    stream.WriteLine(line);
                }
                stream.WriteLine("</head>");

                stream.WriteLine("<body>");
                foreach (string line in body)
                {
                    stream.WriteLine(line);
                }
                stream.WriteLine("</body>");

                stream.WriteLine("</html>");
            }
        }

        private void HeadAddMeta( List<string> head )
        {
            head.Add("<meta charset=\"utf-8\">");
            head.Add("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">");
        }

        private void HeadAddStylesheets(List<string> head)
        {
            head.Add("<link rel=\"stylesheet\" type=\"text/css\" href=\"./css/styles.css\">");
            head.Add("<link rel=\"stylesheet\" type=\"text/css\" href=\"./css/syntaxhighlightingstyles.css\">");
            head.Add("<link rel=\"stylesheet\" type=\"text/css\" href=\"./css/print.css\" media=\"print\">");
        }

        private void HeadAddMermaid(List<string> head)
        {
            head.Add("<script type=\"module\">");
            head.Add("import mermaid from 'https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.esm.min.mjs';");
            // TODO maybe move this to a text file
            head.Add("mermaid.initialize({ startOnLoad: true, theme: 'base', 'themeVariables': {'darkMode': true,'primaryTextColor':'#000','primaryColor': '#fff','primaryBorderColor': '#000','lineColor': 'yellow','secondaryColor': '#006100','tertiaryColor': '#fff' } });");
            head.Add("</script>");
        }

        private List<string> GetIndexHead()
        {
            var head = new List<string>();

            HeadAddMeta(head);
            HeadAddStylesheets(head);
            HeadAddMermaid(head);

            head.Add("<title>unrealcode.net</title>");
            head.Add("<meta name=\"description\" content=\"\">");

            return head;
        }

        private List<string> GetPageHead(Metadata? Meta)
        {
            System.Diagnostics.Debug.Assert(Meta != null);

            var head = new List<string>();

            HeadAddMeta(head);
            HeadAddStylesheets(head);
            HeadAddMermaid(head);

            head.Add("<title>" + Meta.Title + " | unrealcode.net</title>");
            head.Add("<meta name=\"description\" content=\"" + Meta.Title + "\">");

            return head;
        }

        private Element GetHeaderNavbar()
        {
            return new Header("navbar")
                .Add(new Div("sidebar-button"))
                .Add(new A()
                        .Href("index.html")
                        .Add("aria-current", "page")
                        .Class("home-link router-link-exact-active router-link-active")
                        .Add(new Span()
                                .AddText("unrealcode.net")
                                .Class("site-name")
                            )
                )
                .Add(new Div("links")
                    .Add(new Nav("nav-links can-hide")
                            .Add(new Div("nav-item")
                                .Add(new A()
                                    .Href("index.html")
                                    .Add("aria-current", "page")
                                    .Class("nav-link router-link-exact-active router-link-active")
                                    .AddText("Home")
                                    )
                                )
                            )
                );
        }

        private Element GetSidebarMask()
        {
            return new Div().Class("sidebar-mask");
        }


        private Element GetTopNavLinks()
        {
            return new Nav("nav-item")
                .Add(new A()
                    .Href("/")
                    .Add("aria-current", "page")
                    .Class("nav-link router-link-exact-active router-link-active")
                    .AddText("Home")
                    );
        }

        private List<string> GetIndexBody(List<Indexable> indexables)
        {
            List<string> text = new List<string>();

            text = new Div()
                .Addprop("id", "app")
                .Addprop("data-server-rendered", "true")
                .Add(new Div("theme-container no-sidebar")
                      .Add(GetHeaderNavbar())
                      .Add(GetSidebarMask())

                        // aside
                        .Add(new Aside().Class("sidebar")
                            .Add(GetTopNavLinks())
                        )
                        // main 
                        .Add(new Main("home").Add("aria-labelledby", "main-title")

                                //.Add(new Header("hero")
                                //        .Add(new Span("main-header-part1")
                                //                .AddText("UnrealCode:")
                                //            )
                                //)

                                .Add(new Header("hero")
                                        .Add(new Div("main-header-part1")
                                                .AddText("Experiments with Unreal Engine")
                                            )
                                    )

                                .Add(new Div("theme-default-content custom content__default")

                                        .Add(DivsForIndexables(indexables))
                                    )
                            )

                        .Add(PageFooter())




                 )
                .Render();

            return text;
        }

        private Element? CreateSidebarSubheaders(TOCHeading Current)
        {
            if (Current == null || Current.Block == null || Current.children == null || Current.children.Count == 0)
            {
                return null;
            }

            Element ul = new UL("sidebar-sub-headers");
            foreach (var Child in Current.children)
            {
                var title = CombineHeadingText(Child);
                var Anchor = CreateLinkAnchor(title);

                if (bAddSidebarLevelNumbers)
                {
                    if (Child.Block != null)
                    {
                        title += "[" + Child.Block.Level + "]";
                    }
                }

                ul.Add(new LI("sidebar-sub-header")
                            .Add(new A("sidebar-link")
                                      .Href(Anchor)
                                      .AddText(title)
                                      )
                            );

            }

            return ul;
        }

        private string CreateLinkAnchor(string title)
        {
            // convert a string like "Necessary Includes" to "#necessary-includes"

            string Anchor = title.Trim().ToLower().Replace(@" ", @"-");
            return "#" + Anchor;
        }

        private string CombineHeadingText(TOCHeading Current)
        {
            string value = "";

            if (Current != null && Current.Block != null && Current.Block.Inline != null)
            {
                foreach (var item in Current!.Block!.Inline)
                {
                    value += item.ToString();
                }
            }

            return value;
        }

        private Element ListForHeaders(Indexable Ix)
        {
            // make a table of contents for a single article, created from the ## heading markdown lines

            Element element = new UL("sidebar-links sidebar-group-items");

            List<TOCHeading> Headings = new List<TOCHeading>();
            TOCHeading? CurrentHeader = null;

            if (Ix != null && Ix.markdown != null)
            {
                // make a tree of heading, subheading, subheading 

                foreach (HeadingBlock item in Ix.markdown.Descendants<HeadingBlock>())
                {
                    if (item == null) continue;

                    if (CurrentHeader == null)
                    {
                        CurrentHeader = new TOCHeading();
                        CurrentHeader.Block = item;
                        Headings.Add(CurrentHeader);
                    }
                    else
                    {
                        // might be a child of the current header, might be a new header at a higher or equal level
                        int NewLevel = item.Level;
                        int CurrentLevel = CurrentHeader!.Block!.Level;

                        if (NewLevel > CurrentLevel)
                        {
                            TOCHeading NewChild = new TOCHeading();
                            NewChild.Block = item;
                            CurrentHeader.children.Add(NewChild);
                        }
                        else
                        {
                            // close current one, make a new one
                            CurrentHeader = new TOCHeading();
                            CurrentHeader.Block = item;
                            Headings.Add(CurrentHeader);
                        }
                    }
                }

                // now render

                foreach (TOCHeading Current in Headings)
                {
                    var title = CombineHeadingText(Current);
                    var Anchor = CreateLinkAnchor(title);

                    if (bAddSidebarLevelNumbers)
                    {
                        if (Current.Block != null)
                        {
                            title += "[" + Current.Block.Level + "]";
                        }
                    }

                    element.Add(
                        new LI()
                            .Add(new A("sidebar-link")
                                    .Href(Anchor)
                                    .AddText(title)
                                 )
                            .Add(CreateSidebarSubheaders(Current))

                            );
                }

                // then add all child elements in a UL/LI 

                /*
                    * 	<li><a href="/content/BasicsOfJson.html#creating-json" class="sidebar-link">Creating
                                        Json</a>
                                    <ul class="sidebar-sub-headers">
                                        <li class="sidebar-sub-header"><a
                                                href="/content/BasicsOfJson.html#writing-json-to-a-file"
                                                class="sidebar-link">Writing Json to a file</a></li>
                                    </ul>
                                </li>

                    look at the file source.html 
                */
            }
            return element;
        }
        private Element CreateSidebar(Indexable Ix)
        {
            if (Ix == null) throw new ArgumentNullException();

            return new Aside().Class("sidebar")
                //.Add(GetTopNavLinks())
                // one link for each ## header of some level
                .Add(new UL("sidebar-links")
                        .Add(new LI()
                                .Add(new Section("sidebar-group depth-0")
                                        .Add(new P("sidebar-heading open")
                                            .Add(new Span()
                                                    .AddText(Ix!.metadata!.Title)
                                                    )
                                            )
                                            .Add(ListForHeaders(Ix))
                                        )
                                )
                        );


        }

        private Element CreateMainContent(Indexable Ix)
        {
            Element div = new Main().Class("page");

            Element div2 = new Div("theme-default-content content__default");

            div.Add(div2);

            var Pipe = GetPipeline();

            TextWriter TW = new StringWriter();
            var renderer = new Markdig.Renderers.HtmlRenderer(TW);

            bool OneHit = true;

            if (OneHit)
            {
                if (Ix.filecontent != null)
                {
                    var outer = Markdig.Markdown.Convert(Ix.filecontent, renderer, Pipe);
                    div2.AddText(outer.ToString());
                }
            }
            else
            {
                //foreach( var item in Ix.markdown )
                {
                    //var outer = Markdown.Convert( item, renderer, Pipe);
                    //div.AddText(outer.ToString());
                }
            }

            div.Add(PageFooter());

            return div;

        }

        private List<string> GetPageBody(Indexable Ix)
        {
            List<string> text = new List<string>();

            if (Ix.metadata != null)
            {

                text = new Div()
                    .Addprop("id", "app")
                    .Addprop("data-server-rendered", "true")
                    .Add(new Div("theme-container"))
                          .Add(GetHeaderNavbar())
                          .Add(GetSidebarMask())

                          .Add(CreateSidebar(Ix))

                          .Add(CreateMainContent(Ix))



                .Render();

            }

            return text;
        }
        private Element DivsForIndexables(List<Indexable> indexables)
        {
            Element element = new Div();
            foreach (Indexable Ix in indexables)
            {
                // might be index=false Draft=true
                if ( Ix.metadata != null && Ix.metadata.Index)
                {
                    element.Add(new Div()
                                    .Add(new H2()
                                            .Add(new A()
                                                      .Href(Ix.GetOutFileName())
                                                      .AddText(Ix!.metadata!.Title)
                                                 )
                                        )
                                    .Add(new P().AddText(Ix.metadata.Description))
                                    .Add(new A().Href(Ix.GetOutFileName())
                                                .AddText("Read more")
                                        )
                                );
                }
            }

            return element;
        }

        public void RecreateIndex(List<Indexable>? indexables = null )
        {
            // some callers already have the list of indexables
            if (indexables == null)
            {
                indexables = GetIndexables();
            }

            Write(@"output\index.html",
                GetIndexHead(),
                GetIndexBody(indexables));
        }

        private Element PageFooter()
        {
            Element F = new Footer("no-sidebar")
                .Add(new Div()
                         .Add(new P("page-footer")
                                    .AddText("MIT Licensed | Copyright © 2020-2024 John Farrow")
                                    .AddText(": john.farrow@unrealcode.net"))

                    );

            return F;
        }

        public void RefreshBrowserWindows()
        {
            Refresher.Refresh();
        }

        public void UpdateIndexedPages()
        {
            List<Indexable> indexables = GetIndexables();

            foreach (var Ix in indexables)
            {
                Console.WriteLine("updating {0} to {1}", Ix.filename, Ix.GetOutFileName(true).Replace(@"/", @"\"));

                Write(Ix!.GetOutFileName(true), GetPageHead(Ix!.metadata), GetPageBody(Ix));
            }
        }

        public void UpdateOneFile( string? SelectedFile )
        {
            if (SelectedFile == null) return;

            // load all the indexables because we need the list for regenerating the index anyway

            List<Indexable> indexables = GetIndexables();

            // is the changed/created file in the list, maybe it does not have index:true in the front matter
            Indexable? ChosenIx = null;
            foreach( Indexable Ix in indexables )
            {
                if( Ix!.filename!.EndsWith( SelectedFile ))
                {
                    ChosenIx = Ix;
                    break;
                }
            }

            if( ChosenIx != null )
            {
                Console.WriteLine("updating {0} to {1}", ChosenIx.filename, ChosenIx.GetOutFileName(true).Replace(@"/", @"\"));
                Write(ChosenIx!.GetOutFileName(true), GetPageHead(ChosenIx!.metadata), GetPageBody(ChosenIx));

                RecreateIndex(indexables);

                RefreshBrowserWindows();

            }
        }

    }
}