/*using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using TMPro;

public class GridManager : MonoBehaviour
{
    [SerializeField] private int _width, _height, _maxMines;
    [SerializeField] private Tile _tilePrefab;
    [SerializeField] private Tile _mineTile;
    [SerializeField] private Tile[] _numberTiles; // Array of tiles for numbers 1 to 8
    [SerializeField] private Tile _blankTile;
    [SerializeField] private Tile _flagTile;
    [SerializeField] private Tilemap _tilemap; // Reference to the Tilemap component
    [SerializeField] private Transform _cam;

   
    private HashSet<Vector3Int> _revealedPositions = new HashSet<Vector3Int>();


    private List<Vector3Int> _mines = new List<Vector3Int>();
    private int[,] _grid;
    private HashSet<Vector3Int> _flaggedPositions = new HashSet<Vector3Int>();
    private bool firstLeftClick = true;

    private int _flagCount = 0; // Counter for the number of flags placed
    private const int MaxFlags = 10; // Maximum number of flags allowed
    private int _flagsToShow = 10;
    public TextMeshProUGUI _flagsDisplay;
    public GameObject _loss;
    public GameObject _win;

    public TextMeshProUGUI TimerText;
    public float totalTime = 0f;
    private bool gameOverTriggered = false;
    private bool victoryTriggered = false;

    // High score variables
    private int _highScore;
    public TextMeshProUGUI HighScoreText;
    public AudioSource explosionSFX;
    public AudioSource winSFX;
    public AudioSource backgroundMusic;
    private bool musicOn = true;


    void Start()
    {
        GenerateGrid();

        // Load the high score
        _highScore = PlayerPrefs.GetInt("HighScore", int.MaxValue);
        UpdateHighScoreDisplay();
    }

    public void toggleBackgroundMusic()
    {
        if (musicOn)
        {
            backgroundMusic.Pause();
            musicOn = false;
        }
        else
        {
            backgroundMusic.Play();
            musicOn = true;
        }

    }

    void UpdateHighScoreDisplay()
    {
        if (_highScore == int.MaxValue)
        {
            HighScoreText.text = "N/A";
        }
        else
        {
            HighScoreText.text = $"{_highScore}";
        }
    }


    void GenerateGrid()
    {
        _grid = new int[_width, _height];
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                _tilemap.SetTile(new Vector3Int(x, y, 0), _tilePrefab);
                _grid[x, y] = 0;
            }
        }

        _cam.transform.position = new Vector3((float)_width / 2 - 0.5f, (float)_height / 2 - 0.5f, -10);
    }

    void PlaceMines(Vector3Int firstClickPosition)
    {
        int currentMines = 0;
        while (currentMines < _maxMines)
        {
            int x = Random.Range(0, _width);
            int y = Random.Range(0, _height);
            Vector3Int pos = new Vector3Int(x, y, 0);

            if (!_mines.Contains(pos) && pos != firstClickPosition)
            {
                _mines.Add(pos);
                _grid[x, y] = -1; // -1 represents a mine
                currentMines++;
            }
        }

        // Log all mine positions for debugging
        LogMines();
        CalculateNumbers();
    }

    void LogMines()
    {
        Debug.Log("Mines are placed at:");
        foreach (Vector3Int mine in _mines)
        {
            Debug.Log($"Mine at: {mine.x}, {mine.y}");
        }
    }

    void CalculateNumbers()
    {
        int[] dx = { -1, -1, -1, 0, 0, 1, 1, 1 };
        int[] dy = { -1, 0, 1, -1, 1, -1, 0, 1 };

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                if (_grid[x, y] == -1) continue; // Skip mines

                int mineCount = 0;
                for (int i = 0; i < dx.Length; i++)
                {
                    int nx = x + dx[i];
                    int ny = y + dy[i];

                    if (nx >= 0 && nx < _width && ny >= 0 && ny < _height)
                    {
                        if (_grid[nx, ny] == -1)
                        {
                            mineCount++;
                        }
                    }
                }
                _grid[x, y] = mineCount;

                // Debug log the number of adjacent mines for each cell
                Debug.Log($"Cell ({x}, {y}) has {mineCount} adjacent mines.");
            }
        }
    }

    void Update()
    {
        HandleMouseInput();

       
        if (!gameOverTriggered && !victoryTriggered)
        {
            // Calculate the elapsed time since the game started
            totalTime = totalTime + Time.deltaTime;

            // Update the timer display
            UpdateTimerDisplay();
        }

       
    }

    void UpdateTimerDisplay()
    {
        int seconds = Mathf.FloorToInt(totalTime);
        TimerText.text = $"{seconds}";
    }

    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int mouseCellPos = _tilemap.WorldToCell(mouseWorldPos);

            if (mouseCellPos.x >= 0 && mouseCellPos.x < _width && mouseCellPos.y >= 0 && mouseCellPos.y < _height && gameOverTriggered == false)
            {
                if (Input.GetMouseButtonDown(0)) // Left click
                {
                    HandleLeftClick(mouseCellPos);
                }
                else if (Input.GetMouseButtonDown(1)) // Right click
                {
                    HandleRightClick(mouseCellPos);
                }
            }
        }
    }

    void HandleLeftClick(Vector3Int position)
    {
        if (firstLeftClick)
        {
            // Generate mines but be careful not to put a mine at the location of the first click...
            Debug.Log($"Generating {_maxMines} mines, except at {position.x}, {position.y}");
            PlaceMines(position);
            CalculateNumbers(); // Ensure numbers are calculated after placing mines
            firstLeftClick = false;
        }

        if (_flaggedPositions.Contains(position))
        {
            Debug.Log($"Cannot change tile at {position.x}, {position.y} because it is flagged.");
            return;
        }

        // Get the value from the grid for the clicked position
        int cellValue = _grid[position.x, position.y];
        if (cellValue == -1)
        {
            // Handle mine click
            _tilemap.SetTile(position, _mineTile);
            Debug.Log($"Mine clicked at {position.x}, {position.y}");
            GameOver();
        }
        else if (cellValue > 0 && cellValue <= _numberTiles.Length)
        {
            // Handle number click
            _tilemap.SetTile(position, _numberTiles[cellValue - 1]);
            Debug.Log($"Number {cellValue} clicked at {position.x}, {position.y}");
        }
        else if (cellValue == 0)
        {
            // Handle blank tile (zero) click
            RevealZeros(position);
        }

        // Add the position to revealed positions
        _revealedPositions.Add(position);


    }

    void HandleRightClick(Vector3Int position)
    {

        if (_revealedPositions.Contains(position))
        {
            Debug.Log($"Cannot place a flag at {position.x}, {position.y} because it is already revealed.");
            return;
        }


        if (_flaggedPositions.Contains(position))
        {
            // If the position is already flagged, remove the flag
            _tilemap.SetTile(position, _tilePrefab);
            _flaggedPositions.Remove(position);
            _flagCount--;
            _flagsToShow = MaxFlags - _flagCount;
            _flagsDisplay.text = $"{_flagsToShow}";
            Debug.Log($"Flag removed from tile {position.x}, {position.y}. Total flags: {_flagCount}");
        }
        else
        {
            if (_flagCount < MaxFlags)
            {
                // Otherwise, place a flag
                _tilemap.SetTile(position, _flagTile);
                _flaggedPositions.Add(position);
                _flagCount++;
                _flagsToShow = MaxFlags - _flagCount;
                _flagsDisplay.text = $"{_flagsToShow}";
                Debug.Log($"Flag placed on tile {position.x}, {position.y}. Total flags: {_flagCount}");
                CheckWinCondition();
            }
            else
            {
                Debug.Log("Maximum number of flags reached!");
            }
        }
    }

    void RevealZeros(Vector3Int position)
    {
        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();

        queue.Enqueue(position);
        visited.Add(position);

        while (queue.Count > 0)
        {
            Vector3Int current = queue.Dequeue();

            // Reveal the current tile as a blank tile
            _tilemap.SetTile(current, _blankTile);
            _revealedPositions.Add(current); // Mark this position as revealed

            int[] dx = { -1, -1, -1, 0, 0, 1, 1, 1 };
            int[] dy = { -1, 0, 1, -1, 1, -1, 0, 1 };

            for (int i = 0; i < 8; i++)
            {
                Vector3Int neighbor = new Vector3Int(current.x + dx[i], current.y + dy[i], 0);

                if (neighbor.x >= 0 && neighbor.x < _width && neighbor.y >= 0 && neighbor.y < _height && !visited.Contains(neighbor))
                {
                    int neighborValue = _grid[neighbor.x, neighbor.y];
                    if (neighborValue == 0)
                    {
                        queue.Enqueue(neighbor);
                    }
                    else if (neighborValue > 0 && neighborValue <= _numberTiles.Length)
                    {
                        _tilemap.SetTile(neighbor, _numberTiles[neighborValue - 1]);
                        _revealedPositions.Add(neighbor); // Mark this position as revealed
                    }
                    visited.Add(neighbor);
                }
            }
        }
    }

    void CheckWinCondition()
    {
        // If the number of flagged positions is equal to the number of mines, and all flagged positions are mines
        if (_flaggedPositions.Count == _mines.Count && _flaggedPositions.SetEquals(_mines))
        {
            victoryTriggered = true;
            backgroundMusic.Stop();
            winSFX.Play();
            Debug.Log("Congratulations! You won the game!");
            _win.SetActive(true);
            // Add more win logic here, such as showing a win message or transitioning to a win scene.

            // Check and update high score
            int currentScore = Mathf.FloorToInt(totalTime);
            if (currentScore < _highScore)
            {
                _highScore = currentScore;
                PlayerPrefs.SetInt("HighScore", _highScore);
                PlayerPrefs.Save();
                Debug.Log($"New high score: {_highScore} seconds");
            }
        }
    }

    void GameOver()
    {
        gameOverTriggered = true;
        Debug.Log("YOU CLICKED A MINE! GAME OVER!!!");
        _loss.SetActive(true);
        explosionSFX.Play();
        backgroundMusic.Stop();

        
    }
}


*/


