using UnityEngine;
using UnityEngine.Events;

/*
 * AnimationEvents is only a receiver for imported animation clip events on the
 * model child. Gameplay interaction is instant in PlayerController; these
 * methods remain so the imported clips do not log missing receivers.
 */

public class AnimationEvents : MonoBehaviour
{
    public UnityEvent OnStep;

    public void ChopAction() { }

    public void AnimationDone() { }

    public void Interact() { }

    public void Step()
    {
        OnStep?.Invoke();
    }
}
