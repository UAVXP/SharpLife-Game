/***
*
*	Copyright (c) 1996-2001, Valve LLC. All rights reserved.
*	
*	This product contains software technology licensed from Id 
*	Software, Inc. ("Id Technology").  Id Technology (c) 1996 Id Software, Inc. 
*	All Rights Reserved.
*
*   This source code contains proprietary and confidential information of
*   Valve LLC and its suppliers.  Access to this code is restricted to
*   persons who have executed a written SDK license with Valve.  Any access,
*   use or distribution of this code by or to any unlicensed person is illegal.
*
****/

using GoldSource.Mathlib;
using GoldSource.Server.Engine;
using GoldSource.Shared.Engine.StudioModel;
using Server.Engine;
using Server.Persistence;
using System;
using System.Diagnostics;
using System.Linq;

namespace Server.Game.Entities
{
    public abstract class BaseAnimating : BaseDelay
    {
        public const int ActivityNotAvailable = -1;

        // animation needs

        /// <summary>
        /// computed FPS for current sequence
        /// </summary>
        [Persist]
        public float SequenceFrameRate;

        /// <summary>
        /// computed linear movement rate for current sequence
        /// </summary>
        [Persist]
        public float GroundSpeed;

        /// <summary>
        /// last time the event list was checked
        /// </summary>
        [Persist]
        [Time]
        public float LastEventCheck;

        /// <summary>
        /// flag set when StudioAdvanceFrame moves across a frame boundry
        /// </summary>
        [Persist]
        public bool SequenceFinished;

        /// <summary>
        /// true if the sequence loops
        /// </summary>
        [Persist]
        public bool SequenceLoops;

        private StudioHeader GetStudioModel()
        {
            return Engine.Server.GetModel(Edict()) as StudioHeader;
        }

        // Basic Monster Animation functions

        /// <summary>
        /// accumulate animation frame time from last time called until now
        /// advance the animation frame up to the current time
        /// If an flInterval is passed in, only advance animation that number of seconds
        /// </summary>
        /// <param name="interval"></param>
        /// <returns></returns>
        public float StudioFrameAdvance(float interval = 0.0f)
        {
            if (interval == 0.0f)
            {
                interval = Engine.Globals.Time - AnimationTime;
                if (interval <= 0.001f)
                {
                    AnimationTime = Engine.Globals.Time;
                    return 0.0f;
                }
            }

            if (AnimationTime == 0)
            {
                interval = 0.0f;
            }

            Frame += interval * SequenceFrameRate * FrameRate;
            AnimationTime = Engine.Globals.Time;

            if (Frame < 0.0f || Frame >= 256.0f)
            {
                if (SequenceLoops)
                {
                    Frame -= (int)(Frame / 256.0f) * 256.0f;
                }
                else
                {
                    Frame = (Frame < 0.0f) ? 0 : 255;
                }

                SequenceFinished = true; // just in case it wasn't caught in GetEvents
            }

            return interval;
        }

        public SequenceFlags GetSequenceFlags()
        {
            var model = GetStudioModel();

            if (model == null || Sequence < 0 || Sequence >= model.Sequences.Count)
            {
                return SequenceFlags.None;
            }

            var sequence = model.Sequences[Sequence];

            return sequence.Flags;
        }

        public int LookupActivity(int activity)
        {
            //TODO: define 0
            Debug.Assert(activity != 0);

            var model = GetStudioModel();

            if (model == null)
            {
                return 0;
            }

            var sequences = model.Sequences;

            var weighttotal = 0;
            var seq = ActivityNotAvailable;

            for (var i = 0; i < sequences.Count; ++i)
            {
                if (sequences[i].Activity == activity)
                {
                    weighttotal += sequences[i].ActWeight;
                    if (weighttotal == 0 || EngineRandom.Long(0, weighttotal - 1) < sequences[i].ActWeight)
                    {
                        seq = i;
                    }
                }
            }

            return seq;
        }

        public int LookupActivityHeaviest(int activity)
        {
            //TODO: define 0
            Debug.Assert(activity != 0);

            var model = GetStudioModel();

            if (model == null)
            {
                return 0;
            }

            var sequences = model.Sequences;

            var weight = 0;
            var seq = ActivityNotAvailable;

            for (var i = 0; i < sequences.Count; ++i)
            {
                if (sequences[i].Activity == activity && sequences[i].ActWeight > weight)
                {
                    weight = sequences[i].ActWeight;
                    seq = i;
                }
            }

            return seq;
        }

