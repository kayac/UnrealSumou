using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class Lighting : ManualUpdatableMonoBehaviour
{
	[SerializeField] float particleStrenghtThreshold = 100f;
	[SerializeField] float hitFlashIntensity;
	[SerializeField] float hitFlashDecaySpeed;
	[SerializeField] int hitEffectPoolCount;
	[SerializeField] Color[] hitFlashColors;
	[SerializeField] Light mainLight;
	[SerializeField] Light pointLight;
	[SerializeField] VisualEffect _weakHitVFXPrefab;

	public void FlashPointLight(Vector3 position, float strength, bool is2p)
	{
		pointLight.transform.position = position;
		pointLight.intensity += strength * hitFlashIntensity;
		pointLight.color = is2p ? hitFlashColors[1] : hitFlashColors[0];

		if (strength >= particleStrenghtThreshold)
		{
			if (pooledHitEffects == null)
			{
				pooledHitEffects = new List<VisualEffect>();
			}

			int effectIndex;
			if (pooledHitEffects.Count >= hitEffectPoolCount) // 使い回す
			{
				effectIndex = nextHitEffectIndex;
			}
			else
			{
				effectIndex = pooledHitEffects.Count;
				pooledHitEffects.Add(Instantiate(_weakHitVFXPrefab, transform, false));			
			}
			var effect = pooledHitEffects[effectIndex];
			effect.transform.position = position;
			effect.SetBool("Red", is2p);
			effect.Play();
			effectIndex++;
			if (effectIndex >= hitEffectPoolCount)
			{
				effectIndex = 0;
			}
		}
	}

	public override void ManualUpdate(float deltaTime)
	{
		pointLight.intensity *= (1f - (hitFlashDecaySpeed * deltaTime));
	}

	// non public -------
	List<VisualEffect> pooledHitEffects;
	int nextHitEffectIndex;
}
