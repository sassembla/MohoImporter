using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor.IMGUI.Controls;
using UnityEditor;
using System;

//Tree描画のためのClass
public class ExampleTreeElement
{
    public int id { get; set; }
    public string Name { get; set; }
    public ExampleTreeElement Parent { get; private set; }
    private List<ExampleTreeElement> _children = new List<ExampleTreeElement>();
    public List<ExampleTreeElement> Children { get { return _children; } }

    public void AddChild(ExampleTreeElement child)
    {
        if (child.Parent != null)
        {
            child.Parent.RemoveChild(child);
        }
        Children.Add(child);
        child.Parent = this;
    }

    public void RemoveChild(ExampleTreeElement child)
    {
        if (Children.Contains(child))
        {
            Children.Remove(child);
            child.Parent = null;
        }
    }
}

public class MohoSkinnedMeshConvert : EditorWindow
{
    [MenuItem("MohoEditor/MohoSkinnedMeshConvert")]
    private static void Create()
    {
        GetWindow<MohoSkinnedMeshConvert>("MohoSkinnedMeshConvert");
    }

    //Tree Viewの準備
    [SerializeField]
    private TreeViewState _treeViewState;
    private ExampleTreeView _treeView;
    private SearchField _searchField;

    GameObject AnimStudioObject = null;
    List<GameObject> Bones;
    List<GameObject> MeshObject;
    List<int?> MeshObjectRootIndex;

    //Boneの階層構築用
    List<GameObject> BonesRoots;
    List<List<GameObject>> BonesTree; //Boneの階層を丸っと構築します。
    List<List<String>> BonesTreeName; //PullDownの表示用に名前のリストを保持します


    //SkinnedMeshRendererObject用のリスト
    List<GameObject> SkinnedMeshObject;
    List<GameObject[]> SkinnedMeshBone;
    List<String[]> SkinnedMeshBoneName;


    private void OnEnable()
    {
        SetTreeData();
    }

    //Treeの準備
    private void SetTreeData()
    {
        if (_treeViewState == null)
        {
            _treeViewState = new TreeViewState();
        }

        int currentId = 0;
        ExampleTreeElement root;

        if (BonesTree == null || BonesTree.Count == 0)
        {
            _treeView = new ExampleTreeView(_treeViewState);
            currentId = 0;
            root = new ExampleTreeElement { id = ++currentId, Name = "NoData" };
        }
        else
        {
            _treeView = new ExampleTreeView(_treeViewState);
            currentId = 0;
            root = new ExampleTreeElement { id = ++currentId, Name = BonesTree[0][0].transform.root.name };

            for (var i = 0; i < BonesTree.Count; i++)
            {
                var element = new ExampleTreeElement { id = ++currentId, Name = BonesTree[i][0].name };
                for (var j = 1; j < BonesTree[i].Count; j++) //0はrootが入ってるので除外
                {
                    element.AddChild(new ExampleTreeElement { id = ++currentId, Name = BonesTree[i][j].name });
                }
                root.AddChild(element);
            }
        }

        _treeView.Setup(new List<ExampleTreeElement> { root }.ToArray());

        _searchField = new SearchField();
        _searchField.downOrUpArrowKeyPressed += _treeView.SetFocusAndEnsureSelectedItem;
    }

