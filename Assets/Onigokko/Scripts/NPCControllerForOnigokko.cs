using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NPCControllerForOnigokko : MonoBehaviour
{
	public enum NPCState
	{
		None,
		Wait,
		Run,
		Stop,
		PreparingExitGame,
		ExitGame,
	}

	public GameObject VoiceObject;
	public List<AudioClip> StartGameVoice;
	public List<AudioClip> ExitGameVoice;
	public float EscapeDistance = 7.0f;
    public Transform Target;
    public float TargetHeightOffset;
    public float PositionHeightOffset;
	public Step<NPCState> NPCStateStep = new Step<NPCState>(NPCState.None);

	private NavMeshAgent agent;
	private EnvQuery query;
	private Animator animator;
	private AudioSource audioSourceFootStep;
	private AudioSource audioSourceVoice;

	void Start()
	{
		agent = GetComponent<NavMeshAgent>();
		query = GetComponent<EnvQuery>();
		animator = GetComponent<Animator>();
		audioSourceFootStep = GetComponent<AudioSource>();
		audioSourceVoice = VoiceObject.GetComponent<AudioSource>();

		agent.updatePosition = false;
		agent.updateRotation = true;

		NPCStateStep.SetNext(NPCState.Wait);
	}

	void Update()
	{
		// Transit the step and initialize the next step
		while(NPCStateStep.GetNext() != NPCState.None)
		{
			switch(NPCStateStep.DoTransit())
			{
				case NPCState.Wait:
					InitWait();
					break;
				case NPCState.Run:
					InitRun();
					break;
				case NPCState.Stop:
					StartCoroutine(InitStop());
					break;
				case NPCState.PreparingExitGame:
					PrepareToExitGame();
					break;
				case NPCState.ExitGame:
					ExitGame();
					break;
				default:
					break;
			}
		}

		// Update the current step
		float deltaTime = Time.deltaTime;
		switch(NPCStateStep.GetCurrent())
		{
			case NPCState.Wait:
				UpdateWait(deltaTime);
				break;
			case NPCState.Run:
				UpdateRun(deltaTime);
				break;
			case NPCState.Stop:
				UpdateStop(deltaTime);
				break;
			default:
				break;
		}
	}

	void OnTriggerEnter(Collider c)
	{
		if(GameManager.Instance.GameStateStep.GetCurrent() == GameManager.GameState.Playing)
		{
			GameManager.Instance.GameStateStep.SetNext(GameManager.GameState.PlayerWin);
		}
	}

	private void InitWait()
	{
		query.enabled = false;
		agent.destination = transform.position;
		audioSourceFootStep.Stop();
		Vector3 direction = Target.transform.position - transform.position;
		transform.forward = direction.normalized;
	}

	private void UpdateWait(float deltaTime)
	{
		bool inView = HasTargetInView();
		bool visible = HasVisibleTarget();
		query.enabled = inView && visible ? true : false;
	}

	private void InitRun()
	{
		audioSourceFootStep.PlayDelayed(0.5f);
	}

	private void UpdateRun(float deltaTime)
	{
		UpdateDesitination();
	}

	private IEnumerator InitStop()
	{
		query.enabled = false;
		agent.destination = transform.position;
		yield return new WaitForSeconds(0.5f);
		audioSourceFootStep.Stop();
	}

	private void UpdateStop(float deltaTime)
	{
		UpdateDesitination();
	}

	private void UpdateDesitination()
	{
		bool inView = HasTargetInView();
		bool visible = HasVisibleTarget();

		query.enabled = inView && visible ? true : false;

		if(query.enabled && query.BestResult != null)
		{
			agent.SetDestination(query.BestResult.GetWorldPosition());
			if(NPCStateStep.GetCurrent() == NPCState.Stop)
			{
				NPCStateStep.SetNext(NPCState.Run);
			}
		}
	}

	private bool HasTargetInView()
	{
		Vector3 direction = Target.position - transform.position;
		direction = Vector3.Cross(Vector3.Cross(transform.up, direction), transform.up); // up方向成分の除去

		Vector3 forward = transform.forward;
		forward = Vector3.Cross(Vector3.Cross(transform.up, forward), transform.up); // up方向成分の除去
		
		float dot = Vector3.Dot(direction.normalized, forward.normalized);

		bool inView = false;
		if(dot >= 0.0f && direction.magnitude < (2.0f/3.0f*dot + 1.0f/3.0f)*EscapeDistance) 
		{
			inView = true;
		}
		else if(dot < 0.0f && direction.magnitude < 1.0f/3.0f*EscapeDistance)
		{
			inView = true;
		}
		else
		{
			inView = false;
		}

		return inView;
	}

	private bool HasVisibleTarget()
	{
		Vector3 position = transform.position + Vector3.up * PositionHeightOffset;
		Vector3 target = Target.position + Vector3.up * TargetHeightOffset;
		Vector3 direction = target - position;

		RaycastHit raycastHit;
		Physics.Raycast(position, direction, out raycastHit);
		bool visible = (raycastHit.transform == Target.transform) ? true : false;

		return visible;
	}

	private void OnAnimatorMove()
	{
		Vector3 velocity = agent.desiredVelocity;
		if(agent.remainingDistance < agent.stoppingDistance)
		{
			velocity = Vector3.zero;
			if(NPCStateStep.GetCurrent() == NPCState.Run)
			{
				NPCStateStep.SetNext(NPCState.Stop);
			}
			else if(NPCStateStep.GetCurrent() == NPCState.PreparingExitGame)
			{
				NPCStateStep.SetNext(NPCState.ExitGame);
			}
		}

		Vector3 move = transform.InverseTransformDirection(velocity);
		float turnAmmount = Mathf.Atan2(move.x, move.z);
		animator.SetFloat("Forward", move.z, 0.1f, Time.deltaTime);
		animator.SetFloat("Turn", turnAmmount, 0.1f, Time.deltaTime);

		agent.nextPosition = transform.localPosition + animator.deltaPosition;
		transform.localPosition = agent.nextPosition;
	}

    private void OnAnimatorIK(int layerIndex)
    {
		if(NPCStateStep.GetCurrent() == NPCState.Stop
		|| NPCStateStep.GetCurrent() == NPCState.ExitGame)
		{
			Vector3 direction = Target.transform.position - transform.position;
			direction = Vector3.Cross(Vector3.Cross(transform.up, direction), transform.up); // up方向成分の除去
			transform.forward = direction;

			animator.SetLookAtWeight(1.0f, 0.1f, 0.9f, 0.0f, 0f);
			animator.SetLookAtPosition(Target.transform.position);
		}
		else if(NPCStateStep.GetCurrent() == NPCState.Wait)
		{
			Vector3 direction = Target.transform.position - transform.position;
			direction = Vector3.Cross(Vector3.Cross(transform.up, direction), transform.up); // up方向成分の除去
			if(Vector3.Dot(transform.forward, direction.normalized) > 0.5f) // Horizontal FoV = 120 degree.
			{
				animator.SetLookAtWeight(1.0f, 0.1f, 0.9f, 0.0f, 0f);
				animator.SetLookAtPosition(Target.transform.position);
			}
		}
    }

	private void PrepareToExitGame()
	{
		if(NPCStateStep.GetCurrent() == NPCState.PreparingExitGame)
		{
			query.enabled = false;
			Vector3 position = Target.transform.position + 2.0f*Target.transform.forward;
			agent.destination = position;
		}
	}

	private void ExitGame()
	{
		if(NPCStateStep.GetCurrent() == NPCState.ExitGame)
		{
			audioSourceFootStep.Stop();
			int index = Random.Range(0, 4);
			index = (index < 3) ? 0 : 1; // 0: 75%, 1: 25%
			string animationName = "ExitGame" + index;
			audioSourceVoice.clip = ExitGameVoice[index];
			animator.Play(animationName);
			audioSourceVoice.Play();
		}
	}

	public void StartVoice()
	{
		if(NPCStateStep.GetCurrent() == NPCState.Wait)
		{
			audioSourceFootStep.Stop();
			int index = Random.Range(0, 3);
			audioSourceVoice.clip = StartGameVoice[index];
			audioSourceVoice.PlayDelayed(0.3f);
		}
	}
}
