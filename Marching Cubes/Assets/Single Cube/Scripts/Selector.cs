using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

public class Selector : MonoBehaviour
{
    [Header("Settings for selector object")]
    public bool state = false;
    public Material onMaterial;
    public Material offMaterial;

    private bool newState;
    private MeshRenderer renderer;

    private void Start()
    {
        renderer = transform.GetComponent<MeshRenderer>();
    }

    // Update the selector's state
    private void OnMouseDown()
    {
        newState = !newState;

        if (newState == true) this.renderer.material = onMaterial;
        else                  this.renderer.material = offMaterial;
    }

    // Return wether it's been updated or not
    public bool hasUpdated ()
    {
        if (state != newState)
        {
            state = newState;
            return true;
        } else
        {
            return false;
        }
    }
}
