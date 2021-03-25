using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Road : MonoBehaviour
{
    public float width = 22;
    public float carWidthPercentage = 0.5f;
    public bool hasTrees;
    public float treeDistance;
    public RoadNode startNode;
    public RoadNode endNode;
    PlotController plotController;
    RoadController roadController;
    BuildingController buildingController;
    public List<Plot> leftPlots = new List<Plot>();
    public List<Plot> rightPlots = new List<Plot>();
    public Vector3 leftStartSideWalk;
    public Vector3 rightStartSideWalk;
    public Vector3 leftEndSideWalk;
    public Vector3 rightEndSideWalk;

    public void Init(RoadNode start, RoadNode end,
        float width = 22, float carWidthPercentage = 0.5f,
        bool hasTrees = false, float treeDistance = 10f)
    {
        this.width = width;
        this.carWidthPercentage = carWidthPercentage;
        this.hasTrees = hasTrees;
        this.treeDistance = treeDistance;
        roadController = FindObjectOfType<RoadController>();
        startNode = start;
        startNode.AddOutgoingRoad(this);
        endNode = end;
        endNode.AddOutgoingRoad(this);
        CalculateSidewalkEnds();
        CreateSidewalk();
    }

    public void CalculateSidewalkEnds()
    {
        CalculateStartSidewalkBounds();
        CalculateEndSidewalkBounds();
    }

    public void CalculateStartSidewalkBounds()
    {
        Vector3 dir = GetDirectionVector().normalized;
        Vector3 right = new Vector3(dir.z, dir.y, -dir.x) * width * carWidthPercentage / 2f;
        leftStartSideWalk = startNode.Position - right;
        rightStartSideWalk = startNode.Position + right;
    }

    public void CalculateEndSidewalkBounds()
    {
        Vector3 dir = GetDirectionVector().normalized;
        Vector3 right = new Vector3(dir.z, dir.y, -dir.x) * width * carWidthPercentage / 2f;
        leftEndSideWalk = endNode.Position - right;
        rightEndSideWalk = endNode.Position + right;
    }


    void Start()
    {
        plotController = FindObjectOfType<PlotController>();
        roadController = FindObjectOfType<RoadController>();
        buildingController = FindObjectOfType<BuildingController>();
    }

    void Update()
    {

    }

    public void SpawnBuildings()
    {
        var plots = GetAllPlots();
        foreach (var plot in plots)
        {
            plotController.SpawnBuilding(plot);
        }
    }

    public void ClearPlots()
    {
        var plots = GetAllPlots();
        foreach (var plot in plots)
        {
            if (plotController.plots.Contains(plot))
            {
                plotController.plots.Remove(plot);
            }
            foreach (var border in plot.borders)
            {
                Destroy(border);
            }
            plot.borders = new List<GameObject>();
        }
        this.leftPlots = new List<Plot>();
        this.rightPlots = new List<Plot>();
    }

    public void ClearSidewalk(bool left = true, bool right = true, bool leftStartCurve = true, bool leftEndCurve = true,
        bool rightStartCurve = true, bool rightEndCurve = true)
    {
        for (int i = transform.childCount - 1; i >= 0; --i)
        {
            var child = transform.GetChild(i).gameObject;
            if ((left && child.name == "LeftSidewalk") ||
                (right && child.name == "RightSidewalk") ||
                (leftStartCurve && child.name == "LeftStartSidewalkCurve") ||
                (rightStartCurve && child.name == "RightStartSidewalkCurve") ||
                (leftEndCurve && child.name == "LeftEndSidewalkCurve") ||
                (rightEndCurve && child.name == "RightEndSidewalkCurve"))
            {
                DestroyImmediate(child);
            }

        }
    }

    /// <summary>
    /// Creates GameObjects for left and right middle parts of the sidewalk (along road)
    /// </summary>
    public void CreateSidewalk()
    {
        roadController = FindObjectOfType<RoadController>();
        Vector3 right = GetRightVector() * width * (1f - carWidthPercentage) / 4f; ;
        Vector3 start = leftStartSideWalk - right;
        Vector3 end = leftEndSideWalk - right;

        float length = (end - start).magnitude;
        GameObject sidewalk = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sidewalk.name = "LeftSidewalk";
        Vector3 position = (start + end) / 2;
        sidewalk.transform.position = position;
        Vector3 direction = end - start;
        if (direction != Vector3.zero)
        {
            Quaternion rotation = Quaternion.LookRotation(direction) * Quaternion.FromToRotation(Vector3.right, Vector3.forward);
            sidewalk.transform.rotation = rotation;
        }
        sidewalk.transform.localScale = new Vector3(length, roadController.sidewalkThickness, width * (1 - carWidthPercentage) / 2);
        sidewalk.GetComponent<MeshRenderer>().material = roadController.pavementMaterial;
        sidewalk.transform.parent = gameObject.transform;

        start = rightStartSideWalk + right;
        end = rightEndSideWalk + right;

        length = (end - start).magnitude;
        sidewalk = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sidewalk.name = "RightSidewalk";
        position = (start + end) / 2;
        sidewalk.transform.position = position;
        direction = end - start;
        if (direction != Vector3.zero)
        {
            Quaternion rotation = Quaternion.LookRotation(direction) * Quaternion.FromToRotation(Vector3.right, Vector3.forward);
            sidewalk.transform.rotation = rotation;
        }
        sidewalk.transform.localScale = new Vector3(length, roadController.sidewalkThickness, width * (1 - carWidthPercentage) / 2);
        sidewalk.GetComponent<MeshRenderer>().material = roadController.pavementMaterial;
        sidewalk.transform.parent = gameObject.transform;
    }

    public List<Plot> GetAllPlots()
    {
        var allPlots = new List<Plot>();
        allPlots.AddRange(leftPlots);
        allPlots.AddRange(rightPlots);
        return allPlots;
    }

    public Vector3 GetRightVector()
    {
        var dir = GetDirectionVector().normalized;
        return new Vector3(dir.z, dir.y, -dir.x);
    }

    /// <summary>
    /// Finds neighboring roads with the smallest or greatest angle from node
    /// </summary>
    /// <param name="fromStartNode">True if from startNode, false if from endNode</param>
    /// <param name="leftmost">Finds road with the greatest angle if true, or with the smallest angle otherwise</param>
    /// <returns></returns>
    public Road GetNeighboringRoad(bool fromStartNode, bool leftmost, bool mustbeSameSide = true)
    {
        List<Road> roads = new List<Road>();
        Vector3 roadVector = GetDirectionVector();
        RoadNode node = fromStartNode ? startNode : endNode;
        foreach (var road in node.Roads)
        {
            roads.Add(road);
        }

        if (roads.Count <= 1)
        {
            return null;
        }

        roads.Sort((r1, r2) =>
        {
            Vector3 v1 = r1.GetDirectionVector();
            if (r1.endNode == node)
            {
                v1 = -v1;
            }
            Vector3 v2 = r2.GetDirectionVector();
            if (r2.endNode == node)
            {
                v2 = -v2;
            }
            return Mathf.Atan2(v1.x, v1.z).CompareTo(Mathf.Atan2(v2.x, v2.z));
        });

        int ind = roads.IndexOf(this);
        int prevInd = ind == roads.Count - 1 ? 0 : ind + 1;
        int nextInd = ind == 0 ? roads.Count - 1 : ind - 1;
        Road nextRoad = roads[nextInd];
        Road prevRoad = roads[prevInd];

        if (fromStartNode)
        {
            roadVector = -roadVector;
        }
        Road foundRoad = leftmost ? prevRoad : nextRoad;
        if (!mustbeSameSide)
        {
            return foundRoad;
        }
        Vector3 foundVector = foundRoad.GetDirectionVector();
        if (foundRoad.endNode == node)
        {
            foundVector = -foundVector;
        }

        float dot = -roadVector.x * foundVector.z + roadVector.z * foundVector.x;
        if ((dot < 0 && leftmost) || // turns left and searching leftmost
            (dot > 0 && !leftmost)) // turns right and searching rightmost
        {
            return foundRoad;
        }
        else
        {
            return null;
        }
    }

    // returns the node where 2 roads meet, returns null if they don't connect
    public RoadNode GetCommonRoadNode(Road road)
    {
        if (startNode == road.startNode || startNode == road.endNode)
        {
            return startNode;
        }
        else if (endNode == road.startNode || endNode == road.endNode)
        {
            return endNode;
        }
        else
        {
            return null;
        }
    }

    public bool IsParallelTo(Road road)
    {
        Vector3 p0 = startNode.Position;
        Vector3 p1 = endNode.Position;
        Vector3 q0 = road.startNode.Position;
        Vector3 q1 = road.endNode.Position;

        float A1 = p1.z - p0.z;
        float B1 = p0.x - p1.x;

        float A2 = q1.z - q0.z;
        float B2 = q0.x - q1.x;

        float delta = A1 * B2 - A2 * B1;
        return Mathf.Abs(delta) < 0.01f;
    }
    
    // returns distance to the closest point on road
    public float GetDistanceToPoint(Vector3 point)
    {
        // Return minimum distance between line segment vw and point p
        Vector3 p1 = this.startNode.Position;
        Vector3 p2 = this.endNode.Position;
        float l2 = (p1 - p2).magnitude * (p1 - p2).magnitude; // i.e. |w-v|^2 -  avoid a sqrt
        if (l2 == 0.0) return (point - p1).magnitude;   // v == w case
                                                        // Consider the line extending the segment, parameterized as v + t (w - v).
                                                        // We find projection of point p onto the line. 
                                                        // It falls where t = [(p-v) . (w-v)] / |w-v|^2
                                                        // We clamp t from [0,1] to handle points outside the segment vw.
        float t = Mathf.Max(0, Mathf.Min(1, Vector3.Dot(point - p1, p2 - p1) / l2));
        Vector3 projection = p1 + t * (p2 - p1);  // Projection falls on the segment
        float dist = (point - projection).magnitude;

        return dist;
    }

    // returns closest position on road to point - casts the point to the half line of the road
    public Vector3 GetClosestPointOnRoad(Vector3 point)
    {
        Vector3 start = this.startNode.Position;
        Vector3 end = this.endNode.Position;
        return start + Vector3.Project(point - start, end - start);
    }

    // returns angle in degrees between 2 roads
    public float GetAngleBetweenRoad(Road road)
    {
        Vector3 vec1 = GetDirectionVector();
        Vector3 vec2 = road.GetDirectionVector();
        RoadNode common = GetCommonRoadNode(road);
        if (common == road.endNode)
        {
            vec2 = -vec2;
        }
        if (common == endNode)
        {
            vec1 = -vec1;
        }
        return Vector3.Angle(vec1, vec2);
    }

    // returns vector from start node to end node
    public Vector3 GetDirectionVector()
    {
        return endNode.Position - startNode.Position;
    }

    // returns the length of the road
    public float GetLength()
    {
        return (startNode.Position - endNode.Position).magnitude;
    }
}
