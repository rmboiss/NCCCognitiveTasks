using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LSL;
using System.Runtime.InteropServices;
using System;

public class BCITSTLSLController : BaseLSLController
{
    // Inlets
	public float velocity_x;
	public float velocity_y;
	public double timestamp;

    public enum SignalType
    {
        Velocity,
        Position,
        Stimuli,
        Neural
    }

    protected struct CursorPosition
    {
        public float cursor_x;
        public float cursor_y;
    }

    [System.Serializable]
    private struct BCITSTInletsInfo
    {
        public string InletName;
        public SignalType InletType;
        public string InletID;
    }

    private List<BCITSTInletsInfo> allInlets = new List<BCITSTInletsInfo>
    {
        // For testing inlet stream, using the mouse.exe LSL Input module
        // TODO: Add the decoder intention state stream
        new BCITSTInletsInfo{InletName = "NDS-Decoder", InletType = SignalType.Position, InletID = "NDS-Decoder"},
    };
    private List<string> listNames = new List<string>();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out MousePosition lpMousePosition);
    [StructLayout(LayoutKind.Sequential)]
    private struct MousePosition
    {
        public int x;
        public int y;
    }

    liblsl.StreamInfo cursorStreamInfo = new liblsl.StreamInfo(
            "UnityCursor", "Position", 1, liblsl.IRREGULAR_RATE, liblsl.channel_format_t.cf_string, "cp121212");

    protected int cursorPositionStreamID;

    // Start is called before the first frame update
    protected override void Start()
    {

        base.Start();

        // Create the stream and push the first event.
        cursorPositionStreamID = ConfigureOutlet(cursorStreamInfo);

        // Populate inlets
        foreach (BCITSTInletsInfo info in allInlets)
        {
            listNames.Add(info.InletName);
        }

        // Inlets
        for (int ii = 0; ii<allInlets.Count; ii++)
        {
            ConfigureInlet(allInlets[ii].InletName, allInlets[ii].InletType.ToString(), allInlets[ii].InletID);
        }
    }

    protected void Update()
    {

		MousePosition mp;

        Cursor.lockState = CursorLockMode.Confined;
        GetCursorPos(out mp);
        Cursor.lockState = CursorLockMode.None;

        CursorPosition currentCursorPosition = new CursorPosition
        {
            cursor_x = mp.x,
            cursor_y = mp.y
        };

        string pubstring = "{\"Cursor_Position\": " + JsonUtility.ToJson(currentCursorPosition) + "}";

        Write(cursorPositionStreamID, pubstring);
        float[] sample = new float[50];

		liblsl.StreamInlet inl = inlets[0];
		float velocity_x_total = 0.0f;
		float velocity_y_total = 0.0f;
		int count = 0;
		float[] float_sample;

		double lastTimeStamp;

		if (inl != null)
		{
			if (inl.info().channel_format() == liblsl.channel_format_t.cf_float32)
			{
				float_sample = new float[inl.info().channel_count()];
				lastTimeStamp = inl.pull_sample(float_sample, 0.0f);
				if (lastTimeStamp != 0.0)
				{
					ProcessFloat(inl.info().name(), float_sample, lastTimeStamp);
					while ((lastTimeStamp = inl.pull_sample(float_sample, 0.0f)) != 0)
					{
						ProcessFloat(inl.info().name(), float_sample, lastTimeStamp);
						velocity_x_total = velocity_x_total + float_sample[0];
						velocity_y_total = velocity_y_total + float_sample[1];
						count = count + 1;
					}
					velocity_x = 0.0f;
					velocity_y = 0.0f;
					timestamp = Time.time;
					if (count > 0)
					{
						velocity_x = velocity_x_total / (float)count;
						velocity_y = velocity_y_total / (float)count;
					}
				}
			}
			
		}
	}
}