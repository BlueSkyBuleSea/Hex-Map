using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour {

	Mesh hexMesh;
    //顶点列表
	List<Vector3> vertices;
    //颜色列表
	List<Color> colors;
    //三角形面列表
	List<int> triangles;

	MeshCollider meshCollider;

	void Awake () {
		GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
		meshCollider = gameObject.AddComponent<MeshCollider>();
		hexMesh.name = "Hex Mesh";
		vertices = new List<Vector3>();
		colors = new List<Color>();
		triangles = new List<int>();
	}

    //绘制网格
	public void Triangulate (HexCell[] cells) {
		hexMesh.Clear();
		vertices.Clear();
		colors.Clear();
		triangles.Clear();
		for (int i = 0; i < cells.Length; i++) {
			Triangulate(cells[i]);
		}
		hexMesh.vertices = vertices.ToArray();
		hexMesh.colors = colors.ToArray();
		hexMesh.triangles = triangles.ToArray();
		hexMesh.RecalculateNormals();
		meshCollider.sharedMesh = hexMesh;
	}

    //绘制单元格
	void Triangulate (HexCell cell) {
        //一个六边形网格由6个三角面构成
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
			Triangulate(d, cell);
		}
	}

	//绘制单元格的一个三角面
	void Triangulate (HexDirection direction, HexCell cell) {
		Vector3 center = cell.Position;
		EdgeVertices e = new EdgeVertices(
            //v1
			center + HexMetrics.GetFirstSolidCorner(direction),
            //v2
			center + HexMetrics.GetSecondSolidCorner(direction)
		);
		//绘制核心区域（该区域不会参与后续的混合）
		TriangulateEdgeFan(center, e, cell.color);

        //绘制边缘区域
		if (direction <= HexDirection.SE) {
            //TriangulateConnection(direction, cell,v1,v2)
            TriangulateConnection(direction, cell, e);
		}
	}

    //绘制相邻连接的部分
	void TriangulateConnection (
		HexDirection direction, HexCell cell, EdgeVertices e1
	) {
		HexCell neighbor = cell.GetNeighbor(direction);
		if (neighbor == null) {
			return;
		}

        //矩形区域 （该区域只受2个相邻的三角形影响）
		Vector3 bridge = HexMetrics.GetBridge(direction);
		bridge.y = neighbor.Position.y - cell.Position.y;
		EdgeVertices e2 = new EdgeVertices(
			//v3
            e1.v1 + bridge,
            //v4
			e1.v4 + bridge
		);

		if (cell.GetEdgeType(direction) == HexEdgeType.Slope) {
			 //边缘是斜坡
			TriangulateEdgeTerraces(e1, cell, e2, neighbor);
		}
		else {
            //此外直接绘制
            //AddQuad(v1, v2, v3, v4);
            //AddQuadColor(cell.color, neighbor.color);
            TriangulateEdgeStrip(e1, cell.color, e2, neighbor.color);
		}


        //绘制三角形区域（该区域受3个相邻的三角形影响）
		HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
		if (direction <= HexDirection.E && nextNeighbor != null) {
			Vector3 v5 = e1.v4 + HexMetrics.GetBridge(direction.Next());
			v5.y = nextNeighbor.Position.y;

            //找到高度最低的格子
			if (cell.Elevation <= neighbor.Elevation) {
				if (cell.Elevation <= nextNeighbor.Elevation) {
					//当前格子是最低的
					TriangulateCorner(
						e1.v4, cell, e2.v4, neighbor, v5, nextNeighbor
					);
				}
				else {
					//相邻第三格子是最低的
					TriangulateCorner(
						v5, nextNeighbor, e1.v4, cell, e2.v4, neighbor
					);
				}
			}
			else if (neighbor.Elevation <= nextNeighbor.Elevation) {
				//相邻第二格子是最低的
				TriangulateCorner(
					e2.v4, neighbor, v5, nextNeighbor, e1.v4, cell
				);
			}
			else {
				//相邻第三格子是最低的
				TriangulateCorner(
					v5, nextNeighbor, e1.v4, cell, e2.v4, neighbor
				);
			}
		}
	}

    /// <summary>
    /// 计算相邻三边之间的关系
    /// </summary>
    /// <param name="bottom"></param>
    /// <param name="bottomCell"></param>
    /// <param name="left"></param>
    /// <param name="leftCell"></param>
    /// <param name="right"></param>
    /// <param name="rightCell"></param>
	void TriangulateCorner (
		Vector3 bottom, HexCell bottomCell,
		Vector3 left, HexCell leftCell,
		Vector3 right, HexCell rightCell
	) {
		HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
		HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);

		if (leftEdgeType == HexEdgeType.Slope) {
			if (rightEdgeType == HexEdgeType.Slope) {
                // 2 1 2 关系
                TriangulateCornerTerraces(
					bottom, bottomCell, left, leftCell, right, rightCell
				);
			}
			else if (rightEdgeType == HexEdgeType.Flat) {
                // 2 1 1关系
                TriangulateCornerTerraces(
					left, leftCell, right, rightCell, bottom, bottomCell
				);
			}
			else {
                // 2 1 3 关系 
                TriangulateCornerTerracesCliff(
					bottom, bottomCell, left, leftCell, right, rightCell
				);
			}
		}
		else if (rightEdgeType == HexEdgeType.Slope) {
			if (leftEdgeType == HexEdgeType.Flat) {
                // 1 1 2 关系  
                TriangulateCornerTerraces(
					right, rightCell, bottom, bottomCell, left, leftCell
				);
			}
			else {
                //3 1 2 关系
                TriangulateCornerCliffTerraces(
					bottom, bottomCell, left, leftCell, right, rightCell
				);
			}
		}
		else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {        
            if (leftCell.Elevation < rightCell.Elevation) {
                // 4 1 5 关系
                TriangulateCornerCliffTerraces(
					right, rightCell, bottom, bottomCell, left, leftCell
				);
			}
			else {
                // 6 1 5关系
                TriangulateCornerTerracesCliff(
					left, leftCell, right, rightCell, bottom, bottomCell
				);
			}
		}
		else {
            //除上之外 直接绘制
			AddTriangle(bottom, left, right);
			AddTriangleColor(bottomCell.color, leftCell.color, rightCell.color);
		}
	}

    /// <summary>
    /// 绘制边缘矩形区域 （斜坡，梯田分割）
    /// </summary>
    /// <param name="beginLeft"></param>
    /// <param name="beginRight"></param>
    /// <param name="beginCell"></param>
    /// <param name="endLeft"></param>
    /// <param name="endRight"></param>
    /// <param name="endCell"></param>
	void TriangulateEdgeTerraces (
		EdgeVertices begin, HexCell beginCell,
		EdgeVertices end, HexCell endCell
	) {
        //梯田的第一步
		EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
		Color c2 = HexMetrics.TerraceLerp(beginCell.color, endCell.color, 1);

		TriangulateEdgeStrip(begin, beginCell.color, e2, c2);

        //中间步
		for (int i = 2; i < HexMetrics.terraceSteps; i++) {
			EdgeVertices e1 = e2;
			Color c1 = c2;
			e2 = EdgeVertices.TerraceLerp(begin, end, i);
			c2 = HexMetrics.TerraceLerp(beginCell.color, endCell.color, i);
			TriangulateEdgeStrip(e1, c1, e2, c2);
		}

		//最后一步
		TriangulateEdgeStrip(e2, c2, end, endCell.color);
	}

    /// <summary>
    /// 绘制边缘三角形 （梯田法）  （处理 坡—平—坡 关系）
    /// </summary>
    /// <param name="begin">顶点</param>
    /// <param name="beginCell"></param>
    /// <param name="left">边</param>
    /// <param name="leftCell"></param>
    /// <param name="right">边</param>
    /// <param name="rightCell"></param>
	void TriangulateCornerTerraces (
		Vector3 begin, HexCell beginCell,
		Vector3 left, HexCell leftCell,
		Vector3 right, HexCell rightCell
	) {
		Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
		Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
		Color c3 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, 1);
		Color c4 = HexMetrics.TerraceLerp(beginCell.color, rightCell.color, 1);

		AddTriangle(begin, v3, v4);
		AddTriangleColor(beginCell.color, c3, c4);

		for (int i = 2; i < HexMetrics.terraceSteps; i++) {
			Vector3 v1 = v3;
			Vector3 v2 = v4;
			Color c1 = c3;
			Color c2 = c4;
			v3 = HexMetrics.TerraceLerp(begin, left, i);
			v4 = HexMetrics.TerraceLerp(begin, right, i);
			c3 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, i);
			c4 = HexMetrics.TerraceLerp(beginCell.color, rightCell.color, i);
			AddQuad(v1, v2, v3, v4);
			AddQuadColor(c1, c2, c3, c4);
		}

		AddQuad(v3, v4, left, right);
		AddQuadColor(c3, c4, leftCell.color, rightCell.color);
	}

    /// <summary>
    /// 绘制边缘三角形 （处理 斜坡 悬崖的关系）
    /// </summary>
    /// <param name="begin">低</param>
    /// <param name="beginCell"></param>
    /// <param name="left">中</param>
    /// <param name="leftCell"></param>
    /// <param name="right">高</param>
    /// <param name="rightCell"></param>
	void TriangulateCornerTerracesCliff (
		Vector3 begin, HexCell beginCell,
		Vector3 left, HexCell leftCell,
		Vector3 right, HexCell rightCell
	) {
		float b = 1f / (rightCell.Elevation - beginCell.Elevation);
		if (b < 0) {
			b = -b;
		}
        //底部
		Vector3 boundary = Vector3.Lerp(Perturb(begin), Perturb(right), b);
		Color boundaryColor = Color.Lerp(beginCell.color, rightCell.color, b);

		TriangulateBoundaryTriangle(
			begin, beginCell, left, leftCell, boundary, boundaryColor
		);

        //顶部
		if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
			TriangulateBoundaryTriangle(
				left, leftCell, right, rightCell, boundary, boundaryColor
			);
		}
		else {
			AddTriangleUnperturbed(Perturb(left), Perturb(right), boundary);
			AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
		}
	}

    /// <summary>
    /// 绘制边缘三角形 （处理 悬崖 斜坡 的关系）
    /// </summary>
    /// <param name="begin"></param>
    /// <param name="beginCell"></param>
    /// <param name="left"></param>
    /// <param name="leftCell"></param>
    /// <param name="right"></param>
    /// <param name="rightCell"></param>
	void TriangulateCornerCliffTerraces (
		Vector3 begin, HexCell beginCell,
		Vector3 left, HexCell leftCell,
		Vector3 right, HexCell rightCell
	) {
		float b = 1f / (leftCell.Elevation - beginCell.Elevation);
		if (b < 0) {
			b = -b;
		}
		Vector3 boundary = Vector3.Lerp(Perturb(begin), Perturb(left), b);
		Color boundaryColor = Color.Lerp(beginCell.color, leftCell.color, b);

		TriangulateBoundaryTriangle(
			right, rightCell, begin, beginCell, boundary, boundaryColor
		);

		if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
			TriangulateBoundaryTriangle(
				left, leftCell, right, rightCell, boundary, boundaryColor
			);
		}
		else {
			AddTriangleUnperturbed(Perturb(left), Perturb(right), boundary);
			AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
		}
	}

	void TriangulateBoundaryTriangle (
		Vector3 begin, HexCell beginCell,
		Vector3 left, HexCell leftCell,
		Vector3 boundary, Color boundaryColor
	) {
		Vector3 v2 = Perturb(HexMetrics.TerraceLerp(begin, left, 1));
		Color c2 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, 1);

		AddTriangleUnperturbed(Perturb(begin), v2, boundary);
		AddTriangleColor(beginCell.color, c2, boundaryColor);

		for (int i = 2; i < HexMetrics.terraceSteps; i++) {
			Vector3 v1 = v2;
			Color c1 = c2;
			v2 = Perturb(HexMetrics.TerraceLerp(begin, left, i));
			c2 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, i);
			AddTriangleUnperturbed(v1, v2, boundary);
			AddTriangleColor(c1, c2, boundaryColor);
		}

		AddTriangleUnperturbed(v2, Perturb(left), boundary);
		AddTriangleColor(c2, leftCell.color, boundaryColor);
	}

    //实际绘制三角形
	void TriangulateEdgeFan (Vector3 center, EdgeVertices edge, Color color) {
        //更精细了三角面的绘制，采用了多个点
        //原来的一个三角面变成了3个小的三角面，增加了精度
        AddTriangle(center, edge.v1, edge.v2);
		AddTriangleColor(color);
		AddTriangle(center, edge.v2, edge.v3);
		AddTriangleColor(color);
		AddTriangle(center, edge.v3, edge.v4);
		AddTriangleColor(color);
	}

    //实际绘制矩形
	void TriangulateEdgeStrip (
		EdgeVertices e1, Color c1,
		EdgeVertices e2, Color c2
	) {
        //更精细了四边形面的绘制，采用了多个点
        //原来的一个四边形面变成了3个小的四边形面，增加了精度
        AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
		AddQuadColor(c1, c2);
		AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
		AddQuadColor(c1, c2);
		AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
		AddQuadColor(c1, c2);
	}

    //受到扰动的三角形顶点
	void AddTriangle (Vector3 v1, Vector3 v2, Vector3 v3) {
		int vertexIndex = vertices.Count;
        //添加扰动后的位置
        vertices.Add(Perturb(v1));
		vertices.Add(Perturb(v2));
		vertices.Add(Perturb(v3));
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 2);
	}

    //未受扰动的三角形顶点
	void AddTriangleUnperturbed (Vector3 v1, Vector3 v2, Vector3 v3) {
		int vertexIndex = vertices.Count;
		vertices.Add(v1);
		vertices.Add(v2);
		vertices.Add(v3);
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 2);
	}

	void AddTriangleColor (Color color) {
		colors.Add(color);
		colors.Add(color);
		colors.Add(color);
	}

	void AddTriangleColor (Color c1, Color c2, Color c3) {
		colors.Add(c1);
		colors.Add(c2);
		colors.Add(c3);
	}

	void AddQuad (Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4) {
		int vertexIndex = vertices.Count;
        //添加扰动后的位置
        vertices.Add(Perturb(v1));
		vertices.Add(Perturb(v2));
		vertices.Add(Perturb(v3));
		vertices.Add(Perturb(v4));
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 2);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 2);
		triangles.Add(vertexIndex + 3);
	}

	void AddQuadColor (Color c1, Color c2) {
		colors.Add(c1);
		colors.Add(c1);
		colors.Add(c2);
		colors.Add(c2);
	}

	void AddQuadColor (Color c1, Color c2, Color c3, Color c4) {
		colors.Add(c1);
		colors.Add(c2);
		colors.Add(c3);
		colors.Add(c4);
	}

    /// <summary>
    /// 扰动后的顶点坐标
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
	Vector3 Perturb (Vector3 position) {
		Vector4 sample = HexMetrics.SampleNoise(position);
        //采样范围从0~1 变为-1~1 这样正向和负向都有了
        //三个方向  最大位移 1*1*1 = 1.7  目前外径10，故需要将扰动放大 * 扰动强度
        //需要保证中心的位置是平坦的，故垂直方向不要做扰动
        position.x += (sample.x * 2f - 1f) * HexMetrics.cellPerturbStrength;
		position.z += (sample.z * 2f - 1f) * HexMetrics.cellPerturbStrength;
		return position;
	}
}