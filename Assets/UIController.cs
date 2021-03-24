using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum ChosenEditor { None, BuildRoads, Bulldoze, ZoneBuildings, PlaceProps }

public class UIController : MonoBehaviour
{
    EventSystem eventSystem;
    private GameObject lastSelected = null;
    private Vector3 cursorSpriteSize = new Vector3(0, 0, 0);
    public float maxWindowDrawDistance = 600;
    ChosenEditor chosenEditor;
    RoadController roadController;
    BulldozeController bulldozeController;
    BuildingController buildingController;
    ZoningController zoningController;
    CameraController cameraController;
    PropController propController;

    void Start()
    {
        eventSystem = EventSystem.current;
        chosenEditor = ChosenEditor.None;
        roadController = FindObjectOfType<RoadController>();
        bulldozeController = FindObjectOfType<BulldozeController>();
        buildingController = FindObjectOfType<BuildingController>();
        zoningController = FindObjectOfType<ZoningController>();
        cameraController = FindObjectOfType<CameraController>();
        propController = FindObjectOfType<PropController>();
    }

    void Update()
    {
        if (eventSystem != null) // keep focus on currently pressed button
        {
            if (eventSystem.currentSelectedGameObject != null)
            {
                lastSelected = eventSystem.currentSelectedGameObject;
            }
            else
            {
                eventSystem.SetSelectedGameObject(lastSelected);
            }
        }

        PlaceCursorSprite();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            eventSystem.SetSelectedGameObject(null);
            DisableAllEditors();
        }

        UnloadDistantWindows();
    }

    public void UnloadDistantWindows()
    {
        var buildings = GameObject.FindGameObjectsWithTag("Building");
        var cameraPos = cameraController.cameraTransform.position;
        foreach (var building in buildings)
        {
            float distance = 0;
            for (int i = 0; i < building.transform.childCount; ++i)
            {
                var child = building.transform.GetChild(i).gameObject;
                if (child.name == "Walls")
                {
                    var mesh = child.GetComponent<MeshRenderer>();
                    distance = (cameraPos - mesh.bounds.center).magnitude;
                }
                else if (child.name == "Windows")
                {
                    if (distance < maxWindowDrawDistance)
                    {
                        child.SetActive(true);
                    }
                    else
                    {
                        child.SetActive(false);
                    }
                }
            }
        }
    }

    public void PlaceCursorSprite()
    {
        int layerMask = 1 << 8;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;
        if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, layerMask))
        {
            var sprite = GetSpriteObject();
            Vector3 position = hitInfo.point;
            if (chosenEditor == ChosenEditor.BuildRoads)
            {
                RoadNode node = roadController.FindNearbyRoadEnds(position, roadController.nearbyRoadThreshold);
                if (node != null)
                {
                    position = node.Position;
                }
                else
                {
                    (Road road, Vector3 intersection) = roadController.FindNearbyRoadSegments(position);
                    if (road != null)
                    {
                        position = intersection;
                    }
                }
            }
            sprite.transform.position = new Vector3(position.x, position.y + 0.1f, position.z);
            sprite.transform.localScale = cursorSpriteSize;
        }
    }

    public void OnBuildRoadsClick()
    {
        if (chosenEditor == ChosenEditor.BuildRoads)
        {
            eventSystem.SetSelectedGameObject(null);
            DisableAllEditors();
        }
        else
        {
            if (chosenEditor != ChosenEditor.None)
            {
                DisableAllEditors();
            }
            chosenEditor = ChosenEditor.BuildRoads;
            roadController.EnableEditor();
            cursorSpriteSize = new Vector3(6 * roadController.roadWidth, 6 * roadController.roadWidth, 1);
        }
    }

    public GameObject GetSpriteObject()
    {
        return GameObject.FindGameObjectWithTag("CursorSprite");
    }

    public void OnBulldozeClick()
    {
        if (chosenEditor == ChosenEditor.Bulldoze)
        {
            eventSystem.SetSelectedGameObject(null);
            DisableAllEditors();
        }
        else
        {
            if (chosenEditor != ChosenEditor.None)
            {
                DisableAllEditors();
            }
            chosenEditor = ChosenEditor.Bulldoze;
            bulldozeController.EnableEditor();
        }
    }

    public void OnZoneBuildingsClick()
    {
        if (chosenEditor == ChosenEditor.ZoneBuildings)
        {
            eventSystem.SetSelectedGameObject(null);
            DisableAllEditors();
        }
        else
        {
            if (chosenEditor != ChosenEditor.None)
            {
                DisableAllEditors();
            }
            chosenEditor = ChosenEditor.ZoneBuildings;
            zoningController.EnableEditor();
            cursorSpriteSize = new Vector3(12 * zoningController.zoningRadius, 12 * zoningController.zoningRadius, 1);
        }
    }

    public void OnPropPlacingClick()
    {
        if (chosenEditor == ChosenEditor.PlaceProps)
        {
            eventSystem.SetSelectedGameObject(null);
            DisableAllEditors();
        }
        else
        {
            if (chosenEditor != ChosenEditor.None)
            {
                DisableAllEditors();
            }
            chosenEditor = ChosenEditor.PlaceProps;
            propController.EnableEditor();
        }
    }

    void DisableAllEditors()
    {
        cursorSpriteSize = new Vector3(0, 0, 0);
        chosenEditor = ChosenEditor.None;
        lastSelected = null;

        // disable all editors in controllers
        roadController.DisableEditor();
        bulldozeController.DisableEditor();
        zoningController.DisableEditor();
        propController.DisableEditor();
    }
}
