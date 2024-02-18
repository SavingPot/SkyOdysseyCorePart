using UnityEngine;

namespace SP.Tools.Unity
{
    public static class UAnimatorExtensions
    {
        public static void AddFloat(this Animator animator, string animName, float value) => animator.SetFloat(animName, animator.GetFloat(animName) + value);
    }
}
