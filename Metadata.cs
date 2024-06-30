
namespace SiteBuilder
{


    public class Metadata
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Date { get; set; }
        public string? Sidebar { get; set; } // can have "auto"
        public string? UnrealVersion { get; set; } // can have "auto"

        // if index: is true it gets included in the index and the .html file gets generated
        // if index: is false but workinprogress: true then the .html file gets generated but it is not in the index

        public string? Tags { get; set; } // can have "auto"

        public bool Index { get; set; }
        public bool Draft { get; set; }

        public Int32 Order { get; set; }

        public Metadata()
        {
            Index = false;
            Draft = false;
            Tags = "";
        }
    }

}