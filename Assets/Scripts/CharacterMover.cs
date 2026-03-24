using System;
using UnityEngine;
using UnityEngine.AI;

public class CharacterMover : MonoBehaviour
{
    public event Action OnStartedMoving;
    public event Action OnArrived;

    [Header("Settings")]
    public float StoppingDistance = 0.1f;

    private NavMeshAgent agent;
    private CharacterAnimator characterAnimator;
    private bool isMoving;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        characterAnimator = GetComponent<CharacterAnimator>();

        if (agent != null)
        {
            agent.stoppingDistance = StoppingDistance;
        }
    }

    void Update()
    {
        if (agent == null) return;

        if (isMoving && !agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                isMoving = false;
                if (characterAnimator != null)
                    characterAnimator.SetSpeed(0f);
                OnArrived?.Invoke();
            }
        }
    }

    public void MoveTo(Vector3 targetPosition)
    {
        if (agent == null) return;

        agent.SetDestination(targetPosition);
        isMoving = true;

        if (characterAnimator != null)
            characterAnimator.SetSpeed(1f);

        OnStartedMoving?.Invoke();
    }

    public bool IsMoving()
    {
        return isMoving;
    }

    public void Stop()
    {
        if (agent != null)
        {
            agent.ResetPath();
        }
        isMoving = false;
        if (characterAnimator != null)
            characterAnimator.SetSpeed(0f);
    }
}
