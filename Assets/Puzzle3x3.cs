using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Node
{
    // 노드는 parent 만을 갖고 있습니다.
    public int f_scr;
    public int g_scr = 0;
    public int h_scr = 0;
    public Node parent = null;
    public int[] puzzleState;

    public Node(Puzzle3x3 puzzle3x3, int _move, int[] _puzzleState, Node _parent)
    {
        // 부모를 세팅하고 puzzleState를 복사합니다.
        parent = _parent;
        puzzleState = (int[])_puzzleState.Clone();

        // 공백을 찾고 +move 만큼 움직이고, 해당 값과 교체합니다.
        int blankIDX = Array.FindIndex(puzzleState, x => x == 0);
        puzzleState[blankIDX] = puzzleState[blankIDX + _move];
        puzzleState[blankIDX + _move] = 0;

        // 휴리스틱 계산
        f(puzzle3x3);
    }

    // 휴리스틱을 구합니다.
    // h_scr 은 타일이 불 일치하는 값이고,
    // g_scr 은 노드의 깊이를 의미합니다.
    // f_scr 은 h_scr + g_scr 을 더한 값이며, 이를 기준으로 우선탐색을 진행하게 됩니다.
    protected void f(Puzzle3x3 puzzle3x3)
    {
        for (int i = 0; i < 9; ++i)
        {
            if (puzzleState[i] != puzzle3x3.final_state[i])
                ++h_scr;
        }
        if (parent != null) g_scr = parent.g_scr + 1;
        f_scr = g_scr + h_scr;
    }
}

public class Puzzle3x3 : MonoBehaviour
{
    public int[] start_state = {  };
    public int[] final_state = {  };
    public List<Node> oNodes;
    public List<Node> cNodes;
    private Node[] rNodes = null;
    private float ani_delta = 0.0f;
    public float ani_speed = 0.01f;
    private int animIDX = 0;
    private Transform[] cubes = new Transform[9];

    // 큐브를 배치하기 위해 위치를 가져옵니다.
    public Vector3 GetCubePosition(int IDX)
    {
        return new Vector3(3 - IDX % 3 * 3.0f, 0.0f, -3 + 3.0f * (IDX / 3));
    }

    public void AddOpenNode(Node node)
    {
        // 닫힌 노드에 node와 동일한 puzzleState를 가진 요소가 있다면 추가하지 않습니다.
        Node cNode = cNodes.Find(x => x.puzzleState.SequenceEqual(node.puzzleState));
        if (cNode != null)
            return;

        // 열린 노드에 현재 puzzleState와 동일한 요소가 있다면 열린 노드에 추가하지 않습니다.
        Node oNode = oNodes.Find(x => x.puzzleState.SequenceEqual(node.puzzleState));
        if (oNode != null)
            return;

        oNodes.Add(node);
    }

	private void Start ()
    {
        oNodes = new List<Node>();
        cNodes = new List<Node>();
        for (int i = 0; i < 9; ++i) 
        {
            cubes[i] = (transform.Find(i.ToString()));
            cubes[i].localPosition = GetCubePosition(Array.FindIndex(start_state, x => x == i));
        }

        // infinite loop protection
        int limitWhileCount = 10000;
        int whileCount = 0;

        AddOpenNode(new Node(this, 0, start_state, null));
        while (oNodes.Count > 0) 
        {
            Node node = null;
            foreach (var n in oNodes)
            {
                if (node == null || node.f_scr > n.f_scr) 
                    node = n;
            }

            // 닫힌 노드에 추가하고 열린 노드에서 삭제합니다.
            cNodes.Add(node);
            oNodes.Remove(node);

            // h_scr == 0 ---> 정답을 찾았습니다!
            if (node.h_scr == 0)
                break;

            // 공백의 위치를 찾고, 상 하 좌 우 인접 노드를 검사합니다.
            int blankPos = Array.FindIndex(node.puzzleState, x => x == 0);
            if (blankPos > 2)
                AddOpenNode(new Node(this, -3, node.puzzleState, node));
            if (blankPos < 6)
                AddOpenNode(new Node(this, +3, node.puzzleState, node));
            if (blankPos % 3 > 0)
                AddOpenNode(new Node(this, -1, node.puzzleState, node));
            if (blankPos % 3 < 2)
                AddOpenNode(new Node(this, +1, node.puzzleState, node));

            if (++whileCount >= limitWhileCount)
                break;
        }

        if (cNodes.Last().h_scr == 0)
        {
            Stack<Node> rNodeStack = new Stack<Node>();
            var node = cNodes.Last();
            while (node != null)
            {
                rNodeStack.Push(node);
                node = node.parent;
            }
            rNodes = rNodeStack.ToArray();

            // Output File
            var dataPath = Application.dataPath + "/result.txt";
            FileStream fs = new FileStream(dataPath, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
            for (int i = 0; i < rNodes.Count(); ++i) 
            {
                System.Text.StringBuilder strBuilder = new System.Text.StringBuilder();
                if (i == 0) strBuilder.Append("start_state: ");
                else if (i == rNodes.Count() - 1) strBuilder.Append("goal state: ");
                foreach (var state in rNodes[i].puzzleState)
                {
                    strBuilder.Append(state.ToString());
                    strBuilder.Append(" ");
                }
                sw.WriteLine(strBuilder.ToString());
            }
            sw.Close();
            fs.Close();
        }
        else
        {
            Debug.Log("ERROR : 경로 탐색 실패");
        }
    }

    private void Update ()
    {
        // 큐브 이동
        ani_delta += Time.deltaTime;

        if (rNodes == null || animIDX >= rNodes.Count())
            return;

        var curNode = rNodes[animIDX];
        for (int i = 0; i < 9; ++i)
        {
            Transform cube = cubes[curNode.puzzleState[i]];
            cube.position += Vector3.Lerp(cube.position, GetCubePosition(i), 1.0f / ani_speed * ani_delta) - cube.transform.position;
            if (ani_delta > ani_speed)
            {
                ani_delta = 0.0f;
                ++animIDX;
            }
        }
    }
}
