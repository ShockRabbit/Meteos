using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class GameSetting
{
	public const int 	BLOCK_COL = 8;						// 블럭 가로 칸 갯수
	public const int 	BLOCK_ROW = 12;						// 블럭 세로 칸 갯수
	public const float 	ALERT_HEIGHT = 532.0f;				// 게임오버 경고가 발생되는 높이
	public const float 	BLOCK_WIDTH = 128.0f;				// 블럭 너비
	public const float 	BLOCK_HEIGHT = 128.0f;				// 블럭 높이
	public const float 	BLOCK_HEIGHT_HALF = 64.0f;			// 블럭 높이의 반
	public const int 	BLOCK_TYPE_SIZE = 6;				// Lock 을 제외한 블럭의 종류의 갯수
	public const float 	BLOCK_START_Y = 788.0f;				// 블럭이 생성되는 높이
	public const float 	BLOCK_END_Y = -748.0f;				// 바닥 높이
	public const float 	BLOCK_START_X = -448.0f;			// 최좌측 블럭의 x 좌표 값
	public const float 	BLOCK_MASS = 0.007f;				// 블럭의 질량
	public const float 	BLOCK_GEN_TERM_START = 0.7f;		// 블럭이 생성되는 간격 (시간)
	public const float 	BLOCK_FALL_SPEED = -500.0f;			// 블럭 하강 속도
	public const float 	GROUP_FALL_SPEED = -100.0f;			// 그룹 하강 속도
	public const float 	FLOAT_STOP_TIME = 0.2f;				// 그룹이 Rise 상태가 끝난 후 정점에서 잠시 머무르는 시간
	public const float 	RISE_GRAPH_TIMEGAP = 0.1f;			// RISE_SPEED_GRAPH 의 각 값이 배치된 시간 간격
	public const int 	RISE_GRAPH_SIZE = 12;				// RISE_SPEED_GRAPH 의 사이즈
															// RISE_SPEED_GRAPH -> Rise 속도에 대한 그래프
    public static readonly float[] RISE_SPEED_GRAPH 
					= new float[RISE_GRAPH_SIZE] { 1500.0f, 1700.0f, 1900.0f, 1500.0f, 1100.0f,
													700.0f, 350.0f, 150.0f, 50.0f, 30.0f, 15.0f, 5.0f };
	public static float LAND_COMPLETE_TIME = 3.0f;			// 착지 후 착지 완료까지 걸리는 시간
}

public struct MatchInfo			// 매칭 정보. 일반적인 매칭은 Block Swap 시 즉각 처리되지만 매칭(explosion) 가능성이 있는 경우 
{								// (explosion 되었으나 Group 만 다른 경우) 에는 MatchInfo 로 만들어 List 에 저장해놓는다.

	public enum kMatchState {Keep, Break, Explosion}		// Match 의 현 상태 (유지, match 깨짐, 매칭 성공(explosion))
	public enum kMatchDirection {Top, Bottom, Side}			// 매칭 방향 타입

	public kMatchDirection 	eDirection_;			
	public Block 			pPointBlock_;			// 매칭 검사의 주체가되는(중심이 되는) Block
	public int   			iCol_;					// pPointBlock 의 Coloumn
}

public class TouchInfo				// 터치 정보
{
	public int 		iCol_;
	public int 		iRow_;
	public float 	fY_;
	public Block 	pSelectedBlock_;
}

public class GameManager : MonoBehaviour {
	public static GameManager pShared = null;
	Lean.LeanFinger pDraggingFinger_;				// 사용한 어셋 LeanTouch 관련 변수.
	
	[SerializeField]
	private Canvas 		pCvsForMenu_;
	[SerializeField]
	private ParticleSystem pPtcBlockDestroy_;		// Ptc = Particle
	[SerializeField]
	private ParticleSystem pPtcBlockLand_;
	[SerializeField]
	private ParticleSystem pPtcRise_;
	[SerializeField]
	private GameObject 	pBlock_;					// 단순히 첫 셋팅에서 Stack 에 넣을 Block 을 Instantiate 할 때 사용되는 Block 
	[SerializeField]
	SpriteRenderer[] 	pArrImgAlert_;
	[SerializeField]
	Canvas 				pCvsForSelectArea_;
	[SerializeField]
	RectTransform 		pTrsfSelectArea_;
	bool[] 				bArrIsAlertOn_;
	Sequence[] 			pArrSeqAlert_;				// 게임오버 경고의 FadeIn, Out Sequence. 사용한 어셋 DoTween 관련 변수 
	public Sprite[] 	pArrImgBlocks_;
	Stack<Block> 		pStckBlockPool_;

