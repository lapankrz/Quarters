using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class RoadController : MonoBehaviour
{
    public int roadWidth = 16;
    public float roadThickness = 0.05f;
    public float nearbyRoadThreshold = 14;
    public float minRoadLength = 14;
    public float minSnapAngle = 15;
    public float minSnapDistance = 20;
    public float carWidthPercentage = 0.5f;
    public Material roadMaterial;
    public List<Road> Roads
    {
        get
        {
            return FindObjectsOfType<Road>().ToList();    
        }
    }
    int layerMask = 1 << 8;

    bool placingNodes;
    bool dividingStartRoad;
    bool dividingEndRoad;
    Road dividedStartRoad;
    Road dividedEndRoad;

    GameObject currentRoad;
    RoadNode startNode;
    RoadNode endNode;

    GameObject tempRoadMiddle;
    GameObject tempRoadEnd;

    private bool editorEnabled;
    PlotController plotController;

    void Start()
    {
        editorEnabled = false;
        plotController = FindObjectOfType<PlotController>();
    }

    void Update()
    {
        if (editorEnabled)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, layerMask))
            {
                if (placingNodes) // redraw the road to cursor
                {
                    endNode = new RoadNode(hitInfo.point);
                    dividingEndRoad = false;
                    RoadNode node = FindNearbyRoadEnds(endNode, nearbyRoadThreshold);
                    if (node != null)
                    {
                        endNode = node;
                    }
                    else
                    {
                        // check if can snap to right angles
                        if (Input.GetKey("left shift"))
                        {
                            Vector3 drawnRoadDir = hitInfo.point - startNode.Position;
                            List<Road> adjacentRoads = startNode.Roads;
                            float minAngle = Mathf.Infinity;
                            float moduloMinAngle = Mathf.Infinity;
                            Road aligningRoad = null;
                            foreach (Road r in adjacentRoads)
                            {
                                Vector3 roadDir = r.GetDirectionVector();
                                float angle = Vector3.SignedAngle(roadDir, drawnRoadDir, Vector3.up);
                                float moduloAngle = Mathf.Abs(angle) % 90;
                                if (moduloAngle > 45)
                                {
                                    moduloAngle = 90 - moduloAngle;
                                }
                                if (moduloAngle < moduloMinAngle || aligningRoad == null)
                                {
                                    minAngle = angle;
                                    moduloMinAngle = moduloAngle;
                                    aligningRoad = r;
                                }
                            }
                            if (aligningRoad != null && moduloMinAngle < minSnapAngle)
                            {
                                int turn = Mathf.RoundToInt(minAngle / 90.0f);
                                if (minAngle % 90 > 45)
                                {
                                    turn += 1;
                                }
                                turn *= 90;
                                var dir = aligningRoad.GetDirectionVector();
                                Vector3 rotatedVector = Quaternion.AngleAxis(turn, Vector3.up) * dir;

                                Vector3 start = this.startNode.Position;
                                Vector3 end = start + rotatedVector;
                                RoadNode newNode = new RoadNode(start + Vector3.Project(hitInfo.point - start, end - start));
                                if (newNode.GetDistanceToPoint(hitInfo.point) < minSnapDistance)
                                {
                                    endNode = newNode;
                                }
                            }
                        }

                        (Road road, Vector3 intersection) = FindNearbyRoadSegments(hitInfo.point);
                        if (road != null)
                        {
                            endNode = new RoadNode(intersection);
                            dividingEndRoad = true;
                            dividedEndRoad = road;
                        }
                        else
                        {
                            
                        }
                    }

                    if (tempRoadMiddle != null)
                    {
                        Destroy(tempRoadMiddle);
                    }
                    tempRoadMiddle = CreateRoadMiddle(startNode.Position, endNode.Position);
                    tempRoadMiddle.transform.parent = currentRoad.transform;

                    if (tempRoadEnd != null)
                    {
                        Destroy(tempRoadEnd);
                    }
                    tempRoadEnd = CreateRoadEnd(endNode.Position);
                    tempRoadEnd.transform.parent = currentRoad.transform;
                }

                if (Input.GetMouseButtonDown(0))
                {
                    if (!EventSystem.current.IsPointerOverGameObject())
                    {
                        HandleMouseClick(hitInfo.point);
                    }
                }
            }
        }
    }

    // Handle mouse click in road placing mode
    public void HandleMouseClick(Vector3 hitPoint)
    {
        if (placingNodes)
        {
            EndRoad();
        }
        else
        {
            StartRoad(hitPoint);
        }
    }

    // end current road segment
    public void EndRoad()
    {
        Road road = currentRoad.AddComponent<Road>();
        road.Init(startNode, endNode);
        if (road.GetLength() > minRoadLength)
        {
            Roads.Add(road);
            plotController.CreatePlots(road, roadWidth);
            tempRoadEnd = null;
            tempRoadMiddle = null;
            placingNodes = false;
        }

        if (dividingStartRoad)
        {
            DivideRoadInTwo(dividedStartRoad, startNode);
        }
        if (dividingEndRoad)
        {
            DivideRoadInTwo(dividedEndRoad, endNode);
        }

        plotController.EnablePlotOverlay();
    }

    // start drawing new road
    public void StartRoad(Vector3 hitPoint)
    {
        startNode = new RoadNode(hitPoint);
        dividingStartRoad = false;
        RoadNode node = FindNearbyRoadEnds(hitPoint, nearbyRoadThreshold);
        if (node != null)
        {
            startNode = node;
        }
        else
        {
            (Road road, Vector3 intersection) = FindNearbyRoadSegments(hitPoint);
            if (road != null)
            {
                startNode = new RoadNode(intersection);
                dividingStartRoad = true;
                dividedStartRoad = road;
            }
        }

        currentRoad = new GameObject("Road");
        currentRoad.tag = "Road";
        var roadEnd = CreateRoadEnd(startNode.Position);
        roadEnd.transform.parent = currentRoad.transform;

        placingNodes = true;
    }

    // splits a road in two in the position of newNode
    public void DivideRoadInTwo(Road oldRoad, RoadNode newNode)
    {
        GameObject roadObject1 = CreateWholeRoad(oldRoad.startNode.Position, newNode.Position);
        Road road1 = roadObject1.AddComponent<Road>();
        road1.Init(oldRoad.startNode, newNode);
        road1.startNode.Roads.Remove(oldRoad);

        GameObject roadObject2 = CreateWholeRoad(newNode.Position, oldRoad.endNode.Position);
        Road road2 = roadObject2.AddComponent<Road>();
        road2.Init(newNode, oldRoad.endNode);
        road2.endNode.Roads.Remove(oldRoad);

        oldRoad.ClearPlots();
        Destroy(oldRoad.gameObject);

        plotController.CreatePlots(road1, roadWidth);
        plotController.CreatePlots(road2, roadWidth);
    }

    // returns road segment that's closest to position
    public (Road, Vector3) FindNearbyRoadSegments(Vector3 position)
    {
        float minDist = Mathf.Infinity;
        Road closestRoad = null;
        foreach (Road road in Roads)
        {
            float dist = road.GetDistanceToPoint(position);
            if (dist < minDist)
            {
                minDist = dist;
                closestRoad = road;
            }
        }
        if (minDist < nearbyRoadThreshold)
        {
            return (closestRoad, closestRoad.GetClosestPointOnRoad(position));
        }
        else
        {
            return (null, position);
        }
    }

    internal void DeleteRoad(GameObject gameObject)
    {
        Road road = Roads.Find(r => r.gameObject == gameObject);
        road.startNode.Roads.Remove(road);
        road.endNode.Roads.Remove(road);
        Roads.Remove(road);

        foreach (var plot in road.leftPlots)
        {
            plotController.plots.Remove(plot);
        }
        foreach (var plot in road.rightPlots)
        {
            plotController.plots.Remove(plot);
        }

        foreach (Road r in road.startNode.Roads)
        {
            plotController.CreatePlots(r, roadWidth);
        }
        foreach (Road r in road.endNode.Roads)
        {
            plotController.CreatePlots(r, roadWidth);
        }

        Destroy(gameObject);
    }

    public void EnableEditor()
    {
        editorEnabled = true;
        plotController.EnablePlotOverlay();
    }

    public void DisableEditor()
    {
        if (placingNodes)
        {
            Destroy(currentRoad);
            tempRoadMiddle = null;
            tempRoadEnd = null;
            placingNodes = false;
        }
        editorEnabled = false;
        plotController.DisablePlotOverlay();
    }

    public RoadNode FindNearbyRoadEnds(Vector3 point, float threshold = Mathf.Infinity)
    {
        RoadNode node = new RoadNode(point);
        return FindNearbyRoadEnds(node, threshold);
    }

    public RoadNode FindNearbyRoadEnds(RoadNode node, float threshold = Mathf.Infinity)
    {
        float minDist = Mathf.Infinity;
        RoadNode closestNode = null;
        foreach (Road road in Roads)
        {
            if (node != road.startNode && node != road.endNode)
            {
                Vector3 position = node.Position;
                float dist = road.startNode.GetDistanceToPoint(position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestNode = road.startNode;
                }

                dist = road.endNode.GetDistanceToPoint(position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestNode = road.endNode;
                }
            }
        }
        if (minDist < threshold)
        {
            return closestNode;
        }
        else
        {
            return null;
        }
    }

    public GameObject CreateWholeRoad(Vector3 startNode, Vector3 endNode)
    {
        GameObject road = new GameObject("Road");
        road.tag = "Road";
        var start = CreateRoadEnd(startNode);
        start.transform.parent = road.transform;
        var end = CreateRoadEnd(endNode);
        end.transform.parent = road.transform;
        var middle = CreateRoadMiddle(startNode, endNode);
        middle.transform.parent = road.transform;
        return road;
    }

    GameObject CreateRoadMiddle(Vector3 startNode, Vector3 endNode)
    {
        float length = (endNode - startNode).magnitude;
        GameObject roadMiddle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roadMiddle.name = "RoadMiddle";
        Vector3 position = (startNode + endNode) / 2;
        roadMiddle.transform.position = position;
        Vector3 direction = endNode - startNode;
        if (direction != Vector3.zero)
        {
            Quaternion rotation = Quaternion.LookRotation(direction) * Quaternion.FromToRotation(Vector3.right, Vector3.forward);
            roadMiddle.transform.rotation = rotation;
        }
        roadMiddle.transform.localScale = new Vector3(length, roadThickness, roadWidth);
        roadMiddle.GetComponent<MeshRenderer>().material = roadMaterial;
        return roadMiddle;
    }

    GameObject CreateRoadEnd(Vector3 position)
    {
        GameObject roadStart = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        roadStart.name = "RoadEnd";
        roadStart.transform.localScale = new Vector3(roadWidth, roadThickness / 2, roadWidth);
        roadStart.transform.position = position;
        roadStart.GetComponent<MeshRenderer>().material = roadMaterial;
        return roadStart;
    }
}
