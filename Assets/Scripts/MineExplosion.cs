using UnityEngine;
using System.Collections;

public class MineExplosion : MonoBehaviour
{
    [Header("Settings")]
    public float ExplosionDelay = 0.5f;
    public float ParticleLifetime = 2f;

    [Header("Explosion Effect")]
    public Color ExplosionColor = new Color(1f, 0.5f, 0f); // orange

    private ParticleSystem explosionParticles;

    public void Explode()
    {
        // Make the mine visible
        gameObject.SetActive(true);

        // Create explosion particle effect
        CreateExplosionEffect();

        // Trigger character death after delay
        StartCoroutine(ExplosionSequence());
    }

    void CreateExplosionEffect()
    {
        GameObject particleObj = new GameObject("ExplosionVFX");
        particleObj.transform.SetParent(transform);
        particleObj.transform.localPosition = Vector3.up * 0.5f;

        explosionParticles = particleObj.AddComponent<ParticleSystem>();
        explosionParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = explosionParticles.main;
        main.duration = 0.5f;
        main.startLifetime = ParticleLifetime;
        main.startSpeed = 3f;
        main.startSize = 0.3f;
        main.startColor = ExplosionColor;
        main.maxParticles = 50;
        main.loop = false;

        var emission = explosionParticles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 30)
        });

        var shape = explosionParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        var sizeOverLifetime = explosionParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
            AnimationCurve.Linear(0f, 1f, 1f, 0f));

        // Use default particle material
        var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        renderer.material.color = ExplosionColor;

        explosionParticles.Play();
    }

    IEnumerator ExplosionSequence()
    {
        yield return new WaitForSeconds(ExplosionDelay);

        // Trigger character death animation
        CharacterMover mover = FindAnyObjectByType<CharacterMover>();
        if (mover != null)
        {
            mover.Stop();
            CharacterAnimator animator = mover.GetComponent<CharacterAnimator>();
            if (animator != null)
            {
                animator.TriggerDeath();
            }
        }

        yield return new WaitForSeconds(1.5f);

        // Notify GameManager
        GameManager.Instance.OnPlayerDied();
    }
}