using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using TMPro;
using UnityEngine.UI;

public class GridManager : MonoBehaviour
{
    [SerializeField] private int _width, _height, _maxMines;
    [SerializeField] private Tile _tilePrefab;
    [SerializeField] private Tile _mineTile;
    [SerializeField] private Tile[] _numberTiles; // Array of tiles for numbers 1 to 8
    [SerializeField] private Tile _blankTile;
    [SerializeField] private Tile _flagTile;
    [SerializeField] private Tilemap _tilemap; // Reference to the Tilemap component
    [SerializeField] private Transform _cam;

    private HashSet<Vector3Int> _revealedPositions = new HashSet<Vector3Int>();

    private List<Vector3Int> _mines = new List<Vector3Int>();
    private int[,] _grid;
    private HashSet<Vector3Int> _flaggedPositions = new HashSet<Vector3Int>();
    private bool firstLeftClick = true;

    private int _flagCount = 0; // Counter for the number of flags placed
    private const int MaxFlags = 10; // Maximum number of flags allowed
    private int _flagsToShow = 10;
    public TextMeshProUGUI _flagsDisplay;
    public GameObject _loss;
    public GameObject _win;

    public TextMeshProUGUI TimerText;
    public float totalTime = 0f;
    private bool gameOverTriggered = false;
    private bool victoryTriggered = false;

