using UnityEngine;

[RequireComponent(typeof(Transform))]
[RequireComponent(typeof(SpriteRenderer))]
public class LayerByHeigh : MonoBehaviour
{
    [SerializeField] private Transform me;
    [SerializeField] private SpriteRenderer sprite;

    public void Update()
    {
        CalculateOrder();
    }

    private void OnValidate()
    {
        me ??= transform;
        sprite ??= me.GetComponent<SpriteRenderer>();
    }

    private void CalculateOrder()
    {
        sprite.sortingOrder = (int)(me.position.y * 10);
    }
}
