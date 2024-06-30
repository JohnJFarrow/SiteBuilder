using Markdig.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiteBuilder
{
    public class Element
    {
        public Element(string inname)
        {
            name = inname;
        }

        public Element()
        {
        }

        public Element Addprop(string name, string value)
        {
            props.Add(name, value);
            return this;
        }

        public Element Class(string value)
        {
            props.Add("class", value);
            return this;
        }

        public Element Href(string value)
        {
            props.Add("href", value);
            return this;
        }

        public Element Id(string value)
        {
            props.Add("id", value);
            return this;
        }

        public Element Add(string name, string value)
        {
            props.Add(name, value);
            return this;
        }

        public virtual Element Add(Element? sub)
        {
            if (sub == null)
            {
                return this;
            }

            children.Add(sub);
            return this;
        }

        public virtual List<string> Render()
        {
            List<string> text = new List<string>();
            if (props.Count == 0)
            {
                text.Add("<" + name + ">");
            }
            else
            {
                string v = "<" + name;
                foreach (KeyValuePair<string, string> prop in props)
                {
                    v += " " + prop.Key + "=" + "\"" + prop.Value + "\"";
                }
                v += ">";
                text.Add(v);
            }

            foreach (Element child in children)
            {
                text.AddRange(child.Render());
            }

            text.Add("</" + name + ">");
            return text;
        }

        public Element AddText(string? intext)
        {
            if (intext != null)
            {
                Add(new InlineTextElement(intext));
            }
            return this;
        }

        protected Dictionary<string, string> props = new Dictionary<string, string>();
        protected List<Element> children = new List<Element>();
        protected string name = "";

    }

    public class Div : Element
    {
        public Div() : base("div")
        {
        }

        public Div(string Class) : base("div")
        {
            Addprop("class", Class);
        }
    }

    public class Header : Element
    {
        public Header() : base("header")
        {
        }

        public Header(string Class) : base("header")
        {
            Addprop("class", Class);
        }
    }

    public class Footer : Element
    {
        public Footer() : base("footer")
        {
        }

        public Footer(string Class) : base("footer")
        {
            Addprop("class", Class);
        }
    }

    public class A : Element
    {
        public A() : base("a")
        {
        }

        public A(string Class) : base("a")
        {
            Addprop("class", Class);
        }

    }

    public class Nav : Element
    {
        public Nav(string Class) : base("nav")
        {
            Addprop("class", Class);
        }
    }

    public class Span : Element
    {
        public Span() : base("span")
        {
        }

        public Span(string Class) : base("span")
        {
            Addprop("class", Class);
        }

    }


    public class Aside : Element
    {
        public Aside() : base("aside")
        {
        }
    }

    public class UL : Element
    {
        public UL() : base("ul")
        {
        }

        public UL(string Class) : base("ul")
        {
            Addprop("class", Class);
        }
    }

    public class LI : Element
    {
        public LI() : base("li")
        {
        }

        public LI(string Class) : base("li")
        {
            Addprop("class", Class);
        }
    }

    public class Section : Element
    {
        public Section() : base("section")
        {
        }

        public Section(string Class) : base("section")
        {
            Addprop("class", Class);
        }
    }

    public class Table : Element
    {
        public Table() : base("table")
        {
        }

        public Table(string Class) : base("table")
        {
            Addprop("class", Class);
        }
    }

    public class Row : Element
    {
        public Row() : base("tr")
        {
        }

        public Row(string Class) : base("tr")
        {
            Addprop("class", Class);
        }
    }

    public class Cell : Element
    {
        public Cell() : base("td")
        {
        }

        public Cell(string Class) : base("td")
        {
            Addprop("class", Class);
        }
    }

    public class P : Element
    {
        public P() : base("p")
        {
        }

        public P(string Class) : base("p")
        {
            Addprop("class", Class);
        }

    }

    public class H1 : Element
    {
        public H1() : base("h1")
        {
        }

        public H1(string Klass ) : base("h1")
        {
        }
    }

    public class H2 : Element
    {
        public H2() : base("h2")
        {
        }
    }


    public class Main : Element
    {
        public Main() : base("main")
        {
        }

        public Main(string Class) : base("main")
        {
            Addprop("class", Class);
        }

    }

    public class InlineTextElement : Element
    {
        public InlineTextElement(string intext)
        {
            text = intext;
        }

        public override List<string> Render()
        {
            List<string> lines = new List<string>();
            lines.Add(text);
            return lines;
        }

        string text;
    }

    public class TOCHeading
    {
        public HeadingBlock? Block = null;
        public List<TOCHeading> children = new List<TOCHeading>();
    }


}