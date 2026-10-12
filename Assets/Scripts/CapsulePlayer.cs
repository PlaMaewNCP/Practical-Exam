using UnityEngine;
using UnityEngine.SceneManagement;

namespace NatchapholAunjai
{
    [RequireComponent(typeof(CharacterController))]
    public class CapsulePlayer : MonoBehaviour
    {
        public float moveSpeed = 5f;
        public float jumpHeight = 1.5f;
        public float gravity = -9.81f;
        public float mouseSensitivity = 50f;
        
        private CharacterController controller;
        private Vector3 velocity;

        private void Start()
        {
            controller = GetComponent<CharacterController>();
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (Time.timeScale == 0) return; // Don't move or rotate while paused

            // Movement Input
            float x = 0f;
            float z = 0f;
            
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.wKey.isPressed || UnityEngine.InputSystem.Keyboard.current.upArrowKey.isPressed) z += 1f;
                if (UnityEngine.InputSystem.Keyboard.current.sKey.isPressed || UnityEngine.InputSystem.Keyboard.current.downArrowKey.isPressed) z -= 1f;
                if (UnityEngine.InputSystem.Keyboard.current.aKey.isPressed || UnityEngine.InputSystem.Keyboard.current.leftArrowKey.isPressed) x -= 1f;
                if (UnityEngine.InputSystem.Keyboard.current.dKey.isPressed || UnityEngine.InputSystem.Keyboard.current.rightArrowKey.isPressed) x += 1f;
            }

            // Mouse Look
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                Vector2 mouseDelta = UnityEngine.InputSystem.Mouse.current.delta.ReadValue();
                transform.Rotate(Vector3.up * mouseDelta.x * mouseSensitivity * Time.deltaTime);
            }

            // Strafing Movement relative to player's rotation
            Vector3 move = transform.right * x + transform.forward * z;

            // Robust Grounded Check (combines controller flag with downward raycast)
            float rayDistance = (controller != null ? (controller.height / 2f) : 1f) + 0.25f;
            bool isGrounded = controller.isGrounded || Physics.Raycast(transform.position, Vector3.down, rayDistance);

            if (isGrounded)
            {
                if (velocity.y < 0)
                {
                    velocity.y = -2f;
                }

                // Space key jump using New Input System
                if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    float effectiveHeight = jumpHeight > 0.1f ? jumpHeight : 1.8f;
                    velocity.y = Mathf.Sqrt(effectiveHeight * -2f * Physics.gravity.y);
                    Debug.Log("[CapsulePlayer] Jump triggered! Initial Y velocity: " + velocity.y);
                }
            }

            // Apply global physics gravity
            velocity.y += Physics.gravity.y * Time.deltaTime;

            // Combine horizontal strafing and vertical velocity into a single Move call
            Vector3 totalMovement = (move * moveSpeed) + velocity;
            controller.Move(totalMovement * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("DeathZone"))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                SceneManager.LoadScene("Game Over");
            }
            else if (other.CompareTag("Item"))
            {
                var item = other.GetComponent<Item>();
                if (item != null)
                {
                    item.Collect();
                }
                else
                {
                    Destroy(other.gameObject);
                }
            }
        }
    }
}
