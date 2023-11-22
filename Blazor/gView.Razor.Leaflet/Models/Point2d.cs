namespace gView.Razor.Leaflet.Models
{
    public struct Point2d
    {
        public Point2d() { }

        public Point2d(double x, double y) 
            => (X, Y) = (x, y);

        public double X { get; set; }
        public double Y { get; set; }
    }
}