    // High score variables
    private int _highScore;
    public TextMeshProUGUI HighScoreText;
    public AudioSource explosionSFX;
    public AudioSource winSFX;
    public AudioSource backgroundMusic;
    private bool musicOn = true;

    // Toggle mode variables
    private bool isFlagMode = false; // Initially in reveal mode
    public Button toggleModeButton; // Reference to the toggle button
    public TextMeshProUGUI toggleModeButtonText;
    public GameObject toggleButton;

    public GameObject toggleMusicButton;

    void Start()
    {
        GenerateGrid();

        // Load the high score
        _highScore = PlayerPrefs.GetInt("HighScore", int.MaxValue);
        UpdateHighScoreDisplay();

        // Set initial button text
        UpdateToggleButton();
    }

    public void ToggleMode()
    {
        isFlagMode = !isFlagMode;
        UpdateToggleButton();
    }

    void UpdateToggleButton()
    {
        if (isFlagMode)
        {
            toggleModeButtonText.text = "FLAG";
        }
        else
        {
            toggleModeButtonText.text = "REVEAL";
        }
    }

    public void toggleBackgroundMusic()
    {
        if (musicOn)
        {
            backgroundMusic.Pause();
            musicOn = false;
        }
        else
        {
            backgroundMusic.Play();
            musicOn = true;
        }

    }

