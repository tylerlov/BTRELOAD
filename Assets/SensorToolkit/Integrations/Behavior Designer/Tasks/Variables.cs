using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

namespace Micosmo.SensorToolkit.BehaviorDesigner {

    public class SharedSensor : SharedVariable<BasePulsableSensor> {
        public static implicit operator SharedSensor(BasePulsableSensor value) => new SharedSensor { Value = value };
    }

    public class ObjectTypeAttribute : ObjectDrawerAttribute {
        public Type ObjectType { get; }
        public ObjectTypeAttribute(Type objectType) {
            ObjectType = objectType;
        }
    }

}