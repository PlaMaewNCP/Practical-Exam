using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace NatchapholAunjai
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public Text timerText;
        public Text scoreText;
        
        [Header("Game Settings")]
        public float timeLimit = 60f; // 60 seconds to complete
        public int scoreToWin = 3;    // Collect 3 items to win
        
        private float currentTime;
        private int currentScore;
        private bool isGameOver = false;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            currentTime = timeLimit;
            currentScore = 0;
            
            // Auto-find texts if they are not assigned
            if (timerText == null)
            {
                GameObject tGo = GameObject.Find("TimerText");
                if (tGo != null) timerText = tGo.GetComponent<Text>();
            }
            if (scoreText == null)
            {
                GameObject sGo = GameObject.Find("ScoreText");
                if (sGo != null) scoreText = sGo.GetComponent<Text>();
            }
            
            UpdateHUD();
        }

        public bool isPaused = false;

        private void Update()
        {
            if (isGameOver) return;

            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.tabKey.wasPressedThisFrame)
            {
                Debug.Log("Tab Pressed");
                if (isPaused) 
                    ResumeGame();
                else 
                    PauseGame();
            }

            if (!isPaused)
            {
                currentTime -= Time.deltaTime;
                if (currentTime <= 0)
                {
                    currentTime = 0;
                    LoseGame();
                }
                
                UpdateHUD();
            }
        }

        public void PauseGame()
        {
            Time.timeScale = 0f;
            isPaused = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene("Pause", LoadSceneMode.Additive);
        }

        public void ResumeGame()
        {
            Time.timeScale = 1f;
            isPaused = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            SceneManager.UnloadSceneAsync("Pause");
        }


        public void AddScore(int amount)
        {
            if (isGameOver) return;

            currentScore += amount;
            UpdateHUD();

            if (currentScore >= scoreToWin)
            {
                WinGame();
            }
        }

        private void UpdateHUD()
        {
            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(currentTime / 60F);
                int seconds = Mathf.FloorToInt(currentTime - minutes * 60);
                timerText.text = string.Format("Time: {0:00}:{1:00}", minutes, seconds);
            }

            if (scoreText != null)
            {
                scoreText.text = "Score: " + currentScore.ToString();
            }
        }

        private void WinGame()
        {
            isGameOver = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene("Win");
        }

        private void LoseGame()
        {
            isGameOver = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene("Game Over");
        }
    }
}