	Block[] 	pArrFallingBlock_;					// 현재 하강중인 Block
	Block[] 	pArrTopBlock_;						// 각 Column 별로 가장 위쪽에 위치한 Block 들
	int      	iFallingBlockCount_;
	
	public Group pMainGroup_ {get; set;}			// 바닥에 붙어있는 기본 그룹
	
	bool  bIsGameOver_;
	bool  bIsFirstPlay_;
	float fTermForGenerateBlock_;
	
	TouchInfo pTouchInfo_;
	
	List<MatchInfo> 		pListMatchReservation_;		// 매칭(explosion) 가능성이 있는 매칭에 관한 정보를 저장하는 List
	List<Block.kBlockType> 	pListBlockType_;			// 블럭 생성에 사용되는 List. 인접한 블럭에 같은 색상의 블럭을 할당하지 않도록 돕는다
	Queue<ParticleSystem> 	pQueuePoolPtcBlockDestroy_;
	Queue<ParticleSystem> 	pQueuePoolPtcBlockLand_;
	Queue<ParticleSystem> 	pQueuePoolPtcRise_;

	void Awake()
	{
		// 각종 변수 초기화 및 오브젝트 풀 세팅
		if (pShared == null)
			pShared = this;
		
		pMainGroup_ = new Group();
		DOTween.Init();
		
		// LeanTouch 관련 함수 등록
		pDraggingFinger_ = null;
		Lean.LeanTouch.OnFingerDown += OnFingerDown;
		Lean.LeanTouch.OnFingerUp += OnFingerUp;
		
		bIsGameOver_ = true;
		bIsFirstPlay_ = true;
		fTermForGenerateBlock_ = GameSetting.BLOCK_GEN_TERM_START;
		
		pArrFallingBlock_ = new Block[GameSetting.BLOCK_COL];
		pArrTopBlock_ = new Block[GameSetting.BLOCK_COL];
		iFallingBlockCount_ = 0;
		
		pTouchInfo_ = new TouchInfo();
		pTouchInfo_.iCol_ = -1;
		pTouchInfo_.iRow_ = -1;
		pTouchInfo_.pSelectedBlock_ = null;

		int iPtcPoolSize = GameSetting.BLOCK_COL * 7;
		ParticleSystem pTempPtc;
		pQueuePoolPtcBlockDestroy_ = new Queue<ParticleSystem>();
		pQueuePoolPtcBlockLand_ = new Queue<ParticleSystem>();
		pQueuePoolPtcRise_ = new Queue<ParticleSystem>();
		for (int i=0; i<iPtcPoolSize; ++i)
		{
			pTempPtc = Instantiate(pPtcBlockDestroy_);
			pTempPtc.Stop();
			pQueuePoolPtcBlockDestroy_.Enqueue(pTempPtc);

			pTempPtc = Instantiate(pPtcBlockLand_);
			pTempPtc.Stop();
			pQueuePoolPtcBlockLand_.Enqueue(pTempPtc);

			pTempPtc = Instantiate(pPtcRise_);
			pTempPtc.Stop();
			pQueuePoolPtcRise_.Enqueue(pTempPtc);
		}
		
		pStckBlockPool_ = new Stack<Block>();
		GameObject pTempObj;
		int iPoolSize = GameSetting.BLOCK_COL * (GameSetting.BLOCK_ROW+1);
		for (int i=0; i<iPoolSize; ++i)
		{
			pTempObj = Instantiate(pBlock_);
			pTempObj.SetActive(false);
			pStckBlockPool_.Push(pTempObj.GetComponent<Block>());
		}
		
		pListMatchReservation_ = new List<MatchInfo>();
		pListBlockType_ = new List<Block.kBlockType>();
		int iSize = GameSetting.BLOCK_TYPE_SIZE;
		for (int i=0; i<iSize; ++i)
		{
			pListBlockType_.Add((Block.kBlockType)i);
		}

		bArrIsAlertOn_ = new bool[GameSetting.BLOCK_COL];
		pArrSeqAlert_ = new Sequence[GameSetting.BLOCK_COL];
		Sequence pSeqAlert;
		for (int i=0; i<GameSetting.BLOCK_COL; ++i)
		{
			bArrIsAlertOn_[i] = false;
			
			pSeqAlert = DOTween.Sequence();
			pSeqAlert.Append(pArrImgAlert_[i].DOFade(0.0f, 1.0f));
			pSeqAlert.Append(pArrImgAlert_[i].DOFade(1.0f, 1.0f));
			pSeqAlert.SetLoops(-1);
			pSeqAlert.Pause();
			pArrSeqAlert_[i] = pSeqAlert;	
		}
	}

