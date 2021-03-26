using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

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

    // Road options
    public CanvasGroup roadOptionsPanel;
    public Slider roadWidthSlider;
    public Slider carWidthPercentageSlider;
    public Slider treeDistanceSlider;
    public Toggle treeToggle;
    public Text roadWidthText;
    public Text carPercentageText;
    public Text treeDistanceText;

    // Zoning options
    public CanvasGroup zoningOptionsPanel;
    public Dropdown zoneTypeDropdown;
    public Dropdown roofTypeDropdown;
    public RangeSlider buildingHeightSlider;
    public RangeSlider roofHeightSlider;
    public Text buildingHeightMinText;
    public Text buildingHeightMaxText;
    public Text roofHeightMinText;
    public Text roofHeightMaxText;

    // Building info
    bool finishedUpdatingFields = false;
    Building selectedBuilding;
    public CanvasGroup buildingInfoPanel;
    public Text inhabitantCountTitle;
    public Text inhabitantCountText;
    public Dropdown existingZoneTypeDropdown;
    public Dropdown existingRoofTypeDropdown;
    public Slider existingBuildingHeightSlider;
    public Slider existingRoofHeightSlider;
    public Text existingBuildingHeightText;
    public Text existingRoofHeightText;

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

        //road options
        roadWidthSlider.onValueChanged.AddListener(delegate { OnRoadWidthSliderChange(); });
        carWidthPercentageSlider.onValueChanged.AddListener(delegate { OnCarWidthPercentageSliderChange(); });
        treeDistanceSlider.onValueChanged.AddListener(delegate { OnTreeDistanceSliderChange(); });
        treeToggle.onValueChanged.AddListener(delegate { OnTreeToggleChange(); });

        //zoning options
        zoneTypeDropdown.onValueChanged.AddListener(delegate { OnZoneTypeChanged(); });
        roofTypeDropdown.onValueChanged.AddListener(delegate { OnRoofTypeChanged(); });
        buildingHeightSlider.OnValueChanged.AddListener(delegate { OnBuildingHeightChanged(); });
        roofHeightSlider.OnValueChanged.AddListener(delegate { OnRoofHeightChanged(); });

        //building info
        existingZoneTypeDropdown.onValueChanged.AddListener(delegate { OnExistingZoneTypeChanged(); });
        existingRoofTypeDropdown.onValueChanged.AddListener(delegate { OnExistingRoofTypeChanged(); });
        existingBuildingHeightSlider.onValueChanged.AddListener(delegate { OnExistingBuildingHeightChanged(); });
        existingRoofHeightSlider.onValueChanged.AddListener(delegate { OnExistingRoofHeightChanged(); });
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

        if (Input.GetMouseButtonDown(0))
        {
            if (!EventSystem.current.IsPointerOverGameObject() && chosenEditor == ChosenEditor.None)
            {
                HandleMouseClick();
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

    private void HandleMouseClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;
        if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity))
        {
            GameObject obj = hitInfo.collider.transform.root.gameObject;
            if (obj.CompareTag("Building"))
            {
                ShowBuildingInfo(obj);
            }
            else
            {
                CloseAllPanels();
            }
        }
    }

    private void ShowBuildingInfo(GameObject obj)
    {
        buildingInfoPanel.gameObject.SetActive(true);
        var building = obj.GetComponent<Building>();
        selectedBuilding = building;
        finishedUpdatingFields = false;
        inhabitantCountText.text = building.inhabitantCount.ToString();
        existingZoneTypeDropdown.value = (int)building.zoneType;
        existingRoofTypeDropdown.value = (int)building.roofType;
        existingBuildingHeightSlider.value = building.height;
        existingBuildingHeightText.text = building.height.ToString();
        existingRoofHeightSlider.value = building.roofHeight;
        existingRoofHeightText.text = building.roofHeight.ToString();
        finishedUpdatingFields = true;
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
        CloseAllPanels();
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
            roadOptionsPanel.gameObject.SetActive(true);
        }
    }

    public GameObject GetSpriteObject()
    {
        return GameObject.FindGameObjectWithTag("CursorSprite");
    }

    public void OnBulldozeClick()
    {
        CloseAllPanels();
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
        CloseAllPanels();
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
            zoningOptionsPanel.gameObject.SetActive(true);
        }
    }

    public void OnPropPlacingClick()
    {
        CloseAllPanels();
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

        //hide all option panels
        CloseAllPanels();
    }

    void CloseAllPanels()
    {
        roadOptionsPanel.gameObject.SetActive(false);
        zoningOptionsPanel.gameObject.SetActive(false);
        buildingInfoPanel.gameObject.SetActive(false);
    }

    public void OnRoadWidthSliderChange()
    {
        int value = (int)roadWidthSlider.value;
        roadController.roadWidth = value;
        roadWidthText.text = value.ToString();
    }

    public void OnCarWidthPercentageSliderChange()
    {
        float value = carWidthPercentageSlider.value;
        roadController.carWidthPercentage = value;
        carPercentageText.text = value.ToString("0.00");
    }

    public void OnTreeToggleChange()
    {
        bool value = treeToggle.isOn;
        roadController.trees = value;
    }

    public void OnTreeDistanceSliderChange()
    {
        float value = treeDistanceSlider.value;
        roadController.treeDistance = value;
        treeDistanceText.text = value.ToString();
    }

    public void OnZoneTypeChanged()
    {
        ZoneType value = (ZoneType)zoneTypeDropdown.value;
        buildingController.zoneType = value;
    }

    public void OnRoofTypeChanged()
    {
        RoofType value = (RoofType)roofTypeDropdown.value;
        buildingController.roofStyle = value;
    }

    public void OnBuildingHeightChanged()
    {
        int min = (int)buildingHeightSlider.LowValue;
        int max = (int)buildingHeightSlider.HighValue;
        buildingController.minBuildingHeight = min;
        buildingController.maxBuildingHeight = max;
        buildingHeightMinText.text = min.ToString();
        buildingHeightMaxText.text = max.ToString();
    }

    public void OnRoofHeightChanged()
    {
        int min = (int)roofHeightSlider.LowValue;
        if (min == 0)
            min -= 1;
        int max = (int)roofHeightSlider.HighValue;
        if (max == 0)
            max -= 1;
        buildingController.minRoofHeight = min;
        buildingController.maxRoofHeight = max;
        roofHeightMinText.text = min.ToString();
        roofHeightMaxText.text = max.ToString();
    }

    public void OnExistingZoneTypeChanged()
    {
        if (finishedUpdatingFields)
        {
            var value = existingZoneTypeDropdown.value;
            if (selectedBuilding != null)
                selectedBuilding.zoneType = (ZoneType)value;
            UpdateInhabitantInfo();
        }
    }

    public void OnExistingRoofTypeChanged()
    {
        if (finishedUpdatingFields)
        {
            var value = existingRoofTypeDropdown.value;
            if (selectedBuilding != null)
                selectedBuilding.roofType = (RoofType)value;
            selectedBuilding = buildingController.UpdateBuilding(selectedBuilding);
            UpdateInhabitantInfo();
        }
    }

    public void OnExistingBuildingHeightChanged()
    {
        if (finishedUpdatingFields)
        {
            int value = (int)existingBuildingHeightSlider.value;
            existingBuildingHeightText.text = value.ToString();
            if (selectedBuilding != null)
                selectedBuilding.height = value;
            selectedBuilding = buildingController.UpdateBuilding(selectedBuilding);
            UpdateInhabitantInfo();
        }
    }

    public void OnExistingRoofHeightChanged()
    {
        if (finishedUpdatingFields)
        {
            int value = (int)existingRoofHeightSlider.value;
            existingRoofHeightText.text = value.ToString();
            if (selectedBuilding != null)
                selectedBuilding.roofHeight = value;
            selectedBuilding = buildingController.UpdateBuilding(selectedBuilding);
            UpdateInhabitantInfo();
        }
    }

    void UpdateInhabitantInfo()
    {
        inhabitantCountText.text = selectedBuilding.inhabitantCount.ToString();
    }
}
