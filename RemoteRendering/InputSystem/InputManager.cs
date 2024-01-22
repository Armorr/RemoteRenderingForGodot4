using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Godot.RemoteRendering.InputSystem
{
    public static class InputManager
    {
        public static void HandleEvent(InputEvent inputEvent)
        {
            switch (inputEvent)
            {
                case TouchEvent touchEvent:
                    TouchEventManager.HandleEvent(touchEvent);
                    break;
                default:
                    GD.Print("Not support this StateEvent in InputManager!");
                    break;
            }
        }

        
    }

    public static class TouchEventManager
    {
        enum TouchPhase
        {
            None = 0,
            Began = 1,
            Moved = 2,
            Ended = 3,
            Canceled = 4,
            Stationary = 5
        }
        
        public static Dictionary<int, TouchEvent> touchStack = new ();
        public static bool isMoving = false;
        
        public static void HandleEvent(TouchEvent touch)
        {
            //GD.Print(touchStack.Count);
            if (touchStack.ContainsKey(touch.touchId))
            {
                touchStack[touch.touchId] = touch;
                if (touch.phaseId == (byte)TouchPhase.Moved)
                {
                    switch (touchStack.Count)
                    {
                        case 1:
                        {
                            GD.Print(touch.touchId + " Moved!" + " " + touch.delta[0] + "," + touch.delta[1]);
                            var ev = new InputEventScreenDrag();
                            ev.Index = touch.touchId;
                            ev.Position = new Vector2(touch.position[0], touch.position[1]);
                            ev.Relative = new Vector2(touch.delta[0], touch.delta[1]);
                            Input.ParseInputEvent(ev);
                            break;
                        }
                        case 2:
                        {
                            var anotherTouch = touchStack.First(pair => pair.Key != touch.touchId).Value;
                            if (anotherTouch.phaseId != (byte)TouchPhase.Moved)
                            {
                                break;
                            }
                            var startPos1 = new Vector2(touch.startPosition[0], touch.startPosition[1]);
                            var startPos2 = new Vector2(anotherTouch.startPosition[0], anotherTouch.startPosition[1]);
                            
                            var vec1 = new Vector2(touch.delta[0], touch.delta[1]).Normalized();
                            var vec2 = new Vector2(anotherTouch.delta[0], anotherTouch.delta[1]).Normalized();
                            
                            if (vec1.Dot(vec2) < 0)
                            {
                                var ev = new InputEventMagnifyGesture();
                                var originDis = startPos1.DistanceTo(startPos2);
                                var dis = (startPos1 + vec1).DistanceTo(startPos2 + vec2);
                                
                                ev.Factor = originDis < dis ? -1.0f : 1.0f;
                                Input.ParseInputEvent(ev);
                            }
                            break;
                        }
                        default:
                            GD.Print("More fingers NOT allowed!");
                            break;
                    }
                }
                else if (touch.phaseId == (byte)TouchPhase.Ended)
                {
                    touchStack.Remove(touch.touchId);
                    if (isMoving)
                    {
                        isMoving = false;
                        var ev = new InputEventKey();
                        ev.Pressed = false;
                        ev.Keycode = Key.W;
                        Input.ParseInputEvent(ev);
                    }
                }
                else
                {
                    GD.Print("TouchPhase = " + (TouchPhase)touch.phaseId);
                }
            }
            else
            {
                if (touch.phaseId == (byte)TouchPhase.Began)
                {
                    //GD.Print(touch.touchId + " Began!");
                    touchStack.Add(touch.touchId, touch);
                    if (touchStack.Count == 3)
                    {
                        isMoving = true;
                        var ev = new InputEventKey();
                        ev.Pressed = true;
                        ev.Keycode = Key.W;
                        Input.ParseInputEvent(ev);
                    }
                }
                else if (touch.phaseId == (byte)TouchPhase.Ended)
                {
                    //GD.Print(touch.touchId + " End!");
                    if (touch.tapCount == 1)
                    {
                        GD.Print(touch.touchId + " click!");
                        var ev = new InputEventScreenTouch();
                        ev.Pressed = true;
                        //ev.Canceled = true;
                        ev.Position = new Vector2(touch.position[0], touch.position[1]);
                        Input.ParseInputEvent(ev);
                    }
                }
            }
        }
    }
    
}

