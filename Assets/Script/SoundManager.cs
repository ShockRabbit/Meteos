using UnityEngine;
using System.Collections;

public enum SeType						// Sound Effect Type
{
	BlockRise,					
	BlockLanding,				
	BlockDestroy,				
	BlockGameOver,				// 게임오버 연출에서 블럭이 Lock 될 때 효과음
	GameOverAlert,				
	GameOver					
}

public class SoundManager : MonoBehaviour {
	private static SoundManager pShared_ = null;
	public static SoundManager pShared
	{
		get { return pShared_; }
	}
	[SerializeField]
	private AudioSource pAudioSource_;
	[SerializeField]
	private AudioClip[] pArrAudioSe_;				// Se = Sound Effect

	void Awake()
	{
		if (pShared_ == null)
			pShared_ = this;
	}

	public void PlaySe(SeType eSeType)
	{
		pAudioSource_.PlayOneShot(pArrAudioSe_[(int)eSeType]);
	}
}
