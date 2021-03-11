using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BulldozeController : MonoBehaviour
{
    RoadController roadController;
    BuildingController buildingController;
    private bool editorEnabled;
    int layerMask = ~(1 << 8); // NOT Ground
    void Start()
    {
        roadController = FindObjectOfType<RoadController>();
        buildingController = FindObjectOfType<BuildingController>();
        editorEnabled = false;
    }

    void Update()
    {
        if (editorEnabled)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, layerMask))
            {
                if (Input.GetMouseButtonDown(0))
                {
                    if (!EventSystem.current.IsPointerOverGameObject())
                    {
                        GameObject gameObject = hitInfo.collider.gameObject;
                        string name = gameObject.name;
                        if (name == "RoadMiddle" || name == "RoadEnd")
                        {
                            gameObject = gameObject.transform.parent.gameObject;
                            roadController.DeleteRoad(gameObject);
                        }
                        else if (name == "Building")
                        {
                            buildingController.DeleteBuilding(gameObject);
                        }
                        else if (name == "Walls" || name == "Roof")
                        {
                            gameObject = gameObject.transform.parent.gameObject;
                            buildingController.DeleteBuilding(gameObject);
                        }
                    }
                }
            }
        }
    }

    public void EnableEditor()
    {
        editorEnabled = true;
    }

    public void DisableEditor()
    {
        editorEnabled = false;
    }
}