	// Use this for initialization
	void Start () {
			
	}
	
	// Update is called once per frame
	void Update () 
	{
		if (!bIsGameOver_)
		{
			if (pDraggingFinger_ != null)
			{
				Camera pCamera = Camera.main;
				Vector3 pPos = pCamera.ScreenToWorldPoint(pDraggingFinger_.ScreenPosition);
				if (!pTouchInfo_.pSelectedBlock_.IsTouch(pPos.y))
				{
					Block pTarget;
					if (pPos.y > pTouchInfo_.fY_)
					{
						pTarget = pTouchInfo_.pSelectedBlock_.pTop_;
						if (pTarget != null && (pTouchInfo_.pSelectedBlock_.pMyGroup_ == pTarget.pMyGroup_
							|| ((pTouchInfo_.pSelectedBlock_.pMyGroup_.eState_ == Group.kGroupState.Landing
							|| pTouchInfo_.pSelectedBlock_.pMyGroup_.eState_ == Group.kGroupState.Stop)
							&& (pTarget.pMyGroup_.eState_ == Group.kGroupState.Landing
							|| pTarget.pMyGroup_.eState_ == Group.kGroupState.Stop))
							))
						{
							if (pTarget.IsTouch(pPos.y))
							{
								pTarget.Swap(pTouchInfo_.pSelectedBlock_, pTouchInfo_.iCol_);
								if (MatchingCheck(pTouchInfo_.pSelectedBlock_, pTarget, pTouchInfo_.iCol_))
								{
									CancelSelectBlock();
								}
								else
								{
									pTouchInfo_.fY_ = pPos.y;
									pTouchInfo_.iRow_ += 1;
								}
							}
						}
						else
						{
							CancelSelectBlock();
						}
					}
					else
					{
						pTarget = pTouchInfo_.pSelectedBlock_.pBottom_;
						if (pTarget != null && (pTouchInfo_.pSelectedBlock_.pMyGroup_ == pTarget.pMyGroup_
							|| ((pTouchInfo_.pSelectedBlock_.pMyGroup_.eState_ == Group.kGroupState.Landing
							|| pTouchInfo_.pSelectedBlock_.pMyGroup_.eState_ == Group.kGroupState.Stop)
							&& (pTarget.pMyGroup_.eState_ == Group.kGroupState.Landing
							|| pTarget.pMyGroup_.eState_ == Group.kGroupState.Stop))
							))
						{
							if (pTarget.IsTouch(pPos.y))
							{
								pTouchInfo_.pSelectedBlock_.Swap(pTarget, pTouchInfo_.iCol_);
								if (MatchingCheck(pTarget, pTouchInfo_.pSelectedBlock_, pTouchInfo_.iCol_))
								{
									CancelSelectBlock();
								}
								else
								{
									pTouchInfo_.fY_ = pPos.y;
									pTouchInfo_.iRow_ -= 1;	
								}
							}
						}
						else
						{
							CancelSelectBlock();
						}
					}
				}
			}
			
			pMainGroup_.MoveGroup(Time.deltaTime);
			MoveBlock(Time.deltaTime);
		}
	}

	public void GameStart()
	{
		if (bIsFirstPlay_)
		{
			// 초기화 및 블럭생성 시작
			bIsFirstPlay_ = false;
			pCvsForMenu_.gameObject.SetActive(false);
			bIsGameOver_ = false;
			InvokeRepeating("GenerateBlock", 0.0f, fTermForGenerateBlock_);
		}
		else
		{
			StartCoroutine("ReSetAndReStartEvent");
		}
			
		
		
	}

