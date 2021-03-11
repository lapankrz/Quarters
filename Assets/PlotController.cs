using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlotController : MonoBehaviour
{
    public int buildingWidth = 15;
    public int buildingDepth = 20;
    BuildingController buildingController;
    RoadController roadController;
    public List<Plot> plots = new List<Plot>();

    void Start()
    {
        buildingController = FindObjectOfType<BuildingController>();
        roadController = FindObjectOfType<RoadController>();
    }

    void Update()
    {
        
    }

    // Creates plots for both sides of the road
    // recursively fixes plots for neighboring roads
    public void CreatePlots(Road road, int roadWidth)
    {
        CreatePlotsOnRoad(road, true, roadWidth);

        //left side - forward
        Road currRoad = road.GetNeighboringRoad(false, true);
        bool flipped = false;
        if (currRoad != null)
            flipped = road.endNode == currRoad.endNode;
        while (currRoad != null && currRoad != road)
        {
            Road lastRoad = currRoad;
            if (flipped)
            {
                CreatePlotsOnRoad(currRoad, false, roadWidth);
                currRoad = currRoad.GetNeighboringRoad(true, true);
                if (currRoad != null)
                    flipped = lastRoad.startNode == currRoad.endNode;
            }
            else
            {
                CreatePlotsOnRoad(currRoad, true, roadWidth);
                currRoad = currRoad.GetNeighboringRoad(false, true);
                if (currRoad != null)
                    flipped = lastRoad.endNode == currRoad.endNode;
            }
        }

        //left side - backwards
        currRoad = road.GetNeighboringRoad(true, false);
        if (currRoad != null)
            flipped = road.startNode == currRoad.startNode;
        while (currRoad != null && currRoad != road)
        {
            Road lastRoad = currRoad;
            if (flipped)
            {
                CreatePlotsOnRoad(currRoad, false, roadWidth);
                currRoad = currRoad.GetNeighboringRoad(false, false);
                if (currRoad != null)
                    flipped = lastRoad.endNode == currRoad.startNode;
            }
            else
            {
                CreatePlotsOnRoad(currRoad, true, roadWidth);
                currRoad = currRoad.GetNeighboringRoad(true, false);
                if (currRoad != null)
                    flipped = lastRoad.startNode == currRoad.startNode;
            }
        }

        CreatePlotsOnRoad(road, false, roadWidth);

        //right side - forward
        currRoad = road.GetNeighboringRoad(false, false);
        flipped = false;
        if (currRoad != null)
            flipped = road.endNode == currRoad.endNode;
        while (currRoad != null && currRoad != road)
        {
            Road lastRoad = currRoad;
            if (flipped)
            {
                CreatePlotsOnRoad(currRoad, true, roadWidth);
                currRoad = currRoad.GetNeighboringRoad(true, false);
                if (currRoad != null)
                    flipped = lastRoad.startNode == currRoad.endNode;
            }
            else
            {
                CreatePlotsOnRoad(currRoad, false, roadWidth);
                currRoad = currRoad.GetNeighboringRoad(false, false);
                if (currRoad != null)
                    flipped = lastRoad.endNode == currRoad.endNode;
            }
        }

        //right side - backwards
        currRoad = road.GetNeighboringRoad(true, true);
        if (currRoad != null)
            flipped = road.startNode == currRoad.startNode;
        while (currRoad != null && currRoad != road)
        {
            Road lastRoad = currRoad;
            if (flipped)
            {
                CreatePlotsOnRoad(currRoad, true, roadWidth);
                currRoad = currRoad.GetNeighboringRoad(false, true);
                if (currRoad != null)
                    flipped = lastRoad.endNode == currRoad.startNode;
            }
            else
            {
                CreatePlotsOnRoad(currRoad, false, roadWidth);
                currRoad = currRoad.GetNeighboringRoad(true, true);
                if (currRoad != null)
                    flipped = lastRoad.startNode == currRoad.startNode;
            }
        }
    }

    // Creates plots on road on one side
    public void CreatePlotsOnRoad(Road road, bool onLeft, int roadWidth)
    {
        (bool prevCorner, bool nextCorner) = CalculateCorners(road, onLeft, roadWidth, out Vector3[] outerCorners, out Vector3[] innerCorners);
        Vector3 start = outerCorners[0];
        Vector3 end = outerCorners[1];
        if (onLeft)
        {
            List<Plot> leftPlots = new List<Plot>();
            if (prevCorner)
            {
                CalculateCornerBuildingBounds(road, true, true, start, out Vector3 leftCorner, out Vector3 rightCorner, out bool wideAngle);
                if (wideAngle)
                {
                    Vector3 offset = Vector3.Cross(road.GetDirectionVector().normalized, Vector3.up).normalized;
                    Vector3[] corners =
                    {
                        outerCorners[0],
                        rightCorner,
                        rightCorner + offset * buildingDepth,
                        innerCorners[0]
                    };
                    Plot plot = new Plot(corners, road, false);
                    leftPlots.Add(plot);
                }              
                start = rightCorner;
            }
            Plot cornerPlot = null;
            if (nextCorner)
            {
                CalculateCornerBuildingBounds(road, false, true, end, out Vector3 leftCorner, out Vector3 rightCorner, out bool wideAngle);
                if (wideAngle)
                {
                    Vector3 offset = Vector3.Cross(road.GetDirectionVector().normalized, Vector3.up).normalized;
                    Vector3[] corners =
                    {
                        leftCorner,
                        outerCorners[1],
                        innerCorners[1],
                        leftCorner + offset * buildingDepth
                    };
                    cornerPlot = new Plot(corners, road, false);
                }
                else
                {
                    Vector3[] corners =
                    {
                    leftCorner,
                    outerCorners[1],
                    rightCorner,
                    innerCorners[1]
                    };
                    cornerPlot = new Plot(corners, road, true);
                }
                end = leftCorner;
            }
            List<Plot> middlePlots = CreateMiddleLeftPlots(road, start, end);
            leftPlots.AddRange(middlePlots);
            if (cornerPlot != null)
            {
                leftPlots.Add(cornerPlot);
            }
            foreach (var plot in road.leftPlots)
            {
                foreach (var border in plot.borders)
                {
                    Destroy(border);
                }
                plots.Remove(plot);
            }
            road.leftPlots = leftPlots;
            plots.AddRange(leftPlots);
        }
        else
        {
            List<Plot> rightPlots = new List<Plot>();
            Plot cornerPlot = null;
            if (prevCorner)
            {
                CalculateCornerBuildingBounds(road, true, false, start, out Vector3 leftCorner, out Vector3 rightCorner, out bool wideAngle);
                Plot plot;
                if (wideAngle)
                {
                    Vector3 offset = -Vector3.Cross(road.GetDirectionVector().normalized, Vector3.up).normalized;
                    Vector3[] corners =
                    {
                        leftCorner,
                        outerCorners[0],
                        innerCorners[0],
                        leftCorner + offset * buildingDepth
                    };
                    plot = new Plot(corners, road, false);
                }
                else
                {
                    Vector3[] corners =
                    {
                        leftCorner,
                        outerCorners[0],
                        rightCorner,
                        innerCorners[0]
                    };
                    plot = new Plot(corners, road, true);
                }
                rightPlots.Add(plot);
                start = leftCorner;
            }
            if (nextCorner)
            {
                CalculateCornerBuildingBounds(road, false, false, end, out Vector3 leftCorner, out Vector3 rightCorner, out bool wideAngle);
                if (wideAngle)
                {
                    Vector3 offset = -Vector3.Cross(road.GetDirectionVector().normalized, Vector3.up).normalized;
                    Vector3[] corners =
                    {
                        outerCorners[1],
                        rightCorner,
                        rightCorner + offset * buildingDepth,
                        innerCorners[1]
                    };
                    cornerPlot = new Plot(corners, road, false);
                }
                end = rightCorner;
            }
            List<Plot> middlePlots = CreateMiddleRightPlots(road, start, end);
            rightPlots.AddRange(middlePlots);
            if (cornerPlot != null)
            {
                rightPlots.Add(cornerPlot);
            }
            foreach (var plot in road.rightPlots)
            {
                foreach (var border in plot.borders)
                {
                    Destroy(border);
                }
                plots.Remove(plot);
            }
            road.rightPlots = rightPlots;
            plots.AddRange(rightPlots);
        }
    }

    /// <summary>
    /// Calculates plot vertices for corner buildings
    /// </summary>
    /// <param name="road">Road that contains the plot</param>
    /// <param name="onStartNode">Whether the corner building is on the start node or the end node</param>
    /// <param name="onLeft">Whether the plot is on the left or the right of the road</param>
    /// <param name="corner">Position of the corner between this road and neighboring road</param>
    /// <param name="leftCorner">Returned position of left plot corner</param>
    /// <param name="rightCorner">Returned position of right plot corner</param>
    /// <param name="wideAngle">Returned information if the corner is over 135 degrees</param>
    public void CalculateCornerBuildingBounds(Road road, bool onStartNode, bool onLeft,
        Vector3 corner, out Vector3 leftCorner, out Vector3 rightCorner, out bool wideAngle)
    {
        Road prevRoad = null, nextRoad = null;
        Vector3 prevVector, nextVector;
        if (onStartNode)
        {
            if (onLeft)
            {
                nextRoad = road;
                nextVector = road.GetDirectionVector();
                prevRoad = road.GetNeighboringRoad(true, false);
                prevVector = prevRoad.GetDirectionVector();
                if (prevRoad.endNode == road.startNode)
                {
                    prevVector = -prevVector;
                }
            }
            else
            {
                nextRoad = road.GetNeighboringRoad(true, true);
                prevRoad = road;
                prevVector = road.GetDirectionVector();
                nextVector = nextRoad.GetDirectionVector();
                if (nextRoad.endNode == road.startNode)
                {
                    nextVector = -nextVector;
                }
            }
        }
        else
        {
            if (onLeft)
            {
                nextRoad = road.GetNeighboringRoad(false, true);
                prevRoad = road;
                prevVector = -road.GetDirectionVector();
                nextVector = nextRoad.GetDirectionVector();
                if (nextRoad.endNode == road.endNode)
                {
                    nextVector = -nextVector;
                }
            }
            else
            {
                nextRoad = road;
                nextVector = -road.GetDirectionVector();
                prevRoad = road.GetNeighboringRoad(false, false);
                prevVector = prevRoad.GetDirectionVector();
                if (prevRoad.endNode == road.endNode)
                {
                    prevVector = -prevVector;
                }
            }
        }

        float angle = Vector3.Angle(prevVector, nextVector);
        if (angle > 135)
        {
            wideAngle = true;
            leftCorner = corner + prevVector.normalized * buildingWidth;
            rightCorner = corner + nextVector.normalized * buildingWidth;
        }
        else
        {
            wideAngle = false;
            angle = (angle / 2) * Mathf.PI / 180;
            float offset = buildingDepth / Mathf.Tan(angle);
            leftCorner = corner + prevVector.normalized * offset;
            rightCorner = corner + nextVector.normalized * offset;
        }

        // change boundaries to middle of road if no inbetween houses will fit
        float prevDistance = prevRoad.GetLength();
        if (prevDistance <= buildingWidth)
        {
            leftCorner = (prevRoad.startNode.Position + prevRoad.endNode.Position) / 2;
        }
        float nextDistance = nextRoad.GetLength();
        if (nextDistance <= buildingWidth)
        {
            rightCorner = (nextRoad.startNode.Position + nextRoad.endNode.Position) / 2;
        }
    }

    // Creates non-corner plots on the left
    List<Plot> CreateMiddleLeftPlots(Road road, Vector3 start, Vector3 end)
    {
        List<Plot> plots = new List<Plot>();
        var angle = Vector3.Angle(road.GetDirectionVector(), end - start);
        //if (Mathf.Approximately(angle, 0f))
        {
            Vector3 dir = end - start;
            float distance = dir.magnitude;
            Vector3 leftOffset = Vector3.Cross(dir, Vector3.up).normalized;
            int minBuildings = 3;
            if (road.GetNeighboringRoad(true, false) != null)
            {
                minBuildings--;
            }
            if (road.GetNeighboringRoad(false, true) != null)
            {
                minBuildings--;
            }
            int buildingCount = Mathf.Max((int)(distance / buildingWidth), minBuildings);
            Vector3 alongOffset = dir / buildingCount;
            for (int i = 0; i < buildingCount; ++i)
            {
                Vector3[] coords =
                {
                start + i * alongOffset,
                start + (i+1) * alongOffset,
                start + buildingDepth * leftOffset + (i+1) * alongOffset,
                start + buildingDepth * leftOffset + i * alongOffset
            };
                Plot plot = new Plot(coords, road, false);
                plots.Add(plot);
            }
        }
        return plots;
    }

    // Creates non-corner plots on the right
    List<Plot> CreateMiddleRightPlots(Road road, Vector3 start, Vector3 end)
    {
        List<Plot> plots = new List<Plot>();
        var angle = Vector3.Angle(road.GetDirectionVector(), end - start);
        //if (Mathf.Approximately(angle, 0f))
        {
            Vector3 dir = end - start;
            float distance = dir.magnitude;
            Vector3 rightOffset = -Vector3.Cross(dir, Vector3.up).normalized;
            int minBuildings = 3;
            bool neighbor1 = road.GetNeighboringRoad(true, true) != null;
            bool neighbor2 = road.GetNeighboringRoad(false, false) != null;
            if (neighbor1)
            {
                minBuildings--;
            }
            if (neighbor2)
            {
                minBuildings--;
            }
            int buildingCount = Mathf.Max((int)(distance / buildingWidth), minBuildings);
            Vector3 alongOffset = dir / buildingCount;
            for (int i = 0; i < buildingCount; ++i)
            {
                Vector3[] coords =
                {
                start + (i+1) * alongOffset,
                start + i * alongOffset,
                start + buildingDepth * rightOffset + i * alongOffset,
                start + buildingDepth * rightOffset + (i+1) * alongOffset
            };
                Plot plot = new Plot(coords, road, false);
                plots.Add(plot);
            }
        }
        return plots;
    }    

    /// <summary>
    /// Calculates corners of all plots of road
    /// </summary>
    /// <param name="road">Road that contains the plots</param>
    /// <param name="onLeft">Whether the plots are on the left</param>
    /// <param name="roadWidth">Width of the road</param>
    /// <param name="outerCorners">Corners adjacent to road</param>
    /// <param name="innerCorners">Corners adjacent to the courtyard (inside the block)</param>
    /// <returns>Tuple about whether there is a corner building by the start node and by the end node</returns>
    public (bool, bool) CalculateCorners(Road road, bool onLeft, int roadWidth, out Vector3[] outerCorners, out Vector3[] innerCorners)
    {
        Road prevRoad, nextRoad;
        if (onLeft)
        {
            prevRoad = road.GetNeighboringRoad(true, false);
            nextRoad = road.GetNeighboringRoad(false, true);
        }
        else
        {
            prevRoad = road.GetNeighboringRoad(true, true);
            nextRoad = road.GetNeighboringRoad(false, false);
        }

        outerCorners = new Vector3[2];
        innerCorners = new Vector3[2];

        float radius = roadWidth / 2;
        Vector3 currVector = road.GetDirectionVector();

        Vector3 nonCornerOffset = Vector3.Cross(currVector.normalized, Vector3.up); // right
        if (!onLeft)
        {
            nonCornerOffset = -nonCornerOffset;
        }

        if (prevRoad != null) // prevRoad vertices
        {
            Vector3 prevVector = prevRoad.GetDirectionVector();
            if (prevRoad.endNode == road.startNode)
            {
                prevVector = -prevVector;
            }

            float prevAngle = (Vector3.Angle(currVector, prevVector) / 2) * Mathf.PI / 180;
            if (!Mathf.Approximately(Mathf.Abs(prevAngle), Mathf.PI / 2))
            {
                float sine1 = Mathf.Sin(prevAngle);
                float offsetAmount1 = radius / sine1;
                Vector3 offset = ((currVector.normalized + prevVector.normalized) / 2).normalized;
                Vector3 vertex1 = road.startNode.Position + offset * offsetAmount1;
                outerCorners[0] = vertex1;
                float offsetAmount2 = buildingDepth / sine1;
                var innerCorner1 = vertex1 + offset * offsetAmount2;
                innerCorners[0] = innerCorner1;
            }
            else
            {
                outerCorners[0] = road.startNode.Position + nonCornerOffset * radius;
                innerCorners[0] = outerCorners[0] + nonCornerOffset * buildingDepth;
                prevRoad = null;
            }
        }
        else
        {
            outerCorners[0] = road.startNode.Position + nonCornerOffset * radius;
            innerCorners[0] = outerCorners[0] + nonCornerOffset * buildingDepth;
        }

        if (nextRoad != null)
        {
            // nextRoad vertices
            Vector3 nextVector = nextRoad.GetDirectionVector();
            if (nextRoad.endNode == road.endNode)
            {
                nextVector = -nextVector;
            }

            float nextAngle = (Vector3.Angle(-currVector, nextVector) / 2) * Mathf.PI / 180;
            if (!Mathf.Approximately(Mathf.Abs(nextAngle), Mathf.PI / 2))
            {
                float sine2 = Mathf.Sin(nextAngle);
                float offsetAmount1 = radius / sine2;
                Vector3 offset = ((-currVector.normalized + nextVector.normalized) / 2).normalized;
                Vector3 vertex2 = road.endNode.Position + offset * offsetAmount1;
                outerCorners[1] = vertex2;
                float offsetAmount2 = buildingDepth / sine2;
                var innerCorner2 = vertex2 + offset * offsetAmount2;
                innerCorners[1] = innerCorner2;
            }
            else
            {
                outerCorners[1] = road.endNode.Position + nonCornerOffset * radius;
                innerCorners[1] = outerCorners[1] + nonCornerOffset * buildingDepth;
                nextRoad = null;
            }
        }
        else
        {
            outerCorners[1] = road.endNode.Position + nonCornerOffset * radius;
            innerCorners[1] = outerCorners[1] + nonCornerOffset * buildingDepth;
        }

        return (prevRoad != null, nextRoad != null);
    }

    public void SpawnOnAllPlots()
    {
        buildingController.DeleteAllBuildings();
        foreach (Road road in roadController.Roads)
        {
            foreach (Plot plot in road.leftPlots)
            {
                GameObject building = buildingController.CreateSingleBuilding(plot);
            }
            foreach (Plot plot in road.rightPlots)
            {
                GameObject building = buildingController.CreateSingleBuilding(plot);
            }
        }
    }

    public void SpawnBuilding(Plot plot)
    {
        //fit-to-plot test
        //GameObject building = buildingController.CreateBuildingFromBlueprint(plot.corners, plot.isCorner);

        GameObject building = buildingController.CreateSingleBuilding(plot);
    }

    public void EnablePlotOverlay()
    {
        foreach (var plot in plots)
        {
            foreach (var border in plot.borders)
            {
                border.SetActive(true);
            }
        }
    }

    public void DisablePlotOverlay()
    {
        foreach (var plot in plots)
        {
            foreach (var border in plot.borders)
            {
                border.SetActive(false);
            }
        }
    }
}
