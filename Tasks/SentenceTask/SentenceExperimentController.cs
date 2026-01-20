using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Misc_Utilities;

public class SentenceExperimentController : BaseExperimentController
{
    public static SentenceExperimentController custom = null;

    [Header("Sentence Task Parameters")]
    [SerializeField] private string csvFileName = "demo_data.csv";
    public List<string> sentenceSet;

    [Header("Trial Settings")]
    public int repeatsPerWords = 20;
    public bool imaginedMode = true;

    [Header("UI")]
    public Image dividerImage;

    private SentenceLSLController lsl;
    private SentenceTaskInfo info;

    [Header("Decoder Timeout")]
    public float eolTimeoutSeconds = 10f;

    [Header("Response Selection")]
    public float responseTargetDwellSeconds = 0.25f;

    private GameObject hoverTarget = null;
    private float hoverTargetStartTime = 0f;

    // runtime state
    private float imagineStartTime = 0f;
    private bool imagineActive = false;

    private string decodedFull = "";         // what we score at end
    private string targetSentence = "";      // current trial sentence
    private bool awaitingSelection = false;
    private string imagineEndReason = "";

    void Awake()
    {
        if (custom != null)
        {
            Destroy(gameObject);
            return;
        }
        custom = this;

        sentenceSet = LoadSentencesFromCsv(csvFileName);
        if (sentenceSet.Count == 0)
        {
            Debug.LogWarning("No sentences loaded from CSV; falling back to defaults.");
            sentenceSet = new List<string> {
                "Bring it closer.",
                "My family is closer.",
                "What do they like?",
                "How is that good?",
                "Need help here?",
                "Yes, you have it right."
            };
        }

        lsl = GetComponent<SentenceLSLController>();
        if (lsl == null) Debug.LogError("SentenceLSLController not found on this GameObject!");
    }

    public void OnEnable()
    {
        SetInstance();
        AddListeners();

        info = GetComponent<SentenceTaskInfo>();
        base.taskInfo = info;
        base.currentTrial = new SentenceTrialState();

        if (lsl != null)
        {
            lsl.OnDecodedWord += HandleDecodedWord;
            lsl.OnDecodedEol += HandleDecodedEol;
        }
    }

    private void OnDisable()
    {
        if (lsl != null)
        {
            lsl.OnDecodedWord -= HandleDecodedWord;
            lsl.OnDecodedEol -= HandleDecodedEol;
        }
    }

    void Start()
    {
        SetupDivider();
    }

    // -------------------- CSV --------------------

    private List<string> LoadSentencesFromCsv(string fileName)
    {
        var sentences = new List<string>();
        var path = Path.Combine(Application.streamingAssetsPath, fileName);

        Debug.Log("Loading sentences from: " + path);

        if (!File.Exists(path))
        {
            Debug.LogWarning("CSV not found at: " + path);
            return sentences;
        }

        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (string.IsNullOrEmpty(line)) continue;

            // Format: "Sentence, possibly with commas",1
            int lastComma = line.LastIndexOf(',');
            if (lastComma <= 0) continue;

            string firstCol = line.Substring(0, lastComma).Trim();

            // Remove surrounding quotes
            if (firstCol.Length >= 2 && firstCol[0] == '"' && firstCol[firstCol.Length - 1] == '"')
                firstCol = firstCol.Substring(1, firstCol.Length - 2);

            // Unescape doubled quotes
            firstCol = firstCol.Replace("\"\"", "\"").Trim();

            if (!string.IsNullOrEmpty(firstCol))
                sentences.Add(firstCol);
        }

