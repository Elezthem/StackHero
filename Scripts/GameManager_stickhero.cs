using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public enum GameState
{
    START,INPUT,GROWING,NONE
}

public class GameManager_stickhero : MonoBehaviour
{

    [SerializeField]
    private Vector3 startPos;

    [SerializeField]
    private Vector2 minMaxRange, spawnRange;

    [SerializeField]
    private GameObject pillarPrefab, playerPrefab, stickPrefab, diamondPrefab, currentCamera;

    [SerializeField]
    private Transform rotateTransform, endRotateTransform;

    [SerializeField]
    private GameObject scorePanel, startPanel, endPanel;

    [SerializeField]
    private TMP_Text scoreText, scoreEndText, diamondsText, highScoreText;

    private GameObject currentPillar, nextPillar, currentStick, player;

    private int score, diamonds, highScore;

    private float cameraOffsetX;

    private GameState currentState;

    [SerializeField]
    private float stickIncreaseSpeed, maxStickSize;

    public static GameManager_stickhero instance;

    public AudioSource audio;
    public GameObject panel_loading;

    private bool RestartInput { get; set; }
    void OnEnable() { RestartInput = true; }

    private void Start()
    {
        // ¬ызываем метод GameStart() при запуске сцены
        GameStart();
    }


    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        currentState = GameState.START;

        endPanel.SetActive(false);
        scorePanel.SetActive(false);
        startPanel.SetActive(true);

        score = 0;
        diamonds = PlayerPrefs.HasKey("Diamonds_stickhero") ? PlayerPrefs.GetInt("Diamonds_stickhero") : 0;
        highScore = PlayerPrefs.HasKey("HighScore_stickhero") ? PlayerPrefs.GetInt("HighScore_stickhero") : 0;

        scoreText.text = score.ToString();
        diamondsText.text = diamonds.ToString();
        highScoreText.text = highScore.ToString();

        CreateStartObjects();
        cameraOffsetX = currentCamera.transform.position.x - player.transform.position.x;

