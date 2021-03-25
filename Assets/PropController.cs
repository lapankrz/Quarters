using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PropController : MonoBehaviour
{
    GameObject currentProp;
    public GameObject tree;
    public GameObject planter;
    float currentScale;

    private bool editorEnabled;
    int layerMask = 1 << 8; // Ground
    void Start()
    {
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
                if (currentProp == null)
                {
                    CreateTree();
                }
                currentProp.transform.position = hitInfo.point;

                if (Input.GetMouseButtonDown(0))
                {
                    if (!EventSystem.current.IsPointerOverGameObject())
                    {
                        CreateTree();
                        currentProp.transform.position = hitInfo.point;
                    }
                }
            }
        }
    }

    void CreateTree()
    {
        currentProp = getTreeObject(); 
    }

    public GameObject getTreeObject()
    {
        var obj = Instantiate(tree);
        obj.transform.localScale = Vector3.one * Random.Range(1.15f, 1.6f);
        Vector3 dir = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        obj.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        return obj;
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
}
