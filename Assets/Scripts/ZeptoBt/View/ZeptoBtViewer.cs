using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZeptoBt;
using System.Linq;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;

public class ZeptoBtViewer : MonoBehaviour
{
    [SerializeField] ZeptoBtViewNode viewNodePrefab;
    [SerializeField] ZeptoLineUi lineRendererPrefab;
    [SerializeField] Canvas canvas;
    [SerializeField] TMP_Dropdown typeDropdown;
    [SerializeField] TMP_InputField paramsText;
    [SerializeField] TMP_InputField filenameText;
    [SerializeField] Button updateButton;
    [SerializeField] Button saveButton;
    [SerializeField] Button loadButton;
    [SerializeField] ZeptoBtDragger dragger;
    [SerializeField] ZeptoBtTree testTree;
   

    List<ZeptoBtViewNode> viewNodes = new List<ZeptoBtViewNode>();
    List<GameObject> allChildren = new List<GameObject>();
    ZeptoBtViewNode selectedNode;
    ZeptoBtViewNode compositeNode;
    ZeptoBtViewNode createdNode;
    ZeptoBtViewNode viewRoot;
    Vector3 cancelPosition;
    Vector3 viewMoveStartPos;
    Vector3 viewMoveStartMousePos;
    float scale = 1;

    float nodeSize = 100;
    List<string> history = new List<string>();

    Node root;
    public Node Root
    {
        get => root;
        set
        {
            root = value;
            Reset();
        }
    }
    void Reset(bool addHistory = true)
    {
        Node nodeRef = selectedNode != null ? selectedNode.Node : null;
        allChildren.RemoveAll(go =>
        {
            Destroy(go);
            return true;
        });
        viewNodes = new List<ZeptoBtViewNode>();
        viewRoot = CreateView(root);
        Reorganize(viewRoot, 0, new List<int>());
        TraceLinks(viewRoot);
        if(nodeRef != null)
        {
            selectedNode = FindNode(viewRoot, nodeRef);
            if (selectedNode != null) selectedNode.Selected = true;
        }
        if(Root != null && addHistory) history.Add(StringifyNode(Root, 0));
    }

    public ZeptoBtViewNode CreateView(Node node)
    {
        ZeptoBtViewNode viewNode = Instantiate(viewNodePrefab);

        if (node is NodeLeaf) viewNode.Parameters = (node as NodeLeaf).Params.Aggregate("", (a,v) => $"{a} {v}");
        var shortName = "???";
        foreach(var kvp in ZeptoBtTree.StringToAction)
        {
            if (kvp.Value == node.GetType().ToString()) shortName = kvp.Key;
        }
        viewNode.ShortName = shortName;
        viewNode.transform.SetParent(transform, false);
        viewNode.transform.localScale = Vector3.one;
        viewNode.Node = node;
        viewNode.Node.ViewNode = viewNode;
        if(node is NodeComposite)
        {
            (node as NodeComposite).Children.ForEach(child => CreateView(child));
        }

        allChildren.Add(viewNode.gameObject);
        viewNodes.Add(viewNode);

        return viewNode;
    }

    void Reorganize(ZeptoBtViewNode vnode, int depth, List<int> offsets)
    {
        if (offsets.Count <= depth)
        {
            if (offsets.Count > 1)
                offsets.Add(Mathf.Max(0, offsets[offsets.Count - 1] - 1));
            else
                offsets.Add(0);
        }
        vnode.transform.localPosition = new Vector2(offsets[depth] * nodeSize * 1.1f, 5 - depth * nodeSize * 1.1f);
        int offset = offsets[depth];
        for (int i = 0; i < offsets.Count; i++)
        {
            if (i < depth) offsets[i] = offset + 1;
            if (i == depth) offsets[i] = offset + 1;
            if (i > depth) offsets[i] = offset;
        }
       
        if(vnode.Node is NodeComposite)
        {
            var children = (vnode.Node as NodeComposite).Children;
            children.ForEach(child =>
            {
                Reorganize(child.ViewNode, depth + 1, offsets);
            });
            if ((vnode.Node as NodeComposite).Children.Count > 0)
                vnode.transform.position = new Vector3(
                    children.Aggregate(0f, (a, v) => a + v.ViewNode.transform.position.x) / children.Count,
                    vnode.transform.position.y,
                    vnode.transform.position.z
                    );
        }
    }

