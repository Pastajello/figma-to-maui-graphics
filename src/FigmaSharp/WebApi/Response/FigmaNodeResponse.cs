using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace FigmaSharp.Models;


public class FigmaNodeResponse
{
    public string name { get; set; }
    public DateTime lastModified { get; set; }
    public string thumbnailUrl { get; set; }
    public string version { get; set; }
    public string role { get; set; }
    public string editorType { get; set; }
    public string linkAccess { get; set; }

    public Dictionary<string, Node> nodes { get; set; }
}

// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);

public class _26739
{
    public string key { get; set; }
    public string name { get; set; }
    public string styleType { get; set; }
    public bool remote { get; set; }
    public string description { get; set; }
}

public class AbsoluteBoundingBox
{
    public double x { get; set; }
    public double y { get; set; }
    public double width { get; set; }
    public double height { get; set; }
}

public class AbsoluteRenderBounds
{
    public double x { get; set; }
    public double y { get; set; }
    public double width { get; set; }
    public double height { get; set; }
}

public class Background
{
    public string blendMode { get; set; }
    public string type { get; set; }
    public FigmaColor color { get; set; }
    public BoundVariables boundVariables { get; set; }
}

public class BackgroundColor
{
    public double r { get; set; }
    public double g { get; set; }
    public double b { get; set; }
    public double a { get; set; }
}

public class BoundVariables
{
    public List<Fill> fills { get; set; }
    public Color color { get; set; }
}

public class Child
{
    public string id { get; set; }
    public string name { get; set; }
    public string type { get; set; }
    public string scrollBehavior { get; set; }
    public BoundVariables boundVariables { get; set; }
    public ExplicitVariableModes explicitVariableModes { get; set; }
    public List<Child> children { get; set; }
    public string blendMode { get; set; }
    public bool clipsContent { get; set; }
    public List<Background> background { get; set; }
    public List<Fill> fills { get; set; }
    public List<object> strokes { get; set; }
    public double strokeWeight { get; set; }
    public string strokeAlign { get; set; }
    public BackgroundColor backgroundColor { get; set; }
    public List<LayoutGrid> layoutGrids { get; set; }
    public Styles styles { get; set; }
    public List<FillGeometry> fillGeometry { get; set; }
    public List<object> strokeGeometry { get; set; }
    public AbsoluteBoundingBox absoluteBoundingBox { get; set; }
    public AbsoluteRenderBounds absoluteRenderBounds { get; set; }
    public Constraints constraints { get; set; }
    public List<List<double>> relativeTransform { get; set; }
    public Size size { get; set; }
    public List<ExportSetting> exportSettings { get; set; }
    public List<object> effects { get; set; }
    public List<object> interactions { get; set; }
    public string layoutMode { get; set; }
    public double paddingLeft { get; set; }
    public double paddingRight { get; set; }
    public double paddingTop { get; set; }
    public double paddingBottom { get; set; }
    public double itemSpacing { get; set; }
    public string layoutWrap { get; set; }
    public string layoutSizingHorizontal { get; set; }
    public string layoutSizingVertical { get; set; }
    public string counterAxisSizingMode { get; set; }
    public string counterAxisAlignItems { get; set; }
    public string componentId { get; set; }
    public ComponentProperties componentProperties { get; set; }
    public List<Override> overrides { get; set; }
}

public class FigmaColor
{
    public double r { get; set; }
    public double g { get; set; }
    public double b { get; set; }
    public double a { get; set; }
    public string type { get; set; }
    public string id { get; set; }
}

public class ComponentProperties
{
    [JsonProperty("Dark Mode")] public DarkMode DarkMode { get; set; }
}

public class Component
{
    public string key { get; set; }
    public string name { get; set; }
    public string description { get; set; }
    public bool remote { get; set; }
    public string componentSetId { get; set; }
    public List<object> documentationLinks { get; set; }
}

public class ComponentSet
{
    public string key { get; set; }
    public string name { get; set; }
    public string description { get; set; }
    public bool remote { get; set; }
}

public class Constraint
{
    public string type { get; set; }
    public double value { get; set; }
}

public class Constraints
{
    public string vertical { get; set; }
    public string horizontal { get; set; }
}

public class DarkMode
{
    public string value { get; set; }
    public string type { get; set; }
    public BoundVariables boundVariables { get; set; }
}

public class Document
{
    public string id { get; set; }
    public string name { get; set; }
    public string type { get; set; }
    public string scrollBehavior { get; set; }
    public List<FigmaNode> children { get; set; }
    // public BackgroundColor backgroundColor { get; set; }
    // public object prototypeStartNodeID { get; set; }
    // public List<object> flowStartingPoints { get; set; }
    // public PrototypeDevice prototypeDevice { get; set; }
}

public class ExplicitVariableModes
{
    [JsonProperty("VariableCollectionId:db764528ce815b98a270857f4fb5822d2c9a0cd3/267:118")]
    public string VariableCollectionIddb764528ce815b98a270857f4fb5822d2c9a0cd3267118 { get; set; }

    [JsonProperty("VariableCollectionId:db764528ce815b98a270857f4fb5822d2c9a0cd3/186:113")]
    public string VariableCollectionIddb764528ce815b98a270857f4fb5822d2c9a0cd3186113 { get; set; }
}

public class ExportSetting
{
    public string suffix { get; set; }
    public string format { get; set; }
    public Constraint constraint { get; set; }
}

public class Fill
{
    public string type { get; set; }
    public string id { get; set; }
    public string blendMode { get; set; }
    public string scaleMode { get; set; }
    public string imageRef { get; set; }
    public double? opacity { get; set; }
    public FigmaColor color { get; set; }
    public BoundVariables boundVariables { get; set; }
}

public class FillGeometry
{
    public string path { get; set; }
    public string windingRule { get; set; }
}

public class LayoutGrid
{
    public string pattern { get; set; }
    public double sectionSize { get; set; }
    public bool visible { get; set; }
    public FigmaColor color { get; set; }
    public string alignment { get; set; }
    public double gutterSize { get; set; }
    public double offset { get; set; }
    public int count { get; set; }
}

public class Node
{
    public Node Parent { get; set; }
    public FigmaCanvas document { get; set; }
    // public Dictionary<string, FigmaComponent> components { get; set; }
    // public Dictionary<string, ComponentSet> componentSets { get; set; }
    // public int schemaVersion { get; set; }
    // public Styles styles { get; set; }
}

public class Override
{
    public string id { get; set; }
    public List<string> overriddenFields { get; set; }
}

public class PrototypeDevice
{
    public string type { get; set; }
    public string rotation { get; set; }
}

public class Root
{
}

public class Size
{
    public double x { get; set; }
    public double y { get; set; }
}

public class Styles
{
    public string grid { get; set; }

    [JsonProperty("2:6739")] public _26739 _26739 { get; set; }
}