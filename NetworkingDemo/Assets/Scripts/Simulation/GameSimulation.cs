using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System;

public class GameSimulation
{
     static GameState current;
     public static bool isAlive = false;
     public static ConcurrentDictionary<ushort, InputSerialization.FrameInfo> FrameInputDictionary;
     const ushort MAX_FRAME_BUFFER = 8;
     private static HashSet<ushort> RollbackFrames;
     private static Dictionary<int, GameState> GameStateDictionary;
     public static uint rollbackCount = 0;  

    private static InputSerialization.Inputs LastRemoteInputRecieved;

    public static int LocalFrame => current == null ? -1 : current.frameID;
    public static int LastRemoteFrame => LastRemoteInputRecieved == null ? -1 : LastRemoteInputRecieved.FrameID;
    private static void Init(bool p1Local)
    {
        current = new GameState(p1Local);
        FrameInputDictionary = new ConcurrentDictionary<ushort, InputSerialization.FrameInfo>();
        isAlive = true;
        RollbackFrames = new HashSet<ushort>();
        GameStateDictionary = new Dictionary<int, GameState>();
    }

    public static void AddLocalInput(InputSerialization.Inputs input)
    {
        InputSerialization.FrameInfo temp = new InputSerialization.FrameInfo();
        temp.SetLocalInputs(input);
        FrameInputDictionary.AddOrUpdate(input.FrameID, temp, (k, v) => v.ReturnWithNewInput(input, false));
    }

    public static void AddRemoteInput(InputSerialization.Inputs input, bool isPredicted)
    {
        if (!isPredicted) LastRemoteInputRecieved = input;
        InputSerialization.FrameInfo temp = new InputSerialization.FrameInfo();
        temp.SetRemoteInputs(input);
        temp.remoteIsPredicted = isPredicted;
        FrameInputDictionary.AddOrUpdate(input.FrameID, temp, (k, v) =>
        {
            if(!isPredicted && v.remoteIsPredicted)
            {
                InputSerialization.Inputs existingInput;
                if((existingInput = v.GetRemoteInputs()) != input) //predicted input was wrong
                {
                    //we are overwriting an input prediction -- must rollback to correct our mispredict
                    RollbackFrames.Add(input.FrameID);
                }
            }
            else v.remoteIsPredicted = isPredicted;
            return v.ReturnWithNewInput(input, true);
        });
    }

    readonly static long TICKS_PER_FRAME = 166667; //16.67ms for 60fps
    public static void Run(bool p1Local)
    {
        Init(p1Local);
        current.frameID = 0;
        long prev = System.DateTime.UtcNow.Ticks;
        long lag = 0; 
        
     
        while(isAlive) //update loop
        {
            long now = System.DateTime.UtcNow.Ticks;
            long elapsed = now - prev;
            prev = now;
            lag += elapsed;
            while (lag >= TICKS_PER_FRAME) //allows multiple loops in a single frame to catch up if we lag
            {
                    lag -= TICKS_PER_FRAME;
                    //handle rollbacks
                    if (RollbackFrames.Count > 0) current = HandleRollbacks();
                    //get inputs for this frame
                    FrameInputDictionary.TryGetValue((ushort)current.frameID, out InputSerialization.FrameInfo frameInputs);
                    //predict remote inputs
                    PredictRemoteInputs(current.frameID - LastRemoteFrame);
                    //update gamestate
                    current = current.Tick(frameInputs);
                    //handle state
                    if (!GameStateDictionary.ContainsKey(current.frameID))
                    {                     
                        GameStateDictionary.Add(current.frameID, new GameState(current));  //copy gamestate into dictionary
                    }
                    //send gamestate to unity main thread / renderer
                    Transport.current = current;

                    //cleanup (comment this out to store entire frame by frame gamestate for testing/replays)
                    //remove stored information out of max rollback range
                    ushort earliestBufferedFrame =(ushort)(current.frameID - MAX_FRAME_BUFFER);
                    FrameInputDictionary.TryRemove(earliestBufferedFrame, out _);
                    GameStateDictionary.Remove(earliestBufferedFrame);
            }                    
        }
    }
    public static void LoadPreviousGamestate(ushort frameID)
    {
        if (frameID >= current.frameID) return;
        //used for testing only
        lock (GameStateDictionary)
        {
            lock (current)
            {
                current = GameStateDictionary[frameID];
            }
        }
    }

    private static GameState HandleRollbacks()
    {
        GameState g = null;
        lock (GameStateDictionary)
        {
            GameStateDictionary.TryGetValue(RollbackFrames.Min(), out g);  //get earliest rollback frame, could just store the first one found since last rollback, but this helps against out of order packets
            RollbackFrames.Clear();
        }
        if (g == null) return current;
        for (int i = g.frameID; i < current.frameID;)
        {
            //update until we are back at the current frame
            FrameInputDictionary.TryGetValue((ushort)i, out InputSerialization.FrameInfo f);
            g = g.Tick(f);
        }
        rollbackCount++;
        return g;        
    }

    private static void PredictRemoteInputs(int localFrameAdvantage)
    {
       if (localFrameAdvantage <= 0 || LastRemoteInputRecieved == null) return;
       InputSerialization.Inputs predictedRemote = LastRemoteInputRecieved;
       for (int i = 1; i <= localFrameAdvantage; ++i) //fill in missing remote inputs
       {
         predictedRemote.FrameID = (ushort)(LastRemoteFrame + i);
         AddRemoteInput(predictedRemote, true);
       }           
    }

}