        public int LookupSequence(string label)
        {
            if (label == null)
            {
                throw new ArgumentNullException(nameof(label));
            }

            var model = GetStudioModel();

            if (model == null)
            {
                return 0;
            }

            var sequences = model.Sequences;

            for (var i = 0; i < sequences.Count; ++i)
            {
                if (sequences[i].Label.Equals(label, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        public void GetSequenceInfo(out float frameRate, out float groundSpeed)
        {
            var model = GetStudioModel();

            if (model == null || Sequence < 0 || Sequence >= model.Sequences.Count)
            {
                frameRate = 0.0f;
                groundSpeed = 0.0f;
                return;
            }

            var sequence = model.Sequences[Sequence];

            if (sequence.NumFrames > 1)
            {
                frameRate = 256 * sequence.FPS / (sequence.NumFrames - 1);
                groundSpeed = sequence.LinearMovement.Length() * sequence.FPS / (sequence.NumFrames - 1);
            }
            else
            {
                frameRate = 256.0f;
                groundSpeed = 0.0f;
            }
        }

        public void ResetSequenceInfo()
        {
            var model = GetStudioModel();

            if (model == null)
            {
                return;
            }

            GetSequenceInfo(out SequenceFrameRate, out GroundSpeed);
            SequenceLoops = ((GetSequenceFlags() & SequenceFlags.Looping) != 0);
            AnimationTime = Engine.Globals.Time;
            FrameRate = 1.0f;
            SequenceFinished = false;
            LastEventCheck = Engine.Globals.Time;
        }

        public int GetAnimationEvent(out AnimationEvent pMonsterEvent, float start, float end, int index)
        {
            var model = GetStudioModel();

            if (model == null || Sequence < 0 || Sequence >= model.Sequences.Count)
            {
                pMonsterEvent = null;
                return 0;
            }

            var sequence = model.Sequences[Sequence];

            var events = sequence.Events;

            //TODO: shouldn't this be >=?
            if (events.Count == 0 || index > events.Count)
            {
                pMonsterEvent = null;
                return 0;
            }

            if (sequence.NumFrames > 1)
            {
                start *= (sequence.NumFrames - 1) / 256.0f;
                end *= (sequence.NumFrames - 1) / 256.0f;
            }
            else
            {
                start = 0;
                end = 1.0f;
            }

            for (; index < events.Count; ++index)
            {
                // Don't send client-side events to the server AI
                if ((AnimationEventId)events[index].EventId >= AnimationEventId.Client)
                {
                    continue;
                }

                if ((events[index].Frame >= start && events[index].Frame < end)
                || ((sequence.Flags & SequenceFlags.Looping) != 0
                && end >= sequence.NumFrames - 1
                && events[index].Frame < end - sequence.NumFrames + 1))
                {
                    pMonsterEvent = new AnimationEvent
                    {
                        EventId = (AnimationEventId)events[index].EventId,
                        Options = events[index].Options
                    };
                    return index + 1;
                }
            }

            pMonsterEvent = null;
            return 0;
        }

        /// <summary>
        /// Handle events that have happend since last time called up until X seconds into the future
        /// </summary>
        /// <param name="interval"></param>
        public void DispatchAnimEvents(float interval = 0.1f)
        {
            var model = GetStudioModel();

            if (model == null)
            {
                Log.Alert(AlertType.AIConsole, "Gibbed monster is thinking!\n");
                return;
            }

            // FIXME: I have to do this or some events get missed, and this is probably causing the problem below
            //TODO
            interval = 0.1f;

            // FIX: this still sometimes hits events twice
            var start = Frame + ((LastEventCheck - AnimationTime) * SequenceFrameRate * FrameRate);
            var end = Frame + (interval * SequenceFrameRate * FrameRate);
            LastEventCheck = AnimationTime + interval;

            SequenceFinished = false;
            if (end >= 256 || end <= 0.0f)
            {
                SequenceFinished = true;
            }

            for (int index = 0; (index = GetAnimationEvent(out var animEvent, start, end, index)) != 0;)
            {
                HandleAnimEvent(animEvent);
            }
        }

        protected virtual void HandleAnimEvent(AnimationEvent animEvent)
        {
            //Nothing
        }

        public float SetBoneController(int iController, float flValue)
        {
            var model = GetStudioModel();

            if (model == null)
            {
                return flValue;
            }

            var controllers = model.BoneControllers;

            // find first controller that matches the index
            var controller = controllers.FirstOrDefault(c => c.Index == iController);

            if (controller == null)
            {
                return flValue;
            }

            // wrap 0..360 if it's a rotational controller

            if ((controller.Type & (StudioControllerTypes.XR | StudioControllerTypes.YR | StudioControllerTypes.ZR)) != 0)
            {
                // ugly hack, invert value if end < start
                if (controller.End < controller.Start)
                {
                    flValue = -flValue;
                }

                // does the controller not wrap?
                if (controller.Start + 359.0f >= controller.End)
                {
                    if (flValue > ((controller.Start + controller.End) / 2.0) + 180)
                    {
                        flValue -= 360;
                    }

                    if (flValue < ((controller.Start + controller.End) / 2.0) - 180)
                    {
                        flValue += 360;
                    }
                }
                else
                {
                    if (flValue > 360)
                    {
                        flValue -= (int)(flValue / 360.0f) * 360.0f;
                    }
                    else if (flValue < 0)
                    {
                        flValue += (int)((flValue / -360.0f) + 1) * 360.0f;
                    }
                }
            }

            int setting = (int)(255 * (flValue - controller.Start) / (controller.End - controller.Start));

            if (setting < 0)
            {
                setting = 0;
            }

            if (setting > 255)
            {
                setting = 255;
            }

            SetController(iController, (byte)setting);

            return (setting * (1.0f / 255.0f) * (controller.End - controller.Start)) + controller.Start;
        }

        public void InitBoneControllers()
        {
            //TODO: can optimize here by getting model once
            SetController(0, 0);
            SetController(1, 0);
            SetController(2, 0);
            SetController(3, 0);
        }

        public float SetBlending(int iBlender, float flValue)
        {
            var model = GetStudioModel();

            if (model == null || Sequence < 0 || Sequence >= model.Sequences.Count)
            {
                return flValue;
            }

            var sequence = model.Sequences[Sequence];

            if (sequence.GetBlendType(iBlender) == 0)
            {
                return flValue;
            }

            if ((sequence.GetBlendType(iBlender) & (StudioControllerTypes.XR | StudioControllerTypes.YR | StudioControllerTypes.ZR)) != 0)
            {
                // ugly hack, invert value if end < start
                if (sequence.GetBlendEnd(iBlender) < sequence.GetBlendStart(iBlender))
                {
                    flValue = -flValue;
                }

                // does the controller not wrap?
                if (sequence.GetBlendStart(iBlender) + 359.0 >= sequence.GetBlendEnd(iBlender))
                {
                    if (flValue > ((sequence.GetBlendStart(iBlender) + sequence.GetBlendEnd(iBlender)) / 2.0) + 180)
                    {
                        flValue -= 360;
                    }

                    if (flValue < ((sequence.GetBlendStart(iBlender) + sequence.GetBlendEnd(iBlender)) / 2.0) - 180)
                    {
                        flValue += 360;
                    }
                }
            }

            int setting = (int)(255 * (flValue - sequence.GetBlendStart(iBlender)) / (sequence.GetBlendEnd(iBlender) - sequence.GetBlendStart(iBlender)));

            if (setting < 0)
            {
                setting = 0;
            }

            if (setting > 255)
            {
                setting = 255;
            }

            base.SetBlending(iBlender, (byte)setting);

            return (setting * (1.0f / 255.0f) * (sequence.GetBlendEnd(iBlender) - sequence.GetBlendStart(iBlender))) + sequence.GetBlendStart(iBlender);
        }

        public void GetBonePosition(int bone, out Vector origin, out Vector angles)
        {
            Engine.Server.GetBonePosition(Edict(), bone, out origin, out angles);
        }

        public void GetAttachment(int bone, out Vector origin, out Vector angles)
        {
            Engine.Server.GetAttachment(Edict(), bone, out origin, out angles);
        }

        public int FindTransition(int endingAnim, int goalAnim, int startingDirection, out int resultDirection)
        {
            var model = GetStudioModel();

            if (model == null)
            {
                resultDirection = 0;
                return goalAnim;
            }

            var sequences = model.Sequences;

            // bail if we're going to or from a node 0
            if (sequences[endingAnim].EntryNode == 0 || sequences[goalAnim].EntryNode == 0)
            {
                resultDirection = 0;
                return goalAnim;
            }

            // ALERT( at_console, "from %d to %d: ", pEndNode->iEndNode, pGoalNode->iStartNode );

            var endNode = (startingDirection > 0) ? sequences[endingAnim].ExitNode : sequences[endingAnim].EntryNode;

            if (endNode == sequences[goalAnim].EntryNode)
            {
                resultDirection = 1;
                return goalAnim;
            }

            var internNode = model.Transitions[((endNode - 1) * model.Transitions.Count) + (sequences[goalAnim].EntryNode - 1)];

            if (internNode == 0)
            {
                resultDirection = 0;
                return goalAnim;
            }

            // look for someone going
            for (var i = 0; i < sequences.Count; ++i)
            {
                if (sequences[i].EntryNode == endNode && sequences[i].ExitNode == internNode)
                {
                    resultDirection = 1;
                    return i;
                }
                if (sequences[i].NodeFlags != 0)
                {
                    if (sequences[i].ExitNode == endNode && sequences[i].EntryNode == internNode)
                    {
                        resultDirection = -1;
                        return i;
                    }
                }
            }

            Log.Alert(AlertType.Console, "error in transition graph\n");
            resultDirection = 0;
            return goalAnim;
        }

        public void SetBodygroup(int group, int value)
        {
            var model = GetStudioModel();

            //TODO: should be >=?
            if (model == null || group < 0 || group > model.BodyParts.Count)
            {
                return;
            }

            var bodyPart = model.BodyParts[group];

            if (value >= bodyPart.NumModels)
            {
                return;
            }

            int iCurrent = (Body / bodyPart.BaseIndex) % bodyPart.NumModels;

            Body = (Body - (iCurrent * bodyPart.BaseIndex) + (value * bodyPart.BaseIndex));
        }

        public int GetBodygroup(int group)
        {
            var model = GetStudioModel();

            //TODO: should be >=?
            if (model == null || group < 0 || group > model.BodyParts.Count)
            {
                return 0;
            }

            var bodyPart = model.BodyParts[group];

            if (bodyPart.NumModels <= 1)
            {
                return 0;
            }

            return (Body / bodyPart.BaseIndex) % bodyPart.NumModels;
        }

        public bool ExtractBbox(int sequence, out Vector mins, out Vector maxs)
        {
            var model = GetStudioModel();

            if (model == null)
            {
                mins = WorldConstants.g_vecZero;
                maxs = WorldConstants.g_vecZero;
                return false;
            }

            var sequenceDesc = model.Sequences[sequence];

            mins = new Vector(
                sequenceDesc.BBMin[0],
                sequenceDesc.BBMin[1],
                sequenceDesc.BBMin[2]
            );

            maxs = new Vector(
                sequenceDesc.BBMax[0],
                sequenceDesc.BBMax[1],
                sequenceDesc.BBMax[2]
            );

            return true;
        }

        public void SetSequenceBox()
        {
            // Get sequence bbox
            if (ExtractBbox(Sequence, out var mins, out var maxs))
            {
                // expand box for rotation
                // find min / max for rotations
                var yaw = (float)(Angles.y * (Math.PI / 180.0));

                var xvector = new Vector(
                    (float)Math.Cos(yaw),
                    (float)Math.Sin(yaw),
                    0
                );

                var yvector = new Vector(
                    (float)-Math.Sin(yaw),
                    (float)Math.Cos(yaw),
                    0
                );

                var bounds = new Vector[2]
                {
                    mins,
                    maxs
                };

                //TODO: define constants, use int.MIN & int.MAX
                var rmin = new Vector(9999, 9999, 9999);
                var rmax = new Vector(-9999, -9999, -9999);
                Vector basePosition, transformed;

                for (var i = 0; i <= 1; ++i)
                {
                    basePosition.x = bounds[i].x;
                    for (var j = 0; j <= 1; ++j)
                    {
                        basePosition.y = bounds[j].y;
                        for (var k = 0; k <= 1; ++k)
                        {
                            basePosition.z = bounds[k].z;

                            // transform the point
                            transformed.x = (xvector.x * basePosition.x) + (yvector.x * basePosition.y);
                            transformed.y = (xvector.y * basePosition.x) + (yvector.y * basePosition.y);
                            transformed.z = basePosition.z;

                            if (transformed.x < rmin.x)
                            {
                                rmin.x = transformed.x;
                            }

                            if (transformed.x > rmax.x)
                            {
                                rmax.x = transformed.x;
                            }

                            if (transformed.y < rmin.y)
                            {
                                rmin.y = transformed.y;
                            }

                            if (transformed.y > rmax.y)
                            {
                                rmax.y = transformed.y;
                            }

                            if (transformed.z < rmin.z)
                            {
                                rmin.z = transformed.z;
                            }

                            if (transformed.z > rmax.z)
                            {
                                rmax.z = transformed.z;
                            }
                        }
                    }
                }
                rmin.z = 0;
                rmax.z = rmin.z + 1;
                SetSize(rmin, rmax);
            }
        }
    }
}
