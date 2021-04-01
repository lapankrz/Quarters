using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PropController : MonoBehaviour
{
    GameObject currentPrefab;
    GameObject currentProp;
    public GameObject tree;
    public GameObject bigTree;
    public GameObject conifer;
    public GameObject planter;

    public List<GameObject> props;

    private bool editorEnabled;
    int layerMask;
    int propLayer;

    void Start()
    {
        editorEnabled = false;
        currentPrefab = props[0];
        layerMask = ~LayerMask.GetMask("Props");
        propLayer = LayerMask.NameToLayer("Props");
    }

    void Update()
    {
        if (editorEnabled)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, layerMask))
            {
                if (currentProp == null)
                {
                    CreateProp();
                }
                currentProp.transform.position = hitInfo.point;

                if (Input.GetMouseButtonDown(0))
                {
                    if (!EventSystem.current.IsPointerOverGameObject())
                    {
                        CreateProp();
                        currentProp.transform.position = hitInfo.point;
                    }
                }
            }
        }
    }

    void CreateProp()
    {
        currentProp = GetPropObject(currentPrefab);
    }

    public GameObject GetPropObject(GameObject prefab)
    {
        if (prefab != null)
        {
            var obj = Instantiate(prefab);
            if (prefab != planter)
                obj.transform.localScale = Vector3.one * Random.Range(1.15f, 1.6f);
            Vector3 dir = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            obj.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            obj.layer = propLayer;
            return obj;
        }
        else
        {
            return null;
        }
    }

    public GameObject getTreeObject()
    {
        return GetPropObject(tree);
    }

    public GameObject getBigTreeObject()
    {
        return GetPropObject(bigTree);
    }

    public GameObject getConiferObject()
    {
        return GetPropObject(conifer);
    }

    public GameObject getPlanterObject()
    {
        var obj = Instantiate(planter);
        return obj;
    }

    public void EnableEditor()
    {
        editorEnabled = true;
    }

    public void DisableEditor()
    {
        editorEnabled = false;
        if (currentProp != null)
        {
            Destroy(currentProp);
        }
    }

    public void SetPrefab(int index)
    {
        if (index < props.Count)
        {
            currentPrefab = props[index];
            Destroy(currentProp);
        }
    }
}
