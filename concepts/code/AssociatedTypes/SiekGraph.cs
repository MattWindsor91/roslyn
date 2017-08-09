using System;
using System.Collections.Generic;
using System.Concepts;

namespace AssociatedTypes
{
    // Based on Garcia et al.'s 'An Extended Comparative Study of Language Support for Generic Programming'
    // (http://www.osl.iu.edu/publications/prints/2005/garcia05:_extended_comparing05.pdf)
    // and their earlier Haskell code (http://www.osl.iu.edu/research/comparing/haskell_readme.html)

    public concept CEdge<E, [AssociatedType] V>
    {
        V Source(E edge);
        V Target(E edge);
    }

    public concept CGraph<G, [AssociatedType] E, [AssociatedType] V, implicit CEdgeE>
        where CEdgeE : CEdge<E, V>
    {
    }

    public concept CIncidenceGraph<G, [AssociatedType] E, [AssociatedType] V, implicit CEdgeE>
        : CGraph<G, E, V, CEdgeE>
        where CEdgeE : CEdge<E, V>
    {
        IEnumerable<E> OutEdges(V vertex, G graph);
        int OutDegree(V vertex, G graph);
    }

    public concept CBidirectionalGraph<G, [AssociatedType] E, [AssociatedType] V, implicit CEdgeE>
        : CIncidenceGraph<G, E, V, CEdgeE>
        where CEdgeE : CEdge<E, V>
    {
        IEnumerable<E> InEdges(V vertex, G graph);
        int InDegree(V vertex, G graph);
        int Degree(V vertex, G graph);
    }

    public concept CVertexListGraph<G, [AssociatedType] V>
    {
        IEnumerable<V> Vertices(G graph);
        int NumVertices(G graph);
    }

    public abstract class BFSVisitor<G, [AssociatedType] E, [AssociatedType] V, Q, implicit CGraphG, implicit CEdgeE>
        where CGraphG : CGraph<G, E, V, CEdgeE>
        where CEdgeE : CEdge<E, V>
    {
        public virtual void DiscoverVertex(V vertex, G graph, ref Q queue) {}
        public virtual void ExamineVertex(V vertex, G graph, ref Q queue) {}
        public virtual void ExamineEdge(E edge, G graph, ref Q queue) {}
        public virtual void TreeEdge(E edge, G graph, ref Q queue) {}
        public virtual void GreyTarget(E edge, G graph, ref Q queue) {}
        public virtual void BlackTarget(E edge, G graph, ref Q queue) {}
        public virtual void FinishVertex(V vertex, G graph, ref Q queue) {}
    }
    public static class BFS
    {
        public static void Search<Vis, G, [AssociatedType] E, [AssociatedType] V, implicit CIncidenceGraphG, implicit CVertexListGraphG, implicit CEdgeE>(G graph, V root, Vis vis)
            where Vis : BFSVisitor<G, E, V, Queue<V>, CIncidenceGraphG, CEdgeE>
            where CIncidenceGraphG : CIncidenceGraph<G, E, V, CEdgeE>
            where CVertexListGraphG : CVertexListGraph<G, V>
            where CEdgeE : CEdge<E, V>
        {
            var q = new Queue<V>();

            var c = new Dictionary<V, Colour>();
            foreach (var v in CVertexListGraphG.Vertices(graph)) c.Add(v, Colour.White);

            vis.DiscoverVertex(root, graph, ref q);
            c.Remove(root);
            c.Add(root, Colour.Grey);
            q.Enqueue(root);

            while (0 < q.Count)
            {
                var u = q.Dequeue();
                vis.ExamineVertex(u, graph, ref q);

                foreach (E edge in CIncidenceGraphG.OutEdges(u, graph))
                {
                    var v = CEdgeE.Target(edge);
                    vis.ExamineEdge(edge, graph, ref q);

                    switch(c[v])
                    {
                        case Colour.White:
                            vis.TreeEdge(edge, graph, ref q);

                            vis.DiscoverVertex(v, graph, ref q);
                            c.Remove(v);
                            c.Add(v, Colour.Grey);
                            q.Enqueue(v);
                            break;
                        case Colour.Grey:
                            vis.GreyTarget(edge, graph, ref q);
                            break;
                        case Colour.Black:
                            vis.BlackTarget(edge, graph, ref q);
                            break;
                    }
                }

                vis.FinishVertex(u, graph, ref q);
                c.Remove(u);
                c.Add(u, Colour.Black);
            }
        }