    void TraceLinks(ZeptoBtViewNode vnode)
    {
        if (vnode.Node is NodeComposite)
        {
            (vnode.Node as NodeComposite).Children.ForEach(child =>
            {
                var line = Instantiate(lineRendererPrefab, transform);
                line.transform.position = Vector3.zero;
                line.Points.Add((1 / scale) * new Vector2(vnode.transform.position.x, vnode.transform.position.y - nodeSize * 0.2f));
                line.Points.Add((1/ scale) * new Vector2(child.ViewNode.transform.position.x, child.ViewNode.transform.position.y + nodeSize * 0.2f));
                allChildren.Add(line.gameObject);
                TraceLinks(child.ViewNode);
            });
        }
    }

    ZeptoBtViewNode FindNode(ZeptoBtViewNode viewNode, Node node)
    {
        if (node == viewNode.Node)
            return viewNode;
        else
        {
            if(viewNode.Node is NodeComposite)
            {
                for(int i = 0; i < (viewNode.Node as NodeComposite).Children.Count; i++)
                {
                    var check = FindNode((viewNode.Node as NodeComposite).Children[i].ViewNode, node);
                    if (check != null) return check;
                }
            }
        }
        return null;
    }

    public void NodeSave()
    {
        Debug.Log(StringifyNode(Root, 0));
        Fili.WriteAllText(filenameText.text, StringifyNode(Root, 0));
    }

