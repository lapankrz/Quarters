using MathNet.Numerics.LinearAlgebra;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RoofType { Hanseatic, Normal, Random }

public class BuildingController : MonoBehaviour
{
    public List<Material> wallMaterials;
    public List<Material> roofMaterials;
    public int averageBuildingHeight = 20;
    public float buildingHeightOffset = 3;
    public int averageRoofHeight = 5;
    public float roofHeightOffset = 1;
    public RoofType roofStyle = RoofType.Normal;
    RoadController RoadController { get; set; }
    PlotController PlotController { get; set; }

    // window constants
    public GameObject windowObject;
    public float leftRightWindowMargin = 2;
    public float leftRightWindowMarginOffset = 0.5f;
    public float upDownWindowMargin = 1;
    public float upDownWindowMarginOffset = 0.2f;
    public const float windowWidth = 1.5f;
    public const float windowHeight = 2.0f;

    public GameObject doorObject;
    public const float doorHeight = 3.6f;
    public const float doorWidth = 2.2f;

    void Start()
    {
        RoadController = FindObjectOfType<RoadController>();
        PlotController = FindObjectOfType<PlotController>();
    }

    void Update()
    {

    }

    internal void DeleteBuilding(GameObject gameObject)
    {
        for (int i = 0; i < gameObject.transform.childCount; ++i)
        {
            var child = gameObject.transform.GetChild(i).gameObject;
            Destroy(child);
        }
        Destroy(gameObject);
        Plot plot = PlotController.plots.Find(p => p.building == gameObject);
        if (plot != null)
        {
            plot.building = null;
        }
    }

    public GameObject CreateSingleBuilding(Plot plot) // 4 corners of building
{
        Vector3[] corners = plot.corners;
        bool isCorner = plot.isCorner;
        float buildingHeight = averageBuildingHeight + Random.Range(-buildingHeightOffset, buildingHeightOffset+1);
        float roofHeight = averageRoofHeight + Random.Range(-roofHeightOffset, roofHeightOffset + 1);
        Material wallMaterial = wallMaterials[Random.Range(0, wallMaterials.Count)];
        Material roofMaterial = roofMaterials[Random.Range(0, roofMaterials.Count)];

        RoofType roofType = roofStyle;
        if (roofStyle == RoofType.Random)
        {
            int rand = Random.Range(0, 2);
            if (rand == 0)
            {
                roofType = RoofType.Hanseatic;
            }
            else
            {
                roofType = RoofType.Normal;
            }
        }

        Vector3[] vertices = CreateVertices(corners, buildingHeight, roofHeight, roofType, isCorner);

        GameObject buildingObject = new GameObject("Building");
        buildingObject.tag = "Building";

        GameObject walls = CreateWalls(vertices, isCorner, wallMaterial, roofType);
        walls.transform.parent = buildingObject.transform;

        GameObject roof = CreateRoof(vertices, isCorner, roofMaterial, roofType);
        roof.transform.parent = buildingObject.transform;

        Vector3[] wallCorners1 = { vertices[0], vertices[1], vertices[2], vertices[3] };
        Vector3[] wallCorners2 = { vertices[4], vertices[5], vertices[6], vertices[7] };
        if (isCorner)
        {
            wallCorners2[0] = vertices[2];
            wallCorners2[1] = vertices[3];
            wallCorners2[2] = vertices[4];
            wallCorners2[3] = vertices[5];
        }
        Vector3 entryPoint;
        GameObject windows2 = CreateWindowsOnWall(wallCorners2, false, out entryPoint);
        GameObject windows1 = CreateWindowsOnWall(wallCorners1, true, out entryPoint);

        windows1.transform.parent = buildingObject.transform;
        windows2.transform.parent = buildingObject.transform;

        Building building = buildingObject.AddComponent<Building>();
        building.Init(plot, entryPoint, roofType, buildingHeight);
        plot.building = building;

        return buildingObject;
    }

