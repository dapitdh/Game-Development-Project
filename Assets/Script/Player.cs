using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;         // Kecepatan gerak
    public float jumpForce = 5f;         // Kekuatan lompatan
    public float gravityMultiplier = 2f; // Untuk bikin jatuh lebih cepat

    [Header("Ground Check")]
    public Transform groundCheck;        // Posisi cek tanah (empty object di bawah player)
    public float groundDistance = 0.2f;  // Radius cek tanah
    public LayerMask groundMask;         // Layer tanah

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // Cek apakah player menyentuh tanah
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Biar tetap nempel di tanah
        }

        // Input movement
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * moveSpeed * Time.deltaTime);

        //Animasi Berjalan
        if (x != 0 || z != 0)
        {
            GetComponent<Animation>().Play("Walk");
        }
        else
        {
            GetComponent<Animation>().Play("Idle");
        }
    
        // Lompat
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * Physics.gravity.y);
        }

        // Terapkan gravitasi
        velocity.y += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
