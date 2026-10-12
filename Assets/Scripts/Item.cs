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

    public class Item : MonoBehaviour
    {
        [Header("Item Configuration")]
        [Tooltip("Type 1: +3s | Type 2: -5s | Type 3: +500 pts | Type 4: -500 pts")]
        public ItemType itemType = ItemType.AddScore;

        [Header("Visual Effects")]
        public float rotationSpeed = 90f;
        public float bobSpeed = 2f;
        public float bobHeight = 0.25f;

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
        }

        public void Collect()
        {
            if (collected) return;
            collected = true;

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

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                Collect();
            }
        }
    }
}