    private Vector2 leftScrollPos = Vector2.zero;
    private int[] boneIndex;
    private int[] skinnedBoneIndex;
    private List<bool> togglesValue;
    private List<bool> togglesSkinnedMeshValue;
    private void OnGUI()
    {
        //Tree内の要素検索用
        //機能としてあるからつけてあるけど今回いらないから消しておきます
        /*
        using(new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Space(100);
            GUILayout.FlexibleSpace();
            _treeView.searchString = _searchField.OnToolbarGUI(_treeView.searchString);
        }
        */


        EditorGUILayout.LabelField("出力されたFBXをセットしてください");
        AnimStudioObject = EditorGUILayout.ObjectField("MohoImportFBX", AnimStudioObject, typeof(GameObject), true) as GameObject;

        if (GUILayout.Button("Check!"))
        {
            BoneCheck();
            SetTreeData();
            MeshSearch();

            //SkinnedMeshObjectのBoneを所得します
            GetSkinnedBones();
            boneIndex = new int[MeshObject.Count];
            skinnedBoneIndex = new int[SkinnedMeshObject.Count];

            //MeshObjectのチェック
            if (togglesValue == null)
            {
                togglesValue = new List<bool>();
            }
            else
            {
                togglesValue.Clear();
            }

            for (var i = 0; i < MeshObject.Count; i++)
            {
                togglesValue.Add(false);
            }

            //SkinnedMeshObjectのチェック
            if (togglesSkinnedMeshValue == null)
            {
                togglesSkinnedMeshValue = new List<bool>();
            }
            else
            {
                togglesSkinnedMeshValue.Clear();
            }

            for (var i = 0; i < SkinnedMeshObject.Count; i++)
            {
                togglesSkinnedMeshValue.Add(false);
            }

        }

        var rect = EditorGUILayout.GetControlRect(false, 200);
        _treeView.OnGUI(rect);

        if (MeshObject == null)
        {
            EditorGUILayout.LabelField("Meshが存在していません");
        }
        else
        {

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("変換対象", GUILayout.Width(60));
            EditorGUILayout.LabelField("Mesh名");
            EditorGUILayout.LabelField("親Boneの選択");
            EditorGUILayout.EndHorizontal();
            leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos);
            for (var l = 0; l < MeshObject.Count; l++)
            {

                EditorGUILayout.BeginHorizontal();
                if (MeshObjectRootIndex != null)
                {
                    if (MeshObjectRootIndex[l] != null)
                    {
                        togglesValue[l] = EditorGUILayout.ToggleLeft("", togglesValue[l], GUILayout.Width(60));
                    }
                    else
                    {
                        EditorGUILayout.LabelField("");
                    }
                }
                EditorGUILayout.LabelField(MeshObject[l].name);
                if (MeshObjectRootIndex != null)
                {
                    if (MeshObjectRootIndex[l] != null)
                    {
                        boneIndex[l] = EditorGUILayout.Popup(boneIndex[l], BonesTreeName[(int)MeshObjectRootIndex[l]].ToArray());
                    }
                    else
                    {
                        EditorGUILayout.LabelField(AnimStudioObject.name);

                    }
                }
                EditorGUILayout.EndHorizontal();

            }

            if (SkinnedMeshObject != null)
            {
                for (var l = 0; l < SkinnedMeshObject.Count; l++)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (SkinnedMeshObject[l] != null)
                    {
                        togglesSkinnedMeshValue[l] = EditorGUILayout.ToggleLeft("", togglesSkinnedMeshValue[l], GUILayout.Width(60));
                        EditorGUILayout.LabelField(SkinnedMeshObject[l].name);
                        skinnedBoneIndex[l] = EditorGUILayout.Popup(skinnedBoneIndex[l], SkinnedMeshBoneName[l]);
                    }
                    else
                    {
                        EditorGUILayout.LabelField("");
                        EditorGUILayout.LabelField("");
                        EditorGUILayout.LabelField("");
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();
        }

        if (BonesTree != null)
        {
            if (BonesTree.Count != 0 && MeshObject != null)
            {
                if (GUILayout.Button("Convert!"))
                {
                    ConvertObject(AnimStudioObject);
                }
            }
        }
    }

    //SkinnedMeshObjectのBone階層の名前リストを生成します
    void GetSkinnedBones()
    {
        SkinnedMeshBoneName = new List<string[]>();
        foreach (GameObject obj in SkinnedMeshObject)
        {
            Transform[] bones = obj.GetComponent<SkinnedMeshRenderer>().bones;
            List<String> tmpBoneName = new List<string>();
            //Debug.Log(obj.name + " is Bone Count >> " + bones.Length);
            foreach (Transform bone in bones)
            {
                tmpBoneName.Add(bone.name);
            }
            SkinnedMeshBoneName.Add(tmpBoneName.ToArray());
        }
    }

    //Prefabから必要なBoneを収集しリスト化します
    void BoneCheck()
    {
        //BonesRootsの収集
        BonesRoots = new List<GameObject>();
        GetRoots(AnimStudioObject);

        //BonesTreeの初期化
        BonesTree = new List<List<GameObject>>();
        BonesTreeName = new List<List<string>>();
        for (var i = 0; i < BonesRoots.Count; i++)
        {
            List<GameObject> newData = new List<GameObject>();
            List<String> newDataName = new List<string>();
            newData.Add(BonesRoots[i]);
            newDataName.Add(BonesRoots[i].name);
            //RootからそれぞれのBoneを所得
            GetBones(BonesRoots[i], ref newData, ref newDataName);
            BonesTree.Add(newData);
            BonesTreeName.Add(newDataName);
        }

    }

    //各Boneのリストから、それぞれの根本を見つけます
    void GetRoots(GameObject Obj)
    {
        Transform _children = Obj.GetComponentInChildren<Transform>();
        if (_children.childCount == 0) return;

        foreach (Transform obj in _children)
        {
            string name = obj.name;
            Regex regex = new Regex("^B[0-9]{1,}");

            if (regex.IsMatch(name))
            {
                BonesRoots.Add(obj.transform.parent.gameObject);
                //Debug.Log("Match " + obj.transform.parent.name);
                return;
            }
            else
            {
                GetRoots(obj.gameObject);
            }
        }
    }

    //子要素のBoneをすべて確認し集めます
    void GetBones(GameObject Obj, ref List<GameObject> _list, ref List<string> _listName)
    {
        Transform _children = Obj.GetComponentInChildren<Transform>();
        if (_children.childCount == 0) return;

        foreach (Transform obj in _children)
        {
            string name = obj.name;
            Regex regex = new Regex("^B[0-9]{1,}");
            if (regex.IsMatch(name))
            {
                _list.Add(obj.gameObject);
                _listName.Add(obj.name);
            }

            GetBones(obj.gameObject, ref _list, ref _listName);
        }
    }

    //Object
    void ConvertObject(GameObject _animStudioObject)
    {
        //MeshからSkinnedMeshRendererの生成
        CreateSkinnedMesh(ref MeshObject, ref togglesValue);
        //SkinnedMeshObjectのリプレース
        ReplaceSkinnedMesh(ref SkinnedMeshObject, ref togglesSkinnedMeshValue);
    }

    //MeshのあるObejctを探してList作る
    void MeshSearch()
    {
        MeshObject = new List<GameObject>();
        MeshObjectRootIndex = new List<int?>();
        SkinnedMeshObject = new List<GameObject>();

        GetMeshObject(AnimStudioObject, ref MeshObject, ref MeshObjectRootIndex, ref SkinnedMeshObject);
    }

    void GetMeshObject(GameObject Obj, ref List<GameObject> mo, ref List<int?> moRootIndex, ref List<GameObject> skMObj)
    {
        Transform _children = Obj.GetComponentInChildren<Transform>();
        if (_children.childCount == 0) return;

        foreach (Transform obj in _children)
        {
            var MR = obj.GetComponent<MeshRenderer>();
            if (MR != null)
            {
                mo.Add(obj.gameObject);
                moRootIndex.Add(SearchRootBone(obj.gameObject));
            }
            else
            {
                var SMR = obj.GetComponent<SkinnedMeshRenderer>();
                if (SMR != null)
                {
                    skMObj.Add(obj.gameObject);
                }
            }

            GetMeshObject(obj.gameObject, ref mo, ref moRootIndex, ref skMObj);
        }
    }

    int? SearchRootBone(GameObject obj)
    {
        var tempParent = obj.transform.parent;
        do
        {
            for (var i = 0; i < BonesTree.Count; i++)
            {
                //Debug.Log(tempParent.gameObject + " >> " + BonesTree[i][0].name);
                if (tempParent.gameObject == BonesTree[i][0])
                {
                    //Debug.Log("Match!");
                    return i;
                }
            }
            tempParent = tempParent.parent;
            if (tempParent == null)
                return null;
        } while (true);
    }

    void CreateSkinnedMesh(ref List<GameObject> MeshObject, ref List<bool> toggleValue)
    {
        for (var i = 0; i < toggleValue.Count; i++)
        {
            //trueのObjectだけ処理します
            if (toggleValue[i])
            {
                var obj = MeshObject[i];

                //SkinnedMeshObjectsの作製
                GameObject newObj = new GameObject();
                var newSkin = newObj.AddComponent<SkinnedMeshRenderer>();
                var _mesh = obj.GetComponent<MeshFilter>().sharedMesh;
                newSkin.sharedMaterial = obj.GetComponent<MeshRenderer>().sharedMaterial;
                //Debug.Log(i + "] >> " + BonesTree.Count + "." + BonesTree[(int)MeshObjectRootIndex[i]].Count);
                newSkin.rootBone = BonesTree[(int)MeshObjectRootIndex[i]][0].transform;

                //作成したSkinnedmeshRendererの位置をセット
                newSkin.name = obj.name + "Skinned";
                newSkin.transform.parent = obj.transform.parent;
                newSkin.transform.position = obj.transform.position;
                newSkin.transform.localRotation = obj.transform.localRotation;
                newSkin.transform.localScale = obj.transform.localScale;

                //Todo:運用する際はObjectは削除するようにします
                //元モデルを非表示
                MeshObject[i].SetActive(false);

                //Boneの準備
                //SkinnedMesh毎に、BoneとBindPoseを作成
                var tmpList = BonesTree[(int)MeshObjectRootIndex[i]].ToArray();
                Transform[] _bones = new Transform[tmpList.Length];
                Matrix4x4[] _bindPose = new Matrix4x4[tmpList.Length];

                for (var l = 0; l < _bones.Length; l++)
                {
                    _bones[l] = tmpList[l].transform;
                    _bones[l].parent = tmpList[l].transform.parent;
                    _bindPose[l] = _bones[l].worldToLocalMatrix * newSkin.localToWorldMatrix;
                }

                BoneWeight[] _weights = new BoneWeight[_mesh.vertices.Length];

                //この辺でboneのリストを必要な形に整える必要がある
                //リストIndexの頭の数値から、そのＯbjectに子がないところまでのIndexを所得します。
                int indexHead = boneIndex[i];//indexの頭
                int indexEnd = _bones.Length - 1;//indexの仮の終点
                List<Transform> selectBones = new List<Transform>();

                if (indexHead == 0)
                {
                    indexHead = 1;//0の時だけ、Rootは除外したいので外します
                    for (var j = indexHead; j < _bones.Length; j++)
                    {
                        selectBones.Add(_bones[j].transform);
                    }
                }
                else
                {
                    //indexがroot
                    int count = 0;
                    do
                    {
                        selectBones.Add(_bones[count + indexHead].transform);
                        if (_bones[count + indexHead].transform.childCount == 0)
                        {
                            indexEnd = count + indexHead;
                            break;
                        }
                        count++;
                    } while (true);
                }

                Transform[] _bonesArray = selectBones.ToArray();

                /*
                string temp = "";
                foreach(Transform dgbobj in _bonesArray)
                {
                    temp += dgbobj.name + "-";
                }
                Debug.Log(i +  " :" + temp + " [" + indexHead + "-" +indexEnd + "]");
                */

                //拡張頂点のウェイト値を計算
                for (var l = 0; l < _mesh.vertices.Length; l++)
                {

                    //Boneが2個以上あるとき
                    if (_bonesArray.Length > 1)
                    {
                        int[] index = new int[2];
                        float[] distance = new float[2];
                        //頂点のワールド座標
                        Vector3 vecPos = newSkin.localToWorldMatrix.MultiplyPoint(_mesh.vertices[l]);
                        CheckDistance(vecPos, ref index, ref distance, ref _bonesArray);

                        //Boneが選べたのでウェイトを計算します
                        var weight1st = 1f - (distance[0] / (distance[0] + distance[1]));
                        var weight2nd = 1f - weight1st;
                        _weights[l].boneIndex0 = index[0] + indexHead;
                        _weights[l].weight0 = weight1st;
                        _weights[l].boneIndex1 = index[1] + indexHead;
                        _weights[l].weight1 = weight2nd;
                    }
                    else
                    {
                        //Debug.Log("いっこだったよー");
                        //Boneが1個以下のとき
                        _weights[l].boneIndex0 = 0 + indexHead;
                        _weights[l].weight0 = 1;
                    }
                }
                _mesh.boneWeights = _weights;

                //bindPoseをMeshにセット
                _mesh.bindposes = _bindPose;

                //SkinnedMeshRendererに設定をセット
                newSkin.bones = _bones;
                newSkin.sharedMesh = _mesh;
            }
        }
    }

    void ReplaceSkinnedMesh(ref List<GameObject> SkinnedMeshObject, ref List<bool> togglesSkinnedMeshValue)
    {
        for (var i = 0; i < togglesSkinnedMeshValue.Count; i++)
        {
            if (togglesSkinnedMeshValue[i])
            {
                var obj = SkinnedMeshObject[i];
                var baseSMR = obj.GetComponent<SkinnedMeshRenderer>();

                //SkinnedMeshObjectsの複製
                GameObject newObj = new GameObject();
                var newSkin = newObj.AddComponent<SkinnedMeshRenderer>();
                var _mesh = newSkin.sharedMesh = Instantiate(baseSMR.sharedMesh);
                newSkin.sharedMaterials = baseSMR.sharedMaterials;
                newSkin.rootBone = baseSMR.rootBone;
                newSkin.bones = baseSMR.bones;
                newSkin.localBounds = baseSMR.localBounds;

                //作成したSkinnedmeshRendererの位置をセット
                newSkin.name = obj.name + "Repair";
                newSkin.transform.parent = obj.transform.parent;
                newSkin.transform.position = obj.transform.position;
                newSkin.transform.localRotation = obj.transform.localRotation;
                newSkin.transform.localScale = obj.transform.localScale;

                //Todo:運用する際はObjectは削除するようにします
                //元モデルを非表示
                SkinnedMeshObject[i].SetActive(false);

                BoneWeight[] _weights = new BoneWeight[_mesh.vertices.Length];

                //この辺でboneのリストを必要な形に整える必要がある
                //リストIndexの頭の数値から、そのＯbjectに子がないところまでのIndexを所得します。
                int indexHead = skinnedBoneIndex[i];//indexの頭
                int indexEnd = newSkin.bones.Length - 1;//indexの仮の終点
                List<Transform> selectBones = new List<Transform>();

                if (indexHead == 0)
                {
                    indexHead = 1;//0の時だけ、Rootは除外したいので外します
                    for (var j = indexHead; j < newSkin.bones.Length; j++)
                    {
                        selectBones.Add(newSkin.bones[j].transform);
                    }
                }
                else
                {
                    //indexがroot
                    int count = 0;
                    do
                    {
                        selectBones.Add(newSkin.bones[count + indexHead].transform);
                        if (newSkin.bones[count + indexHead].transform.childCount == 0)
                        {
                            indexEnd = count + indexHead;
                            break;
                        }
                        count++;
                    } while (true);
                }

                Transform[] _bonesArray = selectBones.ToArray();

                /*
                string temp = "";
                foreach (Transform dgbobj in _bonesArray)
                {
                    temp += dgbobj.name + "-";
                }
                Debug.Log(i + " :" + temp + " [" + indexHead + "-" + indexEnd + "]");
                */

                //拡張頂点のウェイト値を計算
                for (var l = 0; l < _mesh.vertices.Length; l++)
                {

                    //Boneが2個以上あるとき
                    if (_bonesArray.Length > 1)
                    {
                        int[] index = new int[2];
                        float[] distance = new float[2];
                        //頂点のワールド座標
                        Vector3 vecPos = newSkin.localToWorldMatrix.MultiplyPoint(_mesh.vertices[l]);
                        CheckDistance(vecPos, ref index, ref distance, ref _bonesArray);

                        //Boneが選べたのでウェイトを計算します
                        var weight1st = 1f - (distance[0] / (distance[0] + distance[1]));
                        var weight2nd = 1f - weight1st;
                        _weights[l].boneIndex0 = index[0] + indexHead;
                        _weights[l].weight0 = weight1st;
                        _weights[l].boneIndex1 = index[1] + indexHead;
                        _weights[l].weight1 = weight2nd;
                    }
                    else
                    {
                        //Debug.Log("いっこだったよー");
                        //Boneが1個以下のとき
                        _weights[l].boneIndex0 = 0 + indexHead;
                        _weights[l].weight0 = 1;
                    }
                }
                _mesh.boneWeights = _weights;

                //SkinnedMeshRendererに設定をセット
                newSkin.sharedMesh = _mesh;
            }
        }
    }

    //検索用
    struct Data
    {
        public int Index;
        public float Distance;
    };

    //距離検索ソートを行います
    void CheckDistance(Vector3 vecPos, ref int[] _index, ref float[] _distance, ref Transform[] _bones)
    {

        //_bonesの0はRootなので排除しておきます。
        Data[] distIndex = new Data[_bones.Length];

        //頂点からBoneまでの距離リスト作成
        for (var i = 0; i < distIndex.Length; i++)
        {
            distIndex[i].Index = i;
            distIndex[i].Distance = Vector3.Distance(vecPos, _bones[i].transform.position);
        }

        Array.Sort(distIndex, (a, b) => Math.Sign(a.Distance - b.Distance));

        _index[0] = distIndex[0].Index;
        _index[1] = distIndex[1].Index;
        _distance[0] = distIndex[0].Distance;
        _distance[1] = distIndex[1].Distance;
    }
}


public class ExampleTreeView : TreeView
{
    private ExampleTreeElement[] _baseElements;
    public ExampleTreeView(TreeViewState treeViewState) : base(treeViewState)
    {

    }

    public void Setup(ExampleTreeElement[] baseElements)
    {
        _baseElements = baseElements;
        Reload();
    }

    protected override TreeViewItem BuildRoot()
    {
        var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
        var elements = new List<TreeViewItem>();
        foreach (var baseElement in _baseElements)
        {
            var baseItem = CreateTreeViewItem(baseElement);
            root.AddChild(baseItem);
            AddChildrenRecursive(baseElement, baseItem);
        }
        SetupDepthsFromParentsAndChildren(root);
        return root;
    }

    private void AddChildrenRecursive(ExampleTreeElement model, TreeViewItem item)
    {
        foreach (var childModel in model.Children)
        {
            var childItem = CreateTreeViewItem(childModel);
            item.AddChild(childItem);
            AddChildrenRecursive(childModel, childItem);
        }
    }

    private TreeViewItem CreateTreeViewItem(ExampleTreeElement model)
    {
        return new TreeViewItem { id = model.id, displayName = model.Name };
    }
}