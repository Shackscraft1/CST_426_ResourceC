using UnityEngine;

/*
 * Highlightable toggles the authored selection shader flag on renderers
 * under this object. PlayerController owns when target focus changes; this class
 * only applies the local cosmetic state.
 */

public class Highlightable : MonoBehaviour
{

    Renderer[] _targetRenderers;

    void Awake()
    {
        // TODO Slice 3.1: cache every child renderer and begin unselected.
        _targetRenderers = GetComponentsInChildren<Renderer>();
        
        SetHighlighted(false);
    }

    public void SetHighlighted(bool isHighlighted)
    {
        // TODO Slice 3.3: forward the requested state to ApplyHighlight. </> end of Slice 3
        foreach (var renderer in _targetRenderers)
        {
            renderer.material.SetFloat("_HIghlight_Enabled", isHighlighted ? 1f : 0f);
        }
    }
    
}
