using UnityEngine;
using System.Collections;

public class Block : MonoBehaviour {
	public enum kBlockState {Stop, Fall}
	public enum kBlockType {Blue=0, Green, Red, Pink, Orange, Yellow, Lock}
	public GameObject 		pMyGameObj_;
	public Transform 		pMyTransform_;
	public SpriteRenderer 	pMyRenderer_;
	public kBlockState 		eState_ {get; set;}
	public kBlockType 		eType_ {get; set;}
	public Block 			pTop_ {get; set;}
	public Block 			pBottom_ {get; set;}
	public Block 			pLeft_ {get; set;}
	public Block 			pRight_ {get; set;}
	public Group 			pMyGroup_ {get; set;}
	public ParticleSystem 	pPtcRiseEffect_ {get; set;}
	
	public void SetBlock(kBlockType eType, int Col)
	{
		// Block 생성시 사용되는 함수
		pMyGameObj_.SetActive(true);

		pLeft_ = null;
		pRight_ = null;
		pTop_ = null;
		pBottom_ = null;

		pPtcRiseEffect_ = null;

		eType_ = eType;
		eState_ = kBlockState.Fall;
		pMyRenderer_.sprite = GameManager.pShared.pArrImgBlocks_[(int)eType_];
		pMyTransform_.position = new Vector3(GameSetting.BLOCK_START_X + 128.0f * (float)Col, 
														GameSetting.BLOCK_START_Y, 
														0.0f);
	}

	public void GameOver()
	{
		pMyRenderer_.sprite = GameManager.pShared.pArrImgBlocks_[(int)kBlockType.Lock];
	}

	public void Lock()
	{
		eType_ = kBlockType.Lock;
		pMyRenderer_.sprite = GameManager.pShared.pArrImgBlocks_[(int)eType_];
	}

	public void UnLock()
	{
		eType_ = GameManager.pShared.GetGenBlockType(this);
		pMyRenderer_.sprite = GameManager.pShared.pArrImgBlocks_[(int)eType_];
	}
	
	public void Swap(Block pTargetBottom, int iCol)
	{
		// Pointer Exchange
		Block pTempBottom = pTargetBottom.pBottom_;
		Block pTempLeft = pTargetBottom.pLeft_;
		Block pTempRight = pTargetBottom.pRight_;

		pTargetBottom.pTop_ = pTop_;
		pTargetBottom.pBottom_ = this;
		pTargetBottom.pLeft_ = pLeft_;
		pTargetBottom.pRight_ = pRight_;

		pTop_ = pTargetBottom;
		pBottom_ = pTempBottom;
		pLeft_ = pTempLeft;
		pRight_ = pTempRight;

		// Top, Bottom, Left, Right's Pointer Edit
		if (pBottom_ != null)
			pBottom_.pTop_ = this;
		if (pLeft_ != null)
			pLeft_.pRight_ = this;
		if (pRight_ != null)
			pRight_.pLeft_ = this;

		if (pTargetBottom.pTop_ != null)
			pTargetBottom.pTop_.pBottom_ = pTargetBottom;
		else
			GameManager.pShared.SetTopBlock(iCol, pTargetBottom);
		if (pTargetBottom.pRight_ != null)
			pTargetBottom.pRight_.pLeft_ = pTargetBottom;
		if (pTargetBottom.pLeft_ != null)
			pTargetBottom.pLeft_.pRight_ = pTargetBottom;

		// Position Exchange
		Vector3 pTempPos = pTargetBottom.pMyTransform_.position;
		pTargetBottom.pMyTransform_.position = pMyTransform_.position;
		pMyTransform_.position = pTempPos;
		
		// Group Exchange
		if (pMyGroup_ != pTargetBottom.pMyGroup_)
		{
			pTargetBottom.pMyGroup_.SetBlockTop(iCol, this, pTargetBottom);
			pMyGroup_.SetBlockBottom(iCol, pTargetBottom, this);
			Group pTempGroup = pTargetBottom.pMyGroup_;
			pTargetBottom.pMyGroup_ = pMyGroup_;
			pMyGroup_ = pTempGroup;
		}
		else
		{
			// Organize
			pMyGroup_.OrganizeBlock(iCol, pTargetBottom, this);
		}

		// Select Area Effect Exchange
		if (pTargetBottom.pMyTransform_.childCount != 0)
			GameManager.pShared.ShowBlockSelectArea(this);
		else if (pMyTransform_.childCount != 0)
			GameManager.pShared.ShowBlockSelectArea(pTargetBottom);
	}
	