        if(StateManager.instance.hasSceneStarted)
        {
            GameStart();
        }
    }

    private void Update()
    {
        if (RestartInput)
        {
            if (currentState == GameState.INPUT)
            {
                if (Input.GetMouseButton(0))
                {
                    currentState = GameState.GROWING;
                    ScaleStick();
                }
            }

            if (currentState == GameState.GROWING)
            {
                if (Input.GetMouseButton(0))
                {
                    ScaleStick();
                    audio.Play();
                }
                else
                {
                    StartCoroutine(FallStick());
                }
            }
        }
    }

    void ScaleStick()
    {
        Vector3 tempScale = currentStick.transform.localScale;
        tempScale.y += Time.deltaTime * stickIncreaseSpeed;
        if (tempScale.y > maxStickSize)
            tempScale.y = maxStickSize;
        currentStick.transform.localScale = tempScale;
        
    }

    IEnumerator FallStick()
    {
        currentState = GameState.NONE;
        var x = Rotate(currentStick.transform, rotateTransform, 0.4f);
        yield return x;

        Vector3 movePosition = currentStick.transform.position + new Vector3(currentStick.transform.localScale.y,0,0);
        movePosition.y = player.transform.position.y;
        x = Move(player.transform,movePosition,0.5f); // скоро перса
        yield return x;

        var results = Physics2D.RaycastAll(player.transform.position,Vector2.down);
        var result = Physics2D.Raycast(player.transform.position, Vector2.down);
        foreach (var temp in results)
        {
            if(temp.collider.CompareTag("Platform"))
            {
                result = temp;
            }
        }

        if(!result || !result.collider.CompareTag("Platform"))
        {
            player.GetComponent<Rigidbody2D>().gravityScale = 1f; // hz
            x = Rotate(currentStick.transform, endRotateTransform, 0.5f); //скорость падени€
            yield return x;
            GameOver();
        }
        else
        {
            UpdateScore();

            movePosition = player.transform.position;
            movePosition.x = nextPillar.transform.position.x + nextPillar.transform.localScale.x * 0.5f - 0.35f; //перс и четкость
            x = Move(player.transform, movePosition, 0.2f); // там где он стоит
            yield return x;

            movePosition = currentCamera.transform.position;
            movePosition.x = player.transform.position.x + cameraOffsetX;
            x = Move(currentCamera.transform, movePosition, 0.5f); // камера
            yield return x;

            CreatePlatform();
            SetRandomSize(nextPillar);
            currentState = GameState.INPUT;
            Vector3 stickPosition = currentPillar.transform.position;
            stickPosition.x += currentPillar.transform.localScale.x * 0.5f - 0.05f; // лини€ и передвижение перса
            stickPosition.y = currentStick.transform.position.y;
            stickPosition.z = currentStick.transform.position.z;
            currentStick = Instantiate(stickPrefab, stickPosition, Quaternion.identity);
        }
    }


    void CreateStartObjects()
    {
        CreatePlatform();

        // «адаем позицию спавна игрока
        Vector3 playerPos = new Vector3(-0.75f, 2.394f, 0f);
        player = Instantiate(playerPrefab, playerPos, Quaternion.identity);
        player.name = "Player";

        // «адаем позицию спавна линии
        Vector3 stickPos = new Vector3(1.42f, 1.925f, 0f);
        currentStick = Instantiate(stickPrefab, stickPos, Quaternion.identity);
    }

    void CreatePlatform()
    {
        var currentPlatform = Instantiate(pillarPrefab);
        currentPillar = nextPillar == null ? currentPlatform : nextPillar;
        nextPillar = currentPlatform;
        currentPlatform.transform.position = pillarPrefab.transform.position + startPos;
        Vector3 tempDistance = new Vector3(Random.Range(spawnRange.x,spawnRange.y) + currentPillar.transform.localScale.x*0.5f,0,0);
        startPos += tempDistance;

        if(Random.Range(0,4) == 0)
        {
            var tempDiamond = Instantiate(diamondPrefab);
            Vector3 tempPos = currentPlatform.transform.position;
            tempPos.y = 2.35f;
            tempDiamond.transform.position = tempPos;
        }
    }

    void SetRandomSize(GameObject pillar)
    {
        var newScale = pillar.transform.localScale;
        var allowedScale = nextPillar.transform.position.x - currentPillar.transform.position.x
            - currentPillar.transform.localScale.x * 0.5f - 0.4f;
        newScale.x = Mathf.Max(minMaxRange.x,Random.Range(minMaxRange.x,Mathf.Min(allowedScale,minMaxRange.y)));
        pillar.transform.localScale = newScale;
    }

    void UpdateScore()
    {
        score++;
        scoreText.text = score.ToString();
    }

    void GameOver()
    {
        endPanel.SetActive(true);
        scorePanel.SetActive(false);

        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt("HighScore_stickhero", highScore);
        }

        scoreEndText.text = score.ToString();
        highScoreText.text = highScore.ToString();
        //AdManager.instance.show_ads_ingames();
        RestartInput = false;
    }

    public void UpdateDiamonds()
    {
        diamonds++;
        PlayerPrefs.SetInt("Diamonds_stickhero", diamonds);
        diamondsText.text = diamonds.ToString();
    }

    public void GameStart()
    {
        startPanel.SetActive(false);
        scorePanel.SetActive(true);

        CreatePlatform();
        SetRandomSize(nextPillar);
        currentState = GameState.INPUT;
        
    }

    public void GameRestart()
    {
        panel_loading.SetActive(true);
        StateManager.instance.hasSceneStarted = false;
        SceneManager.LoadScene(0);
        RestartInput = true;
    }

    public void SceneRestart()
    {
        StateManager.instance.hasSceneStarted = true;
        SceneManager.LoadScene(0);
    }

    //Helper Functions
    IEnumerator Move(Transform currentTransform,Vector3 target,float time)
    {
        var passed = 0f;
        var init = currentTransform.transform.position;
        while(passed < time)
        {
            passed += Time.deltaTime;
            var normalized = passed / time;
            var current = Vector3.Lerp(init, target, normalized);
            currentTransform.position = current;
            yield return null;
        }
    }

    IEnumerator Rotate(Transform currentTransform, Transform target, float time)
    {
        var passed = 0f;
        var init = currentTransform.transform.rotation;
        while (passed < time)
        {
            passed += Time.deltaTime;
            var normalized = passed / time;
            var current = Quaternion.Slerp(init, target.rotation, normalized);
            currentTransform.rotation = current;
            yield return null;
        }
    }
}
