using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BulldozeController : MonoBehaviour
{
    RoadController roadController;
    BuildingController buildingController;
    private bool editorEnabled;
    readonly int layerMask = ~(1 << 8); // NOT Ground
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
                        GameObject gameObject = hitInfo.collider.transform.root.gameObject;
                        string name = gameObject.name;
                        if (name == "Road")
                        {
                            roadController.DeleteRoad(gameObject);
                        }
                        else if (name == "Building")
                        {
                            buildingController.DeleteBuilding(gameObject);
                        }
                        else if (gameObject.layer == LayerMask.NameToLayer("Props"))
                        {
                            Destroy(gameObject);
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
