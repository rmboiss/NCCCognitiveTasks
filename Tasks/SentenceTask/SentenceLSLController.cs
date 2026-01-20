using System;
using System.Collections.Generic;
using UnityEngine;
using LSL;

public class SentenceLSLController : BaseLSLController
{
    public event Action<string> OnDecodedWord;
    public event Action OnDecodedEol;
    public event Action<string> OnDecodedAny;

    [SerializeField] private bool logDecodedRaw = false;

    private const string DecoderStreamName = "BCISpeechDecoder";
    private const string DecoderStreamType = "DecodedWord";

    [Serializable]
    private class DecodedEvent
    {
        public string type;
        public string data;
    }

    public enum SignalType { DecodedWord, DecodingDone, DecodingStarted }

    [Serializable]
    private struct BCITSTInletsInfo
    {
        public string InletName;
        public SignalType InletType;
        public string InletID;
    }

    private readonly List<BCITSTInletsInfo> allInlets = new List<BCITSTInletsInfo>
    {
        new BCITSTInletsInfo { InletName = DecoderStreamName, InletType = SignalType.DecodedWord, InletID = "gren" },
    };

    private liblsl.StreamOutlet taskMarkerOutlet;
    private readonly string[] stringSample = new string[1];

    protected override void Start()
    {
        base.Start();

        foreach (var inlet in allInlets)
        {
            Debug.Log($"Configuring inlet name={inlet.InletName}, type={inlet.InletType}");
            ConfigureInlet(inlet.InletName, inlet.InletType.ToString(), inlet.InletID);
        }

        var info = new liblsl.StreamInfo(
            name: "BCIUnitySentence",
            type: "Markers",
            channel_count: 1,
            nominal_srate: 0,
            channel_format: liblsl.channel_format_t.cf_string,
            source_id: "unity-sentence-taskmarkers"
        );

        taskMarkerOutlet = new liblsl.StreamOutlet(info);
    }

    public void SendGoCueSentence(string sentence)
    {
        if (taskMarkerOutlet == null) return;

        string safe = sentence.Replace("\\", "\\\\").Replace("\"", "\\\"");
        string marker = $"{{\"effects\":[{{\"event\":\"go-cue\",\"data\":{{\"sentence\":\"{safe}\"}}}}]}}";
        taskMarkerOutlet.push_sample(new[] { marker });
    }

    private void Update()
    {
        foreach (var inl in inlets)
        {
            if (inl == null) continue;

            // Only handle the decoder stream
            var info = inl.info();
            if (info.name() != DecoderStreamName) continue;
            if (info.type() != DecoderStreamType) continue;

            double ts = inl.pull_sample(stringSample, 0.0f);
            if (ts <= 0.0) continue;

            HandleDecodedJson(stringSample[0]);
        }
    }

    private void HandleDecodedJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        if (logDecodedRaw) Debug.Log("LSL decoded raw: " + json);
        OnDecodedAny?.Invoke(json);

        DecodedEvent evt;
        try { evt = JsonUtility.FromJson<DecodedEvent>(json); }
        catch { return; }

        if (evt == null || string.IsNullOrEmpty(evt.type)) return;

        if (evt.type == "w" && !string.IsNullOrEmpty(evt.data))
            OnDecodedWord?.Invoke(evt.data);
        else if (evt.type == "eol")
            OnDecodedEol?.Invoke();
    }
}