	IEnumerator ReSetAndReStartEvent()
	{
		// 초기화
		pDraggingFinger_ = null;
		iFallingBlockCount_ = 0;
		pTouchInfo_.iCol_ = -1;
		pTouchInfo_.iRow_ = -1;
		pTouchInfo_.pSelectedBlock_ = null;
		pListMatchReservation_.Clear();
		
		pCvsForMenu_.gameObject.SetActive(false);

		// 초기화 및 연출
		for (int i=0; i<GameSetting.BLOCK_COL; ++i)
		{
			bArrIsAlertOn_[i] = false;
			if (pArrFallingBlock_[i] != null)
			{
				ShowBlockDestroyEffect(pArrFallingBlock_[i], i);
				DestroyBlock(pArrFallingBlock_[i]);
				pArrFallingBlock_[i] = null;
			}
		}

		// 코루틴을 이용한 초기화 및 연출. Top 부터 바닥까지 순차적으로 폭발
		bool bIsEventContinue = true;
		Block pBlock;
		while (bIsEventContinue)
		{
			yield return new WaitForSeconds(0.2f);
			bIsEventContinue = false;

			for (int i=0; i<GameSetting.BLOCK_COL; ++i)
			{
				if (pArrTopBlock_[i] != null)
				{
					bIsEventContinue = true;
					pBlock = pArrTopBlock_[i].pBottom_;
					ShowBlockDestroyEffect(pArrTopBlock_[i], i);
					DestroyBlock(pArrTopBlock_[i]);
					pArrTopBlock_[i] = pBlock;
				}
			}
		}

		// 초기화 및 블럭생성 시작
		pMainGroup_ = new Group();
		bIsGameOver_ = false;
		InvokeRepeating("GenerateBlock", 0.0f, fTermForGenerateBlock_);
	}
	
	void GenerateBlock()
	{
		// 블럭 생성은 한 Column 당 최대 한개씩 떨어지도록 하는 것을 원칙으로 한다.
		if (iFallingBlockCount_ >= GameSetting.BLOCK_COL)
			return;

		Block pNewBlock = pStckBlockPool_.Pop();
		int iCol = Random.Range(0, GameSetting.BLOCK_COL);
		while (pArrFallingBlock_[iCol] != null)
		{
			++iCol;
			if (iCol >= GameSetting.BLOCK_COL)
				iCol = 0;
		}
		
		pArrFallingBlock_[iCol] = pNewBlock;
		Block.kBlockType eType = GetGenBlockType(iCol);
		pNewBlock.SetBlock(eType, iCol);
		
		++iFallingBlockCount_;
	}

	Block.kBlockType GetGenBlockType(int iCol)
	{
		// Block Type 결정은 Block 이 착지했을 때 주변에 같은 Type 의 Block 이 없도록 한다.
		Block pBottom = pArrTopBlock_[iCol];
		Block pLeft = pBottom != null ? pBottom.GetMyLeftTop() : 
										(iCol != 0 ? pArrTopBlock_[iCol-1] : null);
		Block pRight = pBottom != null ? pBottom.GetMyRightTop() : 
								(iCol != GameSetting.BLOCK_COL-1 ? pArrTopBlock_[iCol+1] : null);

		if (pLeft == null)
			pLeft = iCol != 0 ? pArrFallingBlock_[iCol-1] : null;
		if (pRight == null)
			pRight = iCol != GameSetting.BLOCK_COL-1 ? pArrFallingBlock_[iCol+1] : null;

		if (pBottom != null)
		{
			if (pBottom.eType_ == Block.kBlockType.Lock)
				pBottom = null;
			else
				pListBlockType_.Remove(pBottom.eType_);
		}
		if (pLeft != null)
		{
			if (pLeft.eType_ == Block.kBlockType.Lock)
				pLeft = null;
			else
				pListBlockType_.Remove(pLeft.eType_);
		}
		if (pRight != null)
		{
			if (pRight.eType_ == Block.kBlockType.Lock)
				pRight = null;
			else
				pListBlockType_.Remove(pRight.eType_);
		}
		
		int iIdx = Random.Range(0, pListBlockType_.Count);
		Block.kBlockType eResult = pListBlockType_[iIdx];
		
		if (pBottom != null)
			pListBlockType_.Add(pBottom.eType_);
		if (pLeft != null && !pListBlockType_.Contains(pLeft.eType_))
			pListBlockType_.Add(pLeft.eType_);
		if (pRight != null && !pListBlockType_.Contains(pRight.eType_))
			pListBlockType_.Add(pRight.eType_);

		return eResult;
	}

