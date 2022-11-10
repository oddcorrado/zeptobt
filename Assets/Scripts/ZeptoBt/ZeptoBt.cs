using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using CodingSeb.ExpressionEvaluator;
using System.Linq;
using System;
using Random = UnityEngine.Random;

// variable extractor
// improve feedback on hit
// improve hit animation bat
// add damage animation
// Node Spawn (fx + blasts)

// kids demo
// check team colors
// check mouse on build
// check team winner
// implement reach
// 10 new levels
// Gems Tunnel Pistons

// ZeptoBtTree impulse VX/Vy
// Check decorators
// NodeLeafShootAt
// NodeLeafOrientTo (for shield)
// Shield element
// Node release
// Death explosion
// help for parses
namespace ZeptoBt
{
    public enum NodeReturn { Failure, Runnning, Success }
    public delegate void ForceTickDelegate();

    public class Node
    {
        public NodeComposite compositeParent;
        public NodeRoot Root { get; set; }

        public int Index { get; set; }

        public ZeptoBtTree Tree { get; set; }

        public ZeptoBtViewNode ViewNode { get; set; }

        public virtual NodeReturn Tick()
        {
            return NodeReturn.Failure;
        }

        public virtual void Abort(int index)
        {
        }

        public virtual void Init()
        { }

        public override string ToString()
        {
            return "node";
        }
    }

    public class NodeParam<T>
    {
        T value;
        string name;
        bool isVar;

        public NodeParam(T init)
        {
            value = init;
        }

        public NodeParam() { }
        public void Set(string data)
        {
            if (typeof(T).IsEnum)
            {
                try
                {
                    value = (T)Enum.Parse(typeof(T), data);
                    isVar = false;
                }
                catch (InvalidCastException e)
                {
                    name = data;
                    isVar = true;
                }
                catch (FormatException e)
                {
                    name = data;
                    isVar = true;
                }
            }
            else
            {
                try
                {
                    value = (T)Convert.ChangeType(data, typeof(T));
                    isVar = false;
                }
                catch (InvalidCastException e)
                {
                    name = data;
                    isVar = true;
                }
                catch (FormatException e)
                {
                    name = data;
                    isVar = true;
                }
            }
        }
        public T Get(ExpressionEvaluator evaluator)
        {
            if(isVar && evaluator.Variables.ContainsKey(name))
            {
                return (T)evaluator.Variables[name];
            }
            return value;
        }
    }
    public class Blackboard
    {
        enum LocalType { Float, Int, String, Bool }

        Dictionary<string, string> Values = new Dictionary<string, string>();
        Dictionary<string, LocalType> Types = new Dictionary<string, LocalType>();

        public (bool, float) TryGetFloat(string variable)
        {
            bool found = Values.ContainsKey(variable);
            if (!found) return (false, 0);

            bool ok = float.TryParse(Values[variable], NumberStyles.Float, CultureInfo.InvariantCulture, out float value);
            return (ok, value);
        }

        public (bool, int) TryGetInt(string variable)
        {
            bool found = Values.ContainsKey(variable);
            if (!found) return (false, 0);

            bool ok = int.TryParse(Values[variable], NumberStyles.Float, CultureInfo.InvariantCulture, out int value);
            return (ok, value);
        }
        public (bool, bool) TryGetBool(string variable)
        {
            bool found = Values.ContainsKey(variable);
            if (!found) return (false, false);

            bool ok = bool.TryParse(Values[variable], out bool value);
            return (ok, value);
        }

        public (bool, string) TryGetString(string variable)
        {
            bool found = Values.ContainsKey(variable);
            if (!found) return (false, "");

            return (true, Values[variable]);
        }

        public void Set(string variable, float value)
        {
            Values.Remove(variable);
            Values[variable] = value.ToString();
            Types.Remove(variable);
            Types[variable] = LocalType.Float;
        }

