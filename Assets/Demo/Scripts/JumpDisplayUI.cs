using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;

public class JumpDisplayUI : MonoBehaviour
{
    [SerializeField] private SimpleMove simpleMove;

    [SerializeField] private GameObject jumpUI;

    [SerializeField] private float showTime;
    // Start is called before the first frame update
    void Start()
    {
        simpleMove.OnJump
            .Subscribe(_ => ShowJump().Forget())
            .AddTo(this);
    }

    private async UniTask ShowJump()
    {
        jumpUI.SetActive(true);
        await UniTask.Delay(TimeSpan.FromSeconds(showTime), cancellationToken:this.GetCancellationTokenOnDestroy());
        jumpUI.SetActive(false);
    }
}
