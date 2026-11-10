using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Door : MonoBehaviour
{
    Animator _animator;

    void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void Open()
    {
        _animator.SetTrigger("Open");
    }

    public void Release()
    {
        if (TryGetComponent(out Collider body))
            body.enabled = false;
    }
}
