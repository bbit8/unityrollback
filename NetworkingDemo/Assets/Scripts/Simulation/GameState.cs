using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameState
{
   public SimPlayer[] players = new SimPlayer[2];

    static readonly FInt32 STARTPOS = 2 ;

    public GameState()
    {
        players[0] = new SimPlayer();
        players[1] = new SimPlayer();

        players[0].pos.x = -STARTPOS;
        players[1].pos.x = STARTPOS;
    }



    public void Tick()
    {
        foreach(SimPlayer s in players)
        {
            s.ApplyInput();
        }
    }


}

public class SimPlayer
{
    public FVec2 pos;
    public FrameInfo f;

    public SimPlayer()
    {
        pos.x = 0;
        pos.y = 0;
    }

    public void ApplyInput()
    {
        (sbyte h, sbyte v) = InputSerialization.ConvertDirectionalInputToAxis(f.GetDirectionalInput());
        pos.x += h * moveSpeed;
        pos.y += v * moveSpeed;
    }

    public SimPlayer Copy()
    {
        SimPlayer s = new SimPlayer { pos = this.pos, f = this.f };
        return s;
    }
    public static readonly FInt32 moveSpeed = FInt32.HALF;
}


public struct FVec2
{
    public  FInt32 x, y;

    public FInt32 Magnitude => FInt32.Sqrt(FInt32.Pow(x, 2) + FInt32.Pow(y, 2));

    public static FInt32 Distance(FVec2 a, FVec2 b) => FInt32.Sqrt(FInt32.Pow(a.x - b.x, 2) + FInt32.Pow(a.y - b.y, 2));
}

