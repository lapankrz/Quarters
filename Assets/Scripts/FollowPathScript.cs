using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPathScript : MonoBehaviour
{
    public Building start, end;
    private List<Vector3> path;
    private float movementSpeed;
    private float lastDist = Mathf.Infinity;

    public void Init(Building start, Building end, List<Vector3> path, float movementSpeed)
    {
        this.start = start;
        this.end = end;
        this.path = path;
        this.movementSpeed = movementSpeed;
    }

    void Start()
    {
        if (path == null)
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (path.Count == 0)
        {
            Destroy(gameObject);
        }
        else
        {
            Vector3 movementDir = (path[0] - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(movementDir);
            float dist = (path[0] - transform.position).magnitude;
            transform.position += movementDir * movementSpeed * Time.deltaTime;
            if (dist < 0.1f || lastDist < dist)
            {
                transform.position = path[0];
                path.RemoveAt(0);
                lastDist = Mathf.Infinity;
            }
            else
            {
                lastDist = dist;
            }
        }
    }
}
