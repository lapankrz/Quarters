using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : MonoBehaviour
{
    BuildingController buildingController;
    PedestrianController pedestrianController;
    VehicleController vehicleController;
    public Plot plot;
    public Vector3 entryPoint;
    public RoofType roofType;
    public int height;
    public int roofHeight;
    public int inhabitantCount;
    public ZoneType zoneType;
    public Material wallMaterial;
    public Material roofMaterial;
    public bool isCorner;

    private readonly float metersPerFloor = 3.5f;
    private readonly float areaPerPerson = 80f;

    public void Init(Plot plot, Vector3 entryPoint, RoofType roofType, int height, int roofHeight,
        ZoneType zoneType, Material wallMaterial, Material roofMaterial, bool isCorner)
    {
        this.plot = plot;
        this.entryPoint = entryPoint;
        this.entryPoint.y = 0;
        this.roofType = roofType;
        this.height = height;
        this.roofHeight = roofHeight;
        this.zoneType = zoneType;
        this.wallMaterial = wallMaterial;
        this.roofMaterial = roofMaterial;
        this.isCorner = isCorner;
        this.inhabitantCount = Mathf.RoundToInt(plot.GetArea() * height / metersPerFloor / areaPerPerson);
    }

    void Start()
    {
        buildingController = FindObjectOfType<BuildingController>();
        pedestrianController = FindObjectOfType<PedestrianController>();
        vehicleController = FindObjectOfType<VehicleController>();
    }

    void Update()
    {
        float chance = Random.Range(0f, 1f);
        if (chance < inhabitantCount / 400000f)
        {
            //pedestrianController.SpawnPedestrian(this);
            vehicleController.SpawnVehicle(this);
        }
    }
}