        public void Set(string variable, int value)
        {
            Values.Remove(variable);
            Values[variable] = value.ToString();
            Types.Remove(variable);
            Types[variable] = LocalType.Int;
        }

        public void Set(string variable, string value)
        {
            Values.Remove(variable);
            Values[variable] = variable;
            Types.Remove(variable);
            Types[variable] = LocalType.String;
        }

        public void Set(string variable, bool value)
        {
            Values.Remove(variable);
            Values[variable] = variable.ToString();
            Types.Remove(variable);
            Types[variable] = LocalType.Bool;
        }

        public bool Equals(string a, string b)
        {
            if (!Values.ContainsKey(a) || !Values.ContainsKey(b)) return false;
            if ((int)Types[a] > 1 || (int)Types[b] > 1)
                if (Types[a] != Types[b]) return false;
            switch (Types[a])
            {
                case LocalType.Float:
                    var (_, fa) = TryGetFloat(a);
                    var (_, fb) = TryGetFloat(b);
                    return Mathf.Abs(fa - fb) < 2 * Mathf.Epsilon;
                case LocalType.Int:
                    var (_, ia) = TryGetInt(a);
                    var (_, ib) = TryGetInt(b);
                    return ia == ib;
                case LocalType.String:
                    var (_, sa) = TryGetString(a);
                    var (_, sb) = TryGetString(b);
                    return sa == sb;
                case LocalType.Bool:
                    var (_, ba) = TryGetBool(a);
                    var (_, bb) = TryGetBool(b);
                    return ba == bb;
            }
            return false;
        }

        public bool Different(string a, string b)
        {
            if (!Values.ContainsKey(a) || !Values.ContainsKey(b)) return false;
            if ((int)Types[a] > 1 || (int)Types[b] > 1)
                if (Types[a] != Types[b]) return false;
            return !Equals(a, b);
        }

        public bool Greater(string a, string b)
        {
            if (!Values.ContainsKey(a) || !Values.ContainsKey(b)) return false;
            if ((int)Types[a] > 1 || (int)Types[b] > 1)
                if (Types[a] != Types[b]) return false;
            switch (Types[a])
            {
                case LocalType.Float:
                    var (_, fa) = TryGetFloat(a);
                    var (_, fb) = TryGetFloat(b);
                    return fa > fb;
                case LocalType.Int:
                    var (_, ia) = TryGetInt(a);
                    var (_, ib) = TryGetInt(b);
                    return ia > ib;
                case LocalType.String:
                    var (_, sa) = TryGetString(a);
                    var (_, sb) = TryGetString(b);
                    return string.Compare(sa, sb) > 0;
                case LocalType.Bool:
                    return false;
            }
            return false;
        }

        public bool Less(string a, string b)
        {
            if (!Values.ContainsKey(a) || !Values.ContainsKey(b)) return false;
            if ((int)Types[a] > 1 || (int)Types[b] > 1)
                if (Types[a] != Types[b]) return false;
            return !Greater(a, b);
        }

        public bool GreaterorEqual(string a, string b)
        {
            if (!Values.ContainsKey(a) || !Values.ContainsKey(b)) return false;
            if ((int)Types[a] > 1 || (int)Types[b] > 1)
                if (Types[a] != Types[b]) return false;
            switch (Types[a])
            {
                case LocalType.Float:
                    var (_, fa) = TryGetFloat(a);
                    var (_, fb) = TryGetFloat(b);
                    return fa >= fb;
                case LocalType.Int:
                    var (_, ia) = TryGetInt(a);
                    var (_, ib) = TryGetInt(b);
                    return ia >= ib;
                case LocalType.String:
                    var (_, sa) = TryGetString(a);
                    var (_, sb) = TryGetString(b);
                    return string.Compare(sa, sb) >= 0;
                case LocalType.Bool:
                    return true;
            }
            return false;
        }

