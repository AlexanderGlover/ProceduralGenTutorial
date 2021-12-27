using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    public float speed = 7.5f;

    [SerializeField]
    Rigidbody rb;

    [SerializeField]
    Camera cam;

    private void FixedUpdate()
    {
        Vector3 playerInput = new Vector3();
        playerInput.x = Input.GetAxis("Horizontal");
        playerInput.z = Input.GetAxis("Vertical");
        playerInput.y = 0f;

        rb.AddForce(playerInput * speed);

        //calc camera position
        Vector3 playerPos = transform.position;
        cam.transform.position = playerPos + new Vector3(0f, 8.55f, -10.6f);
    }
}