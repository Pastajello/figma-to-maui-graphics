using System.ComponentModel;
using Newtonsoft.Json;

namespace FigmaSharp.Models;

public class FigmaNode
{
    [JsonIgnore()]
    [Category("General")]
    [DisplayName("Parent")]
    public FigmaNode Parent { get; set; }

    [Category("General")]
    [DisplayName("Id")]
    public string id { get; set; }

    [Category("General")]
    [DisplayName("Name")]
    public string name { get; set; }

    [Category("General")]
    [DisplayName("Type")]
    public string type { get; set; }

    [Category("General")]
    [DisplayName("Visible")]
    [DefaultValue(true)]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
    public bool visible { get; set; }
        
    [Category ("General")]
    [DisplayName ("Fills")]
    public FigmaPaint[] fills { get; set; }
    public bool HasFills => fills?.Length > 0;

    public override string ToString()
    {
        return string.Format("[{0}:{1}:{2}]", type, id, name);
    }
}