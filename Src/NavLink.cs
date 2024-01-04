namespace RT.Spinneret;

public class NavLink
{
    public NavLink(string section, string text, string href)
    {
        Section = section;
        Text = text;
        Href = href;
    }

    public string Section { get; private set; }
    public string Text { get; private set; }
    public string Href { get; private set; }
}
