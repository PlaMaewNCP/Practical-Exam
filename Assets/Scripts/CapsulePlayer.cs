using UnityEngine;
using UnityEngine.SceneManagement;

namespace NatchapholAunjai
{
    [RequireComponent(typeof(CharacterController))]
    public class CapsulePlayer : MonoBehaviour
    {
        public float moveSpeed = 5f;
        public float gravity = -9.81f;
        
        private CharacterController controller;
        private Vector3 velocity;

        private void Start()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            // Note: Since you are using the new Input System (InputSystemUIInputModule), 
            // standard Input.GetAxis might not work unless Input Handling is set to "Both".
            // To ensure it works out of the box in an exam setting where both might be enabled, 
            // we use legacy Input. If it fails, the project must have "Both" enabled in PlayerSettings.
            float x = 0f;
            float z = 0f;
            
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            // Basic fallback for new input system if legacy is completely disabled
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.wKey.isPressed || UnityEngine.InputSystem.Keyboard.current.upArrowKey.isPressed) z += 1f;
                if (UnityEngine.InputSystem.Keyboard.current.sKey.isPressed || UnityEngine.InputSystem.Keyboard.current.downArrowKey.isPressed) z -= 1f;
                if (UnityEngine.InputSystem.Keyboard.current.aKey.isPressed || UnityEngine.InputSystem.Keyboard.current.leftArrowKey.isPressed) x -= 1f;
                if (UnityEngine.InputSystem.Keyboard.current.dKey.isPressed || UnityEngine.InputSystem.Keyboard.current.rightArrowKey.isPressed) x += 1f;
            }
#else
            x = Input.GetAxis("Horizontal");
            z = Input.GetAxis("Vertical");
#endif

            Vector3 move = transform.right * x + transform.forward * z;
            controller.Move(move * moveSpeed * Time.deltaTime);

            if (controller.isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }

            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("DeathZone"))
            {
                SceneManager.LoadScene("Game Over");
            }
            else if (other.CompareTag("Item"))
            {
                Destroy(other.gameObject);
            }
        }
    }
}