    public GameObject CreateWindowsOnWall(Vector3[] wallCorners, bool addDoor, out Vector3 entryPoint) // lower left, upper left, lower right, upper right
    {
        GameObject windows = new GameObject("Windows");

        float leftRightOffset = leftRightWindowMargin + Random.Range(-1.0f, 1.0f) * leftRightWindowMarginOffset;
        float upDownOffset = upDownWindowMargin + Random.Range(-1.0f, 1.0f) * upDownWindowMarginOffset;
        float bottomOffset = doorHeight - windowHeight;

        float width = (wallCorners[0] - wallCorners[2]).magnitude;
        float height = (wallCorners[0] - wallCorners[1]).magnitude;

        int xWindowCount = (int)((width - leftRightOffset) / (windowWidth + leftRightOffset));
        int yWindowCount = (int)((height - bottomOffset) / (windowHeight + upDownOffset));

        leftRightOffset = (width - xWindowCount * windowWidth) / (xWindowCount + 1);
        upDownOffset = (height - yWindowCount * windowHeight) / (yWindowCount + 1);

        Vector3 xOffset = (wallCorners[2] - wallCorners[0]).normalized;
        Vector3 yOffset = (wallCorners[1] - wallCorners[0]).normalized;
        Vector3 right = Vector3.Cross(xOffset, Vector3.up).normalized;
        Vector3 startPos = wallCorners[0] + xOffset * (leftRightOffset + windowWidth / 2) + yOffset * (upDownOffset + windowHeight / 2) - right * 0.01f;

        float scaleFactor = Random.Range(1f, 1.15f);
        entryPoint = new Vector3();
        for (int i = 0; i < yWindowCount; ++i)
        {
            for (int j = 0; j < xWindowCount; ++j)
            {
                if (addDoor && i == 0 && j == xWindowCount / 2)
                {
                    Vector3 pos = wallCorners[0] + xOffset * ((j + 1) * leftRightOffset + j * windowWidth + windowWidth / 2) + yOffset * (doorHeight / 2);
                    Quaternion rotation = Quaternion.LookRotation(right);
                    Instantiate(doorObject, pos, rotation, windows.transform);
                    entryPoint = pos;
                }
                else
                {
                    Vector3 pos = startPos + i * yOffset * (upDownOffset + windowHeight) + j * xOffset * (leftRightOffset + windowWidth);
                    Quaternion rotation = Quaternion.LookRotation(right);
                    var window = Instantiate(windowObject, pos, rotation, windows.transform);
                    var scale = window.transform.localScale;
                    scale.x *= scaleFactor;
                    scale.y *= scaleFactor;
                    window.transform.localScale = scale;
                }
            }
        }

        return windows;
    }

    // Creates wall and roof vertices based on plot corners
    public Vector3[] CreateVertices(Vector3[] corners, float buildingHeight, float roofHeight, RoofType roofType, bool isCorner)
    {
        Vector3[] vertices = new Vector3[10];

        for (int i = 0; i < corners.Length; ++i)
        {
            Vector3 vec = corners[i];
            vec.y = 0;
            vertices[2 * i] = vec;
            vec.y += buildingHeight;
            vertices[2 * i + 1] = vec;
        }

        if (roofType == RoofType.Hanseatic)
        {
            vertices[8] = (vertices[1] + vertices[3]) / 2;
            vertices[8].y += roofHeight;

            vertices[9] = (vertices[5] + vertices[7]) / 2;
            vertices[9].y += roofHeight;
        }
        else
        {
            if (isCorner)
            {
                Vector3[] newVertices = new Vector3[11];
                for (int i = 0; i < 8; ++i)
                {
                    newVertices[i] = vertices[i];
                }
                newVertices[8] = (vertices[1] + vertices[7]) / 2;
                newVertices[8].y += roofHeight;

                newVertices[9] = (vertices[3] + vertices[7]) / 2;
                newVertices[9].y += roofHeight;

                newVertices[10] = (vertices[5] + vertices[7]) / 2;
                newVertices[10].y += roofHeight;

                vertices = newVertices;
            }
            else
            {
                vertices[8] = (vertices[1] + vertices[7]) / 2;
                vertices[8].y += roofHeight;

                vertices[9] = (vertices[3] + vertices[5]) / 2;
                vertices[9].y += roofHeight;
            }
        }

        return vertices;
    }

