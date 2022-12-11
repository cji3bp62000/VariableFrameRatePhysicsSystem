# VariableFrameRatePhysicsSystem
VariableFrameRatePhysicsSystem は、FixedUpdate の時間幅（Time.fixedDeltaTime）を可変にすることで、<br/>
様々な `Rigidbody`・`FixedUpdate` での不具合を解消しようとする提案手法です。

詳しくは Qiita の紹介記事をご覧ください。<br/>
リンク→ https://qiita.com/tsukimi_neko/items/acf2d1dce01ea97885cf
<br/><br/>

# DL 及び使い方

右側の `Releases` から UnityPackage をダウンロードして、プロジェクトにインポートすれば完成です！ いわゆる Plug and Play で、特に設定も要りません。
<br/><br/>

# VariableFrameRatePhysicsSystem の出来ること

VariableFrameRatePhysicsSystem は下記の不具合を解消しています：

- `Rigidbody` を追従するカメラのカクつき
- `FixedUpdate` の中で、`Input.GetKeyDown()`, `Input.GetKeyUp()` が時々取れない不具合

具体的に、毎フレーム Time.fixedDeltaTime を Time.deltaTime に合わせています。
<br/><br/>

# 3 つの更新手法

VariableFrameRatePhysicsSystem は 3 つの FixedDeltaTimeType を提供しています。それぞれの詳細は下記になります：

- `Fixed`：
  - Unity 本家の手法。fixedDeltaTime は固定で 0.02(s) で更新していきます
  - 毎フレームは **0** 回以上 `FixedUpdate` を実行する
- `Variable`：
  - Qiita 記事で紹介した手法、fixedDeltaTime = deltaTime
  - 例：deltaTime が 0.017ms になったら、fixedDeltaTime も 0.017 にする
  - 毎フレームは必ず **1** 回 `FixedUpdate` を実行する
- `VariableWithSubStep`：
  - `Variable` と同じく、fixedDeltaTime = deltaTime ですが、deltaTime が大きすぎた場合、物理演算の精度が落ちるので<br/>その場合はデフォルトの fixedDeltaTime 設定値で分割して、複数回に渡って物理シミュレーションを行う
  - 例：デフォルト fixedDeltaTime の 20ms で、
    - deltaTime が <font color=#CC0000>16ms</font> の場合、1 フレームは <font color=#4444EE>16ms の合計 1 回 FixedUpdate を実行</font>
    - deltaTime が <font color=#CC0000>33ms</font> の場合、1 フレームは <font color=#4444EE>20 + 13 ms の合計 2 回 FixedUpdate を実行</font>
  - 毎フレームは **1** 回以上 `FixedUpdate` を実行する

デフォルトの設定は「`Variable`」です。
<br/><br/>

# 手法設定
ゲーム開始時のデフォルト手法が設定出来ます。
UnityPackage インポート後、Assets > VariableFrameRatePhysicsSystem > Resources にある ScriptableObject で、開始時の fixedDeltaTime 設定を選択できます。
![Settings](https://user-images.githubusercontent.com/34641639/206865239-5a73907a-2980-4456-86aa-3808b2206648.png)


また、ゲーム中でも、下記のようなコードで、リアルタイムで設定を切り替えられます。

```C#
using VariableFrameRatePhysics;

... ...

private void SetDeltaTimeType()
{
    // Fixed にしたい場合 (Unity 元々の手法)
    VariableFrameRatePhysicsSystem.fixedDeltaTimeType = VariableFrameRatePhysicsSystem.FixedDeltaTimeType.Fixed;

    // Variable にしたい場合 (fixedDeltaTime と deltaTime を合わせる手法)
    VariableFrameRatePhysicsSystem.fixedDeltaTimeType = VariableFrameRatePhysicsSystem.FixedDeltaTimeType.Variable;

    // VariableWithSubStep にしたい場合
    VariableFrameRatePhysicsSystem.fixedDeltaTimeType = VariableFrameRatePhysicsSystem.FixedDeltaTimeType.VariableWithSubStep;
}
```
