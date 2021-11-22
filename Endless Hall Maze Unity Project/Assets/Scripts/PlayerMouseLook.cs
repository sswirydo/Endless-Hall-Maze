using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMouseLook : MonoBehaviour
{
    [SerializeField]
    private float mouseSensitivity = 100f;
    public Transform playerBody; // manipulated object, i.e our player

    private float xRotation = 0f;

    // Start is called before the first frame update
    void Start()
    {
        // mouse is locked and don't fly around in first person view
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        // xRotation is in range [-90;90] -> so player cannot look 360 degrees up & down
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        // Quaternion is responsible for rotation in Unity
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        
             
        playerBody.Rotate(Vector3.up * mouseX); // rotation around Y axis
    }
}





// Source
// Brackeys : FIRST PERSON MOVEMENT in Unity - FPS Controller
// https://www.youtube.com/watch?v=_QajrabyTJc&t=172s