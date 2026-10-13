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
        public float timeLimit = 30f; // Exactly 30 seconds countdown
        public int winScoreThreshold = 10000; // Requires 10,000 points to win
        
        private float currentTime;
        private int currentScore = 0;
        private bool isGameOver = false;

        public bool isPaused = false;

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
            
            // Auto-find texts if they are not assigned in Inspector
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

        private void Update()
        {
            if (isGameOver) return;

            // Pause toggle with Tab key (New Input System)
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.tabKey.wasPressedThisFrame)
            {
                Debug.Log("Tab Pressed");
                if (isPaused) 
                    ResumeGame();
                else 
                    PauseGame();
            }

            // Countdown timer: only ticks when unpaused
            if (!isPaused)
            {
                currentTime -= Time.deltaTime;
                if (currentTime <= 0)
                {
                    currentTime = 0;
                    EvaluateEndGame();
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

        public int GetScore() => currentScore;
        public float GetTimeRemaining() => currentTime;

        public void AddScore(int amount)
        {
            if (isGameOver) return;

            currentScore += amount;
            UpdateHUD();

            // Win condition: if score >= 10,000, trigger win immediately without waiting for timer
            if (currentScore >= winScoreThreshold)
            {
                TriggerWin();
            }
        }

        public void ModifyTime(float seconds)
        {
            if (isGameOver) return;

            currentTime += seconds;
            if (currentTime <= 0)
            {
                currentTime = 0;
                EvaluateEndGame();
            }
            UpdateHUD();
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
                scoreText.text = "Score: " + currentScore.ToString("N0"); // Formats as 10,000
            }
        }

        public void TriggerWin()
        {
            if (isGameOver) return;
            isGameOver = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayWinSound();
            }
            SceneManager.LoadScene("Win");
        }

        private void EvaluateEndGame()
        {
            if (isGameOver) return;

            if (currentScore >= winScoreThreshold)
            {
                TriggerWin();
            }
            else
            {
                isGameOver = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayLoseSound();
                }
                SceneManager.LoadScene("Game Over");
            }
        }
    }
}