        public bool LessOrEqual(string a, string b)
        {
            if (!Values.ContainsKey(a) || !Values.ContainsKey(b)) return false;
            if ((int)Types[a] > 1 || (int)Types[b] > 1)
                if (Types[a] != Types[b]) return false;
            return !GreaterorEqual(a, b);
        }
    }

    public class NodeRoot : NodeComposite
    {
        public Node CurrentNode { get; set; }
        public ExpressionEvaluator Evaluator { get; set; } = new ExpressionEvaluator();

        // evaluator.Variables = new Dictionary<string, object>() {};

        public override NodeReturn Tick()
        {
            Children[0].Tick();
            return NodeReturn.Runnning;
        }
    }
    public class NodeComposite : Node
    {
        protected List<Node> children = new List<Node>();
        public int ChildIndex { get; set; }
        public List<Node> Children { get => children; set { children = value; } }

        public delegate void ExitDelegate(NodeReturn exitValue);
        public event ExitDelegate ExitEvent;

        protected virtual void OnExit(NodeReturn exitValue)
        {
            ExitEvent?.Invoke(exitValue);
        }
    }
    public class NodeDecorator : NodeComposite
    {
    }

    public class NodeDecoratorInvert : NodeDecorator
    {
        public override NodeReturn Tick()
        {
            if (Children.Count != 1) return NodeReturn.Success;
            NodeReturn returnValue = Children[0].Tick();

            switch (returnValue)
            {
                case NodeReturn.Failure: return NodeReturn.Success;
                case NodeReturn.Success: return NodeReturn.Failure;
                default: return NodeReturn.Runnning;
            }
        }
    }

    public class NodeDecoratorSuccessify : NodeDecorator
    {
        public override NodeReturn Tick()
        {
            if (Children.Count != 1) return NodeReturn.Success;
            Children[0].Tick();
            return NodeReturn.Success;
        }
    }

    public class NodeDecoratorFilify : NodeDecorator
    {
        public override NodeReturn Tick()
        {
            if (Children.Count != 1) return NodeReturn.Success;
            Children[0].Tick();
            return NodeReturn.Failure;
        }
    }
    public class NodeDecoratorOnce : NodeDecorator
    {
        private bool done;

        public override NodeReturn Tick()
        {
            if (done) return NodeReturn.Success;
            done = true;
            if (Children.Count != 1) return NodeReturn.Success;
            return Children[0].Tick();
        }
    }

    public class NodeDecoratorGate : NodeDecorator
    {
        private bool done;
        private bool initDone;

        protected override void OnExit(NodeReturn exitEvent)
        {
            done = false;
        }
        public override NodeReturn Tick()
        {
            if (done) return NodeReturn.Success;

            if (Children.Count != 1) return NodeReturn.Success;

            if (!initDone) { compositeParent.ExitEvent += OnExit; initDone = true; }

            NodeReturn nodeReturn = Children[0].Tick();
            if (nodeReturn == NodeReturn.Success) done = true;
            return nodeReturn;
        }
    }

    public class NodeSequence : NodeComposite
    {
        public override NodeReturn Tick()
        {
            int i = 0;
           // Debug.Log($"BT TICK - {this}");
            while (i < children.Count)
            {
                var childReturn = Children[i].Tick();
                if (childReturn == NodeReturn.Runnning)
                {
                    Root.CurrentNode = Children[i];
                }
                if (childReturn == NodeReturn.Failure) OnExit(NodeReturn.Failure);
                if (childReturn != NodeReturn.Success) return childReturn;
                i++;
            }

            OnExit(NodeReturn.Success);
            Root.CurrentNode = this;
            return NodeReturn.Success;
        }
        public override void Abort(int abortIndex)
        {
            if (abortIndex < Index)
            {
                Children.ForEach(child => child.Abort(Index));
                ChildIndex = 0;
            }
            else
            {
                Children.ForEach(child => 
                {
                    if(child.Index < Index || child is NodeComposite)
                        child.Abort(Index);
                });
            }
        }

        public override string ToString()
        {
            return $"NODE SEQ {Index} {Children.Count}";
        }
    }
    public class NodeSelector : NodeComposite
    {

