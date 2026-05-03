using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Equipment : MonoBehaviour
{
    public Transform parent_transform;

    virtual public void Activate(bool inputPress, bool inputHeld) { }
    virtual public void OnSelect() { } // should be used to activate the gameobject + other behavior when deactivating
    virtual public void OnDeselect() { } // should be used to deactivate the game object + other behavior when deactivating
}
