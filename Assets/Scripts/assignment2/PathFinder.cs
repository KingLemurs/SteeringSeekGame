using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PathFinder : MonoBehaviour
{
    // Assignment 2: Implement AStar
    //
    // DO NOT CHANGE THIS SIGNATURE (parameter types + return type)
    // AStar will be given the start node, destination node and the target position, and should return 
    // a path as a list of positions the agent has to traverse to reach its destination, as well as the
    // number of nodes that were expanded to find this path
    // The last entry of the path will be the target position, and you can also use it to calculate the heuristic
    // value of nodes you add to your search frontier; the number of expanded nodes tells us if your search was
    // efficient
    //
    // Take a look at StandaloneTests.cs for some test cases

    public class AStarEntry
    {
        public GraphNode node;
        public AStarEntry from;
        public Vector3 midPoint;
        public float gScore;
        public float fScore;
        
        public AStarEntry(GraphNode node, AStarEntry from, Vector3 midPoint, float gScore, float fScore)
        {
            this.node = node;
            this.from = from;
            this.midPoint = midPoint;
            this.gScore = gScore;
            this.fScore = fScore;
        }
    }
    public static (List<Vector3>, int) AStar(GraphNode start, GraphNode destination, Vector3 target)
    {
        // Implement A* here
        List<Vector3> path = new List<Vector3>() { target };
        int nodesExpanded = 0;
        AStarEntry curr = null;
        
        // node, total heuristic, g score
        List<AStarEntry> q = new List<AStarEntry>();
        List<AStarEntry> visited = new List<AStarEntry>();

        AStarEntry startEntry = new AStarEntry(start, null, start.GetCenter(), 0,
            (start.GetCenter() - destination.GetCenter()).magnitude);
        q.Add(startEntry);

        while (q.Count > 0)
        {
            curr = q[^1];
            q.RemoveAt(q.Count - 1);
            visited.Add(curr);
            nodesExpanded++;
            
            // check if dest is in frontier
            if (curr.node == destination)
            {
                // work backwards from visited hashmap
                AStarEntry pathNode = curr;
                while (pathNode != null)
                {
                    path.Insert(0, pathNode.midPoint);
                    pathNode = pathNode.from;
                }

                break;
            }
            
            // get all neighbors of curr node
            foreach (GraphNeighbor neighbor in curr.node.GetNeighbors())
            {
                bool skip = false;
                
                // check if in visited list
                foreach (var node in visited)
                {
                    if (neighbor.GetNode() == node.node)
                    {
                        skip = true;
                        break;
                    } 
                }

                if (skip)
                {
                    continue;
                }
                
                // calculate new heuristics
                float gScore = curr.gScore + (curr.node.GetCenter() - neighbor.GetNode().GetCenter()).magnitude;
                float hScore = (neighbor.GetNode().GetCenter() - destination.GetCenter()).magnitude;
                AStarEntry entry = new AStarEntry(neighbor.GetNode(), curr, neighbor.GetWall().midpoint, gScore,
                    gScore + hScore);

                bool shouldAdd = true;
                
                // if child already in frontier
                foreach (var node in q)
                {
                    if (neighbor.GetNode() == node.node && gScore >= node.gScore)
                    {
                        shouldAdd = false;
                        break;
                    }
                }

                if (shouldAdd)
                {
                    // push neighbor to frontier
                    Push(q, entry);
                }
            }
            print(q.Count);
            print(visited);
        }

        // return path and number of nodes expanded
        return (path, nodesExpanded);
    }

    // to mimic push() operation on a list
    public static void Push(List<AStarEntry> q, AStarEntry curr)
    {
        // find where to insert curr (treat q as stack)
        for (int i = 0; i < q.Count; i++)
        {
            if (curr.fScore > q[i].fScore)
            {
                q.Insert(i, curr);
                return;
            }
        }
        // if curr has the lowest heuristic, push to end of q
        q.Add(curr);
        foreach (var node in q)
        {
            print(node.fScore);
        }
    }

    public Graph graph;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        EventBus.OnTarget += PathFind;
        EventBus.OnSetGraph += SetGraph;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetGraph(Graph g)
    {
        graph = g;
    }

    // entry point
    public void PathFind(Vector3 target)
    {
        if (graph == null) return;

        // find start and destination nodes in graph
        GraphNode start = null;
        GraphNode destination = null;
        foreach (var n in graph.all_nodes)
        {
            if (Util.PointInPolygon(transform.position, n.GetPolygon()))
            {
                start = n;
            }
            if (Util.PointInPolygon(target, n.GetPolygon()))
            {
                destination = n;
            }
        }
        if (destination != null)
        {
            // only find path if destination is inside graph
            EventBus.ShowTarget(target);
            (List<Vector3> path, int expanded) = PathFinder.AStar(start, destination, target);

            Debug.Log("found path of length " + path.Count + " expanded " + expanded + " nodes, out of: " + graph.all_nodes.Count);
            EventBus.SetPath(path);
        }
    }
}