        public override NodeReturn Tick()
        {
            int i = 0;
           // Debug.Log($"BT TICK - {this}");
            while (i < Children.Count)
            {
                var childReturn = Children[i].Tick();
                //if(childReturn == NodeReturn.Runnning && Children[ChildIndex].Index < Tree.CurrentNode.Index)
                //    Tree.Abort(Children[ChildIndex].Index + 1);
                if (childReturn == NodeReturn.Success || childReturn == NodeReturn.Runnning)
                {
                    Root.CurrentNode = Children[i];
                    if (childReturn == NodeReturn.Success) OnExit(NodeReturn.Success);
                    return childReturn;
                }
                i++;
            }
            Root.CurrentNode = this;
            OnExit(NodeReturn.Failure);
            return NodeReturn.Failure;
        }

        public override void Abort(int abortIndex)
        {
            if (abortIndex < Index)
            {
                Children.ForEach(child => child.Abort(Index));
            }
            else
            {
                Children.ForEach(child =>
                {
                    if (child.Index < Index || child is NodeComposite)
                        child.Abort(Index);
                });
            }
        }

        public override string ToString()
        {
            return $"NODE SELECTOR {Index} {Children.Count}";
        }
    }

    public class NodeLeaf : Node
    {
        public event ForceTickDelegate ForceTickEvent;
        public ZeptoBtAction ZeptoBtAction { get; set; }

        public override void Abort(int index)
        {
            /* if (index < Index)
            {
                parent.Abort(index);
            } */
        }

        public override NodeReturn Tick()
        {
            NodeReturn nr = ZeptoBtAction.Tick();
            return nr;
        }

        public virtual string[] Params { get; set; }

        public virtual void Abort()
        { }
    }

    public class NodeLeafWait : NodeLeaf
    {
        enum Mode { Block, Skip }

        public override string[] Params
        {
            get => base.Params;
            set
            {
                base.Params = value;
                if (base.Params.Length > 0) dd.Set(base.Params[0]);
                if (base.Params.Length > 1) mm.Set(base.Params[1]);
            }
        }


        NodeParam<float> dd = new NodeParam<float>();
        NodeParam<Mode> mm = new NodeParam<Mode>();
        enum Status { Idle, Running, Done }
        Status status;
        private float stopDate;

        public override void Abort(int index)
        {
            Debug.Log($"BT ABORT - {this}");
            status = Status.Idle;
        }

        public override NodeReturn Tick()
        {
            // Debug.Log($"BT TICK - {this}");

            // float localDelay = delayVar == null ? delay: (float)Root.Evaluator.Variables[delayVar];

            switch (status)
            {
                case Status.Idle:
                    stopDate = Tree.CurrentTime + dd.Get(Root.Evaluator);
                    status = Status.Running;
                    return mm.Get(Root.Evaluator) == Mode.Block ? NodeReturn.Runnning : NodeReturn.Runnning;
                case Status.Running:
                    if(mm.Get(Root.Evaluator) == Mode.Block)
                    {
                        if (Tree.CurrentTime > stopDate)
                        {
                            status = Status.Done;
                            return NodeReturn.Success;
                        }
                        else
                            return NodeReturn.Runnning;
                    }
                    else
                    {
                        if (Tree.CurrentTime > stopDate)
                        {
                            status = Status.Idle;
                            return NodeReturn.Success;
                        }
                        else
                            return NodeReturn.Runnning;
                    }

                case Status.Done:
                    return NodeReturn.Success;
            }
            return NodeReturn.Success;
        }

        public override string ToString()
        {
            return $"NODE LEAF WAIT {Index} {status} {stopDate}";
        }
    }