	public bool IsTouch(float fY)
	{
		float fPosY = transform.position.y;
		if (fPosY-GameSetting.BLOCK_HEIGHT_HALF < fY && fPosY+GameSetting.BLOCK_HEIGHT_HALF > fY)
			return true;
		
		return false;
	}

	public void ChainMoveY(float fMoveY)
	{
		transform.Translate(0.0f, fMoveY, 0.0f);
		if (pTop_ != null && pTop_.pMyGroup_ == pMyGroup_)
			pTop_.ChainMoveY(fMoveY);
	}
	
	public void ChainChangeMyGroup(Group pNewGroup, Group pOldGroup)
	{
		pMyGroup_ = pNewGroup;
		if (pTop_ != null && pTop_.pMyGroup_ == pOldGroup)
			pTop_.ChainChangeMyGroup(pNewGroup, pOldGroup);
	}
	
	public Block GetMyLeftTop()
	{
		if (pLeft_ == null)
			return null;
		else
			return pLeft_.pTop_;
	}
	
	public Block GetMyRightTop()
	{
		if (pRight_ == null)
			return null;
		else
			return pRight_.pTop_;
	}
	
	public void FallBlock(Block pTarget, float fMoveY, int iCol)
	{
		float fMyY = transform.position.y;
		float fTargetY; 
		if (pTarget != null)
			fTargetY = pTarget.pMyTransform_.position.y;
		else
		{
			if (fMyY + fMoveY <= GameSetting.BLOCK_END_Y)
			{
				transform.Translate(0.0f, GameSetting.BLOCK_END_Y - fMyY, 0.0f);
				eState_ = kBlockState.Stop;
				GameManager.pShared.pMainGroup_.AddBlock(iCol, this);
				GameManager.pShared.ArriveFallBlock(iCol, this);
				GameManager.pShared.ShowBlockLandEffect(this, iCol);
			}	
			else
				transform.Translate(0.0f, fMoveY, 0.0f);
			
			return;	
		}	
		float fGap = fMyY + fMoveY - fTargetY;
		if (fGap <= GameSetting.BLOCK_HEIGHT)
		{
			switch (pTarget.eState_)
			{
				case kBlockState.Stop:
				eState_ = kBlockState.Stop;
				if (pTarget.pMyGroup_.eState_ == Group.kGroupState.Rise)
				{
					// Destroy
					GameManager.pShared.ShowBlockDestroyEffect(this, iCol);
					GameManager.pShared.DestroyFallBlock(iCol, this);
				}
				else
				{
					GameManager.pShared.ShowBlockLandEffect(this, iCol);
					transform.Translate(0.0f, GameSetting.BLOCK_HEIGHT - fGap + fMoveY, 0.0f);
					pTarget.pMyGroup_.AddBlock(iCol, this);
					GameManager.pShared.ArriveFallBlock(iCol, this);
				}
				
				break;
			}
		}
		else
		{
			transform.Translate(0.0f, fMoveY, 0.0f);
		}
	}
	
	public bool MatchCheckTop(int iCol)
	{
		if (eType_ == kBlockType.Lock)
			return false;
		// BlockTop's Top Check
		if (pTop_ && pTop_.pTop_
			&& eType_ == pTop_.eType_ 
			&& eType_ == pTop_.pTop_.eType_)			
		{
			if (pTop_.pMyGroup_ == pMyGroup_ 
				&& pTop_.pTop_.pMyGroup_ == pMyGroup_)					
			{
				return true;
			}
			else
			{
				MatchInfo pInfo;
				
				pInfo.pPointBlock_ = this;
				pInfo.eDirection_ = MatchInfo.kMatchDirection.Top;
				pInfo.iCol_ = iCol;
				
				GameManager.pShared.AddMatchReservation(pInfo);
				
				return false;
			}
		}
		
		return false;
	}
	
	public MatchInfo.kMatchState MatchCheckTopForReservation(int iCol)
	{
		if (eType_ == kBlockType.Lock)
			return MatchInfo.kMatchState.Break;

		if (pTop_ && pTop_.pTop_
			&& eType_ == pTop_.eType_ 
			&& eType_ == pTop_.pTop_.eType_)			
		{
			if ((pMyGroup_.eState_ == Group.kGroupState.Landing
				|| pMyGroup_.eState_ == Group.kGroupState.Stop)
				&& (pTop_.pMyGroup_.eState_ == Group.kGroupState.Landing
				|| pTop_.pMyGroup_.eState_ == Group.kGroupState.Stop)
				&& (pTop_.pTop_.pMyGroup_.eState_ == Group.kGroupState.Landing
				|| pTop_.pTop_.pMyGroup_.eState_ == Group.kGroupState.Stop))
				{
					Group pGroup = pTop_.pTop_.pMyGroup_;
					pGroup.AddColumn(iCol, this);
					pGroup.RiseReservation();
					pGroup.LockBlock(this);
					pGroup.LockBlock(pTop_);
					pGroup.LockBlock(pTop_.pTop_);
					
					return MatchInfo.kMatchState.Explosion;
				}
				
			return MatchInfo.kMatchState.Keep;
		}
		
		return MatchInfo.kMatchState.Break;
	}
	