	public Block.kBlockType GetGenBlockType(Block pBlock)
	{
		// Block 의 Lock 이 풀렸을 때 Block Type 을 결정하기 위하여 사용되는 함수이다.
		Block pBottom = pBlock.pBottom_;
		Block pLeft = pBlock.pLeft_;
		Block pRight = pBlock.pRight_;
		Block pTop = pBlock.pTop_;

		if (pBottom != null)
		{
			if (pBottom.eType_ == Block.kBlockType.Lock)
				pBottom = null;
			else
				pListBlockType_.Remove(pBottom.eType_);
		}
		if (pLeft != null)
		{
			if (pLeft.eType_ == Block.kBlockType.Lock)
				pLeft = null;
			else
				pListBlockType_.Remove(pLeft.eType_);
		}
		if (pRight != null)
		{
			if (pRight.eType_ == Block.kBlockType.Lock)
				pRight = null;
			else
				pListBlockType_.Remove(pRight.eType_);
		}
		if (pTop != null)
		{
			if (pTop.eType_ == Block.kBlockType.Lock)
				pTop = null;
			else
				pListBlockType_.Remove(pTop.eType_);
		}
		
		int iIdx = Random.Range(0, pListBlockType_.Count);
		Block.kBlockType eResult = pListBlockType_[iIdx];
		
		if (pBottom != null)
			pListBlockType_.Add(pBottom.eType_);
		if (pLeft != null && !pListBlockType_.Contains(pLeft.eType_))
			pListBlockType_.Add(pLeft.eType_);
		if (pRight != null && !pListBlockType_.Contains(pRight.eType_))
			pListBlockType_.Add(pRight.eType_);
		if (pTop != null && !pListBlockType_.Contains(pTop.eType_))
			pListBlockType_.Add(pTop.eType_);

		return eResult;
	}
	
	void MoveBlock(float fDeltaTime)
	{
		// 단독으로 떨어지는 Block 들은 pArrFallingBlock 이 전부이며 이 배열을 이용하여 Block 들을 움직인다.
		float fMoveY = fDeltaTime * GameSetting.BLOCK_FALL_SPEED;
		
		int iSize = GameSetting.BLOCK_COL;
		for (int i=0; i<iSize; ++i)
		{
			if (pArrFallingBlock_[i] == null)
				continue;
			
			pArrFallingBlock_[i].FallBlock(pArrTopBlock_[i], fMoveY, i);
		}
	}
	
	public void ArriveFallBlock(int iCol, Block pNewTopBlock)
	{
		// Block 이 무사히 착지했을 때 호출되는 함수. 최상단 Block 을 관리하는 pArrTopBlock 에 삽입한다.
		pArrFallingBlock_[iCol] = null;
		pArrTopBlock_[iCol] = pNewTopBlock;
		--iFallingBlockCount_;

		// 게임오버 경고 관련
		if (pNewTopBlock.pMyGroup_ == pMainGroup_)
		{
			if (!bArrIsAlertOn_[iCol])
			{
				if (pNewTopBlock.pMyTransform_.position.y >= GameSetting.ALERT_HEIGHT)
				{
					// Alert
					ShowGameOverAlert(iCol);
				}
			}
			else
			{
				if (!bIsGameOver_ && pNewTopBlock.pMyTransform_.position.y >= GameSetting.BLOCK_START_Y + GameSetting.BLOCK_HEIGHT * 2.0f)
				{
					StartCoroutine("GameOver");
				}
			}
		}
		
		// 블럭 선택영역 표시 관련 - 현재 영역 표시 중인 곳에 블럭이 떨어졌으므로 표시 영역을 갱신한다.
		if (pDraggingFinger_ != null && pTouchInfo_.iCol_ == iCol)
		{
			ShowBlockSelectArea(pTouchInfo_.pSelectedBlock_);
		}
	}

