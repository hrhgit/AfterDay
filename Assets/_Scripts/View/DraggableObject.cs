using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 所有可被拖拽的手牌物件（卡牌、令牌等）的基类。
/// 包含了通用的拖拽逻辑。
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(LayoutElement))]
public abstract class DraggableObject : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public GameAsset cardData { get; protected set; }
    public object state { get; protected set; } 
    
    protected CanvasGroup _canvasGroup;
    protected Transform _parentToReturnTo = null;
    protected HandPanelView _handView;

    protected virtual void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _handView = GetComponentInParent<HandPanelView>();
    }

    
    

    #region Drag Logic
    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        if (cardData == null || _handView == null) return;
        _parentToReturnTo = this.transform.parent;
        _handView.OnCardBeginDrag(this); // 使用基类引用
        this.transform.SetParent(this.transform.root);
        _canvasGroup.blocksRaycasts = false;
    }

    public virtual void OnDrag(PointerEventData eventData)
    {
        if (cardData == null || _handView == null) return;
        this.transform.position = eventData.position;
        _handView.OnCardDrag(eventData);
    }

    public virtual void OnEndDrag(PointerEventData eventData)
    {
        if (cardData == null || _handView == null) return;
        int finalIndex = _handView.PlaceholderSiblingIndex;
        _handView.OnCardEndDrag();
        this.transform.SetParent(_parentToReturnTo);
        this.transform.SetSiblingIndex(finalIndex);
        _canvasGroup.blocksRaycasts = true;
    }
    #endregion
}