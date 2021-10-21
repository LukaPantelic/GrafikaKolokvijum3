using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using PR115_2016_Luka_Pantelic.Model;


namespace PR115_2016_Luka_Pantelic
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public enum PowerEntityType
    {
        Substation,
        Switch,
        Node
    }

    public struct PowerEntityStructure
    {
        public Model3D model;
        public string description;
        public PowerEntityType type;
        public SolidColorBrush color;
    }

    public partial class MainWindow : Window
    {
        #region Fields

        public double noviX, noviY;
        public Dictionary<long, PowerEntity> powerEntities;
        public Dictionary<long, LineEntity> lines;
        public static double minLat = 45.2325;
        public static double maxLat = 45.277031;
        public static double minLon = 19.793909;
        public static double maxLon = 19.894459;
        public static double elementWidth = 0.01;
        public static double elementHeight = 0.04;
        public static double lineElementWidth = 0.006;
        public static double lineElementHeight = 0.003;
        public static double rotate = 0.4;
        public static Int32Collection IndiciesObjects = new Int32Collection() { 2, 3, 1, 2, 1, 0, 7, 1, 3, 7, 5, 1, 6, 5, 7, 6, 4, 5, 6, 2, 4, 2, 0, 4, 2, 7, 3, 2, 6, 7, 0, 1, 5, 0, 5, 4 };
        private Model3D gm;
        public Dictionary<Model3D, Tuple<Model3D, Model3D>> pointsAndRanges = new Dictionary<Model3D, Tuple<Model3D, Model3D>>();
        public Tuple<Model3D, Model3D> selectedEnds = null;
        private Dictionary<Point, int> numberOfElementsAtPoint;
        private System.Windows.Point startPoint = new System.Windows.Point();
        private System.Windows.Point diffOffset = new System.Windows.Point();
        public System.Windows.Point startRotationPoint = new System.Windows.Point();
        private int maxZoom = 40;
        private int currentZoom = 1;
        System.Windows.Point positionOfMouse;
        public Dictionary<long, PowerEntityStructure> powerEntityStructures = new Dictionary<long, PowerEntityStructure>();
        public SolidColorBrush selectedColor = new SolidColorBrush(Colors.Yellow);

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            lines = new Dictionary<long, LineEntity>();
            powerEntities = new Dictionary<long, PowerEntity>();
            numberOfElementsAtPoint = new Dictionary<Point, int>();
        }

        #region iscrtavanje elementata, Load()

        private void Load(object sender, RoutedEventArgs e)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load("Geographic.xml");
            XmlNodeList nodeList1 = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Substations/SubstationEntity");
            XmlNodeList nodeList2 = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Nodes/NodeEntity");
            XmlNodeList nodeList3 = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Switches/SwitchEntity");
            XmlNodeList nodeList4 = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Lines/LineEntity");

            //SUBSTATIONS
            foreach (XmlNode node in nodeList1)
            {
                SubstationEntity sub = new SubstationEntity();

                sub.Id = long.Parse(node.SelectSingleNode("Id").InnerText);
                sub.Name = node.SelectSingleNode("Name").InnerText;
                sub.X = double.Parse(node.SelectSingleNode("X").InnerText);
                sub.Y = double.Parse(node.SelectSingleNode("Y").InnerText);

                ToLatLon(sub.X, sub.Y, 34, out noviX, out noviY);
                sub.X = noviX;
                sub.Y = noviY;

                if (CheckRange(noviX, noviY))
                    powerEntities.Add(sub.Id, sub);
                else
                    continue;
            }

            //NODES
            foreach (XmlNode node in nodeList2)
            {
                NodeEntity nod = new NodeEntity();

                nod.Id = long.Parse(node.SelectSingleNode("Id").InnerText);
                nod.Name = node.SelectSingleNode("Name").InnerText;
                nod.X = double.Parse(node.SelectSingleNode("X").InnerText);
                nod.Y = double.Parse(node.SelectSingleNode("Y").InnerText);

                ToLatLon(nod.X, nod.Y, 34, out noviX, out noviY);
                nod.X = noviX;
                nod.Y = noviY;

                if (CheckRange(noviX, noviY))
                    powerEntities.Add(nod.Id, nod);
                else
                    continue;
            }

            //SWITCHES
            foreach (XmlNode node in nodeList3)
            {
                SwitchEntity swc = new SwitchEntity();

                swc.Id = long.Parse(node.SelectSingleNode("Id").InnerText);
                swc.Name = node.SelectSingleNode("Name").InnerText;
                swc.X = double.Parse(node.SelectSingleNode("X").InnerText);
                swc.Y = double.Parse(node.SelectSingleNode("Y").InnerText);
                swc.Status = node.SelectSingleNode("Status").InnerText;

                ToLatLon(swc.X, swc.Y, 34, out noviX, out noviY);
                swc.X = noviX;
                swc.Y = noviY;

                if (CheckRange(noviX, noviY))
                    powerEntities.Add(swc.Id, swc);
                else
                    continue;
            }

            //LINES
            foreach (XmlNode node in nodeList4)
            {
                LineEntity lin = new LineEntity();

                lin.Id = long.Parse(node.SelectSingleNode("Id").InnerText);
                lin.Name = node.SelectSingleNode("Name").InnerText;
                if (node.SelectSingleNode("IsUnderground").InnerText.Equals("true"))
                    lin.IsUnderground = true;
                else
                    lin.IsUnderground = false;

                lin.R = float.Parse(node.SelectSingleNode("R").InnerText);
                lin.ConductorMaterial = node.SelectSingleNode("ConductorMaterial").InnerText;
                lin.LineType = node.SelectSingleNode("LineType").InnerText;
                lin.ThermalConstantHeat = long.Parse(node.SelectSingleNode("ThermalConstantHeat").InnerText);
                lin.FirstEnd = long.Parse(node.SelectSingleNode("FirstEnd").InnerText);
                lin.SecondEnd = long.Parse(node.SelectSingleNode("SecondEnd").InnerText);

                PowerEntity pe1, pe2;
                powerEntities.TryGetValue(lin.FirstEnd, out pe1);
                powerEntities.TryGetValue(lin.SecondEnd, out pe2);

                //samo ukoliko su obe tacke ove linije na mapi u tom slucaju se iscrtava
                if (pe1 != null && pe2 != null)//obe tacke moraju da se nalaze u powerEntities
                {
                    lin.Length = Length(pe1.X, pe2.X, pe1.Y, pe2.Y);
                    lines.Add(lin.Id, lin);
                    List<Point> vertices = new List<Point>();
                    foreach (XmlNode pointNode in node.ChildNodes[9].ChildNodes)
                    {
                        Point p = new Point();
                        p.X = double.Parse(pointNode.SelectSingleNode("X").InnerText);
                        p.Y = double.Parse(pointNode.SelectSingleNode("Y").InnerText);

                        ToLatLon(p.X, p.Y, 34, out double x, out double y);
                        if (!CheckRange(x, y))
                        {
                            vertices = null;
                            break;
                        }
                        p.X = x;
                        p.Y = y;
                        vertices.Add(p);
                    }
                    lin.Vertices = vertices;
                }
            }

            IscrtavanjePowerEntity();
            UcitajButton.IsEnabled = false;
            PrikaziButton.IsEnabled = true;
        }

        #endregion

        #region pomocne funkcije
        public static void ToLatLon(double utmX, double utmY, int zoneUTM, out double latitude, out double longitude)
        {
            bool isNorthHemisphere = true;

            var diflat = -0.00066286966871111111111111111111111111;
            var diflon = -0.0003868060578;

            var zone = zoneUTM;
            var c_sa = 6378137.000000;
            var c_sb = 6356752.314245;
            var e2 = Math.Pow((Math.Pow(c_sa, 2) - Math.Pow(c_sb, 2)), 0.5) / c_sb;
            var e2cuadrada = Math.Pow(e2, 2);
            var c = Math.Pow(c_sa, 2) / c_sb;
            var x = utmX - 500000;
            var y = isNorthHemisphere ? utmY : utmY - 10000000;

            var s = ((zone * 6.0) - 183.0);
            var lat = y / (c_sa * 0.9996);
            var v = (c / Math.Pow(1 + (e2cuadrada * Math.Pow(Math.Cos(lat), 2)), 0.5)) * 0.9996;
            var a = x / v;
            var a1 = Math.Sin(2 * lat);
            var a2 = a1 * Math.Pow((Math.Cos(lat)), 2);
            var j2 = lat + (a1 / 2.0);
            var j4 = ((3 * j2) + a2) / 4.0;
            var j6 = ((5 * j4) + Math.Pow(a2 * (Math.Cos(lat)), 2)) / 3.0;
            var alfa = (3.0 / 4.0) * e2cuadrada;
            var beta = (5.0 / 3.0) * Math.Pow(alfa, 2);
            var gama = (35.0 / 27.0) * Math.Pow(alfa, 3);
            var bm = 0.9996 * c * (lat - alfa * j2 + beta * j4 - gama * j6);
            var b = (y - bm) / v;
            var epsi = ((e2cuadrada * Math.Pow(a, 2)) / 2.0) * Math.Pow((Math.Cos(lat)), 2);
            var eps = a * (1 - (epsi / 3.0));
            var nab = (b * (1 - epsi)) + lat;
            var senoheps = (Math.Exp(eps) - Math.Exp(-eps)) / 2.0;
            var delt = Math.Atan(senoheps / (Math.Cos(nab)));
            var tao = Math.Atan(Math.Cos(delt) * Math.Tan(nab));

            longitude = ((delt * (180.0 / Math.PI)) + s) + diflon;
            latitude = ((lat + (1 + e2cuadrada * Math.Pow(Math.Cos(lat), 2) - (3.0 / 2.0) * e2cuadrada * Math.Sin(lat) * Math.Cos(lat) * (tao - lat)) * (tao - lat)) * (180.0 / Math.PI)) + diflat;
        }

        private bool CheckRange(double lat, double lon)
        {
            if (lat > maxLat || lat < minLat || lon > maxLon || lon < minLon)
                return false;
            return true;
        }

        private double Length(double x1, double x2, double y1, double y2)
        {
            return Math.Sqrt(Math.Pow(x2 - x2, 2) + Math.Pow(y2 - y1, 2));
        }

        #endregion

        #region Isrcrtavanje nodova, sviceva, substitaiona
        private void IscrtavanjePowerEntity()
        {
            double x;
            double y;

            foreach (var node in powerEntities.Values)
            {
                x = Mapping(node.Y, maxLon, minLon, elementWidth);
                y = Mapping(node.X, maxLat, minLat, elementWidth);

                Point3DCollection pos = new Point3DCollection();
                int num = AddObjectCheck(new Point(x, y));

                double position1 = num * elementHeight;
                double position2 = (num + 1) * elementHeight;

                pos.Add(new Point3D(x, y, position1));
                pos.Add(new Point3D(x + elementWidth, y, position1));
                pos.Add(new Point3D(x, y + elementWidth, position1));
                pos.Add(new Point3D(x + elementWidth, y + elementWidth, position1));

                pos.Add(new Point3D(x, y, position2));
                pos.Add(new Point3D(x + elementWidth, y, position2));
                pos.Add(new Point3D(x, y + elementWidth, position2));
                pos.Add(new Point3D(x + elementWidth, y + elementWidth, position2));

                string nodeText;
                PowerEntityType type;
                SolidColorBrush color;

                if (node is NodeEntity)
                {
                    nodeText = "Node" + "\n" + "Id: " + node.Id + "\n" + "Name: " + node.Name;
                    type = PowerEntityType.Node;
                    color = new SolidColorBrush(Colors.Red);
                }
                else if (node is SubstationEntity)
                {
                    nodeText = "Substation" + "\n" + "Id: " + node.Id + "\n" + "Name: " + node.Name;
                    type = PowerEntityType.Substation;
                    color = new SolidColorBrush(Colors.Blue);
                }
                else
                {
                    nodeText = "Switch" + "\n" + "Id: " + node.Id + "\n" + "Name: " + node.Name;
                    type = PowerEntityType.Switch;
                    color = new SolidColorBrush(Colors.Green);
                }

                GeometryModel3D model = new GeometryModel3D();
                MeshGeometry3D geometry = new MeshGeometry3D();
                model.Material = CreateMaterial(color);
                geometry.Positions = pos;
                geometry.TriangleIndices = IndiciesObjects;
                model.Geometry = geometry;
                ModelGroup.Children.Add(model);

                powerEntityStructures.Add(node.Id, new PowerEntityStructure { model = model, description = nodeText, type = type, color = color });
            }
        }

        #endregion

        public static double Mapping(double value, double max, double min, double elementWidth)
        {
            return -1 + (value - min) / (max - min) * (2 - elementWidth);
        }

        public int AddObjectCheck(Point key)
        {
            foreach (var point in numberOfElementsAtPoint.Keys)
            {
                if (IsInRange(key, point))
                {
                    int temp = numberOfElementsAtPoint[point];
                    numberOfElementsAtPoint[point]++;
                    return temp;
                }
            }
            numberOfElementsAtPoint[key] = 1;
            return 0;
        }

        private bool IsInRange(Point key, Point point)
        {
            return Math.Abs(key.X - point.X) <= (elementWidth / 2) && Math.Abs(key.Y - point.Y) <= (elementWidth / 2);
        }

        public static MaterialGroup CreateMaterial(SolidColorBrush color)
        {
            MaterialGroup materialGroup = new MaterialGroup();
            materialGroup.Children.Add(new DiffuseMaterial(color));
            materialGroup.Children.Add(new EmissiveMaterial(color));
            materialGroup.Children.Add(new SpecularMaterial(color, 3));

            return materialGroup;
        }

        #region iscrtavanje i brisanje linija
        private void IscrtavanjeLinija(object sender, RoutedEventArgs e)
        {
            foreach (var line in lines.Values)
            {
                if (line.Vertices == null)
                    continue;

                List<Point> points = new List<Point>();
                foreach (var vertice in line.Vertices)
                {
                    Point point = new Point();
                    point.X = Mapping(vertice.Y, maxLon, minLon, lineElementWidth);
                    point.Y = Mapping(vertice.X, maxLat, minLat, lineElementWidth);
                    points.Add(point);
                }

                for (int i = 0; i < points.Count - 1; i++)
                {
                    Point3DCollection pos = new Point3DCollection();

                    pos.Add(new Point3D(points[i].X, points[i].Y, 0));
                    pos.Add(new Point3D(points[i].X + lineElementWidth, points[i].Y, 0));
                    pos.Add(new Point3D(points[i].X, points[i].Y + lineElementWidth, 0));
                    pos.Add(new Point3D(points[i].X + lineElementWidth, points[i].Y + lineElementWidth, 0));

                    pos.Add(new Point3D(points[i + 1].X, points[i + 1].Y, lineElementHeight));
                    pos.Add(new Point3D(points[i + 1].X + lineElementWidth, points[i + 1].Y, lineElementHeight));
                    pos.Add(new Point3D(points[i + 1].X, points[i + 1].Y + lineElementWidth, lineElementHeight));
                    pos.Add(new Point3D(points[i + 1].X + lineElementWidth, points[i + 1].Y + lineElementWidth, lineElementHeight));

                    GeometryModel3D model = new GeometryModel3D();
                    MeshGeometry3D geometry = new MeshGeometry3D();
                    SolidColorBrush materialColor = new SolidColorBrush(Colors.Black);
                    MaterialGroup materialGroup = new MaterialGroup();

                    materialGroup.Children.Add(new DiffuseMaterial(materialColor));
                    materialGroup.Children.Add(new EmissiveMaterial(materialColor));
                    materialGroup.Children.Add(new SpecularMaterial(materialColor, 3));
                    model.Material = materialGroup;

                    geometry.Positions = pos;
                    geometry.TriangleIndices = IndiciesObjects;
                    model.Geometry = geometry;
                    ModelGroup.Children.Add(model);

                    if (powerEntityStructures[line.FirstEnd].model != null && powerEntityStructures[line.SecondEnd].model != null)
                        pointsAndRanges.Add(model, Tuple.Create(powerEntityStructures[line.FirstEnd].model, powerEntityStructures[line.SecondEnd].model));
                }
            }

            UkloniButton.IsEnabled = true;
            PrikaziButton.IsEnabled = false;
        }

        private void BrisanjeLinija(object sender, RoutedEventArgs e)
        {
            foreach (var item in pointsAndRanges)
            {
                ModelGroup.Children.Remove(item.Key);
            }

            UkloniButton.IsEnabled = false;
            PrikaziButton.IsEnabled = true;
        }

        #endregion

        #region mis

        private void MouseLeftButtonDown_ViewPort(object sender, MouseButtonEventArgs e)
        {
            ViewPort.CaptureMouse();
            startPoint = e.GetPosition(this);
            diffOffset.X = translating.OffsetX;
            diffOffset.Y = translating.OffsetY;
        }

        private void MouseLeftButtonUp_ViewPort(object sender, MouseButtonEventArgs e)
        {
            ViewPort.ReleaseMouseCapture();
        }

        private void MouseLeftButtonUp_Window(object sender, MouseButtonEventArgs e)
        {
            if (ViewPort.IsMouseCaptured)
                ViewPort.ReleaseMouseCapture();
        }

        private void MouseMove_Window(object sender, MouseEventArgs e)
        {
            if (ViewPort.IsMouseCaptured)
            {
                System.Windows.Point end = e.GetPosition(this);
                double offsetX = end.X - startPoint.X;
                double offsetY = end.Y - startPoint.Y;
                double w = this.Width;
                double h = this.Height;
                double translateX = (offsetX * 100) / w;
                double translateY = -(offsetY * 100) / h;
                translating.OffsetX = diffOffset.X + (translateX / (100 * scaling.ScaleX));
                translating.OffsetY = diffOffset.Y + (translateY / (100 * scaling.ScaleX));
            }
            else if (e.MiddleButton == MouseButtonState.Pressed)
            {

                System.Windows.Point end = e.GetPosition(this);
                double offsetX = end.X - startRotationPoint.X;
                double offsetY = end.Y - startRotationPoint.Y;

                if ((Angle1.Angle + rotate * offsetY < 100 && Angle1.Angle + rotate * offsetY > -100))
                {
                    if ((Angle2.Angle + rotate * offsetX < 100 && Angle2.Angle + rotate * offsetX > -100))
                    {
                        Angle1.Angle += rotate * offsetY;
                        Angle2.Angle += rotate * offsetX;
                    }

                }
                startRotationPoint = end;
            }
        }

        private void MouseWheel_ViewPort(object sender, MouseWheelEventArgs e)
        {
            System.Windows.Point p = e.MouseDevice.GetPosition(this);
            double scaleX = 1;
            double scaleY = 1;
            if (e.Delta > 0 && currentZoom < maxZoom)
            {
                scaleX = scaling.ScaleX + 0.1;
                scaleY = scaling.ScaleY + 0.1;
                currentZoom++;
                scaling.ScaleX = scaleX;
                scaling.ScaleY = scaleY;
            }
            else if (e.Delta <= 0 && currentZoom > 0)
            {
                scaleX = scaling.ScaleX - 0.1;
                scaleY = scaling.ScaleY - 0.1;
                currentZoom--;
                scaling.ScaleX = scaleX;
                scaling.ScaleY = scaleY;
            }
        }

        private void MouseRightButtonDown_ViewPort(object sender, MouseButtonEventArgs e)
        {
            positionOfMouse = e.GetPosition(ViewPort);
            Point3D testpoint3D = new Point3D(positionOfMouse.X, positionOfMouse.Y, 0);
            Vector3D testdirection = new Vector3D(positionOfMouse.X, positionOfMouse.Y, 10);

            PointHitTestParameters pointparams = new PointHitTestParameters(positionOfMouse);
            RayHitTestParameters rayparams = new RayHitTestParameters(testpoint3D, testdirection);
            gm = null;
            VisualTreeHelper.HitTest(ViewPort, null, HTResult, pointparams);
        }
        #endregion

        #region HT test
        private HitTestResultBehavior HTResult(HitTestResult rawresult)
        {
            RayHitTestResult rayResult = rawresult as RayHitTestResult;
            bool hasEntered = false;
            if (rayResult != null)
            {
                foreach (var item in powerEntityStructures.Values)
                {
                    if (item.model == rayResult.ModelHit)
                    {
                        gm = rayResult.ModelHit;
                        Tip tooltipWindow = new Tip(item.description, positionOfMouse);
                        tooltipWindow.ShowDialog();
                        hasEntered = true;
                        break;
                    }
                }

                if (!hasEntered)
                {
                    ChangePointsColor(rayResult.ModelHit);
                }
            }

            return HitTestResultBehavior.Stop;
        }

        #endregion

        private void ChangePointsColor(Model3D model)
        {
            GeometryModel3D gm1, gm2;

            try { var test = pointsAndRanges[model]; }
            catch (Exception) { return; }


            if (selectedEnds == null)
            {
                selectedEnds = pointsAndRanges[model];

                gm1 = selectedEnds.Item1 as GeometryModel3D;
                gm2 = selectedEnds.Item2 as GeometryModel3D;
                gm1.Material = CreateMaterial(selectedColor);
                gm2.Material = CreateMaterial(selectedColor);
            }
            else
            {
                gm1 = selectedEnds.Item1 as GeometryModel3D;
                gm2 = selectedEnds.Item2 as GeometryModel3D;

                if ((gm1 == pointsAndRanges[model].Item1 && gm2 == pointsAndRanges[model].Item2) ||
                    (gm2 == pointsAndRanges[model].Item1 && gm1 == pointsAndRanges[model].Item2))
                {
                    foreach (var item in powerEntityStructures.Values)
                    {
                        if (gm1 == item.model)
                            gm1.Material = CreateMaterial(item.color);
                    }

                    foreach (var item in powerEntityStructures.Values)
                    {
                        if (gm2 == item.model)
                            gm2.Material = CreateMaterial(item.color);
                    }

                    selectedEnds = null;
                }
                else
                {
                    foreach (var item in powerEntityStructures.Values)
                    {
                        if (gm1 == item.model)
                            gm1.Material = CreateMaterial(item.color);
                    }

                    foreach (var item in powerEntityStructures.Values)
                    {
                        if (gm2 == item.model)
                            gm2.Material = CreateMaterial(item.color);
                    }

                    selectedEnds = pointsAndRanges[model];
                    (selectedEnds.Item1 as GeometryModel3D).Material = CreateMaterial(selectedColor);
                    (selectedEnds.Item2 as GeometryModel3D).Material = CreateMaterial(selectedColor);
                }
            }
        }

    }
}
