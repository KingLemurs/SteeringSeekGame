using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class NavMesh2 : MonoBehaviour
{
    // implement NavMesh generation here:
    //    the outline are Walls in counterclockwise order
    //    iterate over them, and if you find a reflex angle
    //    you have to split the polygon into two
    //    then perform the same operation on both parts
    //    until no more reflex angles are present
    //
    //    when you have a number of polygons, you will have
    //    to convert them into a graph: each polygon is a node
    //    you can find neighbors by finding shared edges between
    //    different polygons (or you can keep track of this while 
    //    you are splitting)
    public Graph MakeNavMesh(List<Wall> outline)
    {
        Graph g = new Graph();
        g.all_nodes = new List<GraphNode>();
        
        // plan - 
        // First iterate through outline, if there are any angles > 180, add to open set
        // while open set is not empty, add new edges
        Split(0, outline.Count - 1, outline, g);
        
        return g;
    }

    private GraphNode Split(int start, int end, List<Wall> polygon, Graph g)
    {
        if (polygon.Count < 3)
        {
            return null;
        }
        
        for (int i = start; i < end; i++)
        {
            if (polygon[i].Crosses(polygon[i + 1]))
            {
                Vector3 startPoint = polygon[i].end;
                int j = i + 2;
                List<WallEntry> stack = new List<WallEntry>();
                
                // find other end to connect to
                // add possible end points

                while (true)
                {
                    // wrap around list
                    if (j >= polygon.Count)
                    {
                        j = 0;
                    }
                    
                    // exit case
                    Wall curr = polygon[j];
                    if (curr.start == startPoint || curr.end == startPoint)
                    {
                        break;
                    }

                    float angle = Vector3.SignedAngle(startPoint, curr.start, Vector3.up);
                    WallEntry entry = new WallEntry(curr.start, Mathf.Abs(90 - angle), j);
                    Push(stack, entry);
                    
                    angle = Vector3.SignedAngle(startPoint, curr.end, Vector3.up);
                    entry = new WallEntry(curr.end, Mathf.Abs(90 - angle), j);
                    Push(stack, entry);

                    j++;
                }
                
                print(stack.Count);
                
                // pick best possible end point
                Vector3 endPoint = stack[^1].endPoint;
                int endWallIndex = stack[^1].wallIndex;
                
                // to save mem before recursion
                stack.Clear();
                Wall split = new Wall(startPoint, endPoint);
                
                // cache polygon
                List<Wall> polyLeft = new List<Wall>();
                List<Wall> polyRight = new List<Wall>();
                int index = endWallIndex;

                while (true)
                {
                    if (index >= polygon.Count)
                    {
                        index = 0;
                    }

                    if (polygon[index].end == startPoint)
                    {
                        polyLeft.Add(polygon[index]);
                        polyLeft.Add(split);
                        break;
                    }
                    
                    polyLeft.Add(polygon[index]);
                    index++;
                }

                index = i;

                while (true)
                {
                    if (index >= polygon.Count)
                    {
                        index = 0;
                    }

                    if (polygon[index].start == endPoint)
                    {
                        polyRight.Add(polygon[index]);
                        polyRight.Add(split);
                        break;
                    }
                    
                    polyRight.Add(polygon[index]);
                    index++;
                }
                
                print(polygon.Count);
                print(polyLeft.Count);
                print(polyRight.Count);
                return null;
                
                // recurs into the new polygon
                GraphNode addLeft = Split(0, polyLeft.Count - 1, polygon, g);
                GraphNode addRight = Split(0, polyRight.Count - 1, polygon, g);
                // ONLY add node IF there were no splits from child calls
                // which means they added nodes

                if (addLeft != null && addRight != null)
                {
                    // connect nodes
                    addLeft.AddNeighbor(addRight, 0);
                    addRight.AddNeighbor(addLeft, 0);
                    g.outline.Add(split);
                    return null;
                }

                GraphNode node = new GraphNode(g.all_nodes.Count, polygon);
                g.all_nodes.Add(node);
                return node;
            }
        }
        return null;
    }

    private void Push(List<WallEntry> stack, WallEntry entry)
    {
        print("trying to push");
        for (int i = 0; i < stack.Count; i++)
        {
            if (entry.fScore > stack[i].fScore)
            {
                stack.Insert(i, entry);
                return;
            }
        }
        stack.Add(entry);
    }
    
    

    //List<Wall> outline;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        EventBus.OnSetMap += SetMap;
    }

    // Update is called once per frame
    void Update()
    {
       

    }

    public void SetMap(List<Wall> outline)
    {
        Graph navmesh = MakeNavMesh(outline);
        if (navmesh != null)
        {
            Debug.Log("got navmesh: " + navmesh.all_nodes.Count);
            EventBus.SetGraph(navmesh);
        }
    }

    private class WallEntry
    {
        public Vector3 endPoint;
        public float fScore;
        public int wallIndex;

        public WallEntry(Vector3 endPoint, float fScore, int wallIndex)
        {
            this.endPoint = endPoint;
            this.fScore = fScore;
            this.wallIndex = wallIndex;
        }
    }
}
