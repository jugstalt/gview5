using gView.Framework.Carto;
using gView.Framework.Geometry;
using gView.Framework.IO;
using gView.Framework.Symbology.UI;
using gView.Framework.system;
using gView.Framework.UI;
using gView.GraphicsEngine.Abstraction;
using gView.Symbology.Framework.Symbology.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace gView.Framework.Symbology
{
    public enum StepWidthUnit
    {
        Pixel = 0,
        Percent = 99
    }

    [gView.Framework.system.RegisterPlugIn("C13DF1F1-FB0A-4C47-AF73-05A184880612")]
    public class SymbolDotedLineSymbol : Symbol, ILineSymbol, IPropertyPage, IPersistable
    {
        [Browsable(true)]
        [DisplayName("Symbol")]
        [Category("Point Symbol")]
        [UsePointSymbolPicker()]
        public ISymbol PointSymbol
        {
            get; set;
        }

        [Browsable(true)]
        [DisplayName("Symbol")]
        [Category("Line Symbol")]
        [UseLineSymbolPicker()]
        public ISymbol LineSymbol
        {
            get; set;
        }

        [Browsable(true)]
        [DisplayName("Draw Start Point")]
        public bool DrawStartPoint { get; set; }
        [Browsable(true)]
        [DisplayName("Draw Ent Point")]
        public bool DrawEndPoint { get; set; }
        [Browsable(true)]
        [DisplayName("Draw Step Points")]
        public bool DrawStepPoints { get; set; }
        [Browsable(true)]
        [DisplayName("Step Width")]
        public int StepWidth { get; set; }
        [Browsable(true)]
        [DisplayName("Step Unit")]
        public StepWidthUnit StepWidthUnit { get; set; }
        [Browsable(true)]
        [DisplayName("Step Start Pos")]
        public int StepStartPos { get; set; }
        [Browsable(true)]
        [DisplayName("Symbol max distance [pixel]")]
        public int SymbolMaxDistance { get; set; }

        [Browsable(true)]
        [DisplayName("Rotate Symbols")]
        [Category("Point Symbol")]
        public bool UseSymbolRotation { get; set; }

        #region ILineSymbol

        [Browsable(false)]
        public string Name => "Symbol Doted Line";

        [Browsable(false)]
        public SymbolSmoothing SymbolSmothingMode
        {
            set
            {
                if (this.LineSymbol != null)
                {
                    this.LineSymbol.SymbolSmothingMode = value;
                }

                if (this.PointSymbol != null)
                {
                    this.PointSymbol.SymbolSmothingMode = value;
                }
            }
        }

        public bool RequireClone()
        {
            return (LineSymbol != null && LineSymbol.RequireClone()) ||
                   (PointSymbol != null && PointSymbol.RequireClone());
        }

        public bool SupportsGeometryType(GeometryType geomType) => geomType == GeometryType.Polyline;

        public void Draw(IDisplay display, IGeometry geometry)
        {
            try
            {
                IEnvelope dispEnvelope =
                    display.DisplayTransformation != null ?
                    new Envelope(display.DisplayTransformation.TransformedBounds(display)) :
                    new Envelope(display.Envelope);

                //dispEnvelope.Raise(75);
                geometry = SpatialAlgorithms.Clip.PerformClip(dispEnvelope, geometry);
                if (geometry == null)
                {
                    return;
                }

                if (this.LineSymbol != null)
                {
                    //if (this.SymbolSmothingMode != SymbolSmoothing.None)
                    //{
                    //    this.LineSymbol.SymbolSmothingMode = this.SymbolSmothingMode;
                    //}
                    this.LineSymbol.Draw(display, geometry);
                }
                if (this.PointSymbol != null && geometry is IPolyline)
                {
                    //if (this.SymbolSmothingMode != SymbolSmoothing.None)
                    //{
                    //    this.PointSymbol.SymbolSmothingMode = this.SymbolSmothingMode;
                    //}
                    double stepWidthValue = this.StepWidth;
                    double stepStartPosValue = this.StepStartPos;
                    double step = 0, start = 0;
                    double symbolMaxDistance = 0.5 * display.SpatialReference.MetersToDeg(this.SymbolMaxDistance * display.MapScale / (96 / 0.0254));

                    switch (this.StepWidthUnit)
                    {
                        case StepWidthUnit.Pixel:
                            step = display.SpatialReference.MetersToDeg(stepWidthValue * (display.MapScale / (96 / 0.0254)) /* [m] */ );
                            start = display.SpatialReference.MetersToDeg(stepStartPosValue * (display.MapScale / (96 / 0.0254))  /* [m] */);
                            break;
                    }

                    var polyline = ((IPolyline)geometry);

                    foreach (var path in polyline)
                    {
                        if (path == null || path.PointCount == 0)
                        {
                            continue;
                        }

                        var symbolRotation = this.UseSymbolRotation ?
                            this.PointSymbol as ISymbolRotation :
                            null;

                        var length = path.Length;
                        var pointCount = path.PointCount;
                        double dx, dy;

                        PointCollection pointCollection = new PointCollection();
                        if (pointCount >= 2)
                        {
                            switch (this.StepWidthUnit)
                            {
                                case StepWidthUnit.Percent:
                                    step = length * ((float)stepWidthValue / 100f);
                                    start = length * ((float)stepStartPosValue / 100f);
                                    break;
                            }

                            #region Step Points

                            if (this.DrawStepPoints && stepWidthValue >= 1 && step > double.Epsilon)
                            {
                                pointCollection = length > step ?
                                    SpatialAlgorithms.Algorithm.PathPoints(path,
                                                                           fromStat: start > 0.0 ? start : step,
                                                                           toStat: length,
                                                                           step: step,
                                                                           createPointM2: true) :
                                    new PointCollection();
                            }

                            #endregion

                            #region FirstPoint

                            if (this.DrawStartPoint)
                            {
                                dx = path[1].X - path[0].X;
                                dy = path[1].Y - path[0].Y;

                                pointCollection.AddPoint(new PointM2(path[0], 0, Math.Atan2(-dy, dx)));
                            }

                            #endregion

                            #region Last Point

                            if (this.DrawEndPoint)
                            {
                                dx = path[pointCount - 1].X - path[pointCount - 2].X;
                                dy = path[pointCount - 1].Y - path[pointCount - 2].Y;
                                pointCollection.AddPoint(new PointM2(path[path.PointCount - 1], length, Math.Atan2(-dy, dx)));
                            }

                            #endregion
                        }
                        else  // Line with one Vertex!!??  => dont ask => draw!
                        {
                            if (this.DrawStartPoint)
                            {
                                pointCollection = new PointCollection();
                                pointCollection.AddPoint(path[0]);
                            }
                        }

                        var symbolEnvelopes = symbolMaxDistance > 0 ? new List<IEnvelope>() : null;

                        for (int i = 0, i_to = pointCollection.PointCount; i < i_to; i++)
                        {
                            #region Check Min Dinstance (Intersection)

                            if (symbolMaxDistance > 0)
                            {
                                var symbolEnvelope = new Envelope(-symbolMaxDistance, -symbolMaxDistance, symbolMaxDistance, symbolMaxDistance);
                                symbolEnvelope.TranslateTo(pointCollection[i].X, pointCollection[i].Y);

                                if (symbolEnvelope.IntersectsOne(symbolEnvelopes))
                                {
                                    continue;
                                }

                                symbolEnvelopes.Add(symbolEnvelope);
                            }

                            #endregion

                            if (symbolRotation != null)
                            {
                                if (pointCollection[i] is PointM2)
                                {
                                    float rotation = (float)(Convert.ToSingle(((PointM2)pointCollection[i]).M2) * 180.0 / Math.PI);
                                    symbolRotation.Rotation = rotation;
                                }
                                else
                                {
                                    symbolRotation.Rotation = 0;
                                }
                            }


                            this.PointSymbol.Draw(display, pointCollection[i]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void DrawPath(IDisplay display, IGraphicsPath path)
        {

        }

        public void Release()
        {
            if (this.PointSymbol != null)
            {
                this.PointSymbol.Release();
            }

            if (this.LineSymbol != null)
            {
                this.LineSymbol.Release();
            }
        }

        #endregion

        #region IClone2
        public object Clone(CloneOptions options)
        {
            var display = options?.Display;

            if (display == null)
            {
                return Clone();
            }

            float fac = 1;
            if (options.ApplyRefScale && display.MapScale >= 1)
            {
                fac = (float)(display.ReferenceScale / display.MapScale);
                fac = options.RefScaleFactor(fac);
            }
            fac *= options.DpiFactor;

            SymbolDotedLineSymbol cloneSym = new SymbolDotedLineSymbol();
            if (this.LineSymbol != null)
            {
                cloneSym.LineSymbol = this.LineSymbol.Clone(options) as ISymbol;
            }

            if (this.PointSymbol != null)
            {
                cloneSym.PointSymbol = this.PointSymbol.Clone(options) as ISymbol;
            }

            cloneSym.LegendLabel = _legendLabel;

            cloneSym.DrawStartPoint = this.DrawStartPoint;
            cloneSym.DrawEndPoint = this.DrawEndPoint;
            cloneSym.DrawStepPoints = this.DrawStepPoints;
            cloneSym.StepWidthUnit = this.StepWidthUnit;
            cloneSym.SymbolMaxDistance = (int)((float)this.SymbolMaxDistance * fac);

            switch (cloneSym.StepWidthUnit)
            {
                case StepWidthUnit.Pixel:
                    cloneSym.StepWidth = (int)((float)this.StepWidth * fac);
                    cloneSym.StepStartPos = (int)((float)this.StepStartPos * fac);
                    break;
                default:
                    cloneSym.StepWidth = this.StepWidth;
                    cloneSym.StepStartPos = this.StepStartPos;
                    break;
            }

            cloneSym.UseSymbolRotation = this.UseSymbolRotation;

            return cloneSym;
        }

        #endregion

        #region IPropertyPage Member

        public object PropertyPageObject()
        {
            return null;
        }

        public object PropertyPage(object initObject)
        {
            string appPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            Assembly uiAssembly = Assembly.LoadFrom(appPath + @"/gView.Win.Symbology.UI.dll");

            IPropertyPanel p = uiAssembly.CreateInstance("gView.Framework.Symbology.UI.PropertyForm_SymbolDotedLineSymbol") as IPropertyPanel;
            if (p != null)
            {
                return p.PropertyPanel(this);
            }

            return null;
        }

        #endregion

        #region IPersistable

        public new void Load(IPersistStream stream)
        {
            base.Load(stream);

            this.PointSymbol = (ISymbol)stream.Load("pointsymbol");
            this.LineSymbol = (ISymbol)stream.Load("linesymbol");
                
            this.DrawStartPoint = (bool)stream.Load("drawstartpoint", true);
            this.DrawEndPoint = (bool)stream.Load("drawendpoint", true);
            this.DrawStepPoints = (bool)stream.Load("drawsteppoints", true);
            this.StepWidth = (int)stream.Load("stepwidth", (int)20);
            this.StepWidthUnit = (StepWidthUnit)(int)stream.Load("stepwidthunit", (int)StepWidthUnit.Pixel);
            this.StepStartPos = (int)stream.Load("stepstartpos", (int)0);
            this.SymbolMaxDistance = (int)stream.Load("symbolmaxdistance", (int)0);

            this.UseSymbolRotation = (bool)stream.Load("usesymbolrotation", true);
        }

        public new void Save(IPersistStream stream)
        {
            base.Save(stream);

            if (this.PointSymbol != null)
            {
                stream.Save("pointsymbol", this.PointSymbol);
            }

            if (this.LineSymbol != null)
            {
                stream.Save("linesymbol", this.LineSymbol);
            }

            stream.Save("drawstartpoint", this.DrawStartPoint);
            stream.Save("drawendpoint", this.DrawEndPoint);
            stream.Save("drawsteppoints", this.DrawStepPoints);
            stream.Save("stepwidth", this.StepWidth);
            stream.Save("stepwidthunit", (int)this.StepWidthUnit);
            stream.Save("usesymbolrotation", this.UseSymbolRotation);
            stream.Save("stepstartpos", this.StepStartPos);
            stream.Save("symbolmaxdistance", this.SymbolMaxDistance);
        }

        #endregion
    }
}
