using gView.GraphicsEngine.Abstraction;

namespace gView.GraphicsEngine
{
    static public class Current
    {
        static public IGraphicsEngine Engine { get; set; }
        static public IBitmapEncoding Encoder { get; set; }

        static public bool UseSecureDisposingOnUserInteractiveUIs = false;
    }
}
