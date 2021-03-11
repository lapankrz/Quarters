using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ZoningController : MonoBehaviour
{
    PlotController plotController;
    BuildingController buildingController;
    public int zoningRadius = 16;
    private bool editorEnabled = false;
    int layerMask = 1 << 8; // Ground

    // Start is called before the first frame update
    void Start()
    {
        plotController = FindObjectOfType<PlotController>();
        buildingController = FindObjectOfType<BuildingController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (editorEnabled)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, layerMask))
            {
                if (Input.GetMouseButton(0))
                {
                    if (!EventSystem.current.IsPointerOverGameObject())
                    {
                        Vector3 point = hitInfo.point;
                        List<Plot> plots = plotController.plots;
                        foreach (Plot plot in plots)
                        {
                            float dist = plot.GetDistanceToPoint(point);
                            if (dist < zoningRadius)
                            {
                                if (plot.building == null)
                                {
                                    plotController.SpawnBuilding(plot);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public void EnableEditor()
    {
        plotController.EnablePlotOverlay();
        editorEnabled = true;
    }

    public void DisableEditor()
    {
        plotController.DisablePlotOverlay();
        editorEnabled = false;
    }
}
