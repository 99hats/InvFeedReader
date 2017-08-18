namespace Inv.Default
{
  public static class Resources
  {
    static Resources()
    {
      global::Inv.Resource.Foundation.Import(typeof(Resources), "Resources.Resources1.InvResourcePackage.rs");
    }

    public static readonly ResourcesImages Images;
  }

  public sealed class ResourcesImages
  {
    public ResourcesImages() { }

    ///<Summary>41.5 KB</Summary>
    public readonly global::Inv.Image Logo;
  }
}