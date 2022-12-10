using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class AutoMove : MonoBehaviour
{
    [SerializeField] private float moveSpeed;
    [SerializeField] private Vector3 forward;
    [Space(10f)]
    [SerializeField] private bool loop;
    [SerializeField] private Transform moveStart;
    [SerializeField] private Transform moveEnd;

    void Start()
    {
        if (loop) {
            LoopMoveAsync().Forget();
        }
    }

    private async UniTask LoopMoveAsync()
    {
        var ct = this.GetCancellationTokenOnDestroy();
        while (!ct.IsCancellationRequested) {
            // 往路
            await SimpleMoveToAsync(moveEnd.position, ct);

            // 復路
            await SimpleMoveToAsync(moveStart.position, ct);
        }
    }

    private async UniTask SimpleMoveToAsync(Vector3 goal, CancellationToken ct)
    {
        float moveTime = 1f, currentTime = 0f;
        float velocity = (goal - transform.position).magnitude / moveTime;
        while (currentTime < moveTime) {
            transform.position =
                Vector3.MoveTowards(transform.position, goal, velocity * Time.deltaTime);
            currentTime = Mathf.Min(Time.deltaTime + Time.deltaTime, moveTime);

            await UniTask.Yield(ct);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (loop) return;

        transform.position += forward * moveSpeed * Time.deltaTime;
    }
}
