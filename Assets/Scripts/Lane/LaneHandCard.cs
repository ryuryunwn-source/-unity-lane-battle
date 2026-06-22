using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>手札の1枚。クリックで召喚するカードとして選択される。</summary>
public class LaneHandCard : MonoBehaviour, IPointerClickHandler
{
    public int handIndex;

    public void OnPointerClick(PointerEventData eventData)
    {
        LaneGameManager.Instance?.OnHandCardClicked(handIndex);
    }
}