    public class NodeLeafVelocity : NodeLeaf
    {
        public override string[] Params
        {
            get => base.Params;
            set
            {
                base.Params = value;

                if (base.Params.Length > 0)
                {
                    if(!float.TryParse(base.Params[0], NumberStyles.Float, CultureInfo.InvariantCulture, out vx))
                        vxVar = base.Params[0];
                    applyVx = true;
                }

                if (base.Params.Length > 1)
                {
                    if(!float.TryParse(base.Params[1], NumberStyles.Float, CultureInfo.InvariantCulture, out vy))
                        vyVar = base.Params[1];
                    applyVy = true;
                }
            }
        }

        private bool applyVx;
        private bool applyVy;
        private float vx;
        private string vxVar;
        private float vy;
        private string vyVar;

        public override void Abort(int index)
        {
            Tree.ApplyVx = false;
            Tree.ApplyVy = false;
        }
        public override NodeReturn Tick()
        {
           // Debug.Log($"BT TICK - {this}");
            Tree.Vx = vxVar != null ? (float)Root.Evaluator.Variables[vxVar] : vx;
            Tree.ApplyVx = applyVx;
            Tree.Vy = vyVar != null ? (float)Root.Evaluator.Variables[vyVar] : vy;
            Tree.ApplyVy = applyVy;
            return NodeReturn.Success;
        }

        public override string ToString()
        {
            return $"NODE LEAF VEL {Index} {applyVx} {vx}";
        }
    }

    public class NodeLeafRoam : NodeLeaf
    {
        public override string[] Params
        {
            get => base.Params;
            set
            {
                base.Params = value;

                if (base.Params.Length > 0) vel.Set(base.Params[0]);
                if (base.Params.Length > 1) radius.Set(base.Params[1]);
            }
        }

        private NodeParam<float> vel = new NodeParam<float>(2);
        private NodeParam<float> radius = new NodeParam<float>(3);
        private float randomTargetUpdateDate;
        Vector2 target;
        Vector2 spawnPos;

        public override void Init()
        {
            base.Init();
            spawnPos = Tree.transform.position;
        }
        public override NodeReturn Tick()
        {
            // Debug.Log($"BT TICK - {this}");

            if (Tree.CurrentTime > randomTargetUpdateDate)
            {
                var angle = Random.Range(0, Mathf.PI * 2f);
                target = spawnPos + radius.Get(Root.Evaluator) * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                randomTargetUpdateDate = Tree.CurrentTime + 2;
            }

            float d = (target - new Vector2(Tree.transform.position.x, Tree.transform.position.y)).magnitude;
            Vector2 velocity =
                Mathf.Min(vel.Get(Root.Evaluator), d)
                * (target - new Vector2(Tree.transform.position.x, Tree.transform.position.y)).normalized;
            Tree.Vx = velocity.x;
            Tree.Vy = velocity.y;
            Tree.ApplyVx = true;
            Tree.ApplyVy = true;

            return NodeReturn.Success;
        }

        public override string ToString()
        {
            return $"NODE LEAF ROAM {Index} {target}";
        }
    }

    public class NodeLeafProwl : NodeLeaf
    {
        public override string[] Params
        {
            get => base.Params;
            set
            {
                base.Params = value;

                if (base.Params.Length > 0) target.Set(base.Params[0]);
                if (base.Params.Length > 1) vel.Set(base.Params[1]);
                if (base.Params.Length > 2) radius.Set(base.Params[2]);
                if (base.Params.Length > 3) angleStep.Set(base.Params[3]);
            }
        }

        private NodeParam<ZeptoBtTrigger.TriggerType> target = new NodeParam<ZeptoBtTrigger.TriggerType>(ZeptoBtTrigger.TriggerType.Player);
        private NodeParam<float> vel = new NodeParam<float>(2);
        private NodeParam<float> radius = new NodeParam<float>(3);
        private NodeParam<float> angleStep = new NodeParam<float>(0.1f);
        private float angle;

