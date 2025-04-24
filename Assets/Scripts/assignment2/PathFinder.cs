using System;
using UnityEngine;
using System.Collections.Generic;

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
    public static (List<Vector3>, int) AStar(GraphNode start, GraphNode destination, Vector3 target)
    {
        // Implement A* here
        List<Vector3> path = new List<Vector3>() { target };
        int nodesExpanded = 0;
        GraphNode prev = null;
        GraphNode curr = null;
        
        // node, total heuristic, dist from successor
        List<Tuple<GraphNode, float, float>> q = new List<Tuple<GraphNode, float, float>>();
        Dictionary<GraphNode, GraphNode> visited = new Dictionary<GraphNode, GraphNode>();
        
        // this set is specifically for getting open nodes fast
        HashSet<GraphNode> frontier = new HashSet<GraphNode>();
        
        q.Add(new Tuple<GraphNode, float, float>(start, (target - start.GetCenter()).magnitude, 0));
        frontier.Add(start);

        while (q.Count > 0)
        {
            prev = curr;
            curr = q[q.Count - 1].Item1;
            float parentG = q[q.Count - 1].Item3;
            q.RemoveAt(q.Count - 1);
            frontier.Remove(curr);

            if (!visited.ContainsKey(curr))
            {
                visited[curr] = prev;
            }
            
            nodesExpanded++;
            
            // check if dest is in frontier
            if (curr == destination)
            {
                // work backwards from visited hashmap
                GraphNode pathNode = destination;
                while (pathNode != null)
                {
                    path.Insert(0, pathNode.GetCenter());
                    pathNode = visited[pathNode];
                }
            }
            
            // get all neighbors of curr node
            foreach (GraphNeighbor neighbor in curr.GetNeighbors())
            {
                if (visited.ContainsKey(neighbor.GetNode()))
                {
                    continue;
                }
                
                if (!frontier.Contains(neighbor.GetNode()))
                {
                    float gScore = parentG + (curr.GetCenter() - neighbor.GetNode().GetCenter()).magnitude;
                    float hScore = (target - neighbor.GetNode().GetCenter()).magnitude;
                    Tuple<GraphNode, float, float> distNode = new Tuple<GraphNode, float, float>(
                        neighbor.GetNode(), 
                        hScore + gScore,
                        gScore);
                    
                    // push neighbor to frontier
                    Push(q, distNode);
                    frontier.Add(neighbor.GetNode());
                }
            }
            print(q.Count);
        }

        // return path and number of nodes expanded
        return (path, nodesExpanded);
    }

    public static void Push(List<Tuple<GraphNode, float, float>> q, Tuple<GraphNode, float, float> curr)
    {
        // find where to insert curr (treat q as stack)
        for (int i = 0; i < q.Count; i++)
        {
            if (curr.Item2 >= q[i].Item2)
            {
                q.Insert(i, curr);
                return;
            }
        }
        // if curr has the lowest heuristic, push to end of q
        q.Add(curr);
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
