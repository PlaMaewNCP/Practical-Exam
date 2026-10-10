using UnityEngine;

namespace NatchapholAunjai
{
    public class Item : MonoBehaviour
    {
        public float rotationSpeed = 90f;
        public float bobSpeed = 2f;
        public float bobHeight = 0.5f;

        private Vector3 startPos;

        private void Start()
        {
            startPos = transform.position;
        }

        private void Update()
        {
            // Spin
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
            
            // Bob up and down
            float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }
}