	public bool MatchCheckBottom(int iCol)
	{
		if (eType_ == kBlockType.Lock)
			return false;
		
		if (pBottom_ && pBottom_.pBottom_
			&& eType_ == pBottom_.eType_ 
			&& eType_ == pBottom_.pBottom_.eType_)
		{
			if (pBottom_.pMyGroup_ == pMyGroup_ 
				&& pBottom_.pBottom_.pMyGroup_ == pMyGroup_)
			{
				return true;
			}
			else
			{
				MatchInfo pInfo;
				
				pInfo.pPointBlock_ = this;
				pInfo.eDirection_ = MatchInfo.kMatchDirection.Bottom;
				pInfo.iCol_ = iCol;
				
				GameManager.pShared.AddMatchReservation(pInfo);
				
				return false;
			}
		}
		
		return false;
	}
	
	public MatchInfo.kMatchState MatchCheckBottomForReservation(int iCol)
	{
		if (eType_ == kBlockType.Lock)
			return MatchInfo.kMatchState.Break;

		if (pBottom_ && pBottom_.pBottom_
			&& eType_ == pBottom_.eType_ 
			&& eType_ == pBottom_.pBottom_.eType_)			
		{
			if ((pMyGroup_.eState_ == Group.kGroupState.Landing
				|| pMyGroup_.eState_ == Group.kGroupState.Stop)
				&& (pBottom_.pMyGroup_.eState_ == Group.kGroupState.Landing
				|| pBottom_.pMyGroup_.eState_ == Group.kGroupState.Stop)
				&& (pBottom_.pBottom_.pMyGroup_.eState_ == Group.kGroupState.Landing
				|| pBottom_.pBottom_.pMyGroup_.eState_ == Group.kGroupState.Stop))
				{
					Group pGroup = pMyGroup_;
					pGroup.AddColumn(iCol, pBottom_.pBottom_);
					pGroup.RiseReservation();
					pGroup.LockBlock(this);
					pGroup.LockBlock(pBottom_);
					pGroup.LockBlock(pBottom_.pBottom_);
					return MatchInfo.kMatchState.Explosion;
				}
				
			return MatchInfo.kMatchState.Keep;
		}
		
		return MatchInfo.kMatchState.Break;
	}
	
	public bool MatchCheckSide(out Block pStartBlock, int iCol, out int iExpCount, out int iStartIdx)
	{
		int iExpCountForReservation = 1;
	    iExpCount = 1;
		iStartIdx = iCol;
		pStartBlock = this;

		if (eType_ == kBlockType.Lock)
			return false;

		if (pLeft_ != null && pLeft_.eType_ == eType_)
		{
			++iExpCountForReservation;
			if (pLeft_.pMyGroup_ == pMyGroup_)
			{
				pStartBlock = pLeft_;
				--iStartIdx;
				++iExpCount;
				if (pStartBlock.pLeft_ != null && pStartBlock.pLeft_.eType_ == eType_)
				{
					++iExpCountForReservation;
					if (pStartBlock.pLeft_.pMyGroup_ == pStartBlock.pMyGroup_)
					{
						pStartBlock = pStartBlock.pLeft_;
						--iStartIdx;
						++iExpCount;	
					}
				}
			}
			else
			{
				if (pLeft_.pLeft_ != null && pLeft_.pLeft_.eType_ == eType_)
				{
					++iExpCountForReservation;
				}
			}
		}

		if (pRight_ != null && pRight_.eType_ == eType_)
		{
			++iExpCountForReservation;
			if (pRight_.pMyGroup_ == pMyGroup_)
			{
				++iExpCount;
				if (pRight_.pRight_ != null && pRight_.pRight_.eType_ == eType_)
				{
					++iExpCountForReservation;
					if (pRight_.pRight_.pMyGroup_ == pMyGroup_)
					{
						++iExpCount;
					}
				}
			}
			else
			{
				if (pRight_.pRight_ != null && pRight_.pRight_.eType_ == eType_)
				{
					++iExpCountForReservation;
				}
			}
		}

		if (iExpCountForReservation >= 3 && iExpCount != iExpCountForReservation)
		{
			MatchInfo pInfo;
		
			pInfo.eDirection_ = MatchInfo.kMatchDirection.Side;
			pInfo.iCol_ = iCol;
			pInfo.pPointBlock_ = this;
			
			GameManager.pShared.AddMatchReservation(pInfo);
		}
		
		if (iExpCount >= 3)
		{
			return true;
		}
		
		return false;
	}
	
