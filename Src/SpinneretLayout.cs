using RT.TagSoup;

namespace RT.Spinneret;

public abstract class SpinneretLayout
{
    public abstract Tag GetPageHtml(SpinneretPage page);
    public abstract Tag GetErrorHtml(SpinneretPage page, string message);
    public abstract Tag GetExceptionHtml(SpinneretPage page, Exception exception);
}
