using UnityEngine;

namespace NatchapholAunjai
{
    public enum ItemType
    {
        AddTime = 1,        // Type 1: Adds 3 seconds to the timer
        SubtractTime = 2,   // Type 2: Subtracts 5 seconds from the timer
        AddScore = 3,       // Type 3: Adds 500 points to the score
        SubtractScore = 4   // Type 4: Subtracts 500 points from the score
    }

    public class Item : MonoBehaviour, IInteractable
    {
        [Header("Item Configuration")]
        [Tooltip("Type 1: +3s | Type 2: -5s | Type 3: +500 pts | Type 4: -500 pts")]
        public ItemType itemType = ItemType.AddScore;

        [Header("Interaction Settings")]
        [Tooltip("If true, requires pressing 'E' via Interactor (Stage 2). If false, collects automatically on contact (Stage 1).")]
        public bool requireInteraction = false;

        [Header("Visual Effects")]
        public float rotationSpeed = 90f;
        public float bobSpeed = 2f;
        public float bobHeight = 0.25f;

        [Header("Audio")]
        public AudioClip collectSound;

        private Vector3 startPos;
        private bool collected = false;

        private void Start()
        {
            startPos = transform.position;
        }

        private void Update()
        {
            // Spin and hover animation
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
            float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);

            // In Stage 1, collect automatically if player touches or enters item volume
            if (!requireInteraction && !collected && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Stage 2")
            {
                Collider[] hits = Physics.OverlapSphere(transform.position, 0.75f, ~0, QueryTriggerInteraction.Collide);
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].CompareTag("Player") || hits[i].GetComponent<CapsulePlayer>() != null)
                    {
                        Collect();
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Interaction collection method triggered by player Interactor (Stage 2).
        /// </summary>
        public void Interact()
        {
            Collect();
        }

        /// <summary>
        /// Trigger contact collection method (Stage 1).
        /// In Stage 2, items must strictly only be collected via 'E' key interaction, so contact collection is skipped.
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (requireInteraction) return;

            // Strict Stage 2 guard: Stage 2 requires interaction via 'E' key, never on trigger touch
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Stage 2") return;

            // Stage 1 collection: triggered when Player enters collider
            if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null || other.GetComponent<CapsulePlayer>() != null)
            {
                Collect();
            }
        }

        public void Collect()
        {
            if (collected) return;
            collected = true;

            if (AudioManager.Instance != null)
            {
                if (collectSound != null)
                {
                    AudioManager.Instance.PlaySFX(collectSound);
                }
                else
                {
                    AudioManager.Instance.PlayItemSound(itemType);
                }
            }

            if (GameManager.Instance != null)
            {
                switch (itemType)
                {
                    case ItemType.AddTime:
                        GameManager.Instance.ModifyTime(3f);
                        break;
                    case ItemType.SubtractTime:
                        GameManager.Instance.ModifyTime(-5f);
                        break;
                    case ItemType.AddScore:
                        GameManager.Instance.AddScore(500);
                        break;
                    case ItemType.SubtractScore:
                        GameManager.Instance.AddScore(-500);
                        break;
                }
            }

            Destroy(gameObject);
        }
    }
}
