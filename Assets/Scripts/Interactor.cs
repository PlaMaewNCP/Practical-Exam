using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace NatchapholAunjai
{
    public class Interactor : MonoBehaviour
    {
        [Header("Detection Settings")]
        [Tooltip("Maximum distance for raycast detection.")]
        public float raycastDistance = 3.0f;

        [Tooltip("Radius of the overlap sphere.")]
        public float sphereRadius = 1.8f;

        [Tooltip("Forward offset for overlap sphere center relative to player.")]
        public float sphereForwardOffset = 1.2f;

        [Tooltip("Height offset from player pivot for raycast/sphere origin.")]
        public float eyeHeightOffset = 1.2f;

        [Tooltip("Tag required on interactable GameObjects.")]
        public string interactableTag = "Interactable";

        [Tooltip("Layers to consider for interaction.")]
        public LayerMask interactableLayers = ~0;

        [Header("Detection Mode")]
        public bool useRaycast = true;
        public bool useOverlapSphere = true;

        [Header("UI Feedback (Optional)")]
        [Tooltip("UI Text element to display interaction prompt, e.g. '[E] Interact'.")]
        public Text promptText;

        public GameObject CurrentInteractable { get; private set; }

        private Camera mainCamera;

        private void Start()
        {
            mainCamera = Camera.main;

            if (promptText == null)
            {
                GameObject pGo = GameObject.Find("InteractPrompt");
                if (pGo != null)
                {
                    promptText = pGo.GetComponent<Text>();
                }
            }

            if (promptText != null)
            {
                promptText.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            DetectInteractable();

            // Input System: Read 'E' key using New Input System
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                TriggerInteraction();
            }

            UpdateUIFeedback();
        }

        /// <summary>
        /// Detects objects tagged 'Interactable' using a short forward Raycast and/or an Overlap Sphere.
        /// </summary>
        public void DetectInteractable()
        {
            CurrentInteractable = null;

            Vector3 origin = transform.position + Vector3.up * eyeHeightOffset;
            Vector3 forwardDirection = transform.forward;

            // Prefer Camera forward if available for third-person crosshair aiming
            if (mainCamera != null)
            {
                forwardDirection = mainCamera.transform.forward;
            }

            // 1. Primary Check: Forward Raycast (Camera forward or Player forward)
            if (useRaycast)
            {
                // Check in forward look direction (camera or player)
                Ray ray = new Ray(origin, forwardDirection);
                if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, interactableLayers, QueryTriggerInteraction.Collide))
                {
                    GameObject candidate = hit.collider.gameObject;
                    if (IsInteractable(candidate))
                    {
                        CurrentInteractable = candidate;
                        return;
                    }
                }

                // Also check player character's direct forward facing direction
                if (mainCamera != null)
                {
                    Ray playerForwardRay = new Ray(origin, transform.forward);
                    if (Physics.Raycast(playerForwardRay, out RaycastHit playerHit, raycastDistance, interactableLayers, QueryTriggerInteraction.Collide))
                    {
                        GameObject candidate = playerHit.collider.gameObject;
                        if (IsInteractable(candidate))
                        {
                            CurrentInteractable = candidate;
                            return;
                        }
                    }
                }
            }

            // 2. Secondary / Proximity Check: Overlap Sphere around player and in front
            if (useOverlapSphere)
            {
                Vector3 sphereCenter = origin + transform.forward * (sphereForwardOffset * 0.5f);
                Collider[] colliders = Physics.OverlapSphere(sphereCenter, sphereRadius, interactableLayers, QueryTriggerInteraction.Collide);

                float closestDist = float.MaxValue;
                GameObject closestCandidate = null;

                foreach (Collider col in colliders)
                {
                    if (IsInteractable(col.gameObject))
                    {
                        float dist = Vector3.Distance(origin, col.transform.position);
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            closestCandidate = col.gameObject;
                        }
                    }
                }

                if (closestCandidate != null)
                {
                    CurrentInteractable = closestCandidate;
                }
            }
        }

        private bool IsInteractable(GameObject go)
        {
            if (go == null) return false;

            // Direct tag check
            if (go.CompareTag(interactableTag)) return true;

            // Parent tag check (in case collider is on child mesh)
            if (go.transform.parent != null && go.transform.parent.CompareTag(interactableTag)) return true;

            return false;
        }

        /// <summary>
        /// When 'E' is pressed, calls the Interact() method on the detected object.
        /// </summary>
        public void TriggerInteraction()
        {
            if (CurrentInteractable == null) return;

            Debug.Log($"[Interactor] 'E' pressed! Interacting with: {CurrentInteractable.name}");

            // 1. Try IInteractable interface on target or parents
            IInteractable interactable = CurrentInteractable.GetComponent<IInteractable>();
            if (interactable == null)
            {
                interactable = CurrentInteractable.GetComponentInParent<IInteractable>();
            }

            if (interactable != null)
            {
                interactable.Interact();
            }
            else
            {
                // 2. Fallback: SendMessage("Interact") to invoke any method named Interact()
                CurrentInteractable.SendMessage("Interact", SendMessageOptions.DontRequireReceiver);
            }

            // Re-detect immediately after interaction
            DetectInteractable();
        }

        private void UpdateUIFeedback()
        {
            if (promptText != null)
            {
                bool hasTarget = CurrentInteractable != null;
                if (promptText.gameObject.activeSelf != hasTarget)
                {
                    promptText.gameObject.SetActive(hasTarget);
                }

                if (hasTarget)
                {
                    promptText.text = "[E] Interact";
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = transform.position + Vector3.up * eyeHeightOffset;
            Vector3 forwardDirection = transform.forward;

            // Raycast Gizmo
            Gizmos.color = CurrentInteractable != null ? Color.green : Color.yellow;
            Gizmos.DrawRay(origin, forwardDirection * raycastDistance);

            // Overlap Sphere Gizmo
            Vector3 sphereCenter = origin + transform.forward * sphereForwardOffset;
            Gizmos.color = CurrentInteractable != null ? new Color(0, 1, 0, 0.35f) : new Color(1, 1, 0, 0.2f);
            Gizmos.DrawWireSphere(sphereCenter, sphereRadius);
        }
    }
}