	public MatchInfo.kMatchState MatchCheckSideForReservation(int iCol)
	{
		if (eType_ == kBlockType.Lock)
			return MatchInfo.kMatchState.Break;

		int iExpCountForReservation = 1;
	    int iExpCount = 1;
		int iStartIdx = iCol;
		Block pStartBlock = this;
		MatchInfo.kMatchState eResult;
		Group pOgGroup = null;
		if (GameManager.pShared.pMainGroup_ != pMyGroup_)
			pOgGroup = pMyGroup_;
		
		// Check Point Block's Left & Left.Left
		if (pLeft_ != null && pLeft_.eType_ == eType_)
		{
			++iExpCountForReservation;
			if (pLeft_.pMyGroup_.eState_ == Group.kGroupState.Landing || pLeft_.pMyGroup_.eState_ == Group.kGroupState.Stop)
			{
				pStartBlock = pLeft_;
				--iStartIdx;
				++iExpCount;
				if (pOgGroup == null && GameManager.pShared.pMainGroup_ != pLeft_.pMyGroup_)
					pOgGroup = pLeft_.pMyGroup_;
				if (pStartBlock.pLeft_ != null && pStartBlock.pLeft_.eType_ == eType_)
				{
					++iExpCountForReservation;
					if (pStartBlock.pLeft_.pMyGroup_.eState_ == Group.kGroupState.Landing || pStartBlock.pLeft_.pMyGroup_.eState_ == Group.kGroupState.Stop)
					{
						pStartBlock = pStartBlock.pLeft_;
						--iStartIdx;
						++iExpCount;	
						if (pOgGroup == null && GameManager.pShared.pMainGroup_ != pStartBlock.pMyGroup_)
							pOgGroup = pStartBlock.pMyGroup_;
					}
				}
			}
			else
			{
				if (pLeft_.pLeft_ != null && pLeft_.pLeft_.eType_ == eType_)
				{
					++iExpCountForReservation;
				}
			}
		}
		// Check Point Block's Right & Right.Right
		if (pRight_ != null && pRight_.eType_ == eType_)
		{
			++iExpCountForReservation;
			if (pRight_.pMyGroup_.eState_ == Group.kGroupState.Landing || pRight_.pMyGroup_.eState_ == Group.kGroupState.Stop)
			{
				++iExpCount;
				if (pOgGroup == null && GameManager.pShared.pMainGroup_ != pRight_.pMyGroup_)
					pOgGroup = pRight_.pMyGroup_;
				if (pRight_.pRight_ != null && pRight_.pRight_.eType_ == eType_)
				{
					++iExpCountForReservation;
					if (pRight_.pRight_.pMyGroup_.eState_ == Group.kGroupState.Landing || pRight_.pRight_.pMyGroup_.eState_ == Group.kGroupState.Stop)
					{
						++iExpCount;
						if (pOgGroup == null && GameManager.pShared.pMainGroup_ != pRight_.pRight_.pMyGroup_)
							pOgGroup = pRight_.pRight_.pMyGroup_;
					}
					
				}
			}
			else
			{
				if (pRight_.pRight_ != null && pRight_.pRight_.eType_ == eType_)
				{
					++iExpCountForReservation;
				}
			}
		}
		// Check Result - Explosion ? Keep ? Break ?
		if (iExpCount >= 3 && pOgGroup != null && (pMyGroup_.eState_ == Group.kGroupState.Landing || pMyGroup_.eState_ == Group.kGroupState.Stop))
		{
			eResult = MatchInfo.kMatchState.Explosion;
			Block pBlock = pStartBlock;
			for (int i=0; i<iExpCount; ++i)
			{
				if (pBlock.pMyGroup_ != pOgGroup)
				{
					if (pBlock.pMyGroup_ == GameManager.pShared.pMainGroup_)
					{
						pOgGroup.AddColumn(iStartIdx + i, pBlock);
					}
					else
					{
						// Merge
						pOgGroup.Merge(pBlock.pMyGroup_);
					}
				}
				pBlock.pMyGroup_.LockBlock(pBlock);
				pBlock = pBlock.pRight_;
			}
			pOgGroup.RiseReservation();
		}
		else if (iExpCountForReservation >= 3)
		{
			eResult = MatchInfo.kMatchState.Keep;
		}
		else
		{
			eResult = MatchInfo.kMatchState.Break;
		}
		
		return eResult;
	}
}