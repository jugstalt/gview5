namespace gView.Framework.Symbology
{
    public interface ISymbolCollectionItem
    {
        bool Visible { get; set; }
        ISymbol Symbol { get; }
    }
}
