using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPathScript : MonoBehaviour
{
    public Building start, end;
    private List<Vector3> path;
    private float movementSpeed;
    private float timer;
    private Vector3 prevPos;

    public void Init(Building start, Building end, List<Vector3> path, float movementSpeed)
    {
        this.start = start;
        this.end = end;
        this.path = path;
        this.movementSpeed = movementSpeed;
        this.timer = 0;
        this.prevPos = start.entryPoint;
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
            Vector3 movementDir = (path[0] - prevPos).normalized;
            if (movementDir != new Vector3())
            {
                transform.rotation = Quaternion.LookRotation(movementDir);
            }
            float dist = (path[0] - prevPos).magnitude;
            timer += Time.deltaTime * movementSpeed;
            if (transform.position != path[0])
            {
                transform.position = Vector3.Lerp(prevPos, path[0], timer / dist);
            }
            else
            {
                timer = 0;
                prevPos = path[0];
                path.RemoveAt(0);
            }
        }
    }
}
