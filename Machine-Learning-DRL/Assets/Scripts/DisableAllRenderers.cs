using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class DisableAllRenderers : MonoBehaviour
{
    public bool a = false;
    public bool switchbool;
    void Start()
    {
        switchbool = true;
        // Find all MeshRenderer components in the scene
        MeshRenderer[] meshRenderers = FindObjectsOfType<MeshRenderer>();

        // Disable each MeshRenderer
        foreach (MeshRenderer renderer in meshRenderers)
        {
            renderer.enabled = false;
        }
    }

    private void Update()
    {
        if (a)
        {
            // Find all MeshRenderer components in the scene
            MeshRenderer[] meshRenderers = FindObjectsOfType<MeshRenderer>();
             switchbool = !switchbool;
             
            // Disable each MeshRenderer
            foreach (MeshRenderer renderer in meshRenderers)
            {
                renderer.enabled = switchbool;
            }

            a = false;
        }
    }
}
