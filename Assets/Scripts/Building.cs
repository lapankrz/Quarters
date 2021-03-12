using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : MonoBehaviour
{
    PedestrianController pedestrianController;
    VehicleController vehicleController;
    public Plot plot;
    public Vector3 entryPoint;
    public RoofType roofType;
    public float height;
    public int inhabitantCount;

    private readonly float metersPerFloor = 3.5f;
    private readonly float areaPerPerson = 80f;

    public void Init(Plot plot, Vector3 entryPoint, RoofType roofType, float height)
    {
        this.plot = plot;
        this.entryPoint = entryPoint;
        this.entryPoint.y = 0;
        this.roofType = roofType;
        this.inhabitantCount = Mathf.RoundToInt(plot.GetArea() * height / metersPerFloor / areaPerPerson);
    }

    void Start()
    {
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
