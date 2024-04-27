using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class Bag : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject BtnScroll;
    public GameObject ItemScroll;

    public enum ItemType {
        All,
        Equip,
        Food,

    }


    public List<ItemType> Tabs = new List<ItemType>() {
        ItemType.All,
        ItemType.Equip,
        ItemType.Food,
    };

    List<bagDataSim> items;
    int curTabIndex = 0;
    void Awake()
    {
        InitBagTabs();
    }
    void Start()
    {
        items = GetItemsInBag();

        SetTab();
    }

    List<Transform> tabBtns = new List<Transform>();
    List<Transform> itemUIs = new List<Transform>();
    List<bagDataSim> itemDatas = new List<bagDataSim>();

    void InitBagTabs() {
        for (int i = 0; i < Tabs.Count; i++)
        {
            GameObject tabBtn = Instantiate(Resources.Load("Prefabs/TabButton")) as GameObject;
            Transform tabContent = BtnScroll.transform.Find("Viewport/Content");
            tabBtn.transform.SetParent(tabContent);
            int index = i;
            tabBtn.GetComponent<Button>()?.onClick.AddListener(() => {
                SetTab(index);
            });

            tabBtn.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = Enum.GetNames(typeof(ItemType))[index];

            tabBtns.Add(tabBtn.transform);
        }
    }
    void SetTab(int tabIndex = 0) {

        curTabIndex = tabIndex;
        Debug.Log("curTabIndex ：" + curTabIndex);
        RefreshBtnList();
        RefreshItemList();

    }
    void RefreshBtnList()
    {
        for (int i = 0; i < tabBtns.Count; i++)
        {
            tabBtns[i].GetComponent<Button>().interactable = (i != curTabIndex);
        }
    }
    void RefreshItemList()
    {
        ClearItemList();
        Transform itemContent = ItemScroll.transform.Find("Viewport/Content");
        foreach (var itemData in items)
        {

            if (Tabs[curTabIndex] != ItemType.All && itemData.type != Tabs[curTabIndex]) continue;

            GameObject item = Instantiate(Resources.Load("Prefabs/BagItem")) as GameObject;
            item.transform.SetParent(itemContent);

            SetItem(item, itemData);

            itemUIs.Add(item.transform);
            itemDatas.Add(itemData);
        }


        Debug.Log("cur type item num ：" + itemUIs.Count);

    }
    void SetItem(GameObject item,bagDataSim data) 
    {
        item.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "id " + data.id;
    }
    void ClearItemList()
    {
        StopMoving();
        itemUIs.Clear();
        itemDatas.Clear();
        Transform itemContent = ItemScroll.transform.Find("Viewport/Content");
        for (int i = 0; i < itemContent.childCount; i++)
        {
            Transform child = itemContent.GetChild(i);
            Destroy(child.gameObject);
        }
    }

    // click and long press handler and move animation while long press
    void Update()
    {
        ClickAndLongPressHandler();
        BagItemMovingHandler();
    }

    [SerializeField] private bool longPressEnabled = true;



    bool isPressing = false;
    bool isDragBegin = false;
    bool isDragging = false;
    public float longPressTime = 0.6f;
    float pressTime = 0.0f;

    Vector2 beginPos = Vector2.zero;
    GameObject clickItem = null;
    int clickItemIndex = -1;

    float clickCancelOffsetLen = 5;

    void ClickAndLongPressHandler() {
        if (isPressing && longPressEnabled) pressTime += Time.deltaTime;
        else                                pressTime = 0.0f;

        isDragBegin = pressTime > longPressTime && !isDragging;
        
        isDragging = pressTime > longPressTime;
   
        ItemScroll.GetComponent<ScrollRect>().enabled = !isDragging && !isItemMoving;


        if (!isPressing && !isItemMoving && Input.GetMouseButtonDown(0))
        {
            OnClickDown(Input.mousePosition);
        }
        else if (isPressing && !isDragging && Vector2.Distance(beginPos, Input.mousePosition)>= clickCancelOffsetLen) 
        {
            OnClickUp(Input.mousePosition);
        }
        else if (isPressing && Input.GetMouseButtonUp(0))
        {
            OnClickUp(Input.mousePosition);
        }
        else if (isPressing && isDragging)
        {
            OnDragging(Input.mousePosition);
        }

    }

    bool InItemBox(Vector2 pos, GameObject item) {
        var size = item.GetComponent<RectTransform>().rect.size;
        Vector2 itemPos = item.transform.position;
        Vector2 offset = pos - itemPos;
        return Math.Abs(offset.x) < size.x * 0.45 && Math.Abs(offset.y) < size.y * 0.45;
    }

    // 0 in mask; 1 above mask; -1 under mask
    int GetDirByContentMask(Vector2 pos, GameObject mask)
    {
        var size = mask.GetComponent<RectTransform>().rect.size;
        Vector2 itemPos = mask.transform.position;
        Vector2 offset = pos - itemPos;


        if (offset.y > size.y * 0.6)            return 1;
        else if (offset.y < -size.y * 0.6)      return -1;
        else                                    return 0;
    }
 
    void OnClickDown(Vector2 pos)
    {
        Debug.Log("OnClickDown" + pos);
        isPressing = true;
        beginPos = pos;

       for (int i = 0; i < itemUIs.Count; i++)
       {
            Transform item = itemUIs[i];
            if (InItemBox(pos, item.gameObject)) {
                clickItem = item.gameObject;
                clickItemIndex = i;
                break;
            }
        }
    }


    void OnClickUp(Vector2 pos )
    {
        Debug.Log("OnClickUp" + pos);
        isPressing = false;

        if (isDragging) 
        {
            PlayResetPos();
            OnDragOver();
        }
        else if (clickCancelOffsetLen > Vector2.Distance(beginPos, pos)) 
        {
            Debug.Log("(todo add item script) Click item :"+clickItemIndex+" -- "+clickItem);
        }
        else 
        { 
            Debug.Log("Click Cancel"); 
        }

        clickItem = null;
        clickItemIndex = -1;
    }

    void OnDragOver()
    {

        if (clickItem == null) return;
        //clickItem.transform.localScale = Vector3.one;
        ClearItemMask();
        Debug.Log("Drag end");
    }

    void SaveItemPosBeforeDragging(Vector3 pos) 
    {
        itemPosBeforeDragging = pos;
        itemScrollContentPosBeforeDragging = ItemScroll.transform.Find("Viewport/Content").transform.position;
    }

    void OnDragStart() 
    {
        Debug.Log("Drag start");

        SaveItemPosBeforeDragging(clickItem.transform.position);
        SaveInitPos();
        CreateItemMask();
    }

    float moveWaitTime = 0;
    public float moveWaitTimeMax = 0.6f;

    bool DraggingToSlide(Vector2 pos) 
    {
        if (isItemMoving) return false;
        // drag over mask to move the rect
        int dir = GetDirByContentMask(pos, ItemScroll.gameObject);

        ScrollRect itemScrollRect = ItemScroll.GetComponent<ScrollRect>();
        if (dir == 1 && itemScrollRect.verticalNormalizedPosition < 1)
        {
            itemScrollRect.verticalNormalizedPosition += 0.002f;
            return true;
        }
        else if (dir == -1 && itemScrollRect.verticalNormalizedPosition > 0)
        {
            itemScrollRect.verticalNormalizedPosition -= 0.002f;
            return true;
        }
        return false;
    }

    bool DraggingToInsert(Vector2 pos)
    {
        int targetIndex = GetInsertIndex(pos);

        if (!isItemMoving && targetIndex != -1 && targetIndex != clickItemIndex)
        {
            moveWaitTime += Time.deltaTime;
            //Debug.Log(moveWaitTime);
            if (moveWaitTime > moveWaitTimeMax)
            {
                PlayInsertItemTo(clickItemIndex, targetIndex);
                return true;
            }
        }
        else
        {
            moveWaitTime = 0;
        }
        return false;
    }


    void OnDragging(Vector2 pos)
    {
        if (clickItem == null) return;

        //Debug.Log("OnDragging" + pos);

        if (isDragBegin)
        {
            OnDragStart();
        }


        clickItem.transform.position = pos;


        if (DraggingToSlide(pos))
        {
            SaveInitPos();
            return;
        }

        if (DraggingToInsert(pos))
        {
            return;
        }
  

    }


    bool isItemMoving = false;
    float moveTime = 0.0f;
    public float moveAnimationTime = 0.4f;

    List<Vector2> insertFlagPosList = new List<Vector2>();
    List<Vector2> targetPosList = new List<Vector2>();
    List<Vector2> orgPosList = new List<Vector2>();
    List<Vector2> initPosList = new List<Vector2>();
    void BagItemMovingHandler()
    {
        if (isItemMoving)
        {

            moveTime += Time.deltaTime;

            for (int i = 0; i < itemUIs.Count; i++)
            {
                if (i == tempIndex && isDragging) continue;

                float step = GetDelayStep(i,clickItemIndex,tempIndex, moveTime / moveAnimationTime);
                
                float lerpX = Mathf.SmoothStep(orgPosList[i].x, targetPosList[i].x, step);
                float lerpY = Mathf.SmoothStep(orgPosList[i].y, targetPosList[i].y, step);
                Vector3 pos = itemUIs[i].transform.position;
                pos.x = lerpX;
                pos.y = lerpY;
                itemUIs[i].transform.position = pos;
            }
            if (moveTime >= moveAnimationTime) StopMoving();

        }
        else
        {
            moveTime = 0.0f;
        }

        if (itemMask != null && clickItem != null)
        {
            itemMask.transform.position = clickItem.transform.position;
        }

    }

    float GetDelayStep(int index,int holdingIndex, int targetIndex, float step) 
    {
        if (holdingIndex == -1 || targetIndex == -1) return step;

        float delayMax = 0.5f;
        float delayStep = delayMax / Math.Abs(holdingIndex - targetIndex);
        float delay = delayStep* (Math.Abs(index - holdingIndex) -1);

        return (step - delay) / (1 - delay);
    }

    Vector2 itemPosBeforeDragging = Vector2.zero;
    Vector2 itemScrollContentPosBeforeDragging= Vector2.zero;

    void SaveInitPos()
    {
        initPosList.Clear();
        for (int i = 0; i < itemUIs.Count; i++)
        {
            Vector3 initPos = itemUIs[i].transform.position;
            initPosList.Add(initPos);
        }

        if (clickItemIndex!=-1) 
        {
            Vector2 curContentPos = ItemScroll.transform.Find("Viewport/Content").transform.position;
            Vector2 offset = curContentPos - itemScrollContentPosBeforeDragging ;
            Vector2 pos = itemPosBeforeDragging + offset;
            initPosList[clickItemIndex] = pos;
        }

        insertFlagPosList.Clear();
        for (int i = 0; i < itemUIs.Count; i++)
        {
            Vector3 posFlag = itemUIs[i].transform.position;
            insertFlagPosList.Add(posFlag);
        }
    }

    void SaveOrgPos()
    {
        orgPosList.Clear();
        for (int i = 0; i < itemUIs.Count; i++)
        {
            Vector3 orgPos = itemUIs[i].transform.position;
            orgPosList.Add(orgPos);
        }
    }

    void SaveTargetPos()
    {
        targetPosList.Clear();
        for (int i = 0; i < initPosList.Count; i++)
        {
            Vector3 posTarget = initPosList[i];
            targetPosList.Add(posTarget);
        }


    }

    public float insertTiggerDistance = 30.0f;
    int GetInsertIndex(Vector2 pos)
    {
        if (isItemMoving) return -1;

        //Debug.Log("GetInsertIndex insertFlagPosList.Count " + insertFlagPosList.Count);

        for (int i = 0; i < insertFlagPosList.Count; i++)
        {

            if (Vector2.Distance(insertFlagPosList[i], pos) < insertTiggerDistance)
            {
                return i;
            }
        }
        return -1;

    }
    int tempIndex = -1;
    void PlayInsertItemTo(int holdingIndex, int targetIndex)
    {
        if (holdingIndex != targetIndex)
        {
            Debug.Log("PlayInsertItemTo: " + "holdingIndex " + holdingIndex + "; targetIndex " + targetIndex);

            // set move org pos
            SaveOrgPos();


            var tempPos = orgPosList[holdingIndex];
            orgPosList.RemoveAt(holdingIndex);
            orgPosList.Insert(targetIndex, tempPos);
            orgPosList[targetIndex] = clickItem.transform.position;


            SaveTargetPos();

            var tempUI = itemUIs[holdingIndex];
            itemUIs.RemoveAt(holdingIndex);
            itemUIs.Insert(targetIndex, tempUI);

            var tempData = itemDatas[holdingIndex];
            itemDatas.RemoveAt(holdingIndex);
            itemDatas.Insert(targetIndex, tempData);

            moveTime = 0.0f;
            isItemMoving = true;

            tempIndex = targetIndex;


            SaveItemPosBeforeDragging(targetPosList[targetIndex]);
      
        }

    }
    void PlayResetPos()
    {
        Debug.Log("PlayResetPos: ");

        // set move org pos

        SaveOrgPos();

        SaveTargetPos();
        moveTime = 0.0f;
        isItemMoving = true;

    }


    GameObject itemMask = null;
    void CreateItemMask()
    {
        if (clickItem == null) return;

        itemMask = Instantiate(Resources.Load("Prefabs/BagItem")) as GameObject;
        itemMask.transform.SetParent(ItemScroll.transform);
        itemMask.transform.position = clickItem.transform.position;
        itemMask.transform.localScale = Vector3.one * 1.1f;
        SetItem(itemMask, itemDatas[clickItemIndex]);

    }
    void ClearItemMask()
    {
        if (itemMask != null) Destroy(itemMask.gameObject);
        itemMask = null;
    }

    void StopMoving()
    {
        moveTime = 0.0f;
        isItemMoving = false;

        if (tempIndex != -1 && clickItemIndex != -1)
        {

            Debug.Log("clickItemIndex from" + clickItemIndex + " change to " + tempIndex);

            clickItemIndex = tempIndex;
            tempIndex = -1;

        }
    }



    // 模拟背包的数据
    List<bagDataSim> GetItemsInBag() {
        List<bagDataSim> items = new List<bagDataSim>();
        int simId = 0;
        foreach (ItemType t in Enum.GetValues(typeof(ItemType)))
        {
            if (t == ItemType.All) continue;

            int num = UnityEngine.Random.Range(20, 60);
            for (int i = 0; i < num; i++)
            {
                bagDataSim data = new bagDataSim();
                data.type = t;
                data.id = simId;
                data.amount = 1;
                simId++;

                items.Add(data);
            }

        }

        return items;
    }


    public struct bagDataSim 
    {
        public int id;
        public ItemType type;
        public int amount;

    }
}
