using UnityEngine;
using UnityEngine.Serialization;

public class FoodObject : CellObject
{
    [SerializeField, FormerlySerializedAs("AmountGranted")]
    [Range(1, 50), Tooltip("Food restored to the player when this item is picked up.")]
    private int m_AmountGranted = 10;

    public override void PlayerEntered()
    {
        GameManager gameManager = GameManager.Instance;

        gameManager.EffectsManager.PlayFoodCollected(transform.position);
        gameManager.SoundManager.PlayFoodPickup();
        gameManager.BoardManager.RemoveObject(this);

        gameManager.ChangeFood(m_AmountGranted);
    }
}
