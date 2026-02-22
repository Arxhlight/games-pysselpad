using UnityEngine;

[CreateAssetMenu(fileName = "Canvas", menuName = "ScriptableObjects/Canvas", order = 1)]
public class SO_Canvas : ScriptableObject
{
    [SerializeField] private Sprite sprite;
    [SerializeField] private Vector2Int size;
    [SerializeField] Animator animator;
    [SerializeField] AudioClip[] audioClips;
}
