using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plot
{
    public float borderWidth = 0.5f;
    public Vector3[] corners; // left-roadside, right-roadside, right-inside, left-inside
    public List<GameObject> borders;
    public Road adjacentRoad;
    public bool isCorner;
    public Building building;

    public Plot(Vector3[] corners, Road road, bool isCorner = false)
    {
        this.corners = corners;
        this.adjacentRoad = road;
        this.isCorner = isCorner;
        CreateBorders();
    }

    // Create plot borders visible on the map
    public void CreateBorders()
    {
        this.borders = new List<GameObject>();
        for (int i = 0; i < corners.Length; ++i)
        {
            Vector3 start = corners[i];
            Vector3 end = i == corners.Length - 1 ? corners[0] : corners[i + 1];
            GameObject border = GameObject.CreatePrimitive(PrimitiveType.Quad);
            border.SetActive(false);
            var offset = end - start;
            var scale = new Vector3(borderWidth, offset.magnitude + borderWidth, 1);
            var position = start + (offset / 2.0f);
            position.y += 0.01f;
            border.transform.position = position;
            border.transform.localScale = scale;
            border.transform.rotation = Quaternion.LookRotation(Vector3.down, offset);
            borders.Add(border);
        }
    }

    // Returns smallest distance from point to plot
    public float GetDistanceToPoint(Vector3 point)
    {
        float minDist = Mathf.Infinity;
        for (int i = 0; i < corners.Length; ++i)
        {
            // Return minimum distance between line segment vw and point p
            Vector3 p1 = corners[i];
            Vector3 p2 = i == corners.Length - 1 ? corners[0] : corners[i + 1];
            float l2 = (p1 - p2).magnitude * (p1 - p2).magnitude; // i.e. |w-v|^2 -  avoid a sqrt
            if (l2 == 0.0) return (point - p1).magnitude;   // v == w case
                                                    // Consider the line extending the segment, parameterized as v + t (w - v).
                                                    // We find projection of point p onto the line. 
                                                    // It falls where t = [(p-v) . (w-v)] / |w-v|^2
                                                    // We clamp t from [0,1] to handle points outside the segment vw.
            float t = Mathf.Max(0, Mathf.Min(1, Vector3.Dot(point - p1, p2 - p1) / l2));
            Vector3 projection = p1 + t * (p2 - p1);  // Projection falls on the segment
            float dist = (point - projection).magnitude;
            if (dist < minDist)
            {
                minDist = dist;
            }
        }
        return minDist;
    }

    public Vector3 GetCenterPoint()
    {
        Vector3 center = new Vector3();
        foreach (var corner in corners)
        {
            center += corner;
        }
        center /= corners.Length;
        return center;
    }

    public float GetArea()
    {
        float sum = 0;
        for (int i = 0; i < corners.Length; ++i)
        {
            int j = i == corners.Length - 1 ? 0 : i + 1;
            Vector3 c1 = corners[i];
            Vector3 c2 = corners[j];
            sum += c1.x * c2.z - c1.z * c2.x;
        }
        return Mathf.Abs(sum) / 2;
    }
}
