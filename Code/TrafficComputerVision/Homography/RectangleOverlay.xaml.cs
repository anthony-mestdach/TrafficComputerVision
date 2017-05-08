using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Homography
{
    /// <summary>
    /// Interaction logic for RectangleOverlay.xaml
    /// </summary>
    public partial class RectangleOverlay : UserControl
    {
        public event EventHandler RecatangleChanged;
        private bool isDragging;
        private Ellipse[] ellipses;
        private Ellipse anchor;
        private System.Drawing.Point[] points;
        private Line[] lines;
        private Size originalSize;
        private double ActiveSize = 14;
        private double PassiveSize = 10;
        public System.Drawing.Point[] Points 
        {
            get {
                if (points == null) UpdateRectangle();
                return points;
            }
            private set { }
        }

        protected virtual void OnRecatangleChanged(EventArgs e)
        {
            EventHandler handler = RecatangleChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        public RectangleOverlay()
        {
            InitializeComponent();
        }

        private void CtrlMouseEnter(object sender, MouseEventArgs e)
        {
            if ((sender as Ellipse).Height == ActiveSize) return;
            double offset = (ActiveSize - PassiveSize) / 2;//ActiveSize - (sender as Ellipse).Height;
            (sender as Ellipse).Height = ActiveSize;
            (sender as Ellipse).Width = ActiveSize;
            Canvas.SetLeft(sender as UIElement, GetLocation(sender as Ellipse).X - offset);
            Canvas.SetTop(sender as UIElement, GetLocation(sender as Ellipse).Y - offset);
        }

        public void Rectify()
        {
            Canvas.SetLeft(ellipses[3], Canvas.GetLeft(ellipses[0]));
            Canvas.SetLeft(ellipses[2], Canvas.GetLeft(ellipses[1]));
            Canvas.SetTop(ellipses[1], Canvas.GetTop(ellipses[0]));
            Canvas.SetTop(ellipses[2], Canvas.GetTop(ellipses[3]));
            foreach (Ellipse e in ellipses) e.UpdateLayout(); 
            UpdateAnchor();
            UpdateRectangle();
            OnRecatangleChanged(EventArgs.Empty);
        }

        private void CtrlMouseLeave(object sender, MouseEventArgs e)
        {
            if ((sender as Ellipse).Height == PassiveSize) return;
            double offset = (ActiveSize - PassiveSize) / 2;
            (sender as Ellipse).Height = PassiveSize;
            (sender as Ellipse).Width = PassiveSize;
            Canvas.SetLeft(sender as UIElement, GetLocation(sender as Ellipse).X + offset);
            Canvas.SetTop(sender as UIElement, GetLocation(sender as Ellipse).Y + offset);
        }

        private void CtrlMouseLBDown(object sender, MouseButtonEventArgs e)
        {
            isDragging = true;
            var draggableControl = sender as Ellipse;
            draggableControl.CaptureMouse();
        }

        private void CtrlMouseLBUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            var draggable = sender as Ellipse;
            draggable.ReleaseMouseCapture();
        }

        private void CtrlMouseMv(object sender, MouseEventArgs e)
        {
            var draggableControl = sender as Ellipse;
            if (isDragging && draggableControl != null)
            {
                MoveEllipseOrAnchor(draggableControl, Mouse.GetPosition(canvas));
                UpdateRectangle();
                UpdateAnchor();
                OnRecatangleChanged(EventArgs.Empty);
            }
        }

        private Point GetDistance(Point p2, Point p1)
        {
            return new Point((p1.X - p2.X), p1.Y - p2.Y);
        }

        private void MoveEllipseOrAnchor(Ellipse el, Point p)
        {
            if (p.X < 0) p.X = 0;
            if (p.Y < 0) p.Y = 0;
            if (p.X > (canvas.ActualWidth - el.Width)) p.X = canvas.ActualWidth - el.Width;
            if (p.Y > canvas.ActualHeight - el.Height) p.Y = canvas.ActualHeight - el.Height;
            Point anchorL = GetLocation(anchor);
            Canvas.SetLeft(el, p.X);
            Canvas.SetTop(el, p.Y);
            anchor.UpdateLayout();
            if (el == anchor)
            {
                foreach (Ellipse ellipse in ellipses)
                {
                    Point offset = GetDistance(anchorL, GetLocation(ellipse));
                    Point newLocation = GetLocation(anchor);
                    newLocation.Offset(offset.X, offset.Y);
                    MoveEllipseOrAnchor(ellipse, newLocation);
                }
            }
            
        }

        private void UpdateRectangle()
        {
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i].X1 = GetLocation(ellipses[i]).X + (ellipses[i].Width/2);
                lines[i].X2 = GetLocation(ellipses[i < (ellipses.Length - 1) ? i + 1 : 0]).X + (ellipses[i].Width / 2);
                lines[i].Y1 = GetLocation(ellipses[i]).Y + (ellipses[i].Height / 2);
                lines[i].Y2 = GetLocation(ellipses[i < (ellipses.Length - 1) ? i + 1 : 0]).Y + (ellipses[i].Height / 2);
                lines[i].StrokeThickness = 2;
                lines[i].Stroke = Brushes.LightSteelBlue;
            }
            points = ellipses.Select(i =>
            {
                Point p = GetLocation(i);
                return new System.Drawing.Point((int)p.X, (int)p.Y);
            }).ToArray();
            originalSize = new Size(canvas.ActualWidth, canvas.ActualHeight);
        }

        private Point GetLocation(UIElement e)
        {
            return e.TranslatePoint(new Point(0, 0), canvas);
        }

        private void canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            
            Size newSize = new Size(canvas.ActualWidth, canvas.ActualHeight);
            double wRatio = newSize.Width / originalSize.Width;
            double hRatio = newSize.Height / originalSize.Height;
            if (wRatio == 0 || double.IsInfinity(wRatio) || hRatio == 0 || double.IsInfinity(hRatio)) return;
            foreach (Ellipse el in ellipses)
            {
                Point currentPosition = GetLocation(el);
                Canvas.SetLeft(el, currentPosition.X * wRatio);
                Canvas.SetTop(el, currentPosition.Y * hRatio);
                el.UpdateLayout();
            }
            UpdateRectangle();
            UpdateAnchor();
            OnRecatangleChanged(EventArgs.Empty);
        }

        private void canvas_Loaded(object sender, RoutedEventArgs e)
        {
            ellipses = new Ellipse[] { e1, e2, e3, e4 };
            lines = new Line[] { new Line(), new Line(), new Line(), new Line() };
            Array.ForEach(ellipses, el =>
            {
                el.MouseLeftButtonDown += new MouseButtonEventHandler(CtrlMouseLBDown);
                el.MouseLeftButtonUp += new MouseButtonEventHandler(CtrlMouseLBUp);
                el.MouseMove += new MouseEventHandler(CtrlMouseMv);
                el.MouseEnter += new MouseEventHandler(CtrlMouseEnter);
                el.MouseLeave += new MouseEventHandler(CtrlMouseLeave);
            });

            anchor = c1;
            anchor.MouseLeftButtonDown += new MouseButtonEventHandler(CtrlMouseLBDown);
            anchor.MouseLeftButtonUp += new MouseButtonEventHandler(CtrlMouseLBUp);
            anchor.MouseMove += new MouseEventHandler(CtrlMouseMv);
            anchor.MouseEnter += new MouseEventHandler(CtrlMouseEnter);
            anchor.MouseLeave += new MouseEventHandler(CtrlMouseLeave);
            UpdateAnchor();

            Array.ForEach(lines, l =>
            {
                canvas.Children.Add(l);
            });
            lines[1].X1 = 10;
            lines[1].X2 = 50;
            lines[1].Y1 = 10;
            lines[1].Y2 = 10;

            lines[2].X1 = 50;
            lines[2].X2 = 50;
            lines[2].Y1 = 10;
            lines[2].Y2 = 50;

            lines[3].X1 = 50;
            lines[3].X2 = 10;
            lines[3].Y1 = 50;
            lines[3].Y2 = 50;

            lines[2].X1 = 10;
            lines[2].X2 = 10;
            lines[2].Y1 = 50;
            lines[2].Y2 = 10;
            originalSize = new Size(canvas.ActualWidth, canvas.ActualHeight);
            UpdateRectangle();
            UpdateAnchor();
        }

        private void UpdateAnchor()
        {
            Point center = GetMidpoint(
                GetMidpoint(GetLocation(ellipses[0]), GetLocation(ellipses[2])),
                GetMidpoint(GetLocation(ellipses[1]), GetLocation(ellipses[3]))
            );
            Canvas.SetLeft(anchor, center.X);
            Canvas.SetTop(anchor, center.Y);
        }

        private Point GetMidpoint(Point p1, Point p2)
        {
            return new Point(p1.X + 0.5 * (p2.X - p1.X), p1.Y + 0.5 * (p2.Y - p1.Y));
        }

        private void ScaleRectangle(double scale)
        {
            Canvas.SetLeft(ellipses[3], Canvas.GetLeft(ellipses[0]));
            Canvas.SetLeft(ellipses[2], Canvas.GetLeft(ellipses[1]));
            Canvas.SetTop(ellipses[1], Canvas.GetTop(ellipses[0]));
            Canvas.SetTop(ellipses[2], Canvas.GetTop(ellipses[3]));
            Point a = GetLocation(anchor);
            foreach (Ellipse e in ellipses)
            {
                Point offset = GetDistance(a, GetLocation(e));
                Point newLocation = GetLocation(anchor);
                offset = new Point(offset.X * scale, offset.Y * scale);
                newLocation.Offset(offset.X, offset.Y);
                MoveEllipseOrAnchor(e, newLocation);
                e.UpdateLayout();
            }
            UpdateRectangle();
            OnRecatangleChanged(EventArgs.Empty);
        }

        private void btnZoomIn_Click(object sender, RoutedEventArgs e)
        {
            ScaleRectangle(1.1);
        }

        private void btnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            ScaleRectangle(0.9);
        }
    }
}