        public override NodeReturn Tick()
        {
            // Debug.Log($"BT TICK - {this}");

            var go = Tree.GetTriggerObject(target.Get(Root.Evaluator));
            if (go == null) return NodeReturn.Failure;

            Vector2 targetPos = go.transform.position;

            angle += angleStep.Get(Root.Evaluator);
            targetPos = targetPos + radius.Get(Root.Evaluator) * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            if((targetPos - new Vector2(Tree.transform.position.x, Tree.transform.position.y)).magnitude > 0.01f)
            {
                Vector2 velocity =
                    vel.Get(Root.Evaluator)
                    * (targetPos - new Vector2(Tree.transform.position.x, Tree.transform.position.y)).normalized;
                Tree.Vx = velocity.x;
                Tree.Vy = velocity.y;
            }
            else
            {
                Tree.Vx = 0;
                Tree.Vy = 0;
            }

            Tree.ApplyVx = true;
            Tree.ApplyVy = true;

            return NodeReturn.Success;
        }

        public override string ToString()
        {
            return $"NODE LEAF ROAM {Index} {target}";
        }
    }

    public class NodeLeafMoveTo : NodeLeaf
    {
        public override string[] Params
        {
            get => base.Params;
            set
            {
                base.Params = value;

                if (base.Params.Length > 0)
                {
                    vel.Set(base.Params[0]);
                }

                switch (base.Params.Length)
                {
                    case 1:
                        mode = Mode.Random;
                        break;
                    case 2:
                        mode = Mode.Trigger;
                        System.Enum.TryParse<ZeptoBtTrigger.TriggerType>(base.Params[1], true, out triggerTarget);
                        break;
                    case 3:
                        mode = Mode.Pos;
                        x.Set(base.Params[1]);
                        y.Set(base.Params[2]);
                        break;

                }
            }
        }

        private NodeParam<float> vel = new NodeParam<float>();
        private NodeParam<float> x = new NodeParam<float>();
        private NodeParam<float> y = new NodeParam<float>();
        private ZeptoBtTrigger.TriggerType triggerTarget;
        enum Mode { Pos, Trigger, Random }
        private Mode mode;
        private float randomTargetUpdateDate;
        Vector2 target;

        public override NodeReturn Tick()
        {
            // Debug.Log($"BT TICK - {this}");

            switch (mode)
            {
                case Mode.Pos:
                    target = new Vector2(x.Get(Root.Evaluator), y.Get(Root.Evaluator));
                    break;

                case Mode.Trigger:
                    var go = Tree.GetTriggerObject(triggerTarget);
                    if (go == null) return NodeReturn.Failure;

                    target = go.transform.position;
                    break;

                case Mode.Random:
                    if (Tree.CurrentTime > randomTargetUpdateDate)
                    {
                        target =
                            new Vector2(Tree.transform.position.x, Tree.transform.position.y)
                            + new Vector2(Random.Range(-2f, 2f), Random.Range(-2f, 2f)).normalized * 5;
                        randomTargetUpdateDate = Tree.CurrentTime + 2;
                    }


                    break;
            }

            float d = (target - new Vector2(Tree.transform.position.x, Tree.transform.position.y)).magnitude;
            Vector2 velocity =
                Mathf.Min(vel.Get(Root.Evaluator), d)
                * (target - new Vector2(Tree.transform.position.x, Tree.transform.position.y)).normalized;
            Tree.Vx = velocity.x;
            Tree.Vy = velocity.y;
            Tree.ApplyVx = true;
            Tree.ApplyVy = true;

            return NodeReturn.Success;
        }

        public override string ToString()
        {
            return $"NODE LEAF MOVE TO {Index} {mode} {triggerTarget} {target}";
        }
    }


