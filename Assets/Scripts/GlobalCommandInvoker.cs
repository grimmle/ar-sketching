using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRSketchingGeometry.Commands;

public class GlobalCommandInvoker : MonoBehaviour {

    [SerializeField]
    public CommandInvoker invoker;

    void Awake() {
        invoker = new CommandInvoker();
    }
}
