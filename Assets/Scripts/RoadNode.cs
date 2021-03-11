using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadNode
{
    public Vector3 Position { get; set; }
    public List<Road> Roads;

    public RoadNode(Vector3 position)
    {
        Position = position;
        Roads = new List<Road>();
    }

    public float GetDistanceToPoint(Vector3 point)
    {
        return (Position - point).magnitude;
    }

    public void AddOutgoingRoad(Road road)
    {
        Roads.Add(road);
    }

    // Returns the road that connects to node passed as argument (null if there is no such road)
    public Road GetRoadToNode(RoadNode node)
    {
        foreach (var road in Roads)
        {
            if (road.startNode == node || road.endNode == node)
            {
                return road;
            }
        }
        return null;
    }

    public struct AStarNode
    {
        public RoadNode node;
        public float distance;
        public AStarNode(RoadNode node, float distance)
        {
            this.node = node;
            this.distance = distance;
        }
    }

    public List<RoadNode> GetPathToNode(RoadNode goal)
    {
        List<AStarNode> open = new List<AStarNode>();
        open.Add(new AStarNode(this, 0));
        List<RoadNode> close = new List<RoadNode>();

        Dictionary<RoadNode, RoadNode> previous = new Dictionary<RoadNode, RoadNode>();
        Dictionary<RoadNode, float> distance = new Dictionary<RoadNode, float>();
        distance.Add(this, 0);

        while (open.Count > 0)
        {
            open.Sort((n1, n2) => n1.distance.CompareTo(n2.distance));
            var curr = open[0];
            open.RemoveAt(0);
            close.Add(curr.node);

            if (curr.node == goal) // end search - construct path
            {
                return ConstructPath(this, goal, previous);
            }

            foreach (var road in curr.node.Roads)
            {
                var next = road.startNode;
                if (next == curr.node)
                {
                    next = road.endNode;
                }

                if (close.Contains(next))
                {
                    continue;
                }

                if (!open.Exists(n => n.node == next)) // open doesn't contain node
                {
                    open.Add(new AStarNode(next, Mathf.Infinity));
                    distance.Add(next, Mathf.Infinity);
                }

                float newDistance = distance[curr.node] == Mathf.Infinity ? Mathf.Infinity : distance[curr.node] + road.GetLength();
                if (distance[next] > newDistance)
                {
                    distance[next] = newDistance;
                    int ind = open.FindIndex(n => n.node == next);
                    open[ind] = new AStarNode(next, newDistance);
                    if (previous.ContainsKey(next))
                    {
                        previous[next] = curr.node;
                    }
                    else
                    {
                        previous.Add(next, curr.node);
                    }
                }
            }
        }
        return null;
    }

    public List<RoadNode> ConstructPath(RoadNode start, RoadNode end, Dictionary<RoadNode, RoadNode> previous)
    {
        List<RoadNode> path = new List<RoadNode>();
        path.Add(end);
        if (start != end)
        {
            var curr = previous[end];
            while (curr != start)
            {
                path.Add(curr);
                curr = previous[curr];
            }

            path.Add(start);
            path.Reverse();
        }     
        return path;
    }
}