    public class NodeLeafDodge : NodeLeaf
    {
        public override string[] Params
        {
            get => base.Params;
            set
            {
                base.Params = value;

                if (base.Params.Length > 0)
                {
                    if (!float.TryParse(base.Params[0], NumberStyles.Float, CultureInfo.InvariantCulture, out vx))
                        vxVar = base.Params[0];
                }

                if (base.Params.Length > 1)
                {
                    if (!float.TryParse(base.Params[1], NumberStyles.Float, CultureInfo.InvariantCulture, out vy))
                        vyVar = base.Params[1];
                }

                if (base.Params.Length > 2)
                {
                    System.Enum.TryParse<ZeptoBtTrigger.TriggerType>(base.Params[2], true, out triggerTarget);
                }

                if (base.Params.Length > 3)
                {
                    bool.TryParse(base.Params[3], out isJump);
                }

                if (base.Params.Length > 4)
                {
                    bool.TryParse(base.Params[4], out isJump);
                }
            }
        }

        private float vx;
        private string vxVar;
        private float vy;
        private string vyVar;
        private ZeptoBtTrigger.TriggerType triggerTarget;
        private bool isJump;

        public override NodeReturn Tick()
        {
            // Debug.Log($"BT TICK - {this}");


            var go = Tree.GetTriggerObject(triggerTarget);
            if (go == null) return NodeReturn.Success;

            Vector2 dir = Vector2.up;
            Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
            if (rb != null) dir = rb.velocity.Rotate(90f).normalized;
            Vector2 velocity =new Vector2 (
                     dir.x * (vxVar != null ? (float)Root.Evaluator.Variables[vxVar] : vx),
                     dir.y * (vyVar != null ? (float)Root.Evaluator.Variables[vyVar] : vy));
            Tree.Vx = velocity.x;
            Tree.ApplyVx = true;
            if(!isJump || Tree.TriggerCounts[(int)ZeptoBtTrigger.TriggerType.Ground] > 0)
            {
                Tree.ApplyVy = true;
                Tree.Vy = Mathf.Abs(velocity.y);
            }
            else
                Tree.ApplyVy = false;

            return NodeReturn.Runnning;
        }

        public override string ToString()
        {
            return $"NODE LEAF DODGE {Index} {triggerTarget}";
        }
    }

    public class NodeLeafScale : NodeLeaf
    {
        public override string[] Params
        {
            get => base.Params;
            set
            {
                base.Params = value;

                if (base.Params.Length > 0)
                {
                    if (!float.TryParse(base.Params[0], NumberStyles.Float, CultureInfo.InvariantCulture, out sx))
                        sxVar = base.Params[0];
                    applySx = true;
                }

                if (base.Params.Length > 1)
                {
                    if (!float.TryParse(base.Params[1], NumberStyles.Float, CultureInfo.InvariantCulture, out sy))
                        syVar = base.Params[1];
                    applySy = true;
                }
            }
        }

        private bool applySx;
        private bool applySy;
        private float sx;
        private string sxVar;
        private float sy;
        private string syVar;

        public override NodeReturn Tick()
        {
            // Debug.Log($"BT TICK - {this}");
            if (applySx) Tree.Sx = sxVar != null ? (float)Root.Evaluator.Variables[sxVar] : sx;
            if (applySy) Tree.Sy = syVar != null ? (float)Root.Evaluator.Variables[syVar] : sy;

            return NodeReturn.Success;
        }

        public override string ToString()
        {
            return $"NODE LEAF SCALE {Index} {applySx} {sx}";
        }
    }

    public class NodeLeafTrigger : NodeLeaf
    {
        public override string[] Params
        {
            get => base.Params;
            set
            {
                base.Params = value;

                if (base.Params.Length > 0)
                {
                    System.Enum.TryParse<ZeptoBtTrigger.TriggerType>(base.Params[0], true, out type);
                }

                if (base.Params.Length > 1)
                {
                    bool.TryParse(base.Params[1],  out isOn);
                }
            }
        }

        private ZeptoBtTrigger.TriggerType type;
        private bool isOn;

        public override void Abort()
        {
        }
        public override NodeReturn Tick()
        {
            // Debug.Log($"BT TICK - {this}");
            if (Tree.TriggerCounts[(int)type] > 0 == isOn)
                return NodeReturn.Success;
            else
                return NodeReturn.Failure;
        }

