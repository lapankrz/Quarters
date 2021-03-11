using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform cameraTransform;

    public float movementSpeed;
    public float movementTime;
    public float rotationAmount;
    public Vector3 zoomAmount;

    Vector3 newPosition;
    Quaternion newRotation;
    Vector3 newZoom;

    Vector3 dragStartPosition;
    Vector3 dragCurrentPosition;

    Vector3 rotateStartPosition;
    Vector3 rotateCurrentPosition;

    void Start()
    {
        newPosition = transform.position;
        newRotation = transform.rotation;
        newZoom = cameraTransform.localPosition;
    }

    void Update()
    {
        HandleMouseInput();
        HandleMovementInput();
    }

    void HandleMouseInput()
    {
        if (Input.mouseScrollDelta.y != 0)
        {
            newZoom += Input.mouseScrollDelta.y * zoomAmount;
            if (newZoom.y <= 0)
            {
                newZoom -= Input.mouseScrollDelta.y * zoomAmount;
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            rotateStartPosition = Input.mousePosition;
        }
        if (Input.GetMouseButton(1))
        {
            rotateCurrentPosition = Input.mousePosition;
            Vector3 diff = rotateStartPosition - rotateCurrentPosition;
            rotateStartPosition = rotateCurrentPosition;
            newRotation *= Quaternion.Euler(Vector3.up * (-diff.x / 5f));
        }
    }

    void HandleMovementInput()
    {
        float adjustedMovementSpeed = movementSpeed;
        float cameraHeight = cameraTransform.position.y;
        if (cameraHeight > 80)
        {
             adjustedMovementSpeed *= cameraHeight / 80;
        }

        // Panning
        if (Input.GetKey(KeyCode.W))
        {
            newPosition += (transform.forward * adjustedMovementSpeed);
        }
        if (Input.GetKey(KeyCode.S))
        {
            newPosition += (transform.forward * -adjustedMovementSpeed);
        }
        if (Input.GetKey(KeyCode.D))
        {
            newPosition += (transform.right * adjustedMovementSpeed);
        }
        if (Input.GetKey(KeyCode.A))
        {
            newPosition += (transform.right * -adjustedMovementSpeed);
        }

        // Rotating
        if (Input.GetKey(KeyCode.Q))
        {
            newRotation *= Quaternion.Euler(Vector3.up * rotationAmount);
        }
        if (Input.GetKey(KeyCode.E))
        {
            newRotation *= Quaternion.Euler(Vector3.up * -rotationAmount);
        }

        // Zooming
        if (Input.GetKey(KeyCode.R))
        {
            if ((newZoom + zoomAmount).y > 0)
            {
                newZoom += zoomAmount;
            }
        }
        if (Input.GetKey(KeyCode.T))
        {
            newZoom -= zoomAmount;
        }

        transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * movementTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.deltaTime * movementTime);

        cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, newZoom, Time.deltaTime * movementTime);
        //cameraTransform.LookAt(transform.position, Vector3.up);
    }
}
