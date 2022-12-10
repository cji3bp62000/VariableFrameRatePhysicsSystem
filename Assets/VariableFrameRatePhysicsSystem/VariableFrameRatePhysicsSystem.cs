using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using UnityEngine.Scripting;

namespace VariableFrameRatePhysics
{
    /// <summary>
    /// PlayerLoopSystem 更新処理に登録する部
    /// </summary>
    [Preserve]
    public static partial class VariableFrameRatePhysicsSystem
    {
        // マーカー
        public struct RecordCurrentDynamicUnscaledTime { }
        public struct AdjustFixedDeltaTime { }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        [Preserve]
        private static void Initialize()
        {
            SetVariableFrameRatePhysicsSystem();
            // 値の初期化
            defaultFixedDeltaTime = VariableFrameRateSettings.GetDefaultFixedDeltaTimeStep();
            fixedDeltaTimeType = VariableFrameRateSettings.GetFixedDeltaTimeType();
        }

        /// <summary>
        /// 毎フレーム fixedTime を time と同期させるように、fixedDeltaTimeの値を調整する。
        /// 考え方：deltaTime と同じになるように、 fixedDeltaTime を調整する。
        /// 例：0.051 (ms) => 0.02 + 0.02 + 0.011 (ms)
        /// </summary>
        private static void SetVariableFrameRatePhysicsSystem()
        {
            var rootPlayerLoopSystem = PlayerLoop.GetCurrentPlayerLoop();

            for (int i = 0; i < rootPlayerLoopSystem.subSystemList.Length; i++) {
                var subSystem = rootPlayerLoopSystem.subSystemList[i];
                bool isTimeUpdateSystem = subSystem.type == typeof(TimeUpdate);
                bool isFixedUpdateSystem = subSystem.type == typeof(FixedUpdate);

                if (isTimeUpdateSystem || isFixedUpdateSystem) {
                    // ※ TimeUpdate: Time.Time や Time.deltaTime を更新するシステム
                    // FixedUpdate 実行可否判定の前に fixedDeltaTime を調整する必要がありますので、TimeUpdateの直後にも処理追加
                    var updateSubSystemList = new List<PlayerLoopSystem>(subSystem.subSystemList);
                    if (isTimeUpdateSystem) {
                        // FixedUpdate 内用の Time.unscaledTime を記録するシステム
                        updateSubSystemList.Add(CreateRecordCurrentDynamicUnscaledTimeSystem());
                        updateSubSystemList.Add(CreateUpdateFixedTimeAfterTimeUpdateSystem());
                    }
                    else {
                        updateSubSystemList.Add(CreateUpdateFixedDeltaTimeOnFixedUpdateEndSystem());
                    }
                    subSystem.subSystemList = updateSubSystemList.ToArray();
                    rootPlayerLoopSystem.subSystemList[i] = subSystem;
                }
            }

            // 上記変更を適用
            PlayerLoop.SetPlayerLoop(rootPlayerLoopSystem);
        }

        /// <summary> fixedDeltaTime を deltaTime と同期させるシステムを作成 </summary>
        private static PlayerLoopSystem CreateUpdateFixedTimeAfterTimeUpdateSystem()
        {
            return new PlayerLoopSystem()
            {
                type = typeof(AdjustFixedDeltaTime),
                updateDelegate = UpdateFixedDeltaTimeAfterTimeUpdate,
            };
        }

        /// <summary> fixedDeltaTime を deltaTime と同期させるシステムを作成 </summary>
        private static PlayerLoopSystem CreateUpdateFixedDeltaTimeOnFixedUpdateEndSystem()
        {
            return new PlayerLoopSystem()
            {
                type = typeof(AdjustFixedDeltaTime),
                updateDelegate = UpdateFixedDeltaTimeOnFixedUpdateEnd,
            };
        }

        /// <summary> Time.unscaledTime を記録するシステムを作成 </summary>
        private static PlayerLoopSystem CreateRecordCurrentDynamicUnscaledTimeSystem()
        {
            return new PlayerLoopSystem()
            {
                type = typeof(RecordCurrentDynamicUnscaledTime),
                updateDelegate = SetCurrentDynamicUnscaledTime,
            };
        }
    }



    /// <summary>
    /// FixedDeltaTime 調整処理部
    /// </summary>
    public static partial class VariableFrameRatePhysicsSystem
    {
        /// <summary>
        /// fixedDeltaTime の更新タイプ
        /// </summary>
        public enum FixedDeltaTimeType
        {
            /// <summary>
            /// fixedDeltaTime 固定値; 既存方法 <br/>
            /// 1 フレームで 0~ 回 FixedUpdate <br/>
            /// 物理演算が安定している
            /// </summary>
            Fixed = 0,