        public override string ToString()
        {
            return $"NODE LEAF TRIGGER {Index} {isOn} {type} {Tree.TriggerCounts[(int)type] > 0 == isOn}";
        }
    }

    public class NodeLeafExpression : NodeLeaf
    {
        public override string[] Params
        {
            get => base.Params;
            set
            {
                base.Params = value;

                List<string> localParams = base.Params.ToList();

                if (localParams[0] == "!")
                {
                    onlyOnce = true;
                    localParams.RemoveAt(0);
                }

                expression = localParams.Aggregate("", (a, v) => $"{a} {v}");
            }
        }

        private string expression;
        private bool onlyOnce;
        private bool onlyOnceDone;


        public override void Abort()
        {
            onlyOnceDone = false;
        }
        public override NodeReturn Tick()
        {
            if (onlyOnceDone) return NodeReturn.Success;
            if (onlyOnce) onlyOnceDone = true;

            /// if(Root.Evaluator.Variables.ContainsKey("zzz"))
            /// Debug.Log($"BT EVAL before zzz={Root.Evaluator.Variables["zzz"]}");
            /// 
            Debug.Log("EXP " + expression);
            var result = Root.Evaluator.Evaluate(expression);

            // Debug.Log($"BT TICK - {this} result={result}");
            // Debug.Log($"BT EVAL after zzz={Root.Evaluator.Variables["zzz"]}");

            if (result.GetType() == typeof(bool))
                return (bool)result ? NodeReturn.Success : NodeReturn.Failure;
            else
                return NodeReturn.Success;
        }

        public override string ToString()
        {
            return $"NODE LEAF EXPRESSION {Index} {expression}";
        }
    }
#if SPINE
    public class NodeLeafSpine : NodeLeaf
    {
        public override string[] Params
        {
            get => base.Params;
            set
            {
                base.Params = value;

                if (base.Params.Length > 0)
                {
                    animation = base.Params[0];
                }

                if (base.Params.Length > 1)
                {
                    bool.TryParse(base.Params[1], out loop);
                }

                if (base.Params.Length > 2)
                {
                    int.TryParse(base.Params[2], out trackIndex);
                }
            }
        }

        private string animation;
        private int trackIndex;
        private bool loop;

        public override NodeReturn Tick()
        {
            // Debug.Log($"BT TICK - {this}");
            Tree.SetAnimation(animation, trackIndex, loop);

            return NodeReturn.Success;
        }

        public override string ToString()
        {
            return $"NODE LEAF SPINE {Index} {animation} {loop} {trackIndex}";
        }
    }

    public class NodeLeafHit : NodeLeaf
    {
        private int life = 50;

        public override NodeReturn Tick()
        {
            int newLife = Tree.GetLife();
            // Debug.Log($"BT TICK - {this} {newLife} {life}");


            if (newLife != life)
            {
                life = newLife;
                return NodeReturn.Success;
            }

            return NodeReturn.Failure;
        }

        public override string ToString()
        {
            return $"NODE LEAF HIT {Index} {life}";
        }
    }
#endif
    public class NodeLeafActivate : NodeLeaf
    {
        public override string[] Params
        {
            get => base.Params;
            set
            {
                base.Params = value;

                if (base.Params.Length > 0)
                {
                    goName = base.Params[0];
                }

                if (base.Params.Length > 1)
                {
                    bool.TryParse(base.Params[1], out doActivate);
                }
            }
        }

        string goName;
        bool doActivate;

        public override NodeReturn Tick()
        {
            // Debug.Log($"BT TICK - {this}");


            if (Tree.Children.ContainsKey(goName))
            {
                Tree.Children[goName].gameObject.SetActive(doActivate);
                return NodeReturn.Success;
            }

            return NodeReturn.Failure;
        }

        public override string ToString()
        {
            return $"NODE LEAF HIT {Index} {goName} {doActivate}";
        }
    }
}

