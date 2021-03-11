using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianController : MonoBehaviour
{
    public GameObject pedestrianObject;
    private float movementSpeed = 10f;
    RoadController roadController;

    // Start is called before the first frame update
    void Start()
    {
        roadController = FindObjectOfType<RoadController>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SpawnPedestrian(Building start)//, Building to)
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
            var pedestrian = Instantiate(pedestrianObject, pos, Quaternion.identity);
            RoadNode startNode = roadController.FindNearbyRoadEnds(start.entryPoint);
            RoadNode endNode = roadController.FindNearbyRoadEnds(end.entryPoint);
            var script = pedestrian.AddComponent<FollowPathScript>();
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
            shortestPath = new List<Vector3>();
            shortestPath.Add(startRoad.GetClosestPointOnRoad(start.entryPoint));
            shortestPath.Add(startRoad.GetClosestPointOnRoad(end.entryPoint));
            shortestPath.Add(end.entryPoint);
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
        List<Vector3> path = new List<Vector3>();
        Vector3 startPos = start.plot.adjacentRoad.GetClosestPointOnRoad(start.entryPoint); //temporary - replace with door position
        path.Add(startPos);
        if (nodePath != null)
        {
            foreach (var node in nodePath)
            {
                path.Add(node.Position);
            }
        }
        else
        {
            return null;
        }
        Vector3 endPos = end.plot.adjacentRoad.GetClosestPointOnRoad(end.entryPoint); //temp
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
