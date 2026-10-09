using UnityEngine;
using UnityEngine.SceneManagement;

namespace NatchapholAunjai
{
    public class SceneManagement : MonoBehaviour
    {
        // Static variable to store the name of the last played stage
        // This is used for the "Try Again" button on the Game Over screen
        private static string lastPlayedStage = "Stage 1"; 

        // 1. Main Menu Screen Logic
        public void StartGame()
        {
            SceneManager.LoadScene("Stage Selection");
        }

        public void LoadOptions()
        {
            SceneManager.LoadScene("Options", LoadSceneMode.Additive);
        }

        public void CloseOptions()
        {
            SceneManager.UnloadSceneAsync("Options");
        }

        public void LoadCredits()
        {
            SceneManager.LoadScene("Credits");
        }

        public void ExitGame()
        {
            // Quits the application. In the Unity editor, it stops play mode.
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // 2. Options Screen Logic
        // Uses the same ReturnToMainMenu method as Credits, Win, and Game Over

        // 3. Credits Screen Logic
        // Uses the same ReturnToMainMenu method as Options, Win, and Game Over

        // 4. Stage Selection Screen Logic
        public void LoadStage1()
        {
            lastPlayedStage = "Stage 1";
            SceneManager.LoadScene("Stage 1");
        }

        public void LoadStage2()
        {
            lastPlayedStage = "Stage 2";
            SceneManager.LoadScene("Stage 2");
        }

        // 5. Gameplay Screens (Stage 1 & Stage 2) Logic
        public void WinGame()
        {
            SceneManager.LoadScene("Win");
        }

        public void LoseGame()
        {
            SceneManager.LoadScene("Game Over");
        }

        public void ReturnToStageSelection()
        {
            SceneManager.LoadScene("Stage Selection");
        }

        // 6. Win Screen Logic
        // Uses ReturnToMainMenu

        // 7. Game Over Screen Logic
        public void TryAgain()
        {
            if (!string.IsNullOrEmpty(lastPlayedStage))
            {
                SceneManager.LoadScene(lastPlayedStage);
            }
            else
            {
                SceneManager.LoadScene("Stage Selection");
            }
        }

        // Common Logic used by multiple screens
        public void ReturnToMainMenu()
        {
            SceneManager.LoadScene("Main Menu");
        }
    }
}
