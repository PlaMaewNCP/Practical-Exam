using UnityEngine;
using UnityEngine.SceneManagement;

namespace NatchapholAunjai
{
    public class PlayerDeathZoneTrigger : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("DeathZone"))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                SceneManager.LoadScene("Game Over");
            }
        }
    }
}