        return sentences;
    }

    // -------------------- Decoding callbacks --------------------

    private void HandleDecodedWord(string word)
    {
        if (!imagineActive) return;
        if (string.IsNullOrWhiteSpace(word)) return;

        word = word.Trim();

        // UI: show last decoded chunk
        decodedFull = word;

        if (info != null && info.decodedSentenceCueText != null)
            info.decodedSentenceCueText.text = decodedFull;

        // reset timeout window: give max (around 10 sec) from the most recent decoded word
        imagineStartTime = Time.time;
    }

    private void HandleDecodedEol()
    {
        if (!imagineActive) return;
        EndImagineWindow("eol");
    }

    // -------------------- Trials --------------------

    public override void PrepareAllTrials()
    {
        UnityEngine.Random.InitState(DateTime.Now.Millisecond);
        PopulateTrials();
    }

    private void PopulateTrials()
    {
        allTrials.Clear();

        for (int r = 0; r < repeatsPerWords; r++)
        {
            for (int i = 0; i < sentenceSet.Count; i++)
            {
                allTrials.Add(new SentenceTrialState
                {
                    TaskName = taskInfo.taskName,
                    Sentence = sentenceSet[i],
                    ModeImagined = imaginedMode
                });
            }
        }

        // shuffle
        for (int i = 0; i < allTrials.Count; i++)
        {
            int j = UnityEngine.Random.Range(i, allTrials.Count);
            var tmp = allTrials[i];
            allTrials[i] = allTrials[j];
            allTrials[j] = tmp;
        }
    }

    public override void PrepareTrial()
    {
        ResetObjects();

        if (allTrials.Count == 0)
        {
            PrepareAllTrials();
            currentTrialIndex = 0;
        }

        currentTrial = allTrials[0];
        allTrials.RemoveAt(0);

        currentTrial.TrialIndex = currentTrialIndex;
        currentTrialIndex++;

        currentTrial.IsCorrect = false;
        currentTrial.quiet = false;
    }

    public override void ResetObjects()
    {
        taskInfo.fixationPoint.transform.parent.gameObject.SetActive(true);
        SetFixationVisibility(true);

        imagineActive = false;
        imagineStartTime = 0f;
        hoverTarget = null;
        hoverTargetStartTime = 0f;

        EnableResponseTargets(false);

        if (info != null && info.decodedSentenceCueText != null)
            info.decodedSentenceCueText.text = "";

        var animator = GetComponent<Animator>();
        if (animator != null)
            animator.SetBool("ResponsePhOK", false);
    }

    public override void ShowCues()
    {
        decodedFull = "";
        awaitingSelection = false;
        imagineEndReason = "";

        taskInfo.fixationPoint.GetComponent<ExperimentObject>().IsVisible = false;

        if (info != null && info.sentenceCueText != null)
        {
            targetSentence = ((SentenceTrialState)currentTrial).Sentence;

            info.sentenceCueText.text = targetSentence;
            info.sentenceCueCanvas.SetActive(true);

            if (info.decodedSentenceCueText != null)
                info.decodedSentenceCueText.text = "";

            if (dividerImage != null)
                dividerImage.gameObject.SetActive(true);

            if (lsl != null)
                lsl.SendGoCueSentence(targetSentence);
        }
    }

    public override void HideCues()
    {
        if (info != null && info.sentenceCueCanvas != null)
        {
            Publish("{\"event\":\"cue_off\",\"sentence\":\"" + ((SentenceTrialState)currentTrial).Sentence + "\"}");
            BeginImagineWindow();
        }
    }

    private void BeginImagineWindow()
    {
        imagineActive = true;
        imagineStartTime = Time.time;
        hoverTarget = null;
        hoverTargetStartTime = 0f;

        Publish("{\"event\":\"imagine_start\",\"trial\":" + currentTrial.TrialIndex + "}");
        ShowImagineActiveVisual(true);
    }

    private void EnableResponseTargets(bool on)
    {
        for (int target_index = 0; target_index <= 1; target_index++)
        {
            ExperimentObject tgtScd = taskInfo.targetObjects[target_index].GetComponent<ExperimentObject>();
            tgtScd.transform.GetChild(0).GetComponent<Renderer>().enabled = on;

            SphereCollider sc = tgtScd.GetComponent<SphereCollider>();
            if (sc)
            {
                sc.isTrigger = on;
                sc.radius = on ? 1.3f : sc.radius;
                sc.enabled = on;
            }
        }
    }

    private void EndImagineWindow(string reason = "timeout")
    {
        imagineActive = false;
        imagineEndReason = reason;
        ShowImagineActiveVisual(false);

        Publish("{\"event\":\"imagine_end\",\"trial\":" + currentTrial.TrialIndex + ",\"reason\":\"" + reason + "\"}");

        // Show/select targets now
        EnableResponseTargets(true);

        awaitingSelection = true;
    }

    private void ShowImagineActiveVisual(bool on)
    {
        if (info != null && info.imagineBorder != null)
            info.imagineBorder.SetActive(on);
    }

    public void SetFixationVisibility(bool isVisible)
    {
        var fixPoint = taskInfo.fixationPoint.GetComponent<ExperimentObject>();
        fixPoint.IsVisible = isVisible;
        if (Camera.main != null) fixPoint.PointingTo = Camera.main.transform.position;
    }

    // -------------------- Divider --------------------

    public void SetupDivider()
    {
        if (dividerImage == null)
        {
            Debug.LogError("dividerImage is NULL. Assign CenterDivider's Image in the Inspector.");
            return;
        }

        var tex = CreateDashedTexture();
        dividerImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        dividerImage.type = Image.Type.Tiled;
        dividerImage.color = Color.white;
    }

    private Texture2D CreateDashedTexture()
    {
        int width = 64, height = 2, dash = 20, gap = 12;

        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat
        };

        for (int x = 0; x < width; x++)
        {
            bool on = (x % (dash + gap)) < dash;
            for (int y = 0; y < height; y++)
                tex.SetPixel(x, y, on ? Color.black : Color.clear);
        }

        tex.Apply();
        return tex;
    }

    public override void CursorSelect(GameObject go)
    {
        if (go == null)
        {
            selectedObject = SelectedObjectClass.Background;
            go = taskInfo.backgroundObject;
            return;
        }

        // IMPORTANT: If we hit a child collider (Sphere1), climb to the parent that has the Target component
        var targetComp = go.GetComponentInParent<Target>();
        if (targetComp != null)
            go = targetComp.gameObject;

        // ----- ORIGINAL FIXATION RELEASE LOGIC (THIS IS WHAT DRIVES Fixating -> false) -----
        if (go != taskInfo.fixationPoint)
        {
            // If we were fixating and just moved away, start away timer
            if (awayTimer == Mathf.Infinity && fixationTimer != Mathf.Infinity)
            {
                awayTimer = Time.time;
            }
            // If we're away timing and still considered fixating, check tolerance
            else if (awayTimer != Mathf.Infinity && fixationTimer != Mathf.Infinity)
            {
                if ((Time.time - awayTimer) > taskInfo.awayTolerance)
                {
                    fixationTimer = Mathf.Infinity;
                    awayTimer = Mathf.Infinity;
                }
            }
            else
            {
                fixationTimer = Mathf.Infinity;
                awayTimer = Mathf.Infinity;
            }
        }

        // ----- CLASSIFY SELECTION -----
        if (go == taskInfo.fixationPoint && taskInfo.fixationPoint != null)
        {
            awayTimer = Mathf.Infinity;

            if (fixationTimer == Mathf.Infinity) // Begin fixation
                fixationTimer = Time.time;

            selectedObject = SelectedObjectClass.Fixation;
        }
        else if (taskInfo.targetObjects.Contains(go))
        {
            // Require dwell on the same target before accepting selection
            if (hoverTarget != go)
            {
                hoverTarget = go;
                hoverTargetStartTime = Time.time;
                return; // not selected yet
            }

            if ((Time.time - hoverTargetStartTime) < responseTargetDwellSeconds)
                return; // still dwelling

            // Dwell met -> accept selection
            UpdateReactionTime();
            selectedObject = SelectedObjectClass.Target;
            selectedTargetIndex = taskInfo.targetObjects.IndexOf(go);
            return;
        }
        else
        {
            selectedObject = SelectedObjectClass.Background;
        }
    }



    // -------------------- Feedback / response --------------------

    private void LateUpdate()
    {
        if (!imagineActive) return;

        float elapsed = Time.time - imagineStartTime;
        if (elapsed >= eolTimeoutSeconds)
            EndImagineWindow("timeout_no_word_or_eol");
    }

    private string NormalizeSentence(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        s = s.ToLowerInvariant();

        // remove punctuation
        var chars = new System.Collections.Generic.List<char>(s.Length);
        foreach (char c in s)
            if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)) chars.Add(c);

        s = new string(chars.ToArray());
        s = string.Join(" ", s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        return s.Trim();
    }

    private bool IsDecodedCorrect(string decoded, string target)
    {
        return NormalizeSentence(decoded) == NormalizeSentence(target);
    }

    public override int CheckResponse()
    {
        OutcomeEnum = Trial_Outcomes.InvalidResponse;
        if (!awaitingSelection) return 0;

        if (selectedObject == SelectedObjectClass.Target && (selectedTargetIndex == 0 || selectedTargetIndex == 1))
        {
            awaitingSelection = false;

            bool userOk = (selectedTargetIndex == 0);   // ok, not ok
            bool objectiveOk = IsDecodedCorrect(decodedFull, targetSentence);

            // Correctness based on objective truth
            currentTrial.IsCorrect = objectiveOk;

            if (objectiveOk == true)
                OutcomeEnum = Trial_Outcomes.GoodTrial;
            else
                OutcomeEnum = Trial_Outcomes.IncorrectTarget;

            var animator = GetComponent<Animator>();
            if (animator != null)
                animator.SetBool("ResponsePhOK", true);

            string safeTarget = EscapeJson(targetSentence);
            string safeDecoded = EscapeJson(decodedFull);

            Publish("{\"event\":\"trial_result\"" +
                    ",\"trial\":" + currentTrial.TrialIndex +
                    ",\"imagine_end_reason\":\"" + imagineEndReason + "\"" +
                    ",\"user_ok\":" + (userOk ? "true" : "false") +
                    ",\"objective_ok\":" + (objectiveOk ? "true" : "false") +
                    ",\"target\":\"" + safeTarget + "\"" +
                    ",\"decoded\":\"" + safeDecoded + "\"}");

            // Hide targets
            EnableResponseTargets(false);

            return 1;
        }

        return 0;
    }


    private string EscapeJson(string s)
    {
        if (s == null) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    public override void EndResponse()
    {
        // Hide targets visuals (if you want)
        for (int target_index = 0; target_index <= 1; target_index++)
        {
            ExperimentObject tgtScd = taskInfo.targetObjects[target_index].GetComponent<ExperimentObject>();
            tgtScd.transform.GetChild(0).GetComponent<Renderer>().enabled = false;
        }

        // Hide sentence UI + divider
        var pInfo = taskInfo as SentenceTaskInfo;
        if (pInfo != null)
        {
            if (pInfo.sentenceCueCanvas != null) pInfo.sentenceCueCanvas.SetActive(false);
            if (pInfo.sentenceCueText != null) pInfo.sentenceCueText.text = "";
            if (pInfo.decodedSentenceCueText != null) pInfo.decodedSentenceCueText.text = "";
        }

        if (dividerImage != null) dividerImage.gameObject.SetActive(false);
    }

    public override void GiveFeedback()
    {
        if (info != null && info.feedbackText != null)
        {
            info.feedbackText.text = "Trial " + currentTrial.TrialIndex + " done";
            StartCoroutine(ClearFeedbackAfter(0.7f));
        }
    }

    IEnumerator ClearFeedbackAfter(float sec)
    {
        yield return new WaitForSeconds(sec);
        if (info != null && info.feedbackText != null)
            info.feedbackText.text = "";
    }
}
