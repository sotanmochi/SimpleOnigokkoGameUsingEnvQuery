using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class GameManager : MonoBehaviour
{
	private static GameManager instance = null;
	public static GameManager Instance
	{
		get{
			if(instance == null){
				GameManager[] objects = FindObjectsOfType<GameManager>();
				if(objects.Length == 1)
				{
					instance = objects[0];
					DontDestroyOnLoad(instance.gameObject);
				}
			}
			return instance;
		}
	}

	void Awake()
	{
		if(instance == null)
		{
			instance = this;
			DontDestroyOnLoad(this.gameObject);
		}
		else if(instance != this)
        {
			if (Application.isEditor)
			{
				DestroyImmediate(this);
			}
			else
			{
				Destroy(this);
			}
            Debug.LogError(
                " GameManagerは既に他のGameObjectにアタッチされているため、コンポーネントを破棄しました.\n" +
                " アタッチされているGameObjectの名前は " + Instance.gameObject.name + " です.");
            return;
		}
	}

	public enum GameState
	{
		None,
		Preparing,
		ReadyForPlay,
		Playing,
		PlayerWin,
		PlayerLose,
		ReadyForNextGame,
		ExitGame,
	}

	public TextMesh TextMesh;
	public GameObject NonPlayerCharacter;
	public float GameTime = 30.0f; // 30[sec]
	public Step<GameState> GameStateStep = new Step<GameState>(GameState.None);

	private NPCControllerForOnigokko npcController;

	private float remainingGameTime = 0.0f;

	private int gameScoreForPlayer = 0;
	private int gameScoreForNPC = 0;

	void Start()
	{
		npcController = NonPlayerCharacter.GetComponent<NPCControllerForOnigokko>();
		GameStateStep.SetNext(GameState.Preparing);
	}

	void Update()
	{
		// Transit the step and initialize the next step
		while(GameStateStep.GetNext() != GameState.None)
		{
			switch(GameStateStep.DoTransit())
			{
				case GameState.Preparing:
					InitPreparing();
					break;
				case GameState.ReadyForPlay:
					StartCoroutine(InitReadyForPlay());
					break;
				case GameState.Playing:
					InitPlaying();
					break;
				case GameState.PlayerWin:
					StartCoroutine(InitPlayerWin());
					break;
				case GameState.PlayerLose:
					StartCoroutine(InitPlayerLose());
					break;
				case GameState.ReadyForNextGame:
					InitReadyForNextGame();
					break;
				case GameState.ExitGame:
					StartCoroutine(ExitGame());
					break;
				default:
					break;
			}
		}

		// Update the current step
		float deltaTime = Time.deltaTime;
		switch(GameStateStep.GetCurrent())
		{
			case GameState.Preparing:
				UpdatePreparing();
				break;
			case GameState.Playing:
				UpdatePlaying(deltaTime);
				break;
			default:
				break;
		}
	}

	private void InitPreparing()
	{
		ShowMessage("Press space button to start game.\n" + "Press escape button to end game.");
	}

	private void UpdatePreparing()
	{
		if(Input.GetKeyDown("space"))
		{
			GameStateStep.SetNext(GameState.ReadyForPlay);
		}
		else if(Input.GetKeyDown("escape"))
		{
			PrepareToExitGame();
		}
	}

	private IEnumerator InitReadyForPlay()
	{
		yield return new WaitUntil(() => npcController.NPCStateStep.GetCurrent() == NPCControllerForOnigokko.NPCState.Wait);
		ShowMessage("3");
		yield return new WaitForSeconds(1);
		ShowMessage("2");
		yield return new WaitForSeconds(1);
		ShowMessage("1");
		yield return new WaitForSeconds(1);
		ShowMessage("Game Start");
		npcController.NPCStateStep.SetNext(NPCControllerForOnigokko.NPCState.Stop);
		npcController.StartVoice();
		yield return new WaitForSeconds(1);
		GameStateStep.SetNext(GameState.Playing);
	}

	private void InitPlaying()
	{
		remainingGameTime = GameTime;
	}

	private void UpdatePlaying(float deltaTime)
	{
		remainingGameTime -= Time.deltaTime;
		if(remainingGameTime < 0.0f)
		{
			GameStateStep.SetNext(GameState.PlayerLose);
		}
		else{
			ShowMessage(remainingGameTime.ToString("F2"));
		}
	}

	private IEnumerator InitPlayerWin()
	{
		gameScoreForPlayer += 1;
		ShowMessage("You Win.\nTotal Score\nYou: " + gameScoreForPlayer + "\nUnity chan: " + gameScoreForNPC);
		yield return new WaitForSeconds(5);
		GameStateStep.SetNext(GameState.ReadyForNextGame);
	}

	private IEnumerator InitPlayerLose()
	{
		gameScoreForNPC += 1;
		ShowMessage("You Lose.\nTotal Score\nYou: " + gameScoreForPlayer + "\nUnity chan: " + gameScoreForNPC);
		yield return new WaitForSeconds(5);
		GameStateStep.SetNext(GameState.ReadyForNextGame);
	}

	private void InitReadyForNextGame()
	{
		npcController.NPCStateStep.SetNext(NPCControllerForOnigokko.NPCState.Wait);
		GameStateStep.SetNext(GameState.Preparing);
	}

	private void PrepareToExitGame()
	{
		if(GameStateStep.GetCurrent()==GameState.Preparing)
		{
			ShowMessage("");
			npcController.NPCStateStep.SetNext(NPCControllerForOnigokko.NPCState.PreparingExitGame);
			GameStateStep.SetNext(GameState.ExitGame);
		}
	}

	private IEnumerator ExitGame()
	{
		yield return new WaitUntil(() => npcController.NPCStateStep.GetCurrent() == NPCControllerForOnigokko.NPCState.ExitGame);
		yield return new WaitForSeconds(6);
		NonPlayerCharacter.SetActive(false);
		Application.Quit();
	}

	private void ShowMessage(string message)
	{
		if(TextMesh != null)
		{
			TextMesh.text = message;
		}
		else
		{
			Debug.Log(message);
		}
	}
}