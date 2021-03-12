using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class RoadController : MonoBehaviour
{
    public int roadWidth = 22;
    public float roadThickness = 0.05f;
    public float sidewalkThickness = 0.2f;
    public float nearbyRoadThreshold = 14;
    public float minRoadLength = 14;
    public float minSnapAngle = 15;
    public float minSnapDistance = 20;
    public float carWidthPercentage = 0.5f;
    public Material roadMaterial;
    public Material pavementMaterial;

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
    BuildingController buildingController;

    void Start()
    {
        editorEnabled = false;
        plotController = FindObjectOfType<PlotController>();
        buildingController = FindObjectOfType<BuildingController>();
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

    public Vector3 GetPointOnBezierCurve(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        return Vector3.Lerp(Vector3.Lerp(p0, p1, t), Vector3.Lerp(p1, p2, t), t);
    }

    public List<Vector3> GetNPointsOnBezierCurve(Vector3 p0, Vector3 p1, Vector3 p2, int n)
    {
        List<Vector3> points = new List<Vector3>();
        for (int i = 0; i < n; ++i)
        {
            float t = i * 1f / (n - 1);
            Vector3 point = GetPointOnBezierCurve(p0, p1, p2, t);
            points.Add(point);
        }
        return points;
    }

    public void DrawCurvesOnRoadNode(RoadNode node)
    {
        foreach (var road1 in node.Roads)
        {
            bool fromStart1 = node == road1.startNode;
            Road road2 = road1.GetNeighboringRoad(fromStart1, true, false);
            if (road2 != null)
            {
                bool fromStart2 = node == road2.startNode;
                int n = 10;
                List<Vector3> points = GetBezierCurveBetweenRoads(road1, road2, n);
                Vector3 right1 = road1.GetRightVector() * (1 - carWidthPercentage) * roadWidth / 2f;
                Vector3 right2 = road2.GetRightVector() * (1 - carWidthPercentage) * roadWidth / 2f;
                GameObject curve;
                string curveName2;
                if (fromStart1)
                {
                    road1.rightStartSideWalk = points[0];
                    road1.ClearSidewalk(true, true, false, false, true, false);
                    if (fromStart2)
                    {
                        road2.ClearSidewalk(true, true, true, false, false, false);
                        road2.leftStartSideWalk = points[n - 1];
                        curve = CreateSidewalkCurves(road1.rightStartSideWalk, road2.leftStartSideWalk,
                            road1.rightStartSideWalk + right1, road2.leftStartSideWalk - right2,
                            -road1.GetDirectionVector(), -road2.GetDirectionVector());
                        curveName2 = "LeftStartSidewalkCurve";
                    }
                    else
                    {
                        road2.ClearSidewalk(true, true, false, false, false, true);
                        road2.rightEndSideWalk = points[n - 1];
                        curve = CreateSidewalkCurves(road1.rightStartSideWalk, road2.rightEndSideWalk,
                            road1.rightStartSideWalk + right1, road2.rightEndSideWalk + right2,
                            -road1.GetDirectionVector(), road2.GetDirectionVector());
                        curveName2 = "RightEndSidewalkCurve";
                    }
                    curve.name = "RightStartSidewalkCurve";
                }
                else
                {
                    road1.leftEndSideWalk = points[0];
                    road1.ClearSidewalk(true, true, false, true, false, true);
                    if (fromStart2)
                    {
                        road2.ClearSidewalk(true, true, true, false, false, false);
                        road2.leftStartSideWalk = points[n - 1];
                        curve = CreateSidewalkCurves(road1.leftEndSideWalk, road2.leftStartSideWalk,
                            road1.leftEndSideWalk - right1, road2.leftStartSideWalk - right2,
                            road1.GetDirectionVector(), road2.GetDirectionVector());
                        curveName2 = "LeftStartSidewalkCurve";
                    }
                    else
                    {
                        road2.ClearSidewalk(true, true, false, false, false, true);
                        road2.rightEndSideWalk = points[n - 1];
                        curve = CreateSidewalkCurves(road1.leftEndSideWalk, road2.rightEndSideWalk,
                            road1.leftEndSideWalk - right1, road2.rightEndSideWalk + right2,
                            road1.GetDirectionVector(), -road2.GetDirectionVector());
                        curveName2 = "RightEndSidewalkCurve";
                    }
                    curve.name = "LeftEndSidewalkCurve";
                }
                curve.transform.parent = road1.gameObject.transform;

                var curveClone = Instantiate(curve);
                curveClone.transform.parent = road2.gameObject.transform;
                curveClone.name = curveName2;
                road1.CreateSidewalk();
                road2.CreateSidewalk();
            }
        }
    }

    public GameObject CreateSidewalkCurves(Vector3 outside1, Vector3 outside2, Vector3 inside1, Vector3 inside2, Vector3 dir1, Vector3 dir2)
    {
        int n = 10;

        var outsideIntersect = GetPointOfIntersection(outside1, outside2, dir1, dir2);
        var outsidePoints = GetNPointsOnBezierCurve(outside1, outsideIntersect, outside2, n);

        var insideIntersect = GetPointOfIntersection(inside1, inside2, dir1, dir2);
        var insidePoints = GetNPointsOnBezierCurve(inside1, insideIntersect, inside2, n);

        GameObject curve = CreateSidewalkCurveMesh(insidePoints, outsidePoints);
        return curve;
    }

    public GameObject CreateSidewalkCurveMesh(List<Vector3> insidePoints, List<Vector3> outsidePoints)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        int n = insidePoints.Count;
        for (int i = 0; i < n; ++i)
        {
            Vector3 pIn = insidePoints[i];
            pIn.y = sidewalkThickness / 2;
            vertices.Add(pIn);
            pIn.y = 0;
            vertices.Add(pIn);

            Vector3 pOut = outsidePoints[i];
            pOut.y = sidewalkThickness / 2;
            vertices.Add(pOut);
            pOut.y = 0;
            vertices.Add(pOut);
        }

        for (int i = 0; i < n - 1; ++i)
        {
            triangles.Add(4 * i);
            triangles.Add(4 * i + 4);
            triangles.Add(4 * i + 6);

            triangles.Add(4 * i);
            triangles.Add(4 * i + 6);
            triangles.Add(4 * i + 2);

            triangles.Add(4 * i + 1);
            triangles.Add(4 * i + 5);
            triangles.Add(4 * i + 4);

            triangles.Add(4 * i + 1);
            triangles.Add(4 * i + 4);
            triangles.Add(4 * i);

            triangles.Add(4 * i + 2);
            triangles.Add(4 * i + 6);
            triangles.Add(4 * i + 7);

            triangles.Add(4 * i + 2);
            triangles.Add(4 * i + 7);
            triangles.Add(4 * i + 3);
        }

        Mesh mesh = new Mesh();
        buildingController.CreateUniqueVertices(vertices.ToArray(), triangles.ToArray(), out Vector3[] newVertices, out int[] newTriangles);
        mesh.vertices = newVertices;
        mesh.triangles = newTriangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.Optimize();

        GameObject curve = new GameObject("SidewalkCurve", typeof(MeshFilter), typeof(MeshRenderer));
        curve.GetComponent<MeshFilter>().mesh = mesh;
        curve.GetComponent<MeshRenderer>().material = pavementMaterial;
        return curve;
    }

    public Vector3 GetPointOfIntersection(Vector3 p1, Vector3 p2, Vector3 dir1, Vector3 dir2)
    {
        Vector3 p1End = p1 + dir1; // another point in line p1->n1
        Vector3 p2End = p2 + dir2; // another point in line p2->n2

        float m1 = (p1End.z - p1.z) / (p1End.x - p1.x); // slope of line p1->n1
        float m2 = (p2End.z - p2.z) / (p2End.x - p2.x); // slope of line p2->n2

        float b1 = p1.z - m1 * p1.x; // y-intercept of line p1->n1
        float b2 = p2.z - m2 * p2.x; // y-intercept of line p2->n2

        float px = (b2 - b1) / (m1 - m2); // collision x
        float pz = m1 * px + b1; // collision y

        return new Vector3(px, 0, pz); // return statement
    }

    // Get curve on intersection divided into n points
    public List<Vector3> GetBezierCurveBetweenRoads(Road r1, Road r2, int n)
    {
        RoadNode node = r1.GetCommonRoadNode(r2);
        if (node == null)
        {
            return null;
        }
        float angle = r1.GetAngleBetweenRoad(r2);
        float offset;
        if (Mathf.Abs(180 - angle) < 1)
        {
            offset = 0.5f;
        }
        else if (angle <= 90)
        {
            offset = 60f / angle;
        }
        else
        {
            offset = (180f - angle) / 150f;
        }

        Vector3 dir1 = r1.GetDirectionVector().normalized;
        if (node == r1.startNode)
        {
            dir1 = -dir1;
        }
        Vector3 right1 = new Vector3(dir1.z, dir1.y, -dir1.x);
        Vector3 p0 = node.Position - dir1 * roadWidth * offset - right1 * carWidthPercentage * roadWidth / 2f;
        Vector3 other0 = (r1.startNode.Position + r1.endNode.Position) / 2 - right1 * carWidthPercentage * roadWidth / 2f;

        Vector3 dir2 = r2.GetDirectionVector().normalized;
        if (node == r2.startNode)
        {
            dir2 = -dir2;
        }
        Vector3 right2 = new Vector3(dir2.z, dir2.y, -dir2.x);
        Vector3 p2 = node.Position - dir2 * roadWidth * offset + right2 * carWidthPercentage * roadWidth / 2f;
        Vector3 other2 = (r2.startNode.Position + r2.endNode.Position) / 2 + right2 * carWidthPercentage * roadWidth / 2f;

        //Vector3 p1 = GetIntersectionOfLines(other0, p0, p2, other2);
        Vector3 p1 = GetPointOfIntersection(p0, p2, dir1, dir2);

        if (LinesAreParallel(other0, p0, p2, other2))
        {
            List<Vector3> points = new List<Vector3>();
            for (float t = 0f; t <= 1f; t += 1f / n)
            {
                var p = Vector3.Lerp(p0, p2, t);
                points.Add(p);
            }
            return points;
        }

        return GetNPointsOnBezierCurve(p0, p1, p2, n);
    }

    public bool LinesAreParallel(Vector3 p0, Vector3 p1, Vector3 q0, Vector3 q1)
    {
        float A1 = p1.z - p0.z;
        float B1 = p0.x - p1.x;
        float A2 = q1.z - q0.z;
        float B2 = q0.x - q1.x;

        float delta = A1 * B2 - A2 * B1;
        bool parallel = Mathf.Abs(delta) < 0.01f;
        return parallel;
    }

    // intersection of lines p0-p1 and q0-q1
    public Vector3 GetIntersectionOfLines(Vector3 p0, Vector3 p1, Vector3 q0, Vector3 q1)
    {
        float A1 = p1.z - p0.z;
        float B1 = p0.x - p1.x;
        float C1 = A1 * p0.x + B1 * p0.z;

        float A2 = q1.z - q0.z;
        float B2 = q0.x - q1.x;
        float C2 = A2 * q0.x + B2 * q0.z;

        float delta = A1 * B2 - A2 * B1;
        if (Mathf.Abs(delta) < 0.01f)
        {
            return (p1 + q0) / 2;
        }

        float x = (B2 * C1 - B1 * C2) / delta;
        float z = (A1 * C2 - A2 * C1) / delta;

        return new Vector3(x, 0.01f, z);
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

        //test
        DrawCurvesOnRoadNode(startNode);
        DrawCurvesOnRoadNode(endNode);
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

        DrawCurvesOnRoadNode(road1.startNode);
        DrawCurvesOnRoadNode(road2.endNode);
        DrawCurvesOnRoadNode(newNode);

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