            /// <summary>
            /// fixedDeltaTime = deltaTime <br/>
            /// 1 フレームで必ず 1 回 FixedUpdate <br/>
            /// ゲームの他部分と協調性が良い
            /// </summary>
            Variable = 1,

            /// <summary>
            /// deltaTime を DefaultFixedDeltaTime の値で分割して、fixedDeltaTime に設定 <br/>
            /// 1フレームで 1~ 回 FixedUpdate <br/>
            /// Fixed と Variable の折衷 (安定 & 協調性)
            /// </summary>
            VariableWithSubStep = 2,
        }


        private static float defaultFixedDeltaTime = 0.02f;
        private static readonly float checkTimeEpsilon = 0.00001f;
        private static readonly float safeMarginTimeEpsilon = checkTimeEpsilon * 0.1f;

        /// <summary>
        /// DeltaTime のモード切り替え (false = Unity デフォルトの更新処理)
        /// </summary>
        [Preserve]
        public static FixedDeltaTimeType fixedDeltaTimeType
        {
            get => _fixedDeltaTimeType;
            set {
                if (_fixedDeltaTimeType == value) return;

                _fixedDeltaTimeType = value;

                if (!Application.isPlaying) return;
                // 既存の場合、fixedDeltaTime をもとに戻す
                if (_fixedDeltaTimeType == FixedDeltaTimeType.Fixed) {
                    Time.fixedDeltaTime = Time.timeScale * defaultFixedDeltaTime;
                }
            }
        }

        private static FixedDeltaTimeType _fixedDeltaTimeType = FixedDeltaTimeType.Fixed;

        /// <summary> このフレームの Time.unscaledTime </summary>
        private static float currentDynamicUnscaledTime;


        /// <summary>
        /// TimeUpdate 後、タイプを見て fixedDeltaTime を更新する
        /// </summary>
        private static void UpdateFixedDeltaTimeAfterTimeUpdate()
        {
            if (!Application.isPlaying) return;

            switch (fixedDeltaTimeType) {
                case FixedDeltaTimeType.Variable:
                    Time.fixedDeltaTime = Time.deltaTime;
                    break;

                case FixedDeltaTimeType.VariableWithSubStep:
                    AdjustFixedDeltaTimeClamped();
                    break;

                case FixedDeltaTimeType.Fixed:
                default:
                    break;
            }
        }

        /// <summary>
        /// FixedUpdate の最後、タイプを見て fixedDeltaTime を更新する
        /// </summary>
        private static void UpdateFixedDeltaTimeOnFixedUpdateEnd()
        {
            if (!Application.isPlaying) return;

            switch (fixedDeltaTimeType) {
                case FixedDeltaTimeType.VariableWithSubStep:
                    AdjustFixedDeltaTimeClamped();
                    break;

                case FixedDeltaTimeType.Variable:
                case FixedDeltaTimeType.Fixed:
                default:
                    break;
            }
        }

        /// <summary>
        /// Time.time と Time.fixedTime が同期するように fixedDeltaTime を調整する
        /// </summary>
        private static void AdjustFixedDeltaTimeClamped()
        {
            var remainDeltaTime = currentDynamicUnscaledTime - Time.fixedUnscaledTime;
            // fixedTime と Time のが 0 、もしくはデフォルトの fixedDeltaTime 以上の場合、デフォルトの fixedDeltaTime に戻す
            if (remainDeltaTime <= checkTimeEpsilon || remainDeltaTime > defaultFixedDeltaTime) {
                Time.fixedDeltaTime = Time.timeScale * defaultFixedDeltaTime;
            }
            else {
                // 減算で float 誤差が出る可能性があり、FixedUpdate の実行判定が通らない可能性がありますので
                // 本当の deltaTime より微かに低い数値でやる
                Time.fixedDeltaTime = Time.timeScale * (remainDeltaTime - safeMarginTimeEpsilon);
            }
        }

        /// <summary>
        /// FixedUpdate 内は Time.unscaledTime は Time.fixedUnscaledTime になってしまうので、
        /// 外で Time.unscaledTime 記録しておく
        /// </summary>
        private static void SetCurrentDynamicUnscaledTime()
        {
            currentDynamicUnscaledTime = Time.unscaledTime;
        }
    }
}
