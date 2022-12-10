using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VariableFrameRatePhysics;

public class SetSettingsOnStart : MonoBehaviour
{
    [SerializeField] private VariableFrameRatePhysicsSystem.FixedDeltaTimeType fixedDeltaTimeType;

    void Start()
    {
        VariableFrameRatePhysicsSystem.fixedDeltaTimeType = fixedDeltaTimeType;
    }
}