    void UpdateHighScoreDisplay()
    {
        if (_highScore == int.MaxValue)
        {
            HighScoreText.text = "N/A";
        }
        else
        {
            HighScoreText.text = $"{_highScore}";
        }
    }

    void GenerateGrid()
    {
        _grid = new int[_width, _height];
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                _tilemap.SetTile(new Vector3Int(x, y, 0), _tilePrefab);
                _grid[x, y] = 0;
            }
        }

        _cam.transform.position = new Vector3((float)_width / 2 - 0.5f, (float)_height / 2 - 0.5f, -10);
    }

    void PlaceMines(Vector3Int firstClickPosition)
    {
        int currentMines = 0;
        while (currentMines < _maxMines)
        {
            int x = Random.Range(0, _width);
            int y = Random.Range(0, _height);
            Vector3Int pos = new Vector3Int(x, y, 0);

            if (!_mines.Contains(pos) && pos != firstClickPosition)
            {
                _mines.Add(pos);
                _grid[x, y] = -1; // -1 represents a mine
                currentMines++;
            }
        }

        // Log all mine positions for debugging
        LogMines();
        CalculateNumbers();
    }

    void LogMines()
    {
        Debug.Log("Mines are placed at:");
        foreach (Vector3Int mine in _mines)
        {
            Debug.Log($"Mine at: {mine.x}, {mine.y}");
        }
    }

    void CalculateNumbers()
    {
        int[] dx = { -1, -1, -1, 0, 0, 1, 1, 1 };
        int[] dy = { -1, 0, 1, -1, 1, -1, 0, 1 };

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                if (_grid[x, y] == -1) continue; // Skip mines

                int mineCount = 0;
                for (int i = 0; i < dx.Length; i++)
                {
                    int nx = x + dx[i];
                    int ny = y + dy[i];

                    if (nx >= 0 && nx < _width && ny >= 0 && ny < _height)
                    {
                        if (_grid[nx, ny] == -1)
                        {
                            mineCount++;
                        }
                    }
                }
                _grid[x, y] = mineCount;

                // Debug log the number of adjacent mines for each cell
                Debug.Log($"Cell ({x}, {y}) has {mineCount} adjacent mines.");
            }
        }
    }

    void Update()
    {
        HandleTouchInput();

        if (!gameOverTriggered && !victoryTriggered)
        {
            // Calculate the elapsed time since the game started
            totalTime += Time.deltaTime;

            // Update the timer display
            UpdateTimerDisplay();
        }
    }

    void UpdateTimerDisplay()
    {
        int seconds = Mathf.FloorToInt(totalTime);
        TimerText.text = $"{seconds}";
    }

    void HandleTouchInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector3 touchWorldPos = Camera.main.ScreenToWorldPoint(touch.position);
            Vector3Int touchCellPos = _tilemap.WorldToCell(touchWorldPos);

            if (touch.phase == TouchPhase.Began && touchCellPos.x >= 0 && touchCellPos.x < _width && touchCellPos.y >= 0 && touchCellPos.y < _height && !gameOverTriggered && !victoryTriggered)
            {
                if (isFlagMode)
                {
                    HandleRightClick(touchCellPos);
                }
                else
                {
                    HandleLeftClick(touchCellPos);
                }
            }
        }
    }

    void HandleLeftClick(Vector3Int position)
    {
        if (firstLeftClick)
        {
            // Generate mines but be careful not to put a mine at the location of the first click...
            Debug.Log($"Generating {_maxMines} mines, except at {position.x}, {position.y}");
            PlaceMines(position);
            CalculateNumbers(); // Ensure numbers are calculated after placing mines
            firstLeftClick = false;
        }

        if (_flaggedPositions.Contains(position))
        {
            Debug.Log($"Cannot change tile at {position.x}, {position.y} because it is flagged.");
            return;
        }

        // Get the value from the grid for the clicked position
        int cellValue = _grid[position.x, position.y];
        if (cellValue == -1)
        {
            // Handle mine click
            _tilemap.SetTile(position, _mineTile);
            Debug.Log($"Mine clicked at {position.x}, {position.y}");
            GameOver();
        }
        else if (cellValue > 0 && cellValue <= _numberTiles.Length)
        {
            // Handle number click
            _tilemap.SetTile(position, _numberTiles[cellValue - 1]);
            Debug.Log($"Number {cellValue} clicked at {position.x}, {position.y}");
        }
        else if (cellValue == 0)
        {
            // Handle blank tile (zero) click
            RevealZeros(position);
        }

        // Add the position to revealed positions
        _revealedPositions.Add(position);
    }

    void HandleRightClick(Vector3Int position)
    {
        if (_revealedPositions.Contains(position))
        {
            Debug.Log($"Cannot place a flag at {position.x}, {position.y} because it is already revealed.");
            return;
        }

        if (_flaggedPositions.Contains(position))
        {
            // If the position is already flagged, remove the flag
            _tilemap.SetTile(position, _tilePrefab);
            _flaggedPositions.Remove(position);
            _flagCount--;
            _flagsToShow = MaxFlags - _flagCount;
            _flagsDisplay.text = $"{_flagsToShow}";
            Debug.Log($"Flag removed from tile {position.x}, {position.y}. Total flags: {_flagCount}");
        }
        else
        {
            if (_flagCount < MaxFlags)
            {
                // Otherwise, place a flag
                _tilemap.SetTile(position, _flagTile);
                _flaggedPositions.Add(position);
                _flagCount++;
                _flagsToShow = MaxFlags - _flagCount;
                _flagsDisplay.text = $"{_flagsToShow}";
                Debug.Log($"Flag placed on tile {position.x}, {position.y}. Total flags: {_flagCount}");
                CheckWinCondition();
            }
            else
            {
                Debug.Log("Maximum number of flags reached!");
            }
        }
    }

    void RevealZeros(Vector3Int position)
    {
        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();

        queue.Enqueue(position);
        visited.Add(position);

        while (queue.Count > 0)
        {
            Vector3Int current = queue.Dequeue();

            // Reveal the current tile as a blank tile
            _tilemap.SetTile(current, _blankTile);
            _revealedPositions.Add(current); // Mark this position as revealed

            int[] dx = { -1, -1, -1, 0, 0, 1, 1, 1 };
            int[] dy = { -1, 0, 1, -1, 1, -1, 0, 1 };

            for (int i = 0; i < 8; i++)
            {
                Vector3Int neighbor = new Vector3Int(current.x + dx[i], current.y + dy[i], 0);

                if (neighbor.x >= 0 && neighbor.x < _width && neighbor.y >= 0 && neighbor.y < _height && !visited.Contains(neighbor))
                {
                    int neighborValue = _grid[neighbor.x, neighbor.y];
                    if (neighborValue == 0)
                    {
                        queue.Enqueue(neighbor);
                    }
                    else if (neighborValue > 0 && neighborValue <= _numberTiles.Length)
                    {
                        _tilemap.SetTile(neighbor, _numberTiles[neighborValue - 1]);
                        _revealedPositions.Add(neighbor); // Mark this position as revealed
                    }
                    visited.Add(neighbor);
                }
            }
        }
    }

    void CheckWinCondition()
    {
        // If the number of flagged positions is equal to the number of mines, and all flagged positions are mines
        if (_flaggedPositions.Count == _mines.Count && _flaggedPositions.SetEquals(_mines))
        {
            victoryTriggered = true;
            backgroundMusic.Stop();
            winSFX.Play();
            toggleButton.SetActive(false);
            toggleMusicButton.SetActive(false);
            Debug.Log("Congratulations! You won the game!");
            _win.SetActive(true);
            // Add more win logic here, such as showing a win message or transitioning to a win scene.

            // Check and update high score
            int currentScore = Mathf.FloorToInt(totalTime);
            if (currentScore < _highScore)
            {
                _highScore = currentScore;
                PlayerPrefs.SetInt("HighScore", _highScore);
                PlayerPrefs.Save();
                Debug.Log($"New high score: {_highScore} seconds");
            }
        }
    }

    void GameOver()
    {
        gameOverTriggered = true;
        
        Debug.Log("YOU CLICKED A MINE! GAME OVER!!!");
        _loss.SetActive(true);
        explosionSFX.Play();
        backgroundMusic.Stop();
        toggleButton.SetActive(false);
        toggleMusicButton.SetActive(false);

    }
}

