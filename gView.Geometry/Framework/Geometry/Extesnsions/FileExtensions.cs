using gView.Framework.Geometry;
using System.IO;

namespace gView.Geometry.Framework.Geometry.Extesnsions
{

    static public class FileExtensions
    {
        static public ISpatialReference FileSpatialReference(this FileInfo fi)
        {
            FileInfo fiPrj = new FileInfo(fi.FullName.Substring(0, fi.FullName.Length - fi.Extension.Length) + ".prj");
            if (fiPrj.Exists)
            {
                StreamReader sr = new StreamReader(fiPrj.FullName);
                string wkt = sr.ReadToEnd();
                sr.Close();

                return gView.Framework.Geometry.SpatialReference.FromWKT(File.ReadAllText(fiPrj.FullName));
            }

            fiPrj = new FileInfo(fi.FullName.Substring(0, fi.FullName.Length - fi.Extension.Length) + ".wkt");
            if (fiPrj.Exists)
            {
                return gView.Framework.Geometry.SpatialReference.FromWKT(fiPrj.FullName);
            }

            fiPrj = new FileInfo(fi.FullName.Substring(0, fi.FullName.Length - fi.Extension.Length) + ".epsg");
            if (fiPrj.Exists)
            {
                return new gView.Framework.Geometry.SpatialReference(File.ReadAllText(fiPrj.FullName));
            }

            return null;
        }
    }
}
