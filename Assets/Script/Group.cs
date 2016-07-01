using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Group
{
	public enum kGroupState {Stop, Rise, FloatStop, FloatFall, Landing}		// Group 의 동작에 따른 상태
	
	bool 	bIsRiseReservation_;				// Rise 가 예약되어있는 Group 인가를 판별하는 변수
	public 	kGroupState eState_ {get; set;}
	int 	iBlockCount_;
	float 	fMoveSpeed_;
	float 	fMovePhaseTimer_;					// Group 상승(Rise) 시 각 Phase 의 남은 시간
	int   	iMovePhase_;						// GameSetting.RISE_SPEED_GRAPH 의 Phase 를 나타내는 변수
	float 	fFloatStopTime_;					// 그룹이 Rise 상태가 끝난 후 정점에서 잠시 머무르는 시간
	
	Block[] pArrBlockBottom_;					// Group 내에서 가장 밑 바닥에 있는 Block 들
	Block[] pArrBlockTop_;						// Group 내에서 가장 높은 곳에 있는 Block 들
	public 	Group pParent_ {get; set;}			// 연결된 Group. 연결만 되어있을 뿐 위치적인 의미는 없다.
	public 	Group pChild_ {get; set;}			// 상동
	public 	List<Block> pListLockBlock_ {get; set;}	// Block 매칭으로 인하여 Lock 에 걸린 Block 들이 등록된 List
	
	public Group()
	{
		// 초기화
		bIsRiseReservation_ = false;
		eState_ = kGroupState.Stop;
		iBlockCount_ = 0;
		fMoveSpeed_ = 0.0f;
		fMovePhaseTimer_ = 0.0f;
		iMovePhase_ = 0;
		fFloatStopTime_ = GameSetting.FLOAT_STOP_TIME;
		pArrBlockBottom_ = new Block[GameSetting.BLOCK_COL];
		pArrBlockTop_ = new Block[GameSetting.BLOCK_COL];
		for (int i=0; i<GameSetting.BLOCK_COL; ++i)
		{
			pArrBlockBottom_[i] = null;
			pArrBlockTop_[i] = null;
		}
		pParent_ = null;
		pChild_ = null;

		pListLockBlock_ = new List<Block>();
	}
	
	public void SetBlockTop(int iCol, Block pNewTopBlock, Block pOldTopBlock)
	{
		// Swap 함수에서 다른 Group 간의 Block 이 교환될 때 사용되는 함수
		pArrBlockTop_[iCol] = pNewTopBlock;
		if (pArrBlockBottom_[iCol] == pOldTopBlock)
			pArrBlockBottom_[iCol] = pNewTopBlock;
	}

	public void SetBlockBottom(int iCol, Block pNewBottomBlock, Block pOldBottomBlock)
	{
		// Swap 함수에서 다른 Group 간의 Block 이 교환될 때 사용되는 함수
		pArrBlockBottom_[iCol] = pNewBottomBlock;
		if (pArrBlockTop_[iCol] == pOldBottomBlock)
			pArrBlockTop_[iCol] = pNewBottomBlock;
	}

	public void OrganizeBlock(int iCol, Block pNewTopBlock, Block pNewBottomBlock)
	{
		// Swap 함수에서 같은 Group 내에서 Block 이 교환될 때 사용되는 함수
		if (pArrBlockBottom_[iCol] == pNewTopBlock)
			pArrBlockBottom_[iCol] = pNewBottomBlock;
		if (pArrBlockTop_[iCol] == pNewBottomBlock)
			pArrBlockTop_[iCol] = pNewTopBlock;
	}

	public void RiseStart()
	{
		// Group 상승
		bIsRiseReservation_ = false;
		iMovePhase_ = 0;
		fMovePhaseTimer_ = 0.0f;

		float fMass = (float)iBlockCount_ * GameSetting.BLOCK_MASS;
		fMoveSpeed_ = GameSetting.RISE_SPEED_GRAPH[0] * (1.0f - fMass);
		eState_ = kGroupState.Rise;
	}

	public void RiseStartShort()
	{
		// 상승하는 다른 Group 에 의해 짧게 밀려올라갈 때 사용되는 함수. 
		// 시작 Phase 를 후반부로 설정하여 살짝만 올라가도록 한다.
		bIsRiseReservation_ = false;
		iMovePhase_ = 6;
		fMovePhaseTimer_ = 0.0f;

		float fMass = (float)iBlockCount_ * GameSetting.BLOCK_MASS;
		fMoveSpeed_ = GameSetting.RISE_SPEED_GRAPH[6] * (1.0f - fMass);
		eState_ = kGroupState.Rise;
	}

	public void RiseReservation()
	{
		// Rise 예약. 예약된 매칭 정보들이 매칭되었을 때 사용된다.
		bIsRiseReservation_ = true;
	}

	public void ChainRiseFromReservation()
	{
		// 예약된 Rise 를 연쇄적으로 호출한다. GameManager 에서 관리하는 MainGroup 에서만 호출한다.
		if (bIsRiseReservation_)
			RiseStart();
		if (pChild_ != null)
			pChild_.ChainRiseFromReservation();
	}
	
	public bool SetColumn(int iCol, Block pBottom, Block pTop)
	{
		// 새로 Group 이 생성되어 Column 한줄을 삽입할 때 사용되는 함수
		if (pArrBlockBottom_[iCol] != null)
			return false;
		
		pArrBlockBottom_[iCol] = pBottom;
		pArrBlockTop_[iCol] = pTop;
		
		Block pBlock = pBottom;
		while (pBlock != pTop.pTop_)
		{
			pBlock.pMyGroup_ = this;
			pBlock = pBlock.pTop_;
			++iBlockCount_;
		}
		
		return true;
	}
	
	public void AddColumn(int iCol, Block pBottom)
	{	
		// 이미 존재하는 Group 에 Column 한줄을 삽입할 때 사용되는 함수
		Block pTop = pBottom.pMyGroup_.pArrBlockTop_[iCol];
		if (pBottom.pMyGroup_.pArrBlockBottom_[iCol] == pBottom)
		{
			pBottom.pMyGroup_.pArrBlockTop_[iCol] = null;
			pBottom.pMyGroup_.pArrBlockBottom_[iCol] = null;	
		}
		else
		{
			pBottom.pMyGroup_.pArrBlockTop_[iCol] = pBottom.pBottom_;
		}

		Block pBlock = pBottom;
		
		if (pArrBlockBottom_[iCol] == null)
		{
			while (pBlock != pTop.pTop_)
			{
				pBlock.pMyGroup_ = this;
				pBlock = pBlock.pTop_;
				++iBlockCount_;
			}
			pArrBlockTop_[iCol] = pTop;
		}
		else
		{
			while (pBlock != pArrBlockBottom_[iCol])
			{
				pBlock.pMyGroup_ = this;
				pBlock = pBlock.pTop_;
				++iBlockCount_;
			}
		}
		pArrBlockBottom_[iCol] = pBottom;
	}
	
	public void AddBlock(int iCol, Block pBlock)
	{
		// Group 위에 하강 중인 Block 이 착지했을 때 Group 에 Block 을 삽입하기 위한 함수
		++iBlockCount_;
		pBlock.pMyGroup_ = this;
		if (pArrBlockBottom_[iCol] == null)
		{
			pArrBlockBottom_[iCol] = pBlock;
			pArrBlockTop_[iCol] = pBlock;
			
			if (iCol > 0)
			{
				pBlock.pLeft_ = pArrBlockBottom_[iCol-1];
				if (pBlock.pLeft_ != null)
					pBlock.pLeft_.pRight_ = pBlock;
			}
			if (iCol < GameSetting.BLOCK_COL-1)
			{
				pBlock.pRight_ = pArrBlockBottom_[iCol+1];
				if (pBlock.pRight_ != null)
					pBlock.pRight_.pLeft_ = pBlock;
			}
		}
		else
		{
			pBlock.pBottom_ = pArrBlockTop_[iCol];
			pArrBlockTop_[iCol].pTop_ = pBlock;
			pBlock.pLeft_ = pArrBlockTop_[iCol].GetMyLeftTop();
			if (pBlock.pLeft_ != null)
				pBlock.pLeft_.pRight_ = pBlock;
			pBlock.pRight_ = pArrBlockTop_[iCol].GetMyRightTop();
			if (pBlock.pRight_ != null)
				pBlock.pRight_.pLeft_ = pBlock;
			pArrBlockTop_[iCol] = pBlock;
		}
	}
	
	public bool IsTouch(int iCol, float fY, TouchInfo pInfo)
	{
		// 선택된 Block 이 있는지 검사하는 함수. 없으면 Child 의 IsTouch 를 호출한다.
		if (iCol >= GameSetting.BLOCK_COL)
			return false;

		int iRow = 0;
		Block pBlock = pArrBlockBottom_[iCol];
		
		while (pBlock != null)
		{
			if (pBlock.IsTouch(fY))
			{
				pInfo.iCol_ = iCol;
				pInfo.iRow_ = iRow;
				pInfo.fY_ = fY;
				pInfo.pSelectedBlock_ = pBlock;
				return true;
			}
			++iRow;
			pBlock = pBlock.pTop_;
		}
	
		bool bResult = false;
		if (pChild_ != null)
			bResult = pChild_.IsTouch(iCol, fY, pInfo);
		return bResult;
	}
	
	public bool MatchingCheck(Block pBlockTop, Block pBlockBottom, int iCol)
	{
		// 매칭이 되었는지 검사하는 함수. 
		bool bMatchTop = false;
		bool bMatchBottom = false;
		bool bMatchTopSide = false;
		bool bMatchBottomSide = false;
		
		bool bResult = false;
		bool bRservationResult = false;
		// BlockTop's Top Check
		bMatchTop = pBlockTop.MatchCheckTop(iCol);
		if (bMatchTop)
			bResult = true;
		
		// BlockBottom's Bottom Check
		bMatchBottom = pBlockBottom.MatchCheckBottom(iCol);
		if (bMatchBottom)
			bResult = true;
		
		// BlockTop's Side Check
		int iTopExpCount;
		int iTopStartIdx;
		Block pTopStart;
		bMatchTopSide = pBlockTop.MatchCheckSide(out pTopStart, iCol, out iTopExpCount, out iTopStartIdx);
		if (bMatchTopSide)
			bResult = true;
		
		// BlockBottom's Side Check
		int iBottomExpCount;
		int iBottomStartIdx;
		Block pBottomStart;
		bMatchBottomSide = pBlockBottom.MatchCheckSide(out pBottomStart, iCol, out iBottomExpCount, out iBottomStartIdx);
		if (bMatchBottomSide)
			bResult = true;
		
		// Check Match Reservation _ for Landing Group
		bIsRiseReservation_ = GameManager.pShared.CheckMatchReservation();

		// Rise or Make new Group
		if (pBlockTop.pMyGroup_ != pBlockBottom.pMyGroup_)	// 매칭이 시도된 블럭 중 위쪽에 있는 블럭이 속한 그룹이 Landing 이다. 
		{
			if (bMatchTop || bMatchTopSide)					// Landing 중인 그룹내에서 매칭이 이뤄진 경우
			{
				if (bMatchTopSide)
				{
					for (int i=0; i<iTopExpCount; ++i)
					{
						LockBlock(pTopStart);
						pTopStart = pTopStart.pRight_;
					}
				}
				if (bMatchTop)
				{
					LockBlock(pBlockTop);
					LockBlock(pBlockTop.pTop_);
					LockBlock(pBlockTop.pTop_.pTop_);
				}
					
				RiseStart();
			}

			if (bMatchBottom)								// Landing 중인 그룹 바깥에서 매칭이 이뤄진 경우 -> Landing 중인 그룹에 추가 후 상승
			{
				AddColumn(iCol, pBlockBottom.pBottom_.pBottom_);
				RiseStart();
				LockBlock(pBlockBottom);
				LockBlock(pBlockBottom.pBottom_);
				LockBlock(pBlockBottom.pBottom_.pBottom_);
			}
			if (bMatchBottomSide)
			{
				for (int i=0; i<iBottomExpCount; ++i)
				{
					AddColumn(iBottomStartIdx + i, pBottomStart);
					LockBlock(pBottomStart);
					pBottomStart = pBottomStart.pRight_;
				}
				RiseStart();
			}
		}
		else			// 같은 그룹내에서 매칭이 시도된 경우
		{
			// if Matching Succees & State = Stop -> Make Child Group
			if (bResult)
			{
				if (eState_ == kGroupState.Stop)	// 멈춰있는 그룹인 경우 (MainGroup) -> 새로 그룹을 만들어서 Child 로 만들고 Rise 시킨다.
				{
					Group pNewChild = new Group();
					if (pChild_ != null)
					{
						pChild_.pParent_ = pNewChild;
						pNewChild.pChild_ = pChild_;
					}
					pChild_ = pNewChild;
					pChild_.pParent_ = this;
					
					pNewChild.RiseStart();
				}
				else								// Landing, FloatFall 등의 경우
				{
					if (bMatchBottom)
					{
						LockBlock(pBlockBottom);
						LockBlock(pBlockBottom.pBottom_);
						LockBlock(pBlockBottom.pBottom_.pBottom_);
					}
					if (bMatchBottomSide)
					{
						for (int i=0; i<iBottomExpCount; ++i)
						{
							LockBlock(pBottomStart);
							pBottomStart = pBottomStart.pRight_;
						}
					}
					if (bMatchTopSide)
					{
						for (int i=0; i<iTopExpCount; ++i)
						{
							LockBlock(pTopStart);
							pTopStart = pTopStart.pRight_;
						}
					}
					if (bMatchTop)
					{
						LockBlock(pBlockTop);
						LockBlock(pBlockTop.pTop_);
						LockBlock(pBlockTop.pTop_.pTop_);
					}
						
					RiseStart();
					return true;
				}
			}
			else
			{
				return false;
			}
			

			// 멈춰있는 그룹인 경우.. 에서 만들어진 그룹에 매칭된 Block 들로 인해 상승할 Block 들을 삽입한다.
			if (bMatchBottom)
			{
				pChild_.SetColumn(iCol, pBlockBottom.pBottom_.pBottom_, pArrBlockTop_[iCol]);
				if (pBlockBottom.pBottom_.pBottom_ == pArrBlockBottom_[iCol])
				{
					pArrBlockBottom_[iCol] = null;
					pArrBlockTop_[iCol] = null;
				}
				else
				{
					pArrBlockTop_[iCol] = pBlockBottom.pBottom_.pBottom_.pBottom_;
				}
				pChild_.LockBlock(pBlockBottom);
				pChild_.LockBlock(pBlockBottom.pBottom_);
				pChild_.LockBlock(pBlockBottom.pBottom_.pBottom_);
			}
			
			if (bMatchBottomSide)
			{
				for (int i=0; i<iBottomExpCount; ++i)
				{
					if (pChild_.SetColumn(iBottomStartIdx + i, pBottomStart, pArrBlockTop_[iBottomStartIdx + i]))
					{
						if (pBottomStart == pArrBlockBottom_[iBottomStartIdx + i])
						{
							pArrBlockBottom_[iBottomStartIdx + i] = null;
							pArrBlockTop_[iBottomStartIdx + i] = null;
						}
						else
						{
							pArrBlockTop_[iBottomStartIdx + i] = pBottomStart.pBottom_;
						}
					}
					pChild_.LockBlock(pBottomStart);
					pBottomStart = pBottomStart.pRight_;
				}
			}
			
			if (bMatchTopSide)
			{
				for (int i=0; i<iTopExpCount; ++i)
				{
					if (pChild_.SetColumn(iTopStartIdx + i, pTopStart, pArrBlockTop_[iTopStartIdx + i]))
					{
						if (pTopStart == pArrBlockBottom_[iTopStartIdx + i])
						{
							pArrBlockBottom_[iTopStartIdx + i] = null;
							pArrBlockTop_[iTopStartIdx + i] = null;
						}
						else
						{
							pArrBlockTop_[iTopStartIdx + i] = pTopStart.pBottom_;
						}
					}
					pChild_.LockBlock(pTopStart);
					pTopStart = pTopStart.pRight_;
				}
			}
			
			if (bMatchTop)
			{
				if (pChild_.SetColumn(iCol, pBlockTop, pArrBlockTop_[iCol]))
				{
					if (pBlockTop == pArrBlockBottom_[iCol])
					{
						pArrBlockBottom_[iCol] = null;
						pArrBlockTop_[iCol] = null;
					}
					else
					{
						pArrBlockTop_[iCol] = pBlockTop.pBottom_;
					}
				}
				pChild_.LockBlock(pBlockTop);
				pChild_.LockBlock(pBlockTop.pTop_);
				pChild_.LockBlock(pBlockTop.pTop_.pTop_);
			}
		}
		
		if (bRservationResult)
			bResult = true;
		return bResult;
	}
	
	public void MoveGroup(float fDeltaTime)
	{
		float fMoveY = 0;
		if (eState_ == kGroupState.Rise)
		{
			// MovePhase 관리
			fMovePhaseTimer_ += fDeltaTime;
			if (fMovePhaseTimer_ >= GameSetting.RISE_GRAPH_TIMEGAP)
			{
				fMovePhaseTimer_ = 0.0f;
				++iMovePhase_;
				if (iMovePhase_ >= GameSetting.RISE_GRAPH_SIZE)
				{
					iMovePhase_ = 0;
					fMoveSpeed_ = 0.0f;
					eState_ = kGroupState.FloatStop;
					return;		
				}
				else
				{
					float fMass = (float)iBlockCount_ * GameSetting.BLOCK_MASS;
					fMoveSpeed_ = GameSetting.RISE_SPEED_GRAPH[iMovePhase_] * (1.0f - fMass);
				}		
			}
			
			fMoveY = fMoveSpeed_ * fDeltaTime;
			
			// Group Collision Check
			Group pMergeTarget = null;
			CollisionCheck(ref pMergeTarget, ref fMoveY);
			MoveAllBlock(fMoveY);

			// Merge
			if (pMergeTarget != null)
			{
				pMergeTarget.SwallowLowerGroup(this);
				if (pMergeTarget.eState_ == kGroupState.Landing)
				{
					pMergeTarget.RiseStart();
				}
				else
				{
					pMergeTarget.RiseStartShort();
				}
			}
			
			// Top Block Destroy Check
			BlockDestroyCheck();
		}
		else if (eState_ == kGroupState.FloatStop)
		{
			fFloatStopTime_ -= fDeltaTime;
			if (fFloatStopTime_ <= 0)
			{
				eState_ = kGroupState.FloatFall;
				fFloatStopTime_ = GameSetting.FLOAT_STOP_TIME;
				fMoveSpeed_ = GameSetting.GROUP_FALL_SPEED;
			}
		}
		else if (eState_ == kGroupState.FloatFall)
		{
			fMoveY += fMoveSpeed_ * fDeltaTime;

			// Group 내의 pArrBlockBottom 에 등록된 어느 Block 이든지 하나만 골라서 검사하면 된다. 존재하는 Block 을 찾아낸다.
			int iIdx = -1;
			for (int i=0; i<GameSetting.BLOCK_COL; ++i)
			{
				if (pArrBlockBottom_[i] != null)
				{
					iIdx = i;
					break;
				}
			}

			// 존재하는 Bottom Block 으로 충돌 검사 및 이동
			if (iIdx != -1)
			{
				float fMyY = pArrBlockBottom_[iIdx].pMyTransform_.position.y;
				Block pTarget = pArrBlockBottom_[iIdx].pBottom_;
				if (pTarget == null)
				{
					if (fMyY + fMoveY <= GameSetting.BLOCK_END_Y)
					{
						MoveAllBlock(GameSetting.BLOCK_END_Y - fMyY);
						Landing();
					}
					else
					{
						MoveAllBlock(fMoveY);
					}
				}
				else
				{
					if (pTarget.pMyGroup_ == GameManager.pShared.pMainGroup_ || pTarget.pMyGroup_.eState_ == kGroupState.Landing)
					{
						float fGap = fMyY + fMoveY - pTarget.pMyTransform_.position.y;
						if (fGap <= GameSetting.BLOCK_HEIGHT)
						{
							MoveAllBlock(GameSetting.BLOCK_HEIGHT - fGap + fMoveY);
							Landing();
						}
						else
						{
							MoveAllBlock(fMoveY);
						}
					}
					else
					{
						MoveAllBlock(fMoveY);
					}
				}
			}
			
		}
		
		if (pChild_ != null)
			pChild_.MoveGroup(fDeltaTime);
	}
	
	void MoveAllBlock(float fMoveY)
	{
		// 그룹내의 모든 Block 을 이동시키는 함수
		for (int i=0; i<GameSetting.BLOCK_COL; ++i)
		{
			if (pArrBlockBottom_[i] != null)
				pArrBlockBottom_[i].ChainMoveY(fMoveY);
		}
	}

	bool CollisionCheck(ref Group pGroup, ref float fMoveY)
	{
		// 그룹이 상승할 때 상단 부분에 부딪히는 그룹이 없는지 체크하는 함수. 가장 크게 겹치는 그룹을 찾아낸다.
		bool bResult = false;
		float fMyY;
		float fTargetY;
		float fGap;
		for (int i=0; i<GameSetting.BLOCK_COL; ++i)
		{
			if (pArrBlockTop_[i] != null && pArrBlockTop_[i].pTop_ != null)
			{
				fMyY = pArrBlockTop_[i].pMyTransform_.position.y;
				fTargetY = pArrBlockTop_[i].pTop_.pMyTransform_.position.y;
				fGap = fTargetY - fMyY - fMoveY;

				if (fGap <= GameSetting.BLOCK_HEIGHT)
				{
					fMoveY = fTargetY - GameSetting.BLOCK_HEIGHT - fMyY;
					pGroup = pArrBlockTop_[i].pTop_.pMyGroup_;
					bResult = true;
				}
			}
		}
		return bResult;
	}
	
	public void Merge(Group pTarget)	// this is base. this swallow pTarget
	{	
		// 두 그룹을 합치는 함수. pTarget 을 흡수한다.
		pListLockBlock_.AddRange(pTarget.pListLockBlock_);

		// 겹치는 column 이 있는지 탐색한다.
		int iIdx = -1;
		for (int i=0; i<GameSetting.BLOCK_COL; ++i)
		{
			if (pTarget.pArrBlockBottom_[i] != null && pArrBlockBottom_[i] != null)
			{
				iIdx = i;
				break;
			}
		}
		
		if (iIdx == -1)																// 두 그룹이 완전히 분리되어있는 경우이다.
		{
			for (int i=0; i<GameSetting.BLOCK_COL; ++i)
			{
				if (pTarget.pArrBlockBottom_[i] != null)
				{
					pArrBlockBottom_[i] = pTarget.pArrBlockBottom_[i];
					pArrBlockTop_[i] = pTarget.pArrBlockTop_[i];
					pArrBlockBottom_[i].ChainChangeMyGroup(this, pTarget);
				}
			}
		}
		else
		{
			if (pArrBlockBottom_[iIdx].pBottom_ == pTarget.pArrBlockTop_[iIdx])		// UpperGroup = this , LowerGroup = pTarget
			{
				for (int i=0; i<GameSetting.BLOCK_COL; ++i)
				{
					if (pTarget.pArrBlockBottom_[i] != null)
					{
						if (pArrBlockBottom_[i] != null)
						{
							pArrBlockBottom_[i] = pTarget.pArrBlockBottom_[i];
							pArrBlockBottom_[i].ChainChangeMyGroup(this, pTarget);
						}
						else
						{
							pArrBlockBottom_[i] = pTarget.pArrBlockBottom_[i];
							pArrBlockTop_[i] = pTarget.pArrBlockTop_[i];
							pArrBlockBottom_[i].ChainChangeMyGroup(this, pTarget);
						}
					}
				}
			}
			else																	// UpperGroup = pTarget, LowerGroup = this
			{
				for (int i=0; i<GameSetting.BLOCK_COL; ++i)
				{
					if (pTarget.pArrBlockBottom_[i] != null)
					{
						if (pArrBlockBottom_[i] != null)
						{
							pArrBlockTop_[i] = pTarget.pArrBlockTop_[i];
							pTarget.pArrBlockBottom_[i].ChainChangeMyGroup(this, pTarget);
						}
						else
						{
							pArrBlockBottom_[i] = pTarget.pArrBlockBottom_[i];
							pArrBlockTop_[i] = pTarget.pArrBlockTop_[i];
							pArrBlockBottom_[i].ChainChangeMyGroup(this, pTarget);
						}
					}
				}
			}
		}
		
		iBlockCount_ += pTarget.iBlockCount_;

		if (pTarget.eState_ == kGroupState.Landing)
		{
			pTarget.eState_ = kGroupState.Stop;			// prevent LandComplete Coroutine
		}

		pTarget.DestroyGroup();
	}
	
	void SwallowLowerGroup(Group pTarget)
	{
		// 아래쪽에 있는 그룹을 흡수하는 함수
		pListLockBlock_.AddRange(pTarget.pListLockBlock_);

		iBlockCount_ += pTarget.iBlockCount_;
		for (int i=0; i<GameSetting.BLOCK_COL; ++i)
		{
			if (pTarget.pArrBlockBottom_[i] != null)
			{
				pTarget.pArrBlockBottom_[i].ChainChangeMyGroup(this, pTarget);
				if (pArrBlockBottom_[i] == null)
					pArrBlockTop_[i] = pTarget.pArrBlockTop_[i];
				pArrBlockBottom_[i] = pTarget.pArrBlockBottom_[i];
			}
		}
		pTarget.DestroyGroup();
	}

	void SwallowUpperGroup(Group pTarget)
	{
		// 위쪽에 있는 그룹을 흡수하는 함수
		iBlockCount_ += pTarget.iBlockCount_;
		for (int i=0; i<GameSetting.BLOCK_COL; ++i)
		{
			if (pTarget.pArrBlockBottom_[i] != null)
			{
				pTarget.pArrBlockBottom_[i].ChainChangeMyGroup(this, pTarget);
				if (pArrBlockBottom_[i] == null)
					pArrBlockBottom_[i] = pTarget.pArrBlockBottom_[i];
				pArrBlockTop_[i] = pTarget.pArrBlockTop_[i];
			}
		}
		pTarget.DestroyGroup();
	}
	
	void Landing()
	{
		// 그룹이 착지하였을 때 호출되는 함수

		// Rise Effect 제거
		int iSize = pListLockBlock_.Count;
		for (int i=0; i<iSize; ++i)
		{
			GameManager.pShared.RemoveBlockRiseEffect(pListLockBlock_[i]);
		}

		// 착지 이펙트 발생
		iSize = GameSetting.BLOCK_COL;
		for (int i=0; i<iSize; ++i)
		{
			if (pArrBlockBottom_[i] != null)
				GameManager.pShared.ShowBlockLandEffect(pArrBlockBottom_[i], i);
		}

		eState_ = kGroupState.Landing;
		// 잠재적으로 explosion 가능성이 있던 매칭 정보들을 조회하여 정리한다.
		if (GameManager.pShared.CheckMatchReservation())
		{
			GameManager.pShared.CancelSelectBlock();
		}
		
		// 착지 완료를 위한 코루틴 실행
		if (eState_ == kGroupState.Landing)
			GameManager.pShared.StartCoroutine(LandComplete());
	}
	
	IEnumerator LandComplete()
	{
		// 착지 완료를 위한 함수. 착지가 완료되면 MainGroup 에게 흡수된다.
		yield return new WaitForSeconds(GameSetting.LAND_COMPLETE_TIME);
		if (eState_ == kGroupState.Landing)
		{
			eState_ = kGroupState.Stop;
			UnLockBlock();
			GameManager.pShared.pMainGroup_.SwallowUpperGroup(this);
		}
		yield break;
	}

	void BlockDestroyCheck()
	{
		// 그룹내에서 Rise 로 인하여 상단에 위치한 Block 중 파괴된 것이 있는지 체크하여 처리한다.
		Block pBlock;
		for (int i=0; i<GameSetting.BLOCK_COL; ++i)
		{
			if (pArrBlockTop_[i] != null &&
					pArrBlockTop_[i].pMyTransform_.position.y > GameSetting.BLOCK_START_Y)
			{
				pBlock = pArrBlockTop_[i];
				GameManager.pShared.ShowBlockDestroyEffect(i);
				if (pBlock == pArrBlockBottom_[i])
				{
					pArrBlockBottom_[i] = null;
					pArrBlockTop_[i] = null;
				}
				else
				{
					pArrBlockTop_[i] = pBlock.pBottom_;
				}
				
				GameManager.pShared.DestroyTopBlock(i, pBlock);
				--iBlockCount_;
				
				// BlockCount 가 0 이하이면 Group 을 파괴한다.
				if (iBlockCount_ <= 0)
				{
					// 그룹내의 모든 Block 이 파괴되어도 Landing 시 Block Swap 으로 인해 다른 그룹으로 가버린 Block 들은
					// pListLockBlock 에 남아있을 수 있다. Lock 을 풀어줘야한다.
					int iSize = pListLockBlock_.Count;
					for (int j=0; j<iSize; ++j)
					{
						if (pListLockBlock_[j] != null && pListLockBlock_[j].pMyGroup_ != this)
						{
							pListLockBlock_[j].UnLock();
							GameManager.pShared.RemoveBlockRiseEffect(pListLockBlock_[j]);
						}
					}
					pListLockBlock_.Clear();
					DestroyGroup();
				}
			}
		}
	}

	void DestroyGroup()
	{
		eState_ = kGroupState.Stop;
		if (pChild_ != null)
			pChild_.pParent_ = pParent_;
		pParent_.pChild_ = pChild_;
	}

	public void LockBlock(Block pBlock)
	{
		// show Rise effect & change BlockType to Lock
		if (!pListLockBlock_.Contains(pBlock))
			pListLockBlock_.Add(pBlock);
		GameManager.pShared.ShowBlockRiseEffect(pBlock);
		pBlock.Lock();
	}

	public void UnLockBlock()
	{
		int iSize = pListLockBlock_.Count;
		for (int i=0; i<iSize; ++i)
		{
			pListLockBlock_[i].UnLock();
		}
	}
}