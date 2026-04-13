using UnityEngine;

public class UIFade : MonoBehaviour
{
    public static UIFade Instance;

    private static readonly int FADE_OUT_TRIGGER_STRING_HASH = Animator.StringToHash("FadeOut");
    private static readonly int FADE_IN_TRIGGER_STRING_HASH = Animator.StringToHash("FadeIn");

    private Animator animator;

    private void Start()
    {
        if (Instance == null) {
            Instance = this;
        }

        animator = GetComponent<Animator>();
    }

    public void FadeToBlack()
    {
        animator.SetTrigger(FADE_OUT_TRIGGER_STRING_HASH);
    }

    public void FadeToClear()
    {
        animator.SetTrigger(FADE_IN_TRIGGER_STRING_HASH);
    }
}