    string StringifyNode(Node node, int depth)
    {
        string className = node.GetType().ToString();
        string shortName = "bof";
        foreach(var kvp in ZeptoBtTree.StringToAction)
        {
            if (kvp.Value == className) shortName = kvp.Key;
        }

        string str = (node is NodeRoot) ? "" : "\n";
        for (int i = 0; i < depth; i++) str += "-";
        str += shortName;

        if (node is NodeLeaf) str += (node as NodeLeaf).Params.Aggregate("", (a, v) => $"{a} {v}");
        if (node is NodeComposite) str += (node as NodeComposite).Children.Aggregate("", (a, v) => a + StringifyNode(v, depth + 1));
        return str;
    }
    void Update()
    {
        if (Input.GetMouseButtonDown(0) && Input.mousePosition.x < 1620)
        {
            if(selectedNode != null) selectedNode.Selected = false;

            typeDropdown.interactable = true;
            updateButton.interactable = false;

            var mpos = Input.mousePosition;

            selectedNode = viewNodes.Find(vn => (vn.transform.position - mpos).magnitude < nodeSize);
            if (selectedNode != null)
            {
                Debug.Log($"Selected {selectedNode.Node}");
                selectedNode.Selected = true;
                RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                cancelPosition = selectedNode.transform.localPosition;
            }

            if(selectedNode != null)
            {
                var nodeClass = selectedNode.Node.GetType().ToString();
                string nodeType = "";
                Debug.Log(nodeClass);
                foreach (var kvp in ZeptoBtTree.StringToAction)
                {
                    if (kvp.Value == nodeClass)
                    {
                        nodeType = kvp.Key;
                        Debug.Log(kvp.Key);
                    }
                }
                var index = typeDropdown.options.FindIndex(o => o.text == nodeType);
                if (index != -1) typeDropdown.value = index;

                if (selectedNode.Node is NodeLeaf)
                {
                    paramsText.text = (selectedNode.Node as NodeLeaf).Params.Aggregate("", (a, v) => $"{a} {v}");
                    paramsText.interactable = true;
                    updateButton.interactable = true;
                }
                else
                {
                    paramsText.text = "";
                    paramsText.interactable = false;
                    updateButton.interactable = false;
                }

                typeDropdown.interactable = false;
            }
        }

        if (Input.GetMouseButton(0) && selectedNode != null && Input.mousePosition.x < 1620)
        {
            var mpos = Input.mousePosition;
            selectedNode.transform.position = mpos;

            var foundNode = viewNodes.Find(vn => vn != selectedNode && (vn.transform.position - mpos).magnitude < nodeSize);
            if (foundNode != null && foundNode.Node is NodeComposite) // FIXME Decorator
            {
                if (!(foundNode.Node is NodeDecorator) || (foundNode.Node as NodeComposite).Children.Count == 0)
                {
                    compositeNode = foundNode;
                    compositeNode.Selected = true;
                }
            }
            else
            {
                if(compositeNode != null)
                {
                    compositeNode.Selected = false;
                    compositeNode = null;
                }
            }
        }

        if (Input.GetMouseButton(0) && createdNode != null && Input.mousePosition.x < 1620)
        {
            var mpos = Input.mousePosition;
            createdNode.transform.position = mpos;

            var foundNode = viewNodes.Find(vn => vn != createdNode && (vn.transform.position - mpos).magnitude < nodeSize);
            if (foundNode != null && foundNode.Node is NodeComposite) // FIXME DEcorator
            {
                if(!(foundNode.Node is NodeDecorator) || (foundNode.Node as NodeComposite).Children.Count == 0)
                {
                    compositeNode = foundNode;
                    compositeNode.Selected = true;
                }
            }
            else
            {
                if (compositeNode != null)
                {
                    compositeNode.Selected = false;
                    compositeNode = null;
                }
            }
        }

        if (selectedNode != null && Input.GetKeyDown(KeyCode.LeftArrow))
        {
            var parent = selectedNode.Node.compositeParent;
            if(parent != null)
            {
                int index = parent.Children.FindIndex(n => selectedNode.Node == n);
                if(index > 0)
                {
                    var tmp = parent.Children[index - 1];
                    parent.Children[index - 1] = parent.Children[index];
                    parent.Children[index] = tmp;
                    Reset();
                }
            }
        }

        if (selectedNode != null && Input.GetKeyDown(KeyCode.RightArrow))
        {
            var parent = selectedNode.Node.compositeParent;
            if (parent != null)
            {
                int index = parent.Children.FindIndex(n => selectedNode.Node == n);
                if (index < parent.Children.Count - 1)
                {
                    var tmp = parent.Children[index + 1];
                    parent.Children[index + 1] = parent.Children[index];
                    parent.Children[index] = tmp;
                    Reset();
                }
            }
        }

        if (Input.GetMouseButtonUp(0) && selectedNode != null)
        {
            if(compositeNode != null)
            {
                // FIXME: parent on direct child
                selectedNode.Node.compositeParent.Children.Remove(selectedNode.Node);
                (compositeNode.Node as NodeComposite).Children.Add(selectedNode.Node);
                selectedNode.Node.compositeParent = (compositeNode.Node as NodeComposite);
                compositeNode.Selected = false;
                compositeNode = null;
                selectedNode = null;
                Reset();
            }
            else
            {
                Debug.Log("CIAO" + cancelPosition);
                selectedNode.transform.localPosition = cancelPosition;
            }
        }

        if (Input.GetMouseButtonUp(0) && createdNode != null)
        {
            if (compositeNode != null)
            {
                AttachNode(createdNode.Node, compositeNode);
                compositeNode.Selected = false;
                compositeNode = null;
                createdNode = null;
                Reset();
            }
            else
            {
                Destroy(createdNode.gameObject);
                createdNode = null;
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            viewMoveStartPos = transform.position;
            viewMoveStartMousePos = Input.mousePosition;
        }

        if (Input.GetKey(KeyCode.Space))
        {
            transform.position = viewMoveStartPos + (Input.mousePosition - viewMoveStartMousePos);
        }

        if (Mathf.Abs(Input.mouseScrollDelta.y) > 0)
        {
            scale += 0.1f * Input.mouseScrollDelta.y;
            transform.localScale = scale * Vector3.one;
        }

        if (Input.GetKeyDown(KeyCode.Z) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftControl)))
            NodeUndo();
    }

    public void TypeDropdownValueChanged()
    {
        dragger.Interactable = typeDropdown.captionText.text != "";
        Debug.Log(typeDropdown.value + " " + typeDropdown.captionText.text);
    }

    public void NodeUpdate()
    {
        Debug.Log("Update " + paramsText.text);
        (selectedNode.Node as NodeLeaf).Params = paramsText.text.Split(" ");
        Reset();
    }

    Node CreateNode(string type, string parameters)
    {
        var className = ZeptoBtTree.StringToAction[type];
        var node = Activator.CreateInstance(Type.GetType(className));
        if (node is NodeLeaf)
        {
            var leaf = node as NodeLeaf;

            // leaf.Index = nodeIndex++;
            // leaf.compositeParent = closestParentComposite;
            // leaf.Tree = this;
            // leaf.Root = Root;
            leaf.Params = parameters.Split(" ");
            // leaf.Init();
        }


        /* if (parentNode is NodeDecorator)
        {
            (parentNode as NodeDecorator).Child = node as Node;
            parentNode = null;
        }
        else
            (parentNode as NodeComposite).Children.Add(node as Node); */

        if (node is NodeDecorator)
        {
            NodeDecorator decorator = node as NodeDecorator;
            // decorator.Index = nodeIndex++;
            // decorator.compositeParent = closestParentComposite; // check me
            // decorator.Tree = this;
            //  decorator.Root = Root;
            // parentNodes.Add(decorator);
            // decorator.Init();
        }

        return node as Node;
    }

    public void AttachNode(Node node, ZeptoBtViewNode parentViewNode)
    {
        node.compositeParent = parentViewNode.Node as NodeComposite;
        if (node is NodeLeaf)
        {
            var leaf = node as NodeLeaf;
            // leaf.Index = nodeIndex++;
            leaf.Tree = Root.Tree;
            // leaf.Root = Root;
            leaf.Init();
        }


        (parentViewNode.Node as NodeComposite).Children.Add(node as Node);

        if (node is NodeDecorator)
        {
            NodeDecorator decorator = node as NodeDecorator;
            // decorator.Index = nodeIndex++;
            // decorator.compositeParent = closestParentComposite; // check me
            // decorator.Tree = this;
            //  decorator.Root = Root;
            // parentNodes.Add(decorator);
            // decorator.Init();
        }

        allChildren.Add(createdNode.gameObject);
        viewNodes.Add(createdNode);
    }
    public void DragNode()
    {
        createdNode = Instantiate(viewNodePrefab);

        Node node = CreateNode(typeDropdown.captionText.text, paramsText.text);

        createdNode.ShortName = node.ToString();
        createdNode.transform.SetParent(transform, false);
        createdNode.transform.localScale = Vector3.one;
        createdNode.Node = node;
        createdNode.Node.ViewNode = createdNode;

        if (selectedNode != null) selectedNode.Selected = false;
        selectedNode = null;

        Debug.Log("Update " + paramsText.text);
    }

    public void NodeDelete()
    {
        Debug.Log("Delete");
        if(selectedNode.Node.compositeParent != null)
        {
            selectedNode.Node.compositeParent.Children.Remove(selectedNode.Node);
        }
        Reset();
    }

    public void NodeUndo()
    {
        if (history.Count < 2) return;
        testTree.FileData = history[history.Count - 2];
        history.RemoveAt(history.Count - 1);
        testTree.CreateTree();
        Reset(false);
    }

    public void NodeLoad()
    {
        string data = Fili.ReadAllText(filenameText.text);
        testTree.FileData = data;
        testTree.CreateTree();
        Reset(false);
    }
    void Start()
    {
        typeDropdown.ClearOptions();

        foreach(var kvp in ZeptoBtTree.StringToAction)
        {
            var data = new TMP_Dropdown.OptionData();
            data.text = kvp.Key;
            typeDropdown.options.Add(data);
        }

        dragger.Interactable = false;
        typeDropdown.onValueChanged.AddListener((i) => TypeDropdownValueChanged());
    }
}