	void ShowGameOverAlert(int iCol)
	{
		SoundManager.pShared.PlaySe(SeType.GameOverAlert);
		bArrIsAlertOn_[iCol] = true;
		pArrImgAlert_[iCol].gameObject.SetActive(true);
		pArrSeqAlert_[iCol].Restart();
	}

	public void ShowBlockSelectArea(Block pBlock)
	{
		Block pBottom = pBlock;
		Block pTop = pBlock;

		// 선택된 Block 이 교환할 수 있는 가장 바닥의 Block 을 검출
		while (pBottom.pBottom_ != null)
		{
			if (pBottom.pMyGroup_ == pBottom.pBottom_.pMyGroup_)
				pBottom = pBottom.pBottom_;
			else
			{
				if (pBottom.pMyGroup_.eState_ == Group.kGroupState.Landing)
					pBottom = pBottom.pBottom_;
				else
					break;
			}
		}

		// 선택된 Block 이 교환할 수 있는 가장 높은 곳에 있는 Block 을 검출
		while (pTop.pTop_ != null)
		{
			if (pTop.pMyGroup_ == pTop.pTop_.pMyGroup_)
				pTop = pTop.pTop_;
			else
			{
				if (pTop.pTop_.pMyGroup_.eState_ == Group.kGroupState.Landing)
					pTop = pTop.pTop_;
				else
					break;
			}
		}

		// 검출된 Block 들의 정보들을 이용하여 영역을 설정하고 가장 밑에 있는 Block 에 자식으로 붙인다.
		pTrsfSelectArea_.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 
							pTop.pMyTransform_.position.y - pBottom.pMyTransform_.position.y + GameSetting.BLOCK_HEIGHT);

