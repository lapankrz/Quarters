using MathNet.Numerics.LinearAlgebra;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RoofType { Normal, Hanseatic, Random }

public enum ZoneType { Residential, Commercial, Industrial, Office, MixedUse}

public class BuildingController : MonoBehaviour
{
    public List<Material> wallMaterials;
    public List<Material> roofMaterials;
    public int minBuildingHeight = 16;
    public int maxBuildingHeight = 22;
    public int minRoofHeight = 4;
    public int maxRoofHeight = 6;
    public RoofType roofStyle = RoofType.Normal;
    public ZoneType zoneType = ZoneType.Residential;
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

    public GameObject testBlueprint;

    void Start()
    {
        RoadController = FindObjectOfType<RoadController>();
        PlotController = FindObjectOfType<PlotController>();
        roofStyle = RoofType.Normal;
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

    public GameObject CreateSingleBuilding(Plot plot)
    {
        bool isCorner = plot.isCorner;
        int buildingHeight = Random.Range(minBuildingHeight, maxBuildingHeight);
        int roofHeight = Random.Range(minRoofHeight, maxRoofHeight);
        Material wallMaterial = wallMaterials[Random.Range(0, wallMaterials.Count)];
        Material roofMaterial = roofMaterials[Random.Range(0, roofMaterials.Count)];
        RoofType roofType = roofStyle;

        return CreateSingleBuilding(plot, buildingHeight, roofHeight, wallMaterial, roofMaterial, isCorner, roofType);
    }

    public GameObject CreateSingleBuilding(Plot plot, int buildingHeight, int roofHeight,
        Material wallMaterial, Material roofMaterial, bool isCorner, RoofType roofType)
    {
        Vector3[] corners = plot.corners;
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
        building.Init(plot, entryPoint, roofType, buildingHeight, roofHeight, zoneType, wallMaterial, roofMaterial, isCorner);
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

    public GameObject CreateBuildingFromBlueprint(Plot plot)
    {
        bool isCorner = plot.isCorner;
        int buildingHeight = Random.Range(minBuildingHeight, maxBuildingHeight);
        int roofHeight = Random.Range(minRoofHeight, maxRoofHeight);
        Material wallMaterial = wallMaterials[Random.Range(0, wallMaterials.Count)];
        Material roofMaterial = roofMaterials[Random.Range(0, roofMaterials.Count)];
        RoofType roofType = roofStyle;

        return CreateBuildingFromBlueprint(plot, buildingHeight, roofHeight, wallMaterial, roofMaterial, isCorner, roofType);
    }

    // WIP: fits prefab to plot corners
    public GameObject CreateBuildingFromBlueprint(Plot plot, int buildingHeight, int roofHeight,
        Material wallMaterial, Material roofMaterial, bool isCorner, RoofType roofType)
    {
        Vector3[] corners = plot.corners;
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

        Mesh mesh = new Mesh();
        Material material = wallMaterials[Random.Range(0, wallMaterials.Count)];

        Mesh blueprintMesh = testBlueprint.GetComponent<MeshFilter>().sharedMesh;
        Vector3[] blueprintVertices = NormalizeBlueprintVertices(blueprintMesh.vertices);
        Vector3[] vertices = new Vector3[blueprintMesh.vertexCount];
        for (int i = 0; i < blueprintMesh.vertexCount; ++i)
        {
            var vect = blueprintVertices[i];
            var v = Vector<double>.Build.Dense(new double[] { vect.x, vect.z, 1 });
            var res = T * v;
            res /= res[2];
            vertices[i] = new Vector3((float)res[0], vect.y * 20f, (float)res[1]);
        }
        mesh.vertices = vertices;
        mesh.triangles = blueprintMesh.triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.Optimize();

        GameObject buildingObject = new GameObject("Building", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
        buildingObject.tag = "Building";
        buildingObject.GetComponent<MeshFilter>().sharedMesh = mesh;
        buildingObject.GetComponent<MeshCollider>().sharedMesh = mesh;

        var materialObject = Instantiate(testBlueprint);
        buildingObject.GetComponent<MeshRenderer>().materials = materialObject.GetComponent<MeshRenderer>().materials;
        Destroy(materialObject);

        var vert = Vector<double>.Build.Dense(new double[] { 0.5, 0, 1 });
        var result = T * vert;
        result /= result[2];
        var entryPoint = new Vector3((float)result[0], 0, (float)result[1]);

        Building building = buildingObject.AddComponent<Building>();
        building.Init(plot, entryPoint, roofType, buildingHeight, roofHeight, zoneType, wallMaterial, roofMaterial, isCorner);
        plot.building = building;

        return buildingObject;
    }

    Vector3[] NormalizeBlueprintVertices(Vector3[] vertices)
    {
        Vector3[] newVertices = new Vector3[vertices.Length];
        float minX, minY, minZ;
        minX = minY = minZ = float.MaxValue;
        float maxX, maxY, maxZ;
        maxX = maxY = maxZ = float.MinValue;
        foreach (var v in vertices)
        {
            if (v.x < minX)
                minX = v.x;
            if (v.x > maxX)
                maxX = v.x;
            if (v.y < minY)
                minY = v.y;
            if (v.y > maxY)
                maxY = v.y;
            if (v.z < minZ)
                minZ = v.z;
            if (v.z > maxX)
                maxZ = v.z;
        }
        for (int i = 0; i < vertices.Length; ++i)
        {
            Vector3 v = vertices[i];
            float x = Mathf.InverseLerp(minX, maxX, v.x);
            float y = Mathf.InverseLerp(minY, maxY, v.y);
            float z = Mathf.InverseLerp(minZ, maxZ, v.z);
            newVertices[i] = new Vector3(x, y, z);
        }
        return newVertices;
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

    public Building UpdateBuilding(Building b)
    {
        var obj = CreateSingleBuilding(b.plot, b.height, b.roofHeight, b.wallMaterial, b.roofMaterial, b.isCorner, b.roofType);
        Destroy(b.gameObject);
        return obj.GetComponent<Building>();
    }

    int[] wallsVertexLUT = { 1, 3, 0, 0, 3, 2, 2, 3, 5, 2, 5, 4, 4, 5, 6, 6, 5, 7, 6, 7, 1, 6, 1, 0, 1, 7, 5, 1, 5, 3 };
    int[] hansaWallsLUT = { 1, 8, 3, 5, 9, 7 };
    int[] nonHansaWallsLUT = { 3, 9, 5, 7, 8, 1 };
    int[] nonHansaCornerWallsLUT = { 5, 10, 7, 7, 8, 1 };

    int[] hansaRoofLUT = { 3, 8, 9, 3, 9, 5, 7, 9, 8, 7, 8, 1 };
    int[] nonHansaRoofLUT = { 1, 8, 9, 1, 9, 3, 5, 9, 7, 9, 8, 7 };
    int[] nonHansaCornerVertexLUT = { 8, 9, 1, 1, 9, 3, 3, 9, 10, 3, 10, 5, 7, 10, 9, 7, 9, 8 };
}