        public static void CSearch<Vis, G, [AssociatedType] E, [AssociatedType] V, [AssociatedType] VS, [AssociatedType] ES, implicit CIncidenceGraphG, implicit CEnumerableEG, implicit CEnumerableVG, implicit CEdgeE>(G graph, V root, Vis vis)
            where Vis : BFSVisitor<G, E, V, Queue<V>, CIncidenceGraphG, CEdgeE>
            where CIncidenceGraphG : CIncidenceGraph<G, E, V, CEdgeE>
            where CEnumerableEG : CEnumerable<(V, G), E, ES>
            where CEnumerableVG : CEnumerable<G, V, VS>
            where CEdgeE : CEdge<E, V>
        {
            var q = new Queue<V>();

            var c = new Dictionary<V, Colour>();

            VS vertices = CEnumerableVG.GetEnumerator(graph);
            while (true)
            {
                if (!CEnumerableVG.MoveNext(ref vertices)) break;
                c.Add(CEnumerableVG.Current(ref vertices), Colour.White);
            }

            vis.DiscoverVertex(root, graph, ref q);
            c.Remove(root);
            c.Add(root, Colour.Grey);
            q.Enqueue(root);

            while (0 < q.Count)
            {
                var u = q.Dequeue();
                vis.ExamineVertex(u, graph, ref q);

                ES edges = CEnumerableEG.GetEnumerator((u, graph));
                while (true)
                {
                    if (!CEnumerableEG.MoveNext(ref edges)) break;
                    E edge = CEnumerableEG.Current(ref edges);

                    var v = CEdgeE.Target(edge);
                    vis.ExamineEdge(edge, graph, ref q);

                    switch(c[v])
                    {
                        case Colour.White:
                            vis.TreeEdge(edge, graph, ref q);

                            vis.DiscoverVertex(v, graph, ref q);
                            c.Remove(v);
                            c.Add(v, Colour.Grey);
                            q.Enqueue(v);
                            break;
                        case Colour.Grey:
                            vis.GreyTarget(edge, graph, ref q);
                            break;
                        case Colour.Black:
                            vis.BlackTarget(edge, graph, ref q);
                            break;
                    }
                }

                vis.FinishVertex(u, graph, ref q);
                c.Remove(u);
                c.Add(u, Colour.Black);
            }
        }
    }

    enum Colour
    {
        White,
        Grey,
        Black
    }

    struct AdjacencyList
    {
        public List<int>[] list;

        public AdjacencyList(int n, (int, int)[] pairs)
        {
            list = new List<int>[n];
            for (int i = 0; i < n; i++) list[i] = new List<int>();

            foreach (var pair in pairs)
            {
                list[pair.Item1].Add(pair.Item2);
            }
        }
    }

    struct Vertex
    {
        public int id;
    }

    struct Edge
    {
        public int source;
        public int target;
    }

    instance CEdgeEdge : CEdge<Edge, Vertex>
    {
        Vertex Source(Edge edge) => new Vertex { id = edge.source };
        Vertex Target(Edge edge) => new Vertex { id = edge.target };
    }

    instance CIncidenceGraphAdjacencyList : CIncidenceGraph<AdjacencyList, Edge, Vertex, CEdgeEdge>
    {
        IEnumerable<Edge> OutEdges(Vertex vertex, AdjacencyList graph)
        {
            foreach (int toID in graph.list[vertex.id]) yield return new Edge { source = vertex.id, target = toID };
        }
        int OutDegree(Vertex vertex, AdjacencyList graph) => graph.list[vertex.id].Count;
    }
    instance CEnumerableAdjacencyListOutEdge : CEnumerable<(Vertex, AdjacencyList), Edge, (int, List<int>[], int, Edge)>
    {
        (int, List<int>[], int, Edge) GetEnumerator((Vertex, AdjacencyList) idAndGraph) => (idAndGraph.Item1.id, idAndGraph.Item2.list, -1, default(Edge));
        void Reset(ref (int, List<int>[], int, Edge) enumerator)
        {
            enumerator.Item3 = -1;
            enumerator.Item4 = default(Edge);
        }
        bool MoveNext(ref (int, List<int>[], int, Edge) enumerator)
        {
            if (++enumerator.Item3 >= (enumerator.Item2[enumerator.Item1].Count)) return false;
            enumerator.Item4 = new Edge { source = enumerator.Item1, target = enumerator.Item2[enumerator.Item1][enumerator.Item3] };
            return true;
        }
        Edge Current(ref (int, List<int>[], int, Edge) enumerator) => enumerator.Item4;
        void Dispose(ref (int, List<int>[], int, Edge) enumerator) { }
    }

