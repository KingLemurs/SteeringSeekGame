using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class NavMesh : MonoBehaviour
{
    List<Wall> _outline;
    // turns the outline into a navmesh graph
    public Graph MakeNavMesh(List<Wall> outline)
    {
        _outline = outline;
        Graph g = new Graph();
        g.all_nodes = new List<GraphNode>();

        // convert wall objects into a list of points
        List<Vector3> polygon = new List<Vector3>();
        for (int i = outline.Count - 1; i >= 0; i--)
        {
            polygon.Add(outline[i].start);
        }

        // split the polygon into convex parts
        List<List<Vector3>> convexPolygons = Decompose(polygon);
        // turn each polygon into a list of walls, then graph nodes
        List<List<Wall>> wallPolygons = new List<List<Wall>>();
        for (int i = 0; i < convexPolygons.Count; i++)
        {
            List<Wall> walls = PolygonToWalls(convexPolygons[i]);
            wallPolygons.Add(walls);
            g.all_nodes.Add(new GraphNode(i, walls));
        }

        // connect nodes if they share a wall (they are neighbors)
        for (int i = 0; i < wallPolygons.Count; i++)
        {
            for (int j = i + 1; j < wallPolygons.Count; j++)
            {
                for (int edgeA = 0; edgeA < wallPolygons[i].Count; edgeA++)
                {
                    for (int edgeB = 0; edgeB < wallPolygons[j].Count; edgeB++)
                    {
                        if (wallPolygons[i][edgeA].Same(wallPolygons[j][edgeB]))
                        {
                            g.all_nodes[i].AddNeighbor(g.all_nodes[j], edgeA);
                            g.all_nodes[j].AddNeighbor(g.all_nodes[i], edgeB);
                            break;
                        }
                    }
                }
            }
        }

        return g;
    }

    // checks if the corner bends inward (reflex)
    private bool IsReflex(Vector3 prev, Vector3 curr, Vector3 next)
    {
        Wall a = new Wall(prev, curr);
        Wall b = new Wall(curr, next);
        
        print(Vector3.Dot(a.normal, b.direction));
        return Vector3.Dot(a.normal, b.direction) > 0;
    }

    // checks if the whole polygon is already convex
    private bool IsConvex(List<Vector3> poly)
    {
        for (int i = 0; i < poly.Count; i++)
        {
            Vector3 prev = poly[(i - 1 + poly.Count) % poly.Count];
            Vector3 curr = poly[i];
            Vector3 next = poly[(i + 1) % poly.Count];
            if (IsReflex(prev, curr, next)) return false;
        }
        return true;
    }

    // splits the polygon into convex shapes recursively
    private List<List<Vector3>> Decompose(List<Vector3> polygon)
    {
        if (IsConvex(polygon) || polygon.Count < 4) return new List<List<Vector3>>() { polygon };

        int n = polygon.Count;
        Debug.Log("Decomposing polygon with " + n + " vertices");

        for (int i = 0; i < n; i++)
        {
            Vector3 prev = polygon[(i - 1 + n) % n];
            Vector3 curr = polygon[i];
            Vector3 next = polygon[(i + 1) % n];

            if (IsReflex(prev, curr, next))
            {
                for (int j = 0; j < n; j++)
                {
                    if (i == j || (j + 1) % n == i || (i + 1) % n == j) continue;

                    Vector3 p1 = polygon[i];
                    Vector3 p2 = polygon[j];
                    
                    // log angle
                    print("got here");
                    

                    // check if the line crosses the shape
                    if (!IntersectsAny(polygon, p1, p2))
                    {
                        // build the first half of the split
                        List<Vector3> poly1 = new List<Vector3>();
                        int k = i;
                        while (k != (j + 1) % n)
                        {
                            poly1.Add(polygon[k]);
                            k = (k + 1) % n;
                        }
                        poly1.Add(polygon[j]);

                        // build the second half of the split
                        List<Vector3> poly2 = new List<Vector3>();
                        k = j;
                        while (k != (i + 1) % n)
                        {
                            poly2.Add(polygon[k]);
                            k = (k + 1) % n;
                        }
                        poly2.Add(polygon[i]);

                        var result = new List<List<Vector3>>();
                        result.AddRange(Decompose(poly1));
                        result.AddRange(Decompose(poly2));
                        return result;
                    }
                }
            }
        }

        Debug.LogWarning("Polygon could not be decomposed further");
        return new List<List<Vector3>>() { polygon };
    }

    // turns a list of points into wall objects
    private List<Wall> PolygonToWalls(List<Vector3> poly)
    {
        List<Wall> walls = new List<Wall>();
        for (int i = 0; i < poly.Count; i++)
        {
            Vector3 a = poly[i];
            Vector3 b = poly[(i + 1) % poly.Count];
            walls.Add(new Wall(a, b));
        }
        return walls;
    }

    // checks if a new line intersects any polygon edges
    private bool IntersectsAny(List<Vector3> poly, Vector3 a, Vector3 b)
    {
        for (int i = 0; i < poly.Count; i++)
        {
            Vector3 c = poly[i];
            Vector3 d = poly[(i + 1) % poly.Count];
            if ((a == c && b == d) || (a == d && b == c)) continue;
            if (SegmentsIntersect(a, b, c, d)) return true;
        }
        
        return false;
    }

    // basic segment intersection check
    private bool SegmentsIntersect(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        return (CCW(a, c, d) != CCW(b, c, d)) && (CCW(a, b, c) != CCW(a, b, d));
    }

    // checks if three points make a counter-clockwise turn
    private bool CCW(Vector3 a, Vector3 b, Vector3 c)
    {
        return (c.z - a.z) * (b.x - a.x) > (b.z - a.z) * (c.x - a.x);
    }
    

    void Start()
    {
        EventBus.OnSetMap += SetMap;
    }

    void Update() {}

    // this gets called when the map is set
    public void SetMap(List<Wall> outline)
    {
        Graph navmesh = MakeNavMesh(outline);
        if (navmesh != null)
        {
            Debug.Log("got navmesh: " + navmesh.all_nodes.Count);
            EventBus.SetGraph(navmesh);
        }
    }
}