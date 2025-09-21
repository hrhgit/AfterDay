using UnityEngine;
using System.Collections.Generic;

#if INK_PRESENT
using Ink.Runtime;
#endif

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMPro.TextMeshProUGUI dialogueText;
    [SerializeField] private Transform choicesParent;
    [SerializeField] private GameObject choiceButtonPrefab;
    [SerializeField] private ResourceCommitmentView resourceCommitView;

#if INK_PRESENT
    private Story _currentStory;
#endif
    private EventData _sourceEventData;

    private void Awake() => Instance = this;

    public void StartDialogue(TextAsset inkJson, EventData sourceEvent)
    {
        _sourceEventData = sourceEvent;
#if INK_PRESENT
        _currentStory = new Story(inkJson.text);
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        RefreshView();
#else
        Debug.LogWarning("Ink 未启用 (缺少 INK_PRESENT 宏)。直接结算事件结果。");
        EventManager.Instance.ResolveEventResults(_sourceEventData);
#endif
    }

#if INK_PRESENT
    private void RefreshView()
    {
        if (_currentStory == null) return;

        // 清理旧选项按钮（如果需要）
        if (choicesParent != null)
        {
            for (int i = choicesParent.childCount - 1; i >= 0; i--)
            {
                Destroy(choicesParent.GetChild(i).gameObject);
            }
        }

        while (_currentStory.canContinue)
        {
            string line = _currentStory.Continue();
            if (dialogueText != null) dialogueText.text = line;
            if (ParseCurrentTags()) return; // 如果标签导致暂停，则中断刷新
        }

        if (_currentStory.currentChoices.Count > 0)
        {
            DisplayChoices();
        }
        else
        {
            EndDialogue();
        }
    }

    private bool ParseCurrentTags()
    {
        foreach (string tag in _currentStory.currentTags)
        {
            if (tag.StartsWith("REQUIRE"))
            {
                // 解析并展示资源投入界面（这里留桩）
                // resourceCommitView.Show(reqData, OnCommitSuccess, OnCommitCancel);
                return true; // 暂停
            }
            else if (tag.StartsWith("RESULT"))
            {
                Debug.Log("Mid-dialogue result executed: " + tag);
            }
        }
        return false; // 不暂停
    }

    private void OnCommitSuccess()
    {
        _currentStory.ChoosePathString("success");
        RefreshView();
    }

    private void OnCommitCancel()
    {
        _currentStory.ChoosePathString("failure");
        RefreshView();
    }

    private void DisplayChoices() { }

    private void EndDialogue()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        EventManager.Instance.ResolveEventResults(_sourceEventData);
    }
#endif
}