    instance CVertexListGraphAdjacencyList : CVertexListGraph<AdjacencyList, Vertex>
    {
        IEnumerable<Vertex> Vertices(AdjacencyList graph)
        {
            for (int i = 0; i < graph.list.Length; i++) yield return new Vertex { id = i };
        }
        int NumVertices(AdjacencyList graph) => graph.list.Length;
    }
    instance CEnumerableAdjacencyListVertex : CEnumerable<AdjacencyList, Vertex, (int, int, Vertex)>
    {
        (int, int, Vertex) GetEnumerator(AdjacencyList graph) => (graph.list.Length, -1, default(Vertex));
        void Reset(ref (int, int, Vertex) enumerator)
        {
            enumerator.Item2 = -1;
            enumerator.Item3 = default(Vertex);
        }
        bool MoveNext(ref (int, int, Vertex) enumerator)
        {
            if (++enumerator.Item2 >= enumerator.Item1) return false;
            enumerator.Item3 = new Vertex { id = enumerator.Item2 };
            return true;
        }
        Vertex Current(ref (int, int, Vertex) enumerator) => enumerator.Item3;
        void Dispose(ref (int, int, Vertex) enumerator) { }
    }

    class TestVisitor : BFSVisitor<AdjacencyList, Edge, Vertex, Queue<Vertex>, CIncidenceGraphAdjacencyList, CEdgeEdge>
    {
        public List<int> ids = new List<int>();

        public override void ExamineVertex(Vertex vertex, AdjacencyList graph, ref Queue<Vertex> queue)
        {
            ids.Add(vertex.id);
        }
    }

    public static class GraphTest
    {
        private static AdjacencyList RandomGraph()
        {
            Random random = new Random();
            int prob = 3;

            int count = 500;

            var adjs = new List<(int, int)>();
            for (int i = 0; i < count; i++)
            {
                for (int j = 0; j < count; j++)
                {
                    if (random.Next(prob) == 0) adjs.Add((i, j));
                }
            }

            return new AdjacencyList(count, adjs.ToArray());
        }

        public static void Run()
        {
            var graph = new AdjacencyList(7, new(int, int)[] { (0, 1), (1, 2), (1, 3), (3, 4), (0, 4), (4, 5), (3, 6) });

            var vis = new TestVisitor();
            BFS.Search(graph, new Vertex { id=0 }, vis);
            Console.Out.Write("Iterator BFS traversal (should be 0142356):");
            foreach (int v in vis.ids) Console.Out.Write($" {v}");
            Console.Out.WriteLine();
            Console.Out.WriteLine();

            vis = new TestVisitor();
            BFS.CSearch(graph, new Vertex { id = 0 }, vis);
            Console.Out.Write("Concept BFS traversal (should be 0142356): ");
            foreach (int v in vis.ids) Console.Out.Write($" {v}");
            Console.Out.WriteLine();
            Console.Out.WriteLine();

            // BFS 100 500-vertex graphs to test perf.
            // Somewhat unscientific.
            double itime = 0;
            double ctime = 0;
            Timer t;
            for (int i = 0; i < 100; i++)
            {
                graph = RandomGraph();

                vis = new TestVisitor();
                t = new Timer();
                BFS.Search(graph, new Vertex { id = 0 }, vis);
                itime += t.Check();

                vis = new TestVisitor();
                t = new Timer();
                BFS.CSearch(graph, new Vertex { id = 0 }, vis);
                ctime += t.Check();
            }

            Console.Out.WriteLine($"Iterator BFS gauntlet: {itime}s");
            Console.Out.WriteLine($"Concept BFS gauntlet:  {ctime}s");
        }
    }
}