    public GameObject CreateWalls(Vector3[] vertices, bool isCorner, Material material, RoofType roofType)
    {
        int[] triangles = CreateWallsTriangles(vertices, isCorner, roofType);

        Mesh mesh = new Mesh();
        CreateUniqueVertices(vertices, triangles, out Vector3[] newVertices, out int[] newTriangles);
        mesh.vertices = newVertices;
        mesh.triangles = newTriangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.Optimize();

        GameObject walls = new GameObject("Walls", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
        walls.GetComponent<MeshFilter>().mesh = mesh;
        walls.GetComponent<MeshRenderer>().material = material;
        walls.GetComponent<MeshCollider>().sharedMesh = mesh;

        return walls;
    }

    public GameObject CreateRoof(Vector3[] vertices, bool isCorner, Material material, RoofType roofType)
    {
        int[] triangles = CreateRoofTriangles(vertices, isCorner, roofType);

        Mesh mesh = new Mesh();
        CreateUniqueVertices(vertices, triangles, out Vector3[] newVertices, out int[] newTriangles);
        mesh.vertices = newVertices;
        mesh.triangles = newTriangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.Optimize();

        GameObject roof = new GameObject("Roof", typeof(MeshFilter), typeof(MeshRenderer));
        roof.GetComponent<MeshFilter>().mesh = mesh;
        roof.GetComponent<MeshRenderer>().material = material;
        //roof.GetComponent<MeshCollider>().sharedMesh = mesh;

        return roof;
    }

    int[] CreateWallsTriangles(Vector3[] vertices, bool isCorner, RoofType roofType)
    {
        int[] triangles = new int[36];

        for (int i = 0; i < 30; ++i)
        {
            triangles[i] = wallsVertexLUT[i];
        }

        for (int i = 0; i < 6; ++i)
        {
            if (roofType == RoofType.Hanseatic)
            {
                triangles[30 + i] = hansaWallsLUT[i];
            }
            else
            {
                if (isCorner)
                {
                    triangles[30 + i] = nonHansaCornerWallsLUT[i];
                }
                else
                {
                    triangles[30 + i] = nonHansaWallsLUT[i];
                }
            }
        }

        return triangles;
    }

    int[] CreateRoofTriangles(Vector3[] vertices, bool isCorner, RoofType roofType)
    {
        int[] triangles = new int[12];

        if (roofType == RoofType.Hanseatic)
        {
            for (int i = 0; i < 12; ++i)
            {
                triangles[i] = hansaRoofLUT[i];
            }
        }
        else
        {
            if (isCorner)
            {
                int[] newTriangles = new int[18];
                for (int i = 0; i < 18; ++i)
                {
                    newTriangles[i] = nonHansaCornerVertexLUT[i];
                }
                triangles = newTriangles;
            }
            else
            {
                for (int i = 0; i < 12; ++i)
                {
                    triangles[i] = nonHansaRoofLUT[i];
                }
            }
        }

        return triangles;
    }

    // WIP: fits prefab to plot corners
    public GameObject CreateBuildingFromBlueprint(Vector3[] corners, bool isCorner)
    {
        float[] x = { corners[0].x, corners[3].x, corners[2].x, corners[1].x };
        float[] y = { corners[0].z, corners[3].z, corners[2].z, corners[1].z };
        var A = Matrix<double>.Build.DenseOfArray(new double[,] {
            { 0, 0, 1, 0, 0, 0, 0, 0},
            { 0, 0, 0, 0, 0, 1, 0, 0 },
            { 0, 1, 1, 0, 0, 0, 0, -x[1] },
            { 0, 0, 0, 0, 1, 1, 0, -y[1] },
            { 1, 1, 1, 0, 0, 0, -x[2], -x[2] },
            { 0, 0, 0, 1, 1, 1, -y[2], -y[2] },
            { 1, 0, 1, 0, 0, 0, -x[3], 0 },
            { 0, 0, 0, 1, 0, 1, -y[3], 0 }

        });
        var b = Vector<double>.Build.Dense(new double[] { x[0], y[0], x[1], y[1], x[2], y[2], x[3], y[3] });
        var X = A.Solve(b);
        var T = Matrix<double>.Build.DenseOfArray(new double[,] {
            { X[0], X[1], X[2] },
            { X[3], X[4], X[5] },
            { X[6], X[7], 1 }
        });

        //test
        Material material = wallMaterials[Random.Range(0, wallMaterials.Count)];

        Mesh mesh = new Mesh();

        Vector3[] vertices = { new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(0, 1, 1), new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(0, 1, 1) };
        Vector3[] newVertices = new Vector3[6];
        for (int i = 0; i < 6; ++i)
        {
            var vect = vertices[i];
            var v = Vector<double>.Build.Dense(new double[] { vect.x, vect.y, 1 });
            var res = T * v;
            res /= res[2];
            newVertices[i] = new Vector3((float)res[0], 0.1f, (float)res[1]);
        }
        int[] triangles = { 0, 2, 1, 3, 5, 4 };

        mesh.vertices = newVertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.Optimize();

        GameObject building = new GameObject("Building", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
        building.tag = "Building";
        building.GetComponent<MeshFilter>().mesh = mesh;
        building.GetComponent<MeshRenderer>().material = material;
        building.GetComponent<MeshCollider>().sharedMesh = mesh;

        return building;
    }
    
    // duplicates mesh vertices so that every triangle has unique ones - necessary for flat shading
    public void CreateUniqueVertices(Vector3[] vertices, int[] triangles, out Vector3[] newVertices, out int[] newTriangles)
    {
        List<Vector3> newVerts = new List<Vector3>();
        List<int> newTris = new List<int>();

        int ind = 0;
        for (int i = 0; i < triangles.Length / 3; ++i)
        {
            int tri1 = triangles[3 * i];
            int tri2 = triangles[3 * i + 1];
            int tri3 = triangles[3 * i + 2];

            newVerts.Add(vertices[tri1]);
            newVerts.Add(vertices[tri2]);
            newVerts.Add(vertices[tri3]);

            newTris.Add(ind);
            newTris.Add(ind + 1);
            newTris.Add(ind + 2);

            ind += 3;
        }
        newTriangles = newTris.ToArray();
        newVertices = newVerts.ToArray();
    }

    internal void DeleteAllBuildings()
    {
        GameObject[] buildings = GameObject.FindGameObjectsWithTag("Building");
        foreach (GameObject building in buildings)
        {
            Destroy(building);
        }
    }

    int[] wallsVertexLUT = { 1, 3, 0, 0, 3, 2, 2, 3, 5, 2, 5, 4, 4, 5, 6, 6, 5, 7, 6, 7, 1, 6, 1, 0, 1, 7, 5, 1, 5, 3 };
    int[] hansaWallsLUT = { 1, 8, 3, 5, 9, 7 };
    int[] nonHansaWallsLUT = { 3, 9, 5, 7, 8, 1 };
    int[] nonHansaCornerWallsLUT = { 5, 10, 7, 7, 8, 1 };

    int[] hansaRoofLUT = { 3, 8, 9, 3, 9, 5, 7, 9, 8, 7, 8, 1 };
    int[] nonHansaRoofLUT = { 1, 8, 9, 1, 9, 3, 5, 9, 7, 9, 8, 7 };
    int[] nonHansaCornerVertexLUT = { 8, 9, 1, 1, 9, 3, 3, 9, 10, 3, 10, 5, 7, 10, 9, 7, 9, 8 };
}
