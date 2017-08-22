namespace Inv.Default
{
  public static class Resources
  {
    static Resources()
    {
      global::Inv.Resource.Foundation.Import(typeof(Resources), "Resources.Resources1.InvResourcePackage.rs");
    }

    public static readonly ResourcesImages Images;
    public static readonly ResourcesText Text;
  }

  public sealed class ResourcesImages
  {
    public ResourcesImages() { }

    ///<Summary>41.5 KB</Summary>
    public readonly global::Inv.Image Logo;
    ///<Summary>18.6 KB</Summary>
    public readonly global::Inv.Image Placeholder;
  }

  public sealed class ResourcesText
  {
    public ResourcesText() { }

    ///<Summary>0.0 KB</Summary>
    public readonly global::Inv.Binary Newsapi;
  }
}