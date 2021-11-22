using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static GameMacros;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller;

    public float speed = 12f; // Common speed of the player
    public float backSpeed = 8f; // Speed of the player if he's going backward
    public float gravity = -9.81f; 
    public float jumpHeight = 10f; // Height of a jump
    public float standingHeight = 1f; // Common height of the player
    public float crouchHeight = 0.6f; // Height of the player when crouched
    public float crouchSpeedCoef = 0.5f; // Reduce coefficient of speed when crouched
    public float sprintSpeedCoef = 1.7f; // Boost coefficient of speed when sprinting

    public Transform groundCheck;
    public float groundDistance = 0.4f; // radius of check
    // LayerMask : specify object we should check for.
    public LayerMask groundMask;

    Vector3 velocity;
    bool isGrounded;

    float speedCoef = 1f;
    bool isCrouched;


    // Update is called once per frame
    void Update()
    {
        // Check if player is on the ground 
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // Crouching mechanism
        Crouch();

        // Sprint mechanism
        Sprint();

        // Vertical Plane
        Gravity();
        Jump();
        controller.Move(velocity * Time.deltaTime);

        // Horizontal Plane
        Displacement();

        Interact();

        RageQuit();
    }

    void RageQuit() // FIXME Temporary.
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("PlayerMovement:RageQuit(): App quit.");
            Application.Quit();
        }
    }


    void Interact()
    {
        RaycastHit hitObject;
        if (Input.GetKeyDown(KeyCode.F))
        {
            Transform cameraTransform = Camera.main.transform;
            if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hitObject, 10f))
            {
                Debug.Log("HIT: " + hitObject.collider.name);
                ObjectInteraction intrObject = hitObject.collider.GetComponent<ObjectInteraction>();
                if (intrObject != null)
                {
                    Debug.Log("COLOR" + intrObject.Color);
                    if (hitObject.collider.name == "Orb")
                        GameManagerObject.GetComponent<GameBehaviour>().CollectOrb(intrObject.Color);
                    else if (hitObject.collider.name == "Rune")
                        GameManagerObject.GetComponent<GameBehaviour>().FillRune(intrObject.Color);
                    else
                        Debug.Log("ObjectInteraction: UFO");
                }
            }
        }
    }



    void Displacement()
    {
        // -- Getting Input -- //
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // This would make our player move in the same direction no matter where the camera looks ; we don't want that.
        // Vector3 move = new Vector3(x, 0f, z); 

        // Direction in which we want to move, based on x/z movement & the direction in which the player is facing.
        Vector3 move = transform.right * x + transform.forward * z;
        if(z < 0) // Check if the player is moving backward
        {
            controller.Move(move.normalized * backSpeed * Time.deltaTime * speedCoef);
        }
        else
        {
            controller.Move(move.normalized * speed * Time.deltaTime * speedCoef);
        }
        // (added normalized in order to keep the same speed even if the movement follows a diagonal)
    }

    void Gravity()
    {
        if (isGrounded && velocity.y <= 0)
            velocity.y = -2f;
        else if (isGrounded == false)
        {
            velocity.y += gravity * Time.deltaTime;
        }
    }

    void Jump()
    {
        if (isGrounded && !isCrouched)
        {
            if (Input.GetButtonDown("Jump"))
            {
                velocity.y += Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
    }

    void Crouch()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl)) // Check if LeftShift is pressed
        {
            transform.localScale -= new Vector3(0.0f, standingHeight - crouchHeight, 0.0f); // Change scale of the player
            speedCoef = crouchSpeedCoef; // Change speed coefficient
            isCrouched = true;// Change the state of the player
        }
        else if (Input.GetKeyUp(KeyCode.LeftControl)) // Check if LeftShift is released
        {
            transform.localScale += new Vector3(0.0f, standingHeight - crouchHeight, 0.0f); // Change scale of the player
            speedCoef = 1f; // Change speed coefficient
            isCrouched = false; // Change the state of the player
        }
    }

    void Sprint()
    {
        if (isGrounded && !isCrouched)
        {
            if (Input.GetKeyDown(KeyCode.LeftShift)) // Check if LeftShift is pressed
            {
                speedCoef = sprintSpeedCoef; // Change speed coefficient
            }
            else if (Input.GetKeyUp(KeyCode.LeftShift)) // Check if LeftShift is released
            {
                speedCoef = 1f; // Change speed coefficient
            }
        }
    }


    public GameObject GameManagerObject;

    public GameObject northTP;
    public GameObject southTP;
    public GameObject westTP;
    public GameObject eastTP;

    public Transform northDest;
    public Transform southDest;
    public Transform westDest;
    public Transform eastDest;

    public GameObject botttomOfTheWorld;

    void OnTriggerEnter(Collider col)
    {
        Debug.Log("COLLISION DETECTED.");
        Debug.Log(col.gameObject.name);
        // 1) transition into black screen TODO
        //      https://www.youtube.com/watch?v=Oadq-IrOazg
        // 2) move player DONE
        // 2BIS) update player's rotation correctly TODO
        // 3) update roopm DONE
        // 4) revert black screen TODO

        if (col.gameObject.name == "tpNorth")
        {
            this.gameObject.transform.position = southDest.transform.position;
            this.gameObject.transform.rotation = southDest.transform.rotation;
            GameManagerObject.GetComponent<GameBehaviour>().MovePlayer(NORTH);
        }

        else if (col.gameObject.name == "tpSouth")
        {
            this.gameObject.transform.position = northDest.transform.position;
            this.gameObject.transform.rotation = northDest.transform.rotation;
            GameManagerObject.GetComponent<GameBehaviour>().MovePlayer(SOUTH);
        }

        else if (col.gameObject.name == "tpWest")
        {
            this.gameObject.transform.position = eastDest.transform.position;
            this.gameObject.transform.rotation = eastDest.transform.rotation;
            GameManagerObject.GetComponent<GameBehaviour>().MovePlayer(WEST);
        }

        else if (col.gameObject.name == "tpEast")
        {
            this.gameObject.transform.position = westDest.transform.position;
            this.gameObject.transform.rotation = westDest.transform.rotation;
            GameManagerObject.GetComponent<GameBehaviour>().MovePlayer(EAST);
        }

        else if (col.gameObject.name == "BottomDetection")
        {
            GameManagerObject.GetComponent<GameBehaviour>().YouLoose();
        }
    }
}



// Source
// FIRST PERSON MOVEMENT in Unity - FPS Controller
// https://www.youtube.com/watch?v=_QajrabyTJc&t=172s