using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using UnityEngine;

// SynchronizeBallPos ���� (max�� ���� �� �׽�Ʈ)
// startGame Delay (ī��Ʈ �ϱ�)
// ���̵� ���� �ֱ� (�� �ӵ�.. �̰� �� ��������? startGame���� �� �ӵ� �޾ƿͼ� �����ϱ� ����?)
// startGame�� leftScore, rightScore �޾Ƽ� ����
// gameOver reason (enum to string)
// paddle ������ ���� ������ ��..
// ������ ����

public class GameManager : MonoBehaviour
{
	[DllImport("__Internal")]
	private static extern void UnityException(string data);
	[DllImport("__Internal")]
	private static extern void ValidCheck(string data);
	[DllImport("__Internal")]
	private static extern void Init();

	static GameManager instance;
	[SerializeField] Paddle leftPaddle;
	[SerializeField] Paddle rightPaddle;
	Score score;
	Ball ball;
	Enums.PlayerSide mySide;
	Coroutine validCheckCoroutine;
	bool isOver;

	private GameManager() { }

	void Awake()
	{
		Inintialize();
	}

	void Start()
	{
		score = Score.GetInstance();
		ball = Ball.GetInstance();

		// call js function
#if UNITY_WEBGL == true && UNITY_EDITOR == false
		Init();
#endif
	}

	void Inintialize()
	{
		instance = this;
		isOver = false;
		mySide = Enums.PlayerSide.NONE;
	}

	public static GameManager GetInstance()
	{
		return instance;
	}

	public bool GetIsOver()
	{
		return isOver;
	}

	public Enums.PlayerSide GetMySide()
	{
		return mySide;
	}

	public void NextGame(Vector3 ballDir)
	{
		ball.ReSetBall(ballDir);
		leftPaddle.ResetPos();
		rightPaddle.ResetPos();
	}

	// call js function
	IEnumerator StartValidCheck()
	{
		while (true)
		{
			float nextTime = Random.Range(1f, 5f);

			yield return new WaitForSecondsRealtime(nextTime);

			string data = JsonUtility.ToJson(new JsonStructs.ValidCheckStruct(leftPaddle, rightPaddle, ball));

			// call js function
#if UNITY_WEBGL == true && UNITY_EDITOR == false
			ValidCheck(data);
#endif
		}
	}

	// call from react
	public void StartGame(string data)
	{
		JsonStructs.StartGame sgs = JsonUtility.FromJson<JsonStructs.StartGame>(data);
		if (sgs.isFirst)
		{
			mySide = sgs.side;
			if (mySide == Enums.PlayerSide.LEFT)
				leftPaddle.SetAvailable();
			else if (mySide == Enums.PlayerSide.RIGHT)
				rightPaddle.SetAvailable();
			else
			{
				// call js function
#if UNITY_WEBGL == true && UNITY_EDITOR == false
				UnityException("GameManager.StartGame() : PlayerSide is NONE");
#endif
			}
			score.Initialize();
			validCheckCoroutine = StartCoroutine(StartValidCheck());
		}
		NextGame(new Vector3(sgs.ballDirX, sgs.ballDirY, sgs.ballDirZ));
		isOver = false;
	}

	// call from react
	public void GameOver(string data)
	{
		ball.Initialize();
		StopCoroutine(validCheckCoroutine);
		JsonStructs.GameOver gos = JsonUtility.FromJson<JsonStructs.GameOver>(data);
		score.Finish(gos);
		isOver = true;
	}
}
