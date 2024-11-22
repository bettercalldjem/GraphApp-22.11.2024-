using Microsoft.Maui.Controls;
using Newtonsoft.Json;
using SkiaSharp;
using SkiaSharp.Views.Maui.Controls;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Collections.Generic;
using SkiaSharp.Views.Maui;

namespace GraphApp
{
    public partial class MainPage : ContentPage
    {
        public ObservableCollection<Vertex> Vertices { get; set; } = new ObservableCollection<Vertex>();
        public ObservableCollection<Edge> Edges { get; set; } = new ObservableCollection<Edge>();

        public ICommand AddVertexCommand { get; }
        public ICommand AddEdgeCommand { get; }
        public ICommand SaveGraphCommand { get; }
        public ICommand LoadGraphCommand { get; }
        public ICommand RunDijkstraCommand { get; }
        public ICommand RunDFSCommand { get; }
        public ICommand RunBFSCommand { get; }

        public MainPage()
        {
            InitializeComponent();
            AddVertexCommand = new Command(AddVertex);
            AddEdgeCommand = new Command(AddEdge);
            SaveGraphCommand = new Command(SaveGraph);
            LoadGraphCommand = new Command(LoadGraph);
            RunDijkstraCommand = new Command(RunDijkstra);
            RunDFSCommand = new Command(RunDFS);
            RunBFSCommand = new Command(RunBFS);
            BindingContext = this;
        }

        private void AddVertex()
        {
            var vertex = new Vertex { Label = $"V{Vertices.Count + 1}", Position = new SKPoint(100 + Vertices.Count * 50, 100) };
            Vertices.Add(vertex);
            GraphCanvas.InvalidateSurface();
        }

        private void AddEdge()
        {
            if (Vertices.Count < 2) return;
            var edge = new Edge { From = Vertices[Vertices.Count - 2], To = Vertices[Vertices.Count - 1], Weight = 1 };
            Edges.Add(edge);
            GraphCanvas.InvalidateSurface();
        }

        private void SaveGraph()
        {
            var graph = new Graph { Vertices = Vertices.ToList(), Edges = Edges.ToList() };
            var json = JsonConvert.SerializeObject(graph);
            var file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "graph.json");
            File.WriteAllText(file, json);
        }

        private void LoadGraph()
        {
            var file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "graph.json");
            if (File.Exists(file))
            {
                var json = File.ReadAllText(file);
                var graph = JsonConvert.DeserializeObject<Graph>(json);
                Vertices = new ObservableCollection<Vertex>(graph.Vertices);
                Edges = new ObservableCollection<Edge>(graph.Edges);
                OnPropertyChanged(nameof(Vertices));
                OnPropertyChanged(nameof(Edges));
                GraphCanvas.InvalidateSurface();
            }
        }

        private void RunDijkstra()
        {
            if (Vertices.Count == 0) return;

            var distances = new Dictionary<Vertex, int>();
            var previous = new Dictionary<Vertex, Vertex>();
            var queue = new List<Vertex>(Vertices);

            foreach (var vertex in Vertices)
            {
                distances[vertex] = int.MaxValue;
                previous[vertex] = null;
            }
            distances[Vertices[0]] = 0;

            while (queue.Count > 0)
            {
                queue.Sort((v1, v2) => distances[v1] - distances[v2]);
                var u = queue[0];
                queue.RemoveAt(0);

                foreach (var edge in Edges.Where(e => e.From == u))
                {
                    var alt = distances[u] + edge.Weight;
                    if (alt < distances[edge.To])
                    {
                        distances[edge.To] = alt;
                        previous[edge.To] = u;
                    }
                }
            }

            // Подсветка пути
            var path = new List<Vertex>();
            var target = Vertices[Vertices.Count - 1];
            while (previous[target] != null)
            {
                path.Add(target);
                target = previous[target];
            }
            path.Add(Vertices[0]);
            path.Reverse();

            // Рисование пути на канвасе
            GraphCanvas.PaintSurface += (s, e) =>
            {
                var canvas = e.Surface.Canvas;
                foreach (var vertex in path)
                {
                    using (var paint = new SKPaint { Color = SKColors.Red, StrokeWidth = 2 })
                    {
                        canvas.DrawCircle(vertex.Position.X, vertex.Position.Y, 20, paint);
                    }
                }
            };

            GraphCanvas.InvalidateSurface();
        }

        private void RunDFS()
        {
            if (Vertices.Count == 0) return;

            var visited = new HashSet<Vertex>();
            var stack = new Stack<Vertex>();
            stack.Push(Vertices[0]);

            GraphCanvas.PaintSurface += (s, e) =>
            {
                var canvas = e.Surface.Canvas;
                while (stack.Count > 0)
                {
                    var vertex = stack.Pop();
                    if (!visited.Contains(vertex))
                    {
                        visited.Add(vertex);
                        // Подсветка текущей вершины
                        using (var paint = new SKPaint { Color = SKColors.Green, IsAntialias = true })
                        {
                            canvas.DrawCircle(vertex.Position.X, vertex.Position.Y, 20, paint);
                        }

                        foreach (var neighbor in Edges.Where(e => e.From == vertex).Select(e => e.To))
                        {
                            stack.Push(neighbor);
                        }
                    }
                }
            };

            GraphCanvas.InvalidateSurface();
        }

        private void RunBFS()
        {
            if (Vertices.Count == 0) return;

            var visited = new HashSet<Vertex>();
            var queue = new Queue<Vertex>();
            queue.Enqueue(Vertices[0]);

            GraphCanvas.PaintSurface += (s, e) =>
            {
                var canvas = e.Surface.Canvas;
                while (queue.Count > 0)
                {
                    var vertex = queue.Dequeue();
                    if (!visited.Contains(vertex))
                    {
                        visited.Add(vertex);
                        // Подсветка текущей вершины
                        using (var paint = new SKPaint { Color = SKColors.Blue, IsAntialias = true })
                        {
                            canvas.DrawCircle(vertex.Position.X, vertex.Position.Y, 20, paint);
                        }

                        foreach (var neighbor in Edges.Where(e => e.From == vertex).Select(e => e.To))
                        {
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            };

            GraphCanvas.InvalidateSurface();
        }

        private void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.White);

            // Рисование ребер
            foreach (var edge in Edges)
            {
                var start = edge.From.Position;
                var end = edge.To.Position;
                using (var paint = new SKPaint { Color = SKColors.Black, StrokeWidth = 2 })
                {
                    canvas.DrawLine(start.X, start.Y, end.X, end.Y, paint);
                }
            }

            // Рисование вершин
            foreach (var vertex in Vertices)
            {
                using (var paint = new SKPaint { Color = SKColors.Blue, IsAntialias = true })
                {
                    canvas.DrawCircle(vertex.Position.X, vertex.Position.Y, 20, paint);
                    using (var textPaint = new SKPaint { Color = SKColors.White, TextSize = 20, IsAntialias = true })
                    {
                        canvas.DrawText(vertex.Label, vertex.Position.X - 10, vertex.Position.Y + 10, textPaint);
                    }
                }
            }
        }
    }

    public class Vertex
    {
        public string Label { get; set; }
        public SKPoint Position { get; set; }
    }

    public class Edge
    {
        public Vertex From { get; set; }
        public Vertex To { get; set; }
        public int Weight { get; set; }
    }

    public class Graph
    {
        public List<Vertex> Vertices { get; set; }
        public List<Edge> Edges { get; set; }
    }
}
