using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleController : MonoBehaviour
{
    public GameObject vehicleObject;
    private float movementSpeed = 25f;
    RoadController roadController;

    void Start()
    {
        roadController = FindObjectOfType<RoadController>();

    }

    void Update()
    {
        
    }

    public void SpawnVehicle(Building start)//, Building to)
    {
        var buildings = FindObjectsOfType<Building>();
        int buildingCount = buildings.Length;
        if (buildingCount >= 2)
        {
            int endInd = Random.Range(0, buildingCount);
            Building end = buildings[endInd];
            while (end != null && start == end)
            {
                endInd = Random.Range(0, buildingCount);
                end = buildings[endInd];
            }

            Vector3 pos = start.entryPoint;
            var vehicle = Instantiate(vehicleObject, pos, Quaternion.identity);
            RoadNode startNode = roadController.FindNearbyRoadEnds(start.entryPoint);
            RoadNode endNode = roadController.FindNearbyRoadEnds(end.entryPoint);
            var script = vehicle.AddComponent<FollowPathScript>();
            List<Vector3> path = null;
            if (startNode != null && endNode != null)
            {
                path = FindPath(start, end);
            }
            script.Init(start, end, path, movementSpeed);
        }
    }

    List<Vector3> FindPath(Building start, Building end)
    {
        var minDist = Mathf.Infinity;
        List<Vector3> shortestPath = null;
        Road startRoad = start.plot.adjacentRoad;
        RoadNode[] startNodes =
        {
            startRoad.startNode,
            startRoad.endNode
        };
        Road endRoad = end.plot.adjacentRoad;
        RoadNode[] endNodes =
        {
            endRoad.startNode,
            endRoad.endNode
        };

        if (startRoad == endRoad)
        {
            shortestPath = CreatePathFromNodes(start, end, new List<RoadNode>());
        }
        else
        {
            for (int i = 0; i < 2; ++i)
            {
                for (int j = 0; j < 2; ++j)
                {
                    var startNode = startNodes[i];
                    var endNode = endNodes[j];
                    var nodePath = startNode.GetPathToNode(endNode);
                    var path = CreatePathFromNodes(start, end, nodePath);
                    var dist = GetDistanceOfPath(path);
                    if (dist < minDist || shortestPath == null)
                    {
                        minDist = dist;
                        shortestPath = path;
                    }
                }
            }
        }

        return shortestPath;
    }

    public List<Vector3> CreatePathFromNodes(Building start, Building end, List<RoadNode> nodePath)
    {
        if (nodePath == null)
        {
            return null;
        }
        float roadWidth = roadController.roadWidth;
        List<Vector3> path = new List<Vector3>();
        Vector3 startCenter = start.plot.adjacentRoad.GetClosestPointOnRoad(start.entryPoint);
        Vector3 endCenter = end.plot.adjacentRoad.GetClosestPointOnRoad(end.entryPoint);
        Vector3 pos1, pos2;

        Vector3 dir;
        if (nodePath.Count == 0)
        {
            dir = (endCenter - startCenter).normalized;
        }
        else
        {
            dir = (nodePath[0].Position - startCenter).normalized;
        }
        var right = new Vector3(dir.z, dir.y, -dir.x);
        var startPos = startCenter + right * roadWidth * roadController.carWidthPercentage / 4;
        path.Add(startPos);

        if (nodePath != null && nodePath.Count > 0)
        {
            pos2 = nodePath[0].Position - dir * roadWidth / 2 + right * roadWidth * roadController.carWidthPercentage / 4;
            path.Add(pos2);

            Vector3 lastPos1 = startPos;
            Vector3 lastPos2 = pos2;
            for (int i = 0; i < nodePath.Count; ++i)
            {
                Vector3 currPos = nodePath[i].Position;
                Vector3 nextPos = i == nodePath.Count - 1 ? endCenter : nodePath[i + 1].Position;
                dir = (nextPos - currPos).normalized;
                right = new Vector3(dir.z, dir.y, -dir.x);

                pos1 = currPos + dir * roadWidth / 2 + right * roadWidth * roadController.carWidthPercentage / 4;
                pos2 = nextPos - dir * roadWidth / 2 + right * roadWidth * roadController.carWidthPercentage / 4;

                Vector3 intersection = roadController.GetIntersectionOfLines(lastPos1, lastPos2, pos1, pos2);
                var points = roadController.GetNPointsOnBezierCurve(lastPos2, intersection, pos1, 20);
                path.AddRange(points);

                path.Add(pos1);
                if (i < nodePath.Count - 1)
                {
                    path.Add(pos2);
                }

                lastPos1 = pos1;
                lastPos2 = pos2;
            }
        }
        else
        {
            return null;
        }

        Vector3 lastNodePos = nodePath.Count > 0 ? nodePath[nodePath.Count - 1].Position : startCenter;
        dir = (endCenter - lastNodePos).normalized;
        right = new Vector3(dir.z, dir.y, -dir.x);

        Vector3 endPos = endCenter + right * roadWidth * roadController.carWidthPercentage / 4;
        path.Add(endPos);
        path.Add(end.entryPoint);

        return path;
    }

    public float GetDistanceOfPath(List<Vector3> path)
    {
        if (path == null)
        {
            return Mathf.Infinity;
        }
        float dist = 0;
        for (int i = 0; i < path.Count - 1; ++i)
        {
            dist += (path[i + 1] - path[i]).magnitude;
        }
        return dist;
    }
}