		pCvsForSelectArea_.gameObject.SetActive(true);
		pCvsForSelectArea_.transform.SetParent(pBottom.pMyTransform_);
		pTrsfSelectArea_.position = new Vector3(pBottom.pMyTransform_.position.x, 
												pBottom.pMyTransform_.position.y - GameSetting.BLOCK_HEIGHT_HALF, 
												0.0f);
	}

	void HideBlockSelectArea()
	{
		pCvsForSelectArea_.transform.SetParent(null);
		pCvsForSelectArea_.gameObject.SetActive(false);
	}

	void HideGameOverAlert(int iCol)
	{
		bArrIsAlertOn_[iCol] = false;
		pArrSeqAlert_[iCol].Pause();
		pArrImgAlert_[iCol].gameObject.SetActive(false);
	}

	IEnumerator GameOver()
	{
		bIsGameOver_ = true;
		pDraggingFinger_ = null;
		CancelInvoke("GenerateBlock");
		HideBlockSelectArea();

		// GameOver 연출 및 추후 pArrTopBlock 초기화에 문제가 없도록 하기 위한 처리
		Block[] pTempArrTopBlock = new Block[GameSetting.BLOCK_COL];
		for (int i=0; i<GameSetting.BLOCK_COL; ++i)
		{
			if (bArrIsAlertOn_[i])
				HideGameOverAlert(i);

			pTempArrTopBlock[i] = pArrTopBlock_[i];	
			while (pTempArrTopBlock[i].pBottom_ != null)
				pTempArrTopBlock[i] = pTempArrTopBlock[i].pBottom_;
		}

		// 코루틴을 이용하여 바닥부터 꼭대기까지 순차적으로 Lock 이 걸리는 연출
		bool bIsEventContinue = true;
		while (bIsEventContinue)
		{
			yield return new WaitForSeconds(0.2f);
			bIsEventContinue = false;
			SoundManager.pShared.PlaySe(SeType.BlockGameOver);
			for (int i=0; i<GameSetting.BLOCK_COL; ++i)
			{
				if (pTempArrTopBlock[i] != null)
				{
					bIsEventContinue = true;
					pTempArrTopBlock[i].GameOver();
					RemoveBlockRiseEffect(pTempArrTopBlock[i], true);
					pTempArrTopBlock[i] = pTempArrTopBlock[i].pTop_;
				}
			}
		}

		for (int i=0; i<GameSetting.BLOCK_COL; ++i)
		{
			if (pArrFallingBlock_[i] != null)
				pArrFallingBlock_[i].GameOver();
		}

		SoundManager.pShared.PlaySe(SeType.GameOver);
		pCvsForMenu_.gameObject.SetActive(true);

		yield break;
	}

	public void DestroyBlock(Block pBlock)
	{
		RemoveBlockRiseEffect(pBlock, true);

		if (pBlock.pLeft_ != null)
			pBlock.pLeft_.pRight_ = null;
		if (pBlock.pRight_ != null)
			pBlock.pRight_.pLeft_ = null;
		if (pBlock.pBottom_ != null)
			pBlock.pBottom_.pTop_ = null;

		pBlock.pMyGameObj_.SetActive(false);
		pStckBlockPool_.Push(pBlock);
	}

	public void DestroyTopBlock(int iCol, Block pBlock)
	{
		// Group 이 상승하여 상단에 위치한 Block 이 삭제되었을 때 호출되는 함수
		if (bArrIsAlertOn_[iCol])
			HideGameOverAlert(iCol);

		pArrTopBlock_[iCol] = pBlock.pBottom_;
		DestroyBlock(pBlock);
	}

	public void RemoveBlockRiseEffect(Block pBlock)
	{
		// RiseEffect 만 제거. Group 이 착지했을 때 사용
		// (Unlock 은 Group 파괴 혹은 착지 완료 시 _ Unlock 을 위해서는 아직 Group 내의 list 에 등록되어 있어야한다.)
		if (pBlock.pPtcRiseEffect_ != null)
		{
			pBlock.pPtcRiseEffect_.Stop();
			pQueuePoolPtcRise_.Enqueue(pBlock.pPtcRiseEffect_);
			pBlock.pPtcRiseEffect_.transform.SetParent(null);
			pBlock.pPtcRiseEffect_ = null;
		}
	}

	public void RemoveBlockRiseEffect(Block pBlock, bool bRemoveFromLockList)
	{
		// RiseEffect 제거 및 Group 내의 List 에서 제거
		RemoveBlockRiseEffect(pBlock);
		if (bRemoveFromLockList)
		{
			if (pBlock.pMyGroup_ != null)
				pBlock.pMyGroup_.pListLockBlock_.Remove(pBlock);
		}
	}

	public void DestroyFallBlock(int iCol, Block pBlock)
	{
		// 하강하던 Block 이 상승하는 Group 과 충돌하여 파괴되는 경우 호출되는 함수
		pArrFallingBlock_[iCol] = null;
		--iFallingBlockCount_;
		pBlock.pMyGameObj_.SetActive(false);
		pStckBlockPool_.Push(pBlock);
	}
	
	public void OnFingerDown(Lean.LeanFinger pFinger)
	{
		if (bIsGameOver_)
			return;

		Camera pCamera = Camera.main;
		Vector3 pPos = pCamera.ScreenToWorldPoint(pFinger.ScreenPosition);
		
		int iCol = (int)(((pPos.x - GameSetting.BLOCK_START_X) / GameSetting.BLOCK_WIDTH) + 0.5f);
		
		bool bIsTouch = pMainGroup_.IsTouch(iCol, pPos.y, pTouchInfo_);
		if (bIsTouch)
		{
			pDraggingFinger_ = pFinger;
			ShowBlockSelectArea(pTouchInfo_.pSelectedBlock_);
		}
	}
	
	public void OnFingerUp(Lean.LeanFinger pFinger)
	{
		if (pFinger == pDraggingFinger_)
		{
			CancelSelectBlock();
		}
	}

	public void CancelSelectBlock()
	{
		pDraggingFinger_ = null;
		HideBlockSelectArea();
	}

	public void SetTopBlock(int iCol, Block pNewTopBlock)
	{
		pArrTopBlock_[iCol] = pNewTopBlock;
	}
	
	bool MatchingCheck(Block pBlockTop, Block pBlockBottom, int iCol)
	{
		return pBlockTop.pMyGroup_.MatchingCheck(pBlockTop, pBlockBottom, iCol);
	}
	
	public void AddMatchReservation(MatchInfo pInfo)
	{
		pListMatchReservation_.Add(pInfo);
	}
	
	public bool CheckMatchReservation()
	{
		// List 에 등록된 explosion 가능성이 있는 정보들을 최신 정보로 갱신. Keep = 유지 / Break = 파기 / Explosion = 매칭 성공
		bool bIsExplosion = false;
		Block pBlock;
		MatchInfo pInfo;
		int iSize = pListMatchReservation_.Count;
		for (int i=0; i<iSize; ++i)
		{
			pInfo = pListMatchReservation_[i];
			pBlock = pInfo.pPointBlock_;
			MatchInfo.kMatchState eResult;
			
			if (pInfo.eDirection_ == MatchInfo.kMatchDirection.Bottom)
			{
				eResult = pBlock.MatchCheckBottomForReservation(pInfo.iCol_);
			}
			else if (pInfo.eDirection_ == MatchInfo.kMatchDirection.Top)
			{
				eResult = pBlock.MatchCheckTopForReservation(pInfo.iCol_);
			}
			else            
			{
				eResult = pBlock.MatchCheckSideForReservation(pInfo.iCol_);
			}
			
			if (eResult == MatchInfo.kMatchState.Break) 
			{
				pListMatchReservation_.RemoveAt(i);
				--i;
				--iSize;
			}

			if (eResult == MatchInfo.kMatchState.Explosion)
			{
				pListMatchReservation_.RemoveAt(i);
				--i;
				--iSize;

				bIsExplosion = true;
			}
		}

		// 매칭 정보들의 Rise 를 모아서 일괄 처리 (Landing 상태가 Rise 로 변하면 관련된 다른 매칭 정보들의 매칭(explosion)을 방해하게 된다.)
		if (bIsExplosion)
			pMainGroup_.ChainRiseFromReservation();

		return bIsExplosion;
	}

	public void ShowBlockDestroyEffect(int iCol)
	{
		// Block 이 화면 밖으로 나갈 때 호출되는 DestroyEffect 함수
		SoundManager.pShared.PlaySe(SeType.BlockDestroy);

		ParticleSystem pPtcSys = pQueuePoolPtcBlockDestroy_.Dequeue();
		pPtcSys.transform.position = new Vector3(GameSetting.BLOCK_START_X + GameSetting.BLOCK_WIDTH * (float)iCol,
												 GameSetting.BLOCK_START_Y,
												 0.0f);
		pPtcSys.Play();
		pQueuePoolPtcBlockDestroy_.Enqueue(pPtcSys);
	}

	public void ShowBlockDestroyEffect(Block pBlock, int iCol)
	{
		// Block 이 파괴될 때 호출되는 DestroyEffect 함수
		SoundManager.pShared.PlaySe(SeType.BlockDestroy);

		ParticleSystem pPtcSys = pQueuePoolPtcBlockDestroy_.Dequeue();
		pPtcSys.transform.position = new Vector3(GameSetting.BLOCK_START_X + GameSetting.BLOCK_WIDTH * (float)iCol,
												 pBlock.pMyTransform_.position.y - GameSetting.BLOCK_HEIGHT_HALF,
												 0.0f);
		pPtcSys.Play();
		pQueuePoolPtcBlockDestroy_.Enqueue(pPtcSys);
	}

	public void ShowBlockLandEffect(Block pBlock, int iCol)
	{
		SoundManager.pShared.PlaySe(SeType.BlockLanding);

		ParticleSystem pPtcSys = pQueuePoolPtcBlockLand_.Dequeue();
		pPtcSys.transform.position = new Vector3(GameSetting.BLOCK_START_X + GameSetting.BLOCK_WIDTH * (float)iCol,
												 pBlock.pMyTransform_.position.y - GameSetting.BLOCK_HEIGHT_HALF,
												 0.0f);
		pPtcSys.Play();
		pQueuePoolPtcBlockLand_.Enqueue(pPtcSys);
	}

	public void ShowBlockRiseEffect(Block pBlock)
	{
		if (pBlock == null || pBlock.pPtcRiseEffect_ != null)
			return;
		SoundManager.pShared.PlaySe(SeType.BlockRise);

		ParticleSystem pPtcSys = pQueuePoolPtcRise_.Dequeue();
		pPtcSys.transform.SetParent(pBlock.pMyTransform_);
		pPtcSys.transform.localPosition = new Vector3(0.0f, -GameSetting.BLOCK_HEIGHT_HALF, 0.0f);
		pPtcSys.Play();
		pBlock.pPtcRiseEffect_ = pPtcSys;
	}
}
