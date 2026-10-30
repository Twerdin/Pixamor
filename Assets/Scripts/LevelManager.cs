using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pixamor
{
    /// <summary>
    /// LevelManager manages transitions and reloading for levels: "1", "2", "3".
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [Header("Current Level Info")]
        [SerializeField] private int currentLevel = 1;

        public int CurrentLevel => currentLevel;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Loads level by number (1, 2, 3)
        /// </summary>
        public void LoadLevel(int levelNumber)
        {
            currentLevel = levelNumber;
            SceneManager.LoadScene(levelNumber.ToString());
        }

        /// <summary>
        /// Loads the next level. If on level 3, wraps back to level 1.
        /// </summary>
        public void LoadNextLevel()
        {
            int nextLevel = currentLevel + 1;
            if (nextLevel > 3)
            {
                nextLevel = 1;
            }
            LoadLevel(nextLevel);
        }

        /// <summary>
        /// Restarts the currently active level.
        /// </summary>
        public void RestartLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
