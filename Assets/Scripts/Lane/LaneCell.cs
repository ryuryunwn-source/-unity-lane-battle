using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>盤面の1セル。クリックで召喚先として選択される。</summary>
public class LaneCell : MonoBehaviour, IPointerClickHandler
{
    public int lane;
    public int col;

    public void OnPointerClick(PointerEventData eventData)
    {
        LaneGameManager.Instance?.OnCellClicked(lane, col);
    }
}
