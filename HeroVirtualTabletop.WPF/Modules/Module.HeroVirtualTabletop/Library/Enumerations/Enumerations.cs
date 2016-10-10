﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.Library.Enumerations
{
    public enum AnimationSequenceType
    {
        And,
        Or
    }

    public enum IdentityType
    {
        Model,
        Costume
    }
    public enum ClipboardAction
    {
        Clone, 
        Cut, 
        Link,
        CloneLink
    }
    public enum ExpansionUpdateEvent
    {
        Filter,
        Delete,
        Paste,
        DragDrop
    }
    public enum AnimationType
    {
        Movement,
        Sound,
        FX,
        Reference,
        Sequence,
        Pause
    }
    public enum ReferenceType
    {
        Link,
        Copy
    }

    public enum AttackEffectOption
    {
        None,
        Stunned,
        Unconcious,
        Dying,
        Dead
    }
    public enum KnockBackOption
    {
        KnockDown,
        KnockBack
    }

    public enum AttackResultOption
    {
        Hit,
        Miss
    }

    public enum AttackMode
    {
        None,
        Attack,
        Defend
    }

    public enum MovementDirection
    {
        Right,
        Left,
        Forward,
        Backward,
        Upward,
        Downward,
        Still
    }
}
